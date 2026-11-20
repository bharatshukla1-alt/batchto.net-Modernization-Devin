using System.Globalization;
using CardDemo.Batch.Restart;
using CardDemo.Domain;

namespace CardDemo.Batch.Programs;

/// <summary>POSTTRAN STEP15 - post the daily transaction file (CBTRN02C).</summary>
[Trace("app/cbl/CBTRN02C.cbl")]
public sealed class Cbtrn02c : IBatchProgram
{
    public const string Name = "CBTRN02C";

    /// <summary>Test hook: fail after this many input records to exercise chunk restart.</summary>
    public int? FailAfter { get; init; }

    public int Execute(StepContext ctx)
    {
        var cat = ctx.Catalog;
        var transact = cat.Cluster(ctx.Dsn("TRANFILE"));
        var xref = cat.Cluster(ctx.Dsn("XREFFILE"));
        var accounts = cat.Cluster(ctx.Dsn("ACCTFILE"));
        var tcatbal = cat.Cluster(ctx.Dsn("TCATBALF"));
        var daily = cat.ReadSequential(ctx.Dsn("DALYTRAN"), Layouts.DailyTransaction.Length);
        var rejectsDsn = ctx.Dsn("DALYREJS");

        var checkpoint = ctx.Checkpoints?.Load(ctx.Job, ctx.Step);
        var start = checkpoint?.Position ?? 0;
        decimal processed = 0, rejected = 0;
        if (checkpoint is null)
        {
            ctx.Display("START OF EXECUTION OF PROGRAM CBTRN02C");
            transact.Clear(); // [Trace("CBTRN02C 0100-TRANFILE-OPEN", "BR-PST-010")] OPEN OUTPUT
            cat.WriteSequential(rejectsDsn, []);
        }
        else
        {
            cat.TruncateSequential(rejectsDsn, long.Parse(checkpoint.State["rejectBytes"], CultureInfo.InvariantCulture));
            processed = decimal.Parse(checkpoint.State["processed"], CultureInfo.InvariantCulture);
            rejected = decimal.Parse(checkpoint.State["rejected"], CultureInfo.InvariantCulture);
            ctx.Sysout.AddRange(checkpoint.State["sysout"].Split('\n', StringSplitOptions.RemoveEmptyEntries));
        }

        var pendingRejects = new List<byte[]>();
        for (var i = start; i < daily.Count; i++)
        {
            if (FailAfter is { } n && i == n)
            {
                throw new AbendException(999, $"injected failure at record {n + 1}");
            }
            var tran = new FixedRecord(Layouts.DailyTransaction, daily[i]);
            ctx.RecordsRead++;
            processed++;
            var (reason, desc, xrefRec, acctRec) = Validate(tran, xref, accounts);
            if (reason == 0)
            {
                Post(ctx, tran, xrefRec!, acctRec!, transact, tcatbal, accounts);
            }
            else
            {
                rejected++;
                pendingRejects.Add(RejectRecord(tran, reason, desc));
            }
            if ((i + 1 - start) % ctx.ChunkSize == 0 && ctx.Checkpoints is not null)
            {
                Commit(ctx, rejectsDsn, pendingRejects, i + 1, processed, rejected);
            }
        }
        ctx.Display("TRANSACTIONS PROCESSED :" + CobolCodec.DisplayUnsigned(processed, 9));
        ctx.Display("TRANSACTIONS REJECTED  :" + CobolCodec.DisplayUnsigned(rejected, 9));
        cat.AppendSequential(rejectsDsn, pendingRejects);
        if (ctx.Checkpoints is null)
        {
            cat.CommitAll();
        }
        else
        {
            ctx.Checkpoints.Complete(ctx.Job, ctx.Step);
        }
        ctx.Display("END OF EXECUTION OF PROGRAM CBTRN02C");
        return rejected > 0 ? 4 : 0; // [Trace("CBTRN02C", "BR-PST-011")]
    }

    private static void Commit(StepContext ctx, string rejectsDsn, List<byte[]> rejects, int position, decimal processed, decimal rejected)
    {
        var rejectBytes = ctx.Catalog.AppendSequential(rejectsDsn, rejects);
        rejects.Clear();
        ctx.Checkpoints!.Commit(new Checkpoint(ctx.Job, ctx.Step, position, new Dictionary<string, string>
        {
            ["rejectBytes"] = rejectBytes.ToString(CultureInfo.InvariantCulture),
            ["processed"] = processed.ToString(CultureInfo.InvariantCulture),
            ["rejected"] = rejected.ToString(CultureInfo.InvariantCulture),
            ["sysout"] = string.Join('\n', ctx.Sysout),
        }, DateTime.UtcNow));
    }

