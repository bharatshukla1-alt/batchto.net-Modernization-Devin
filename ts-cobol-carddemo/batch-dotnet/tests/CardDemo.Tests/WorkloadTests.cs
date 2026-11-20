using System.Diagnostics;
using CardDemo.Batch.Programs;
using CardDemo.Orchestration;
using Xunit;

namespace CardDemo.Tests;

public class WorkloadTests
{
    private static readonly string Root = FindRoot();

    private static string FindRoot()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "AGENTS.md")))
        {
            d = d.Parent;
        }
        return d?.FullName ?? throw new InvalidOperationException("repository root not found");
    }

    private static string Temp(string name) => Path.Combine(Path.GetTempPath(), "carddemo-tests", name + "-" + Guid.NewGuid().ToString("N")[..8]);

    public static TheoryData<string, string> Cases()
    {
        var data = new TheoryData<string, string>();
        foreach (var chain in new[] { "posting", "interest" })
        {
            foreach (var dir in Directory.EnumerateDirectories(Path.Combine(FindRoot(), "fixtures", chain)).Order(StringComparer.Ordinal))
            {
                data.Add(chain, Path.GetFileName(dir));
            }
        }
        return data;
    }

    /// <summary>Contract: every golden fixture passes the repository comparator (field level, SYSOUT, RC).</summary>
    [Theory]
    [MemberData(nameof(Cases))]
    public void Target_matches_legacy_golden_files(string chain, string fixture)
    {
        var outDir = Temp(fixture);
        ChainRunner.Run(chain, Path.Combine(Root, "fixtures", chain, fixture), outDir);
        var psi = new ProcessStartInfo("python3", ["tools/parity/compare.py", chain, fixture, "--actual", outDir, "--label", ".NET 8 target"])
        {
            WorkingDirectory = Root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var p = Process.Start(psi)!;
        var output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
        p.WaitForExit();
        Assert.True(p.ExitCode == 0, output);
    }

    public static TheoryData<string, string> Db2Cases()
    {
        var data = new TheoryData<string, string>();
        foreach (var chain in new[] { "trantype-update", "trantype-dynamic" })
        {
            foreach (var dir in Directory.EnumerateDirectories(Path.Combine(FindRoot(), "fixtures", chain)).Order(StringComparer.Ordinal))
            {
                data.Add(chain, Path.GetFileName(dir));
            }
        }
        return data;
    }

    /// <summary>Db2 contract (CARDDEMO_DB2 set): SYSOUT, RC and table snapshots equal the legacy golden files.</summary>
    [Theory]
    [MemberData(nameof(Db2Cases))]
    public void Db2_target_matches_legacy_golden_files(string chain, string fixture)
    {
        var db2 = Environment.GetEnvironmentVariable("CARDDEMO_DB2");
        if (string.IsNullOrEmpty(db2))
        {
            return;
        }
        lock (Db2Lock)
        {
            var outDir = Temp(fixture);
            ChainRunner.Run(chain, Path.Combine(Root, "fixtures", chain, fixture), outDir, db2: db2);
            var expected = Path.Combine(Root, "fixtures", chain, fixture, "expected");
            Assert.Equal(File.ReadAllText(Path.Combine(expected, "rc.json")), File.ReadAllText(Path.Combine(outDir, "rc.json")));
            foreach (var sub in new[] { "sysout", "db" })
            {
                foreach (var file in Directory.EnumerateFiles(Path.Combine(expected, sub)))
                {
                    Assert.Equal(File.ReadAllText(file), File.ReadAllText(Path.Combine(outDir, sub, Path.GetFileName(file))));
                }
            }
        }
    }

    private static readonly object Db2Lock = new();

    [Fact]
    public void Missing_db2_connection_keeps_previous_output()
    {
        var outDir = Temp("nodb2");
        Directory.CreateDirectory(outDir);
        File.WriteAllText(Path.Combine(outDir, "keep.txt"), "x");
        Assert.Throws<InvalidOperationException>(() => ChainRunner.Run("trantype-update", Path.Combine(Root, "fixtures", "trantype-update", "t01-add"), outDir));
        Assert.True(File.Exists(Path.Combine(outDir, "keep.txt")));
    }

    /// <summary>CHAR(2) codes with a trailing blank match like ocesql's untyped host variables; comment-only DYNSQL gives EXECUTE SQLCODE -212 (as on the estate) and COBTUPDT still applies its records.</summary>
    [Fact]
    public void Db2_padded_code_and_blank_dynsql()
    {
        var db2 = Environment.GetEnvironmentVariable("CARDDEMO_DB2");
        if (string.IsNullOrEmpty(db2))
        {
            return;
        }
        lock (Db2Lock)
        {
            var caseDir = Temp("db2case");
            Directory.CreateDirectory(Path.Combine(caseDir, "input", "db"));
            File.Copy(Path.Combine(Root, "fixtures", "trantype-update", "t01-add", "input", "db", "seed.sql"), Path.Combine(caseDir, "input", "db", "seed.sql"));
            File.WriteAllText(Path.Combine(caseDir, "input", "DYNSQL.txt"), "-- nothing to run\n");
            File.WriteAllText(Path.Combine(caseDir, "input", "INPFILE.txt"), "A7 Padded\r\nU7 Renamed\r\nD7 \r\n");
            var outDir = Temp("db2out");
            Assert.Equal(0, ChainRunner.Run("trantype-dynamic", caseDir, outDir, db2: db2)); // CBTRNDYN RC 8 is lost after CALL COBTUPDT (GAP-007)
            var sysout = File.ReadAllText(Path.Combine(outDir, "sysout", "MNTTRDYN.STEP1.txt"));
            Assert.Contains("CBTRNDYN: EXECUTE SQLCODE -0000000212", sysout, StringComparison.Ordinal);
            Assert.Contains("RECORD UPDATED SUCCESSFULLY", sysout, StringComparison.Ordinal);
            Assert.DoesNotContain("7 ", File.ReadAllText(Path.Combine(outDir, "db", "carddemo.transaction_type.csv")), StringComparison.Ordinal);
            Assert.DoesNotContain("\r", sysout, StringComparison.Ordinal);
        }
    }

    /// <summary>Chunk checkpoint + step restart: a POSTTRAN failure after record 2, restarted, gives the clean-run outputs.</summary>
    [Fact]
    public void Posttran_restart_from_checkpoint_matches_clean_run()
    {
        var fixture = Path.Combine(Root, "fixtures", "posting", "p01-baseline-mixed");
        var clean = Temp("clean");
        var restarted = Temp("restart");
        ChainRunner.Run("posting", fixture, clean);

        var rc = ChainRunner.Run("posting", fixture, restarted, chunkSize: 1,
            programOverride: (job, step) => job == "POSTTRAN" ? new Cbtrn02c { FailAfter = 2 } : null);
        Assert.Equal(999, rc);
        Assert.True(File.Exists(Path.Combine(restarted, "checkpoints", "POSTTRAN.STEP15.checkpoint.json")));

        Assert.True(ChainRunner.Run("posting", fixture, restarted, restart: true, chunkSize: 1) <= 4);
        Assert.False(File.Exists(Path.Combine(restarted, "state", "chain.json")));
        AssertSameOutputs(clean, restarted);
        var ledger = File.ReadAllLines(Path.Combine(restarted, "ledger", "batch_run_ledger.jsonl"));
        Assert.Contains(ledger, l => l.Contains("\"Status\":\"ABEND\"", StringComparison.Ordinal));
    }

    private sealed class SimulatedCrash : Exception;

    /// <summary>Kills the run at one durability boundary of POSTTRAN's second chunk (chunk size 1).</summary>
    private static Action<string> CrashAt(string phase)
    {
        var appended = 0;
        var crashed = false;
        return p =>
        {
            appended += p == "appended" ? 1 : 0;
            if (!crashed && appended == 2 && p == phase)
            {
                crashed = true;
                throw new SimulatedCrash();
            }
        };
    }

    /// <summary>p01: chunk 2 posts a transaction (replay = double charge); p02: chunk 2 writes a reject (replay = duplicate reject).</summary>
    private static void CrashAndRestart(string fixtureCase, string phase, CardDemo.Data.IKsdsPersistence? persistence)
    {
        var fixture = Path.Combine(Root, "fixtures", "posting", fixtureCase);
        var clean = Temp("clean-" + phase);
        var restarted = Temp("crash-" + phase);
        ChainRunner.Run("posting", fixture, clean, persistence);
        Assert.Throws<SimulatedCrash>(() => ChainRunner.Run("posting", fixture, restarted, persistence, chunkSize: 1, commitHook: CrashAt(phase)));
        Assert.True(ChainRunner.Run("posting", fixture, restarted, persistence, restart: true, chunkSize: 1) <= 4);
        AssertSameOutputs(clean, restarted);
    }

    /// <summary>File mode: a crash at any point of a chunk commit neither loses nor replays postings or rejects.</summary>
    [Theory]
    [InlineData("p01-baseline-mixed", "appended")]
    [InlineData("p01-baseline-mixed", "journal")]
    [InlineData("p01-baseline-mixed", "applied")]
    [InlineData("p01-baseline-mixed", "done")]
    [InlineData("p02-invalid-card", "appended")]
    [InlineData("p02-invalid-card", "journal")]
    [InlineData("p02-invalid-card", "applied")]
    [InlineData("p02-invalid-card", "done")]
    public void Posting_chunk_commit_is_atomic_on_files(string fixtureCase, string phase) => CrashAndRestart(fixtureCase, phase, null);

    /// <summary>PostgreSQL mode (CARDDEMO_PG set): stores and checkpoint commit in one transaction.</summary>
    [Theory]
    [InlineData("p01-baseline-mixed", "pg-before-commit")]
    [InlineData("p01-baseline-mixed", "committed")]
    [InlineData("p02-invalid-card", "appended")]
    [InlineData("p02-invalid-card", "pg-before-commit")]
    [InlineData("p02-invalid-card", "committed")]
    public void Posting_chunk_commit_is_atomic_on_postgres(string fixtureCase, string phase)
    {
        var cs = Environment.GetEnvironmentVariable("CARDDEMO_PG");
        if (string.IsNullOrEmpty(cs))
        {
            return;
        }
        CrashAndRestart(fixtureCase, phase, new CardDemo.Data.PostgresPersistence(cs, CardDemo.Data.DatasetCatalog.Clusters.ToDictionary(kv => kv.Key, kv => kv.Value.Table)));
    }

    /// <summary>A fresh run of another chain must not delete a failed posting run's PostgreSQL checkpoint.</summary>
    [Fact]
    public void Other_chain_keeps_posting_checkpoint_on_postgres()
    {
        var cs = Environment.GetEnvironmentVariable("CARDDEMO_PG");
        if (string.IsNullOrEmpty(cs))
        {
            return;
        }
        var pg = new CardDemo.Data.PostgresPersistence(cs, CardDemo.Data.DatasetCatalog.Clusters.ToDictionary(kv => kv.Key, kv => kv.Value.Table));
        pg.SaveAll([], "POSTTRAN.STEP15", "{}");
        new CardDemo.Data.DatasetCatalog(Temp("readcard"), pg).ResetCheckpoints(["READCARD"]);
        Assert.Equal("{}", pg.LoadCheckpoint("POSTTRAN.STEP15"));
        new CardDemo.Data.DatasetCatalog(Temp("posting"), pg).ResetCheckpoints(["POSTTRAN", "TRANREPT"]);
        Assert.Null(pg.LoadCheckpoint("POSTTRAN.STEP15"));
    }

    private sealed class Failing : CardDemo.Batch.IBatchProgram
    {
        public int Execute(CardDemo.Batch.StepContext ctx) => throw new IOException("injected report failure");
    }

    /// <summary>TRANREPT fails after POSTTRAN succeeded: the restart must not post the day twice.</summary>
    [Fact]
    public void Tranrept_restart_does_not_repost()
    {
        var fixture = Path.Combine(Root, "fixtures", "posting", "p01-baseline-mixed");
        var clean = Temp("clean");
        var restarted = Temp("restart");
        ChainRunner.Run("posting", fixture, clean);
        Assert.Equal(999, ChainRunner.Run("posting", fixture, restarted, programOverride: (job, step) => step == "STEP10R" ? new Failing() : null));
        Assert.True(ChainRunner.Run("posting", fixture, restarted, restart: true) <= 4);
        Assert.False(File.Exists(Path.Combine(restarted, "state", "chain.json")));
        AssertSameOutputs(clean, restarted);
    }

    [Fact]
    public void Missing_input_dataset_abends_instead_of_posting_an_empty_day()
    {
        var caseDir = Temp("nodaly");
        Directory.CreateDirectory(Path.Combine(caseDir, "input"));
        foreach (var f in Directory.EnumerateFiles(Path.Combine(Root, "fixtures", "posting", "p01-baseline-mixed", "input")).Where(f => !f.Contains("DALYTRAN.PS.txt", StringComparison.Ordinal)))
        {
            File.Copy(f, Path.Combine(caseDir, "input", Path.GetFileName(f)));
        }
        Assert.Equal(999, ChainRunner.Run("posting", caseDir, Temp("nodaly-out")));
    }

    private static void AssertSameOutputs(string clean, string restarted)
    {
        Assert.Equal(File.ReadAllText(Path.Combine(clean, "rc.json")), File.ReadAllText(Path.Combine(restarted, "rc.json")));
        foreach (var sub in new[] { "datasets", "sysout" })
        {
            Assert.Equal(Directory.EnumerateFiles(Path.Combine(clean, sub)).Select(Path.GetFileName).Order(), Directory.EnumerateFiles(Path.Combine(restarted, sub)).Select(Path.GetFileName).Order());
            foreach (var file in Directory.EnumerateFiles(Path.Combine(clean, sub)))
            {
                var other = Path.Combine(restarted, sub, Path.GetFileName(file));
                Assert.True(File.Exists(other), $"missing {sub}/{Path.GetFileName(file)} after restart");
                Assert.Equal(File.ReadAllBytes(file), File.ReadAllBytes(other));
            }
        }
    }

    [Fact]
    public void Readacct_and_readcard_run_clean()
    {
        foreach (var (chain, fixture) in new[] { ("readacct", "r01-sample"), ("readcard", "c01-sample") })
        {
            var outDir = Temp(chain);
            Assert.Equal(0, ChainRunner.Run(chain, Path.Combine(Root, "batch-dotnet", "fixtures", chain, fixture), outDir));
            Assert.NotEmpty(Directory.EnumerateFiles(Path.Combine(outDir, "sysout")));
        }
    }
}
