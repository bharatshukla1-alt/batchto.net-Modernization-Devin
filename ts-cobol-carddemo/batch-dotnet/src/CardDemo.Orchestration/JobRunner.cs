using System.Text;
using System.Text.Json;
using CardDemo.Batch;
using CardDemo.Batch.Restart;
using CardDemo.Data;
using CardDemo.Domain;

namespace CardDemo.Orchestration;

public sealed record JobState(Dictionary<string, int> Completed, Dictionary<string, int> Generations);

public sealed record LedgerEntry(string RunId, string Job, string Step, string Program, DateTime StartedAt, DateTime EndedAt, int ReturnCode, string Status, int RecordsRead, int RecordsWritten, string? Message);

/// <summary>
/// Runs jobs step by step like JES: return codes, abend stops the job, a batch run ledger
/// (ledger/batch_run_ledger.jsonl) and step-level restart (state/&lt;JOB&gt;.json lists committed steps).
/// </summary>
[Trace("JES2 job execution / tools/runjcl/runjcl.py Runner")]
public sealed class JobRunner(DatasetCatalog catalog, DateTime clock, string runId)
{
    private readonly JsonSerializerOptions _json = new() { WriteIndented = false };

    /// <summary>Legacy runner quirk reproduced for rc.json: the step map is not cleared after setup jobs (ADR-004).</summary>
    public Dictionary<string, int> StepReturnCodes { get; } = new(StringComparer.Ordinal);
    public int ChunkSize { get; init; } = 1000;
    public bool Restart { get; init; }
    public Func<string, string, IBatchProgram?>? ProgramOverride { get; init; }
    public Action<string>? Log { get; init; }

    private string LedgerPath => Path.Combine(catalog.WorkDir, "ledger", "batch_run_ledger.jsonl");
    private string StatePath(string job) => Path.Combine(catalog.WorkDir, "state", job + ".json");

    public int Run(JobDefinition job)
    {
        var state = Restart && File.Exists(StatePath(job.Name))
            ? JsonSerializer.Deserialize<JobState>(File.ReadAllText(StatePath(job.Name)))!
            : new JobState([], []);
        catalog.BeginJob(state.Generations);
        var completed = state.Completed;
        var maxRc = 0;
        foreach (var step in job.Steps)
        {
            if (completed.TryGetValue(step.Name, out var prior))
            {
                Record(step.Name, prior);
                Log?.Invoke($"{job.Name}.{step.Name} skipped on restart (RC {prior})");
                continue;
            }
            var ctx = new StepContext(job.Name, step.Name, catalog, step.Dd, clock)
            {
                Parm = step.Parm,
                ChunkSize = ChunkSize,
                Checkpoints = new CatalogCheckpointStore(catalog),
            };
            foreach (var dsn in step.Dd.Values)
            {
                catalog.Resolve(dsn); // allocate this step's (+n) generations now ...
            }
            SaveState(job.Name, completed); // ... so they stay pinned even if the process dies mid-step
            var program = ProgramOverride?.Invoke(job.Name, step.Name) ?? step.Program();
            var started = DateTime.UtcNow;
            int rc;
            string status;
            string? message = null;
            try
            {
                rc = program.Execute(ctx);
                status = "COMPLETED";
            }
            catch (AbendException ex)
            {
                rc = ex.Code;
                status = "ABEND";
                message = ex.Message;
            }
            catch (Exception ex) when (ex is IOException or FormatException or OverflowException or InvalidOperationException or KeyNotFoundException or ArgumentException)
            {
                rc = 999;
                status = "ABEND";
                message = ex.GetType().Name + ": " + ex.Message;
                ctx.Display("ABENDING PROGRAM - " + message);
            }
            WriteSysout(job.Name, step.Name, ctx.Sysout);
            AppendLedger(new LedgerEntry(runId, job.Name, step.Name, step.ProgramName, started, DateTime.UtcNow, rc, status, ctx.RecordsRead, ctx.RecordsWritten, message));
            Record(step.Name, rc);
            maxRc = Math.Max(maxRc, rc);
            Log?.Invoke($"{job.Name}.{step.Name} {step.ProgramName,-8} RC={rc:0000} read={ctx.RecordsRead} written={ctx.RecordsWritten} {status}");
            if (status == "ABEND")
            {
                SaveState(job.Name, completed);
                return rc;
            }
            completed[step.Name] = rc;
        }
        if (File.Exists(StatePath(job.Name)))
        {
            File.Delete(StatePath(job.Name));
        }
        return Math.Max(maxRc, completed.Values.DefaultIfEmpty(0).Max());
    }

    private void Record(string step, int rc)
    {
        StepReturnCodes[step.Split('.')[^1]] = rc;
        StepReturnCodes[step] = rc;
    }

    private void SaveState(string job, Dictionary<string, int> completed)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(StatePath(job))!);
        File.WriteAllText(StatePath(job), JsonSerializer.Serialize(new JobState(completed, catalog.JobGenerations.ToDictionary(kv => kv.Key, kv => kv.Value))));
    }

    private void WriteSysout(string job, string step, List<string> lines)
    {
        if (lines.Count == 0)
        {
            return;
        }
        var dir = Path.Combine(catalog.WorkDir, "sysout");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, $"{job}.{step}.txt"), string.Concat(lines.Select(l => l + "\n")), Encoding.Latin1);
    }

    private void AppendLedger(LedgerEntry entry)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LedgerPath)!);
        File.AppendAllText(LedgerPath, JsonSerializer.Serialize(entry, _json) + "\n");
    }
}
