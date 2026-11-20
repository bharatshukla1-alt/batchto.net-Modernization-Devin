using System.Globalization;
using System.Text.RegularExpressions;
using CardDemo.Domain;

namespace CardDemo.Data;

/// <summary>
/// The catalog: resolves DSNs (including GDG relative generations) to files under datasets/ and
/// hands out KSDS stores. KSDS persistence is pluggable (files or PostgreSQL).
/// </summary>
[Trace("z/OS catalog + GDG (DEFGDGB.jcl)")]
public sealed partial class DatasetCatalog(string workDir, IKsdsPersistence? ksdsPersistence = null)
{
    private readonly Dictionary<string, int> _jobGenerations = new(StringComparer.Ordinal);
    private readonly Dictionary<string, KeyedStore> _open = new(StringComparer.Ordinal);

    public static readonly IReadOnlyDictionary<string, (RecordLayout Layout, int KeyOffset, int KeyLength, (int, int)? Alt, string Table)> Clusters =
        new Dictionary<string, (RecordLayout, int, int, (int, int)?, string)>(StringComparer.Ordinal)
        {
            ["AWS.M2.CARDDEMO.ACCTDATA.VSAM.KSDS"] = (Layouts.Account, 0, 11, null, "vsam_acctdata"),
            ["AWS.M2.CARDDEMO.CARDDATA.VSAM.KSDS"] = (Layouts.Card, 0, 16, null, "vsam_carddata"),
            ["AWS.M2.CARDDEMO.CARDXREF.VSAM.KSDS"] = (Layouts.CardXref, 0, 16, (25, 11), "vsam_cardxref"),
            ["AWS.M2.CARDDEMO.TCATBALF.VSAM.KSDS"] = (Layouts.TranCatBal, 0, 17, null, "vsam_tcatbalf"),
            ["AWS.M2.CARDDEMO.DISCGRP.VSAM.KSDS"] = (Layouts.DisclosureGroup, 0, 16, null, "vsam_discgrp"),
            ["AWS.M2.CARDDEMO.TRANTYPE.VSAM.KSDS"] = (Layouts.TranType, 0, 2, null, "vsam_trantype"),
            ["AWS.M2.CARDDEMO.TRANCATG.VSAM.KSDS"] = (Layouts.TranCategory, 0, 6, null, "vsam_trancatg"),
            ["AWS.M2.CARDDEMO.TRANSACT.VSAM.KSDS"] = (Layouts.Transaction, 0, 16, null, "vsam_transact"),
        };

    public string WorkDir { get; } = workDir;

    /// <summary>Connection used by DSN tasks (IKJEFT01 steps); null when the chain has no Db2 work.</summary>
    public string? Db2ConnectionString { get; init; }
    public string DatasetDir => Path.Combine(WorkDir, "datasets");
    public IKsdsPersistence Persistence { get; } = ksdsPersistence ?? new FilePersistence(Path.Combine(workDir, "datasets"));

    /// <summary>A new job starts: (+n) references are re-evaluated against the catalog.</summary>
    public void BeginJob(IReadOnlyDictionary<string, int>? pinned = null)
    {
        _jobGenerations.Clear();
        foreach (var (k, v) in pinned ?? new Dictionary<string, int>())
        {
            _jobGenerations[k] = v;
        }
    }

    /// <summary>(+n) generations created by the current job - persisted so a restart writes the same generation.</summary>
    public IReadOnlyDictionary<string, int> JobGenerations => _jobGenerations;

    public string Resolve(string dsn)
    {
        var m = GdgRef().Match(dsn);
        if (!m.Success)
        {
            return dsn.EndsWith(".AIX.PATH", StringComparison.Ordinal) ? dsn[..^9] + ".KSDS" : dsn;
        }
        var baseName = m.Groups[1].Value;
        var rel = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
        var current = Generations(baseName).DefaultIfEmpty(0).Max();
        int gen;
        if (rel > 0)
        {
            if (!_jobGenerations.TryGetValue(baseName, out var created))
            {
                created = current + rel;
                _jobGenerations[baseName] = created;
            }
            gen = created;
        }
        else
        {
            var before = _jobGenerations.TryGetValue(baseName, out var c) ? c - 1 : current;
            gen = before + rel;
        }
        return $"{baseName}.G{gen:0000}V00";
    }

