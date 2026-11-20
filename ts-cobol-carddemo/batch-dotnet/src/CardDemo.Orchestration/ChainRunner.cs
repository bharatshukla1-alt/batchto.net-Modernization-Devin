using System.Text;
using CardDemo.Data;
using CardDemo.Domain;

namespace CardDemo.Orchestration;

/// <summary>Runs a chain on a fixture case and writes the parity contract: datasets/, sysout/, rc.json.</summary>
[Trace("tools/parity/record.py run_case")]
public static class ChainRunner
{
    public static readonly DateTime FrozenClock = new(2022, 7, 5, 10, 0, 0, DateTimeKind.Unspecified);

    public static int Run(string chain, string caseDir, string outDir, IKsdsPersistence? persistence = null, Action<string>? log = null,
        bool restart = false, Func<string, string, Batch.IBatchProgram?>? programOverride = null, int chunkSize = 1000, Action<string>? commitHook = null, string? db2 = null)
    {
        var (setup, jobs) = JobCatalog.Chains[chain];
        JobCatalog.Db2Tables.TryGetValue(chain, out var db2Tables);
        if (db2Tables is not null && db2 is null)
        {
            throw new InvalidOperationException($"chain {chain} runs Db2 programs: pass a PostgreSQL connection (--db2)");
        }
        if (!restart)
        {
            if (Directory.Exists(outDir))
            {
                Directory.Delete(outDir, recursive: true);
            }
            Directory.CreateDirectory(Path.Combine(outDir, "datasets"));
            foreach (var input in Directory.EnumerateFiles(Path.Combine(caseDir, "input"), "*.txt"))
            {
                File.Copy(input, Path.Combine(outDir, "datasets", Path.GetFileName(input)));
            }
        }
        if (db2Tables is not null && !restart)
        {
            Db2Fixture.Reset(db2!, db2Tables.Select(t => t.Table), Path.Combine(caseDir, "input", "db", "seed.sql"));
        }
        var catalog = new DatasetCatalog(outDir, persistence) { CommitHook = commitHook, Db2ConnectionString = db2 };
        if (restart)
        {
            catalog.RecoverPendingCommit();
        }
        else
        {
            catalog.ResetCheckpoints(jobs);
        }
        var runner = new JobRunner(catalog, FrozenClock, Guid.NewGuid().ToString("N")) { Log = log, Restart = restart, ProgramOverride = programOverride, ChunkSize = chunkSize };
        var rcs = new SortedDictionary<string, (int MaxRc, SortedDictionary<string, int> Steps)>(StringComparer.Ordinal);
        if (!restart)
        {
            foreach (var job in setup)
            {
                var rc = runner.Run(JobCatalog.Jobs[job]);
                if (rc > 8)
                {
                    throw new InvalidOperationException($"setup job {job} failed with RC {rc}");
                }
            }
        }
        var statePath = Path.Combine(outDir, "state", "chain.json");
        var state = restart && File.Exists(statePath)
            ? System.Text.Json.JsonSerializer.Deserialize<ChainState>(File.ReadAllText(statePath))!
            : new ChainState([], [], []);
        foreach (var (job, rc) in state.MaxRc)
        {
            rcs[job] = (rc, new SortedDictionary<string, int>(state.Steps[job], StringComparer.Ordinal));
        }
        var resumed = false;
        var overall = rcs.Values.Select(v => v.MaxRc).DefaultIfEmpty(0).Max();
        foreach (var job in jobs)
        {
            if (state.MaxRc.TryGetValue(job, out var done) && done <= 8)
            {
                log?.Invoke($"{job} completed in the failed run (RC {done}) - skipped on restart");
                continue;
            }
            if (restart && !resumed)
            {
                resumed = true;
                foreach (var (k, v) in state.CarriedCodes)
                {
                    runner.StepReturnCodes[k] = v;
                }
            }
            state.CarriedCodes.Clear();
            foreach (var (k, v) in runner.StepReturnCodes)
            {
                state.CarriedCodes[k] = v;
            }
            SaveChainState(statePath, state);
            var rc = runner.Run(JobCatalog.Jobs[job]);
            var steps = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (var (k, v) in runner.StepReturnCodes.Where(kv => !kv.Key.Contains('.', StringComparison.Ordinal) || kv.Key.StartsWith(job, StringComparison.Ordinal)))
            {
                steps[k] = v;
            }
            rcs[job] = (rc, steps);
            state.MaxRc[job] = rc;
            state.Steps[job] = new Dictionary<string, int>(steps);
            SaveChainState(statePath, state);
            runner.StepReturnCodes.Clear();
            overall = rcs.Values.Max(v => v.MaxRc);
            if (rc > 8)
            {
                break;
            }
        }
        if (persistence is PostgresPersistence)
        {
            catalog.UnloadClusters();
        }
        if (db2Tables is not null)
        {
            Db2Fixture.Snapshot(db2!, db2Tables, Path.Combine(outDir, "db"));
        }
        File.WriteAllText(Path.Combine(outDir, "rc.json"), RcJson(rcs));
        if (overall <= 8 && File.Exists(statePath))
        {
            File.Delete(statePath);
        }
        return overall;
    }

    /// <summary>Chain recovery metadata: finished jobs with their RCs and the step codes carried into the next job.</summary>
    public sealed record ChainState(Dictionary<string, int> MaxRc, Dictionary<string, Dictionary<string, int>> Steps, Dictionary<string, int> CarriedCodes);

    private static void SaveChainState(string path, ChainState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path + ".tmp", System.Text.Json.JsonSerializer.Serialize(state));
        File.Move(path + ".tmp", path, overwrite: true);
    }

    /// <summary>Same text as Python json.dumps(rcs, indent=1, sort_keys=True).</summary>
    public static string RcJson(SortedDictionary<string, (int MaxRc, SortedDictionary<string, int> Steps)> rcs)
    {
        var sb = new StringBuilder("{\n");
        var jobs = rcs.ToList();
        for (var j = 0; j < jobs.Count; j++)
        {
            sb.Append($" \"{jobs[j].Key}\": {{\n  \"maxrc\": {jobs[j].Value.MaxRc},\n  \"steps\": {{\n");
            var steps = jobs[j].Value.Steps.ToList();
            for (var s = 0; s < steps.Count; s++)
            {
                sb.Append($"   \"{steps[s].Key}\": {steps[s].Value}{(s < steps.Count - 1 ? "," : "")}\n");
            }
            sb.Append($"  }}\n }}{(j < jobs.Count - 1 ? "," : "")}\n");
        }
        return sb.Append('}').ToString();
    }
}
