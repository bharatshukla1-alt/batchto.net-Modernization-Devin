using CardDemo.Domain;

namespace CardDemo.Batch.Programs;

/// <summary>DFSORT subset used by TRANREPT STEP05R: SORT FIELDS=(pos,len,ZD,A) with INCLUDE COND=(pos,len,GE,c1,AND,pos,len,LE,c2).</summary>
[Trace("app/jcl/TRANREPT.jcl STEP05R SORT")]
public sealed class SortStep(int lrecl, int sortPos, int sortLen, int includePos, int includeLen, string low, string high) : IBatchProgram
{
    public int Execute(StepContext ctx)
    {
        var input = ctx.Catalog.ReadSequential(ctx.Dsn("SORTIN"), lrecl);
        ctx.RecordsRead = input.Count;
        var output = input
            .Where(r =>
            {
                var v = CobolCodec.Text(r.AsSpan(includePos - 1, includeLen));
                return string.CompareOrdinal(v, low) >= 0 && string.CompareOrdinal(v, high) <= 0;
            })
            .OrderBy(r => CobolCodec.DecodeZoned(r.AsSpan(sortPos - 1, sortLen), 0)) // stable (GAP-005)
            .ToList();
        ctx.Catalog.WriteSequential(ctx.Dsn("SORTOUT"), output);
        ctx.RecordsWritten = output.Count;
        return 0;
    }
}

public enum IdcamsAction { DefineGdg, Delete, Define, Repro, BuildIndex }

/// <summary>IDCAMS / SDSF provisioning steps of the setup jobs (DEFGDGB, ACCTFILE, XREFFILE, ...).</summary>
[Trace("IDCAMS")]
public sealed class IdcamsStep(IdcamsAction action, string? dataset = null, string? inDd = null, string? outDd = null, int lrecl = 0) : IBatchProgram
{
    public int Execute(StepContext ctx)
    {
        var cat = ctx.Catalog;
        switch (action)
        {
            case IdcamsAction.Delete when dataset is not null && cat.IsCluster(dataset):
                cat.Cluster(dataset).Clear();
                cat.Cluster(dataset).Commit();
                break;
            case IdcamsAction.Repro:
                var records = cat.ReadSequential(ctx.Dsn(inDd!), lrecl);
                var target = ctx.Dsn(outDd!);
                if (cat.IsCluster(target))
                {
                    var store = cat.Cluster(target);
                    var skipped = 0;
                    foreach (var r in records)
                    {
                        if (!store.Write(new FixedRecord(store.Layout, r)))
                        {
                            skipped++; // duplicate key: first record kept, as the legacy loader does
                        }
                    }
                    store.Commit();
                    ctx.RecordsRead = records.Count;
                    ctx.RecordsWritten = records.Count - skipped;
                    if (skipped > 0)
                    {
                        ctx.Display($"IDC3314I {skipped} DUPLICATE KEY(S) SKIPPED");
                        return 4;
                    }
                    break;
                }
                else
                {
                    cat.WriteSequential(target, records);
                }
                ctx.RecordsRead = ctx.RecordsWritten = records.Count;
                break;
            default:
                break; // DEFINE CLUSTER/AIX/PATH/GDG, BLDINDEX, SDSF: the keyed store and catalog provide these.
        }
        return 0;
    }
}
