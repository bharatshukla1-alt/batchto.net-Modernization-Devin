using CardDemo.Data;
using CardDemo.Orchestration;

if (args.Length == 1 && args[0] == "describe")
{
    var jobs = JobCatalog.Jobs.ToDictionary(j => j.Key, j => j.Value.Steps.Select(s => new { step = s.Name, program = s.ProgramName, parm = s.Parm, dd = s.Dd }));
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { jobs, chains = JobCatalog.Chains.ToDictionary(c => c.Key, c => new { setup = c.Value.Setup, jobs = c.Value.Jobs }) }));
    return 0;
}
// carddemo-batch run-chain <chain> <fixture-case-dir> <out-dir> [--postgres <conn>] [--restart] [--quiet]
if (args.Length < 4 || args[0] != "run-chain")
{
    Console.Error.WriteLine("usage: carddemo-batch run-chain <posting|interest> <fixture-case-dir> <out-dir> [--postgres <connection>] [--db2 <connection>] [--restart] [--quiet]");
    return 2;
}
var opts = args.Skip(4).ToList();
IKsdsPersistence? persistence = null;
var pg = opts.IndexOf("--postgres");
if (pg >= 0)
{
    persistence = new PostgresPersistence(opts[pg + 1], DatasetCatalog.Clusters.ToDictionary(kv => kv.Key, kv => kv.Value.Table));
}
var quiet = opts.Contains("--quiet");
var db2 = opts.IndexOf("--db2") is var i && i >= 0 ? opts[i + 1] : null;
var rc = ChainRunner.Run(args[1], args[2], args[3], persistence, quiet ? null : Console.WriteLine, restart: opts.Contains("--restart"), db2: db2);
if (!quiet)
{
    Console.WriteLine($"chain {args[1]} ended with max RC {rc}");
}
return rc > 8 ? 1 : 0;