    public string PathOf(string dsn) => Path.Combine(DatasetDir, Resolve(dsn) + ".txt");

    public bool IsCluster(string dsn) => Clusters.ContainsKey(Resolve(dsn));

    public KeyedStore Cluster(string dsn)
    {
        var name = Resolve(dsn);
        if (!_open.TryGetValue(name, out var store))
        {
            var c = Clusters[name];
            var persistence = Persistence is PostgresPersistence ? Persistence : new FilePersistence(DatasetDir);
            store = new KeyedStore(name, c.Layout, c.KeyOffset, c.KeyLength, persistence, c.Alt);
            _open[name] = store;
            if (Persistence is PostgresPersistence)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(UsedClustersPath)!);
                File.AppendAllLines(UsedClustersPath, [name]); // a restarted run unloads the same clusters as a clean one
            }
        }
        return store;
    }

    /// <summary>Reads an input dataset; a dataset that is not catalogued fails the step (JCL error / OPEN failure), unlike an empty one.</summary>
    public List<byte[]> ReadSequential(string dsn, int lrecl)
    {
        if (IsCluster(dsn))
        {
            return Cluster(dsn).Scan().Select(r => r.Bytes).ToList();
        }
        var path = PathOf(dsn);
        return File.Exists(path) ? SequentialFile.Read(path, lrecl) : throw new FileNotFoundException($"dataset {Resolve(dsn)} not catalogued", path);
    }

    public void WriteSequential(string dsn, IEnumerable<byte[]> records, bool append = false) =>
        SequentialFile.Write(PathOf(dsn), records, append);

    /// <summary>Unload every cluster touched by the run to datasets/ (IDCAMS REPRO to a flat file) for comparison.</summary>
    public void UnloadClusters()
    {
        if (File.Exists(UsedClustersPath))
        {
            foreach (var name in File.ReadAllLines(UsedClustersPath).Distinct(StringComparer.Ordinal).ToList())
            {
                Cluster(name);
            }
        }
        var files = new FilePersistence(DatasetDir);
        foreach (var s in _open.Values)
        {
            files.Save(s.Dsn, s.Layout, s.Scan().Select(r => r.Bytes));
        }
    }

    public void CommitAll() => CommitUnit(null, null);

    /// <summary>Fault-injection point for tests: called with the name of each durability boundary.</summary>
    public Action<string>? CommitHook { get; set; }

    private string UsedClustersPath => Path.Combine(WorkDir, "state", "clusters.txt");

    private string JournalPath => Path.Combine(WorkDir, "state", "commit.journal");

    private string CheckpointPath(string key) => Path.Combine(WorkDir, "checkpoints", key + ".checkpoint.json");

    public string? LoadCheckpoint(string key) =>
        Persistence is PostgresPersistence pg ? pg.LoadCheckpoint(key)
        : File.Exists(CheckpointPath(key)) ? File.ReadAllText(CheckpointPath(key)) : null;

    /// <summary>Appends to a sequential dataset and returns its new length (recorded in the checkpoint so a restart can cut off an uncommitted tail).</summary>
    public long AppendSequential(string dsn, IEnumerable<byte[]> records)
    {
        SequentialFile.Write(PathOf(dsn), records, append: true);
        CommitHook?.Invoke("appended");
        return new FileInfo(PathOf(dsn)).Length;
    }

    /// <summary>Restores a sequential dataset to the length recorded at the last committed checkpoint.</summary>
    public void TruncateSequential(string dsn, long length)
    {
        Directory.CreateDirectory(DatasetDir);
        using var fs = new FileStream(PathOf(dsn), FileMode.OpenOrCreate);
        fs.SetLength(length);
    }

    /// <summary>
    /// Commits every open store together with a restart checkpoint as one unit (checkpointKey null: no checkpoint change;
    /// checkpointJson null: delete it). PostgreSQL uses one transaction. Files use a redo journal: the journal rename is the
    /// commit point, and <see cref="RecoverPendingCommit"/> replays a journal left by a crash.
    /// </summary>
    public void CommitUnit(string? checkpointKey, string? checkpointJson)
    {
        var stores = _open.Values.Select(s => (s.Dsn, s.Layout, Records: (IEnumerable<byte[]>)s.Scan().Select(r => r.Bytes).ToList())).ToList();
        if (Persistence is PostgresPersistence pg)
        {
            pg.SaveAll(stores, checkpointKey, checkpointJson, CommitHook);
            CommitHook?.Invoke("committed");
            return;
        }
        var files = new FilePersistence(DatasetDir);
        var entries = stores.Select(s => new JournalEntry(Path.GetRelativePath(WorkDir, files.PathOf(s.Dsn)), Convert.ToBase64String(SequentialFile.Encode(s.Records)))).ToList();
        if (checkpointKey is not null)
        {
            entries.Add(new JournalEntry(Path.GetRelativePath(WorkDir, CheckpointPath(checkpointKey)),
                checkpointJson is null ? null : Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(checkpointJson))));
        }
        Directory.CreateDirectory(Path.GetDirectoryName(JournalPath)!);
        DurableWrite(JournalPath, System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(entries));
        CommitHook?.Invoke("journal");
        Apply(entries);
        File.Delete(JournalPath);
        CommitHook?.Invoke("done");
    }

    /// <summary>Completes a file-mode commit that was interrupted after its journal became durable.</summary>
    public void RecoverPendingCommit()
    {
        if (File.Exists(JournalPath))
        {
            Apply(System.Text.Json.JsonSerializer.Deserialize<List<JournalEntry>>(File.ReadAllBytes(JournalPath))!);
            File.Delete(JournalPath);
        }
        if (File.Exists(JournalPath + ".tmp"))
        {
            File.Delete(JournalPath + ".tmp");
        }
    }

    /// <summary>A fresh run of these jobs must not resume a checkpoint an earlier run of them left in the database.</summary>
    public void ResetCheckpoints(IEnumerable<string> jobs) => (Persistence as PostgresPersistence)?.ClearCheckpoints(jobs);

    private sealed record JournalEntry(string Path, string? Content);

    private void Apply(List<JournalEntry> entries)
    {
        for (var i = 0; i < entries.Count; i++)
        {
            var path = Path.Combine(WorkDir, entries[i].Path);
            if (entries[i].Content is { } content)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                DurableWrite(path, Convert.FromBase64String(content));
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
            }
            if (i == 0)
            {
                CommitHook?.Invoke("applied");
            }
        }
    }

    private static void DurableWrite(string path, byte[] bytes)
    {
        using (var fs = new FileStream(path + ".tmp", FileMode.Create))
        {
            fs.Write(bytes);
            fs.Flush(flushToDisk: true);
        }
        File.Move(path + ".tmp", path, overwrite: true);
    }

    private IEnumerable<int> Generations(string baseName)
    {
        if (!Directory.Exists(DatasetDir))
        {
            yield break;
        }
        foreach (var f in Directory.EnumerateFiles(DatasetDir, baseName + ".G*V00.txt"))
        {
            var g = Path.GetFileName(f)[(baseName.Length + 2)..(baseName.Length + 6)];
            if (int.TryParse(g, NumberStyles.None, CultureInfo.InvariantCulture, out var n))
            {
                yield return n;
            }
        }
    }

    [GeneratedRegex(@"^(.+)\(([+-]?\d+)\)$")]
    private static partial Regex GdgRef();
}
