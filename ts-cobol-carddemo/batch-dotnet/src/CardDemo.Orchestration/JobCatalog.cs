using CardDemo.Batch;
using CardDemo.Batch.Programs;
using CardDemo.Domain;

namespace CardDemo.Orchestration;

public sealed record StepDefinition(string Name, Func<IBatchProgram> Program, IReadOnlyDictionary<string, string> Dd, string? Parm = null)
{
    public string ProgramName { get; init; } = "IDCAMS";
}

public sealed record JobDefinition(string Name, IReadOnlyList<StepDefinition> Steps);

/// <summary>Job definitions transcribed from app/jcl (one StepDefinition per EXEC, DD names kept).</summary>
[Trace("app/jcl")]
public static class JobCatalog
{
    private const string P = "AWS.M2.CARDDEMO.";

    private static Dictionary<string, string> Dd(params string[] pairs)
    {
        var d = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < pairs.Length; i += 2)
        {
            d[pairs[i]] = P + pairs[i + 1];
        }
        return d;
    }

    private static StepDefinition Idcams(string step, IdcamsAction action, string? dsn = null) =>
        new(step, () => new IdcamsStep(action, dsn is null ? null : P + dsn), Dd());

    private static StepDefinition Repro(string step, string from, string to, int lrecl) =>
        new(step, () => new IdcamsStep(IdcamsAction.Repro, null, "INFILE", "OUTFILE", lrecl), Dd("INFILE", from, "OUTFILE", to));

    private static JobDefinition Cluster(string job, string name, int lrecl) => new(job,
    [
        Idcams("STEP05", IdcamsAction.Delete, name + ".VSAM.KSDS"),
        Idcams("STEP10", IdcamsAction.Define),
        Repro("STEP15", name + ".PS", name + ".VSAM.KSDS", lrecl),
    ]);

    private static JobDefinition ClusterWithAix(string job, string name, string source, int lrecl, bool cics) => new(job,
    [
        .. cics ? new[] { new StepDefinition("CLCIFIL", () => new IdcamsStep(IdcamsAction.Define), Dd()) { ProgramName = "SDSF" } } : [],
        Idcams("STEP05", IdcamsAction.Delete, name + ".VSAM.KSDS"),
        Idcams("STEP10", IdcamsAction.Define),
        Repro("STEP15", source, name + ".VSAM.KSDS", lrecl),
        Idcams("STEP20", IdcamsAction.Define),
        Idcams("STEP25", IdcamsAction.Define),
        Idcams("STEP30", IdcamsAction.BuildIndex),
        .. cics ? new[] { new StepDefinition("OPCIFIL", () => new IdcamsStep(IdcamsAction.Define), Dd()) { ProgramName = "SDSF" } } : [],
    ]);

    public static readonly IReadOnlyDictionary<string, JobDefinition> Jobs = new Dictionary<string, JobDefinition>(StringComparer.Ordinal)
    {
        ["DEFGDGB"] = new("DEFGDGB", [Idcams("STEP05", IdcamsAction.DefineGdg)]),
        ["MNTTRDB2"] = new("MNTTRDB2", [new StepDefinition("STEP1", () => new Dsnrun("COBTUPDT", null),
            new Dictionary<string, string>(StringComparer.Ordinal) { ["INPFILE"] = "INPFILE" }) { ProgramName = "IKJEFT01" }]),
        ["MNTTRDYN"] = new("MNTTRDYN", [new StepDefinition("STEP1", () => new Dsnrun("CBTRNDYN", "COBTUPDT"),
            new Dictionary<string, string>(StringComparer.Ordinal) { ["DYNSQL"] = "DYNSQL", ["INPFILE"] = "INPFILE" }) { ProgramName = "IKJEFT01" }]),
        ["DALYREJS"] = new("DALYREJS", [Idcams("STEP05", IdcamsAction.DefineGdg)]),
        ["ACCTFILE"] = Cluster("ACCTFILE", "ACCTDATA", 300),
        ["TCATBALF"] = Cluster("TCATBALF", "TCATBALF", 50),
        ["DISCGRP"] = Cluster("DISCGRP", "DISCGRP", 50),
        ["TRANTYPE"] = Cluster("TRANTYPE", "TRANTYPE", 60),
        ["TRANCATG"] = Cluster("TRANCATG", "TRANCATG", 60),
        ["CARDFILE"] = Cluster("CARDFILE", "CARDDATA", 150),
        ["XREFFILE"] = ClusterWithAix("XREFFILE", "CARDXREF", "CARDXREF.PS", 50, cics: false),
        ["TRANFILE"] = ClusterWithAix("TRANFILE", "TRANSACT", "DALYTRAN.PS.INIT", 350, cics: true),
        ["POSTTRAN"] = new("POSTTRAN",
        [
            new("STEP15", () => new Cbtrn02c(), Dd("TRANFILE", "TRANSACT.VSAM.KSDS", "DALYTRAN", "DALYTRAN.PS", "XREFFILE", "CARDXREF.VSAM.KSDS",
                "DALYREJS", "DALYREJS(+1)", "ACCTFILE", "ACCTDATA.VSAM.KSDS", "TCATBALF", "TCATBALF.VSAM.KSDS")) { ProgramName = Cbtrn02c.Name },
        ]),
        ["TRANREPT"] = new("TRANREPT",
        [
            Repro("STEP05R.PRC001", "TRANSACT.VSAM.KSDS", "TRANSACT.BKUP(+1)", 350),
            new("STEP05R", () => new SortStep(350, 263, 16, 305, 10, "2022-01-01", "2022-07-06"),
                Dd("SORTIN", "TRANSACT.BKUP(+1)", "SORTOUT", "TRANSACT.DALY(+1)")) { ProgramName = "SORT" },
            new("STEP10R", () => new Cbtrn03c(), Dd("TRANFILE", "TRANSACT.DALY(+1)", "CARDXREF", "CARDXREF.VSAM.KSDS", "TRANTYPE", "TRANTYPE.VSAM.KSDS",
                "TRANCATG", "TRANCATG.VSAM.KSDS", "DATEPARM", "DATEPARM", "TRANREPT", "TRANREPT(+1)")) { ProgramName = Cbtrn03c.Name },
        ]),
        ["READACCT"] = new("READACCT",
        [
            new("PREDEL", () => new IdcamsStep(IdcamsAction.Define), Dd()) { ProgramName = "IEFBR14" },
            new("STEP05", () => new Cbact01c(), Dd("ACCTFILE", "ACCTDATA.VSAM.KSDS", "OUTFILE", "ACCTDATA.PSCOMP", "ARRYFILE", "ACCTDATA.ARRYPS", "VBRCFILE", "ACCTDATA.VBPS")) { ProgramName = Cbact01c.Name },
        ]),
        ["READCARD"] = new("READCARD",
        [
            new("STEP05", () => new Cbact02c(), Dd("CARDFILE", "CARDDATA.VSAM.KSDS")) { ProgramName = Cbact02c.Name },
        ]),
        ["INTCALC"] = new("INTCALC",
        [
            new("STEP15", () => new Cbact04c(), Dd("TCATBALF", "TCATBALF.VSAM.KSDS", "XREFFILE", "CARDXREF.VSAM.KSDS", "XREFFIL1", "CARDXREF.VSAM.AIX.PATH",
                "ACCTFILE", "ACCTDATA.VSAM.KSDS", "DISCGRP", "DISCGRP.VSAM.KSDS", "TRANSACT", "SYSTRAN(+1)"), "2022071800") { ProgramName = Cbact04c.Name },
        ]),
    };

    /// <summary>Chains as in tools/runjcl/chains.json (setup jobs, then the scheduled jobs).</summary>
    public static readonly IReadOnlyDictionary<string, (string[] Setup, string[] Jobs)> Chains = new Dictionary<string, (string[], string[])>(StringComparer.Ordinal)
    {
        ["posting"] = (["DEFGDGB", "DALYREJS", "ACCTFILE", "XREFFILE", "TCATBALF", "DISCGRP", "TRANTYPE", "TRANCATG", "TRANFILE"], ["POSTTRAN", "TRANREPT"]),
        ["interest"] = (["DEFGDGB", "ACCTFILE", "XREFFILE", "TCATBALF", "DISCGRP"], ["INTCALC"]),
        ["readacct"] = (["ACCTFILE"], ["READACCT"]),
        ["readcard"] = (["CARDFILE"], ["READCARD"]),
        ["trantype-update"] = ([], ["MNTTRDB2"]),
        ["trantype-dynamic"] = ([], ["MNTTRDYN"]),
    };

    /// <summary>Tables reset from input/db/seed.sql and snapshotted (ORDER BY as in chains.json) for Db2 chains.</summary>
    public static readonly IReadOnlyDictionary<string, (string Table, string Order)[]> Db2Tables = new Dictionary<string, (string, string)[]>(StringComparer.Ordinal)
    {
        ["trantype-update"] = [("carddemo.transaction_type", "tr_type"), ("carddemo.transaction_type_categor", "trc_type_code, trc_type_category")],
        ["trantype-dynamic"] = [("carddemo.transaction_type", "tr_type"), ("carddemo.transaction_type_categor", "trc_type_code, trc_type_category")],
    };
}