    [Trace("CBTRN02C 1500-VALIDATE-TRAN", "BR-PST-001..004")]
    public static (int Reason, string Desc, FixedRecord? Xref, FixedRecord? Account) Validate(FixedRecord tran, Data.KeyedStore xref, Data.KeyedStore accounts)
    {
        if (!xref.TryRead(tran.Raw("DALYTRAN-CARD-NUM"), out var x))
        {
            return (100, "INVALID CARD NUMBER FOUND", null, null);
        }
        if (!accounts.TryRead(x.Raw("XREF-ACCT-ID"), out var acct))
        {
            return (101, "ACCOUNT RECORD NOT FOUND", x, null);
        }
        var (reason, desc) = (0, "");
        var tempBal = acct.Number("ACCT-CURR-CYC-CREDIT") - acct.Number("ACCT-CURR-CYC-DEBIT") + tran.Number("DALYTRAN-AMT");
        if (acct.Number("ACCT-CREDIT-LIMIT") < tempBal)
        {
            (reason, desc) = (102, "OVERLIMIT TRANSACTION");
        }
        if (string.CompareOrdinal(acct.Raw("ACCT-EXPIRAION-DATE"), tran.Raw("DALYTRAN-ORIG-TS")[..10]) < 0)
        {
            (reason, desc) = (103, "TRANSACTION RECEIVED AFTER ACCT EXPIRATION");
        }
        return (reason, desc, x, acct);
    }

    [Trace("CBTRN02C 2500-WRITE-REJECT-REC", "BR-PST-005")]
    private static byte[] RejectRecord(FixedRecord tran, int reason, string desc)
    {
        var rej = new FixedRecord(Layouts.RejectRecord, tran.Bytes);
        rej.Set("REJ-FAIL-REASON", reason);
        rej.Set("REJ-FAIL-REASON-DESC", desc);
        return rej.Bytes;
    }

    [Trace("CBTRN02C 2000-POST-TRANSACTION")]
    private static void Post(StepContext ctx, FixedRecord tran, FixedRecord xref, FixedRecord acct, Data.KeyedStore transact, Data.KeyedStore tcatbal, Data.KeyedStore accounts)
    {
        var posted = new FixedRecord(Layouts.Transaction);
        foreach (var f in Layouts.Transaction.Fields.Where(f => !f.IsFiller && f.Name != "TRAN-PROC-TS"))
        {
            var src = Layouts.DailyTransaction["DALY" + f.Name];
            Array.Copy(tran.Bytes, src.Offset, posted.Bytes, f.Offset, f.Length);
        }
        posted.Set("TRAN-PROC-TS", ctx.Db2Timestamp); // BR-PST-009
        var amt = tran.Number("DALYTRAN-AMT");

        // 2700-UPDATE-TCATBAL [BR-PST-006, BR-PST-007]
        var key = xref.Raw("XREF-ACCT-ID") + tran.Raw("DALYTRAN-TYPE-CD") + tran.Raw("DALYTRAN-CAT-CD");
        if (!tcatbal.TryRead(key, out var bal))
        {
            ctx.Display($"TCATBAL record not found for key : {key}.. Creating.");
            bal = new FixedRecord(Layouts.TranCatBal)
                .Set("TRANCAT-ACCT-ID", xref.Raw("XREF-ACCT-ID"))
                .Set("TRANCAT-TYPE-CD", tran.Raw("DALYTRAN-TYPE-CD"))
                .Set("TRANCAT-CD", tran.Raw("DALYTRAN-CAT-CD"))
                .Set("TRAN-CAT-BAL", amt);
            tcatbal.Write(bal);
        }
        else
        {
            bal.Set("TRAN-CAT-BAL", bal.Number("TRAN-CAT-BAL") + amt);
            tcatbal.Rewrite(bal);
        }

        // 2800-UPDATE-ACCOUNT-REC [BR-PST-008]
        acct.Set("ACCT-CURR-BAL", acct.Number("ACCT-CURR-BAL") + amt);
        var cycField = amt >= 0 ? "ACCT-CURR-CYC-CREDIT" : "ACCT-CURR-CYC-DEBIT";
        acct.Set(cycField, acct.Number(cycField) + amt);
        accounts.Rewrite(acct);

        // 2900-WRITE-TRANSACTION-FILE
        if (!transact.Write(posted))
        {
            ctx.Display("ERROR WRITING TO TRANSACTION FILE");
            throw new AbendException(999, "duplicate TRAN-ID " + posted.Raw("TRAN-ID"));
        }
        ctx.RecordsWritten++;
    }
}
