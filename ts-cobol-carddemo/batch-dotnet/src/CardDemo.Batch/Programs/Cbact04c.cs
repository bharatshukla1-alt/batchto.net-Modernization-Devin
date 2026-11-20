using CardDemo.Domain;

namespace CardDemo.Batch.Programs;

/// <summary>INTCALC STEP15 - monthly interest on transaction-category balances (CBACT04C).</summary>
[Trace("app/cbl/CBACT04C.cbl")]
public sealed class Cbact04c : IBatchProgram
{
    public const string Name = "CBACT04C";

    public int Execute(StepContext ctx)
    {
        var cat = ctx.Catalog;
        var tcatbal = cat.Cluster(ctx.Dsn("TCATBALF"));
        var xref = cat.Cluster(ctx.Dsn("XREFFIL1"));
        var accounts = cat.Cluster(ctx.Dsn("ACCTFILE"));
        var discgrp = cat.Cluster(ctx.Dsn("DISCGRP"));
        var parmDate = (ctx.Parm ?? "").PadRight(10)[..10];
        var systran = new List<byte[]>();

        ctx.Display("START OF EXECUTION OF PROGRAM CBACT04C");
        var firstTime = true;
        var lastAcct = new string(' ', 11);
        decimal totalInt = 0;
        var suffix = 0;
        FixedRecord account = new(Layouts.Account);
        FixedRecord xrefRec = new(Layouts.CardXref);
        foreach (var bal in tcatbal.Scan())
        {
            ctx.RecordsRead++;
            ctx.Display(bal.ToString());
            var acctId = bal.Raw("TRANCAT-ACCT-ID");
            if (acctId != lastAcct)
            {
                if (!firstTime)
                {
                    UpdateAccount(account, totalInt, accounts);
                }
                firstTime = false;
                totalInt = 0;
                lastAcct = acctId;
                if (!accounts.TryRead(acctId, out account))
                {
                    ctx.Display("ACCOUNT NOT FOUND: " + acctId);
                    Abend(ctx, "ERROR READING ACCOUNT FILE");
                }
                if (!xref.TryReadAlternate(acctId, out xrefRec)) // BR-INT-011 (GAP-003)
                {
                    ctx.Display("ACCOUNT NOT FOUND: " + acctId);
                    Abend(ctx, "ERROR READING XREF FILE");
                }
            }
            var rate = InterestRate(ctx, discgrp, account.Raw("ACCT-GROUP-ID"), bal);
            if (rate != 0) // BR-INT-002
            {
                var monthly = MonthlyInterest(bal.Number("TRAN-CAT-BAL"), rate);
                totalInt += monthly;
                suffix = (suffix + 1) % 1_000_000; // WS-TRANID-SUFFIX PIC 9(06): ADD 1 truncates high-order digit
                systran.Add(InterestTransaction(ctx, parmDate, suffix, account, xrefRec, monthly).Bytes);
                ctx.RecordsWritten++;
            }
            // 1400-COMPUTE-FEES: no logic in the legacy program [BR-INT-012]
        }
        // BR-INT-006: the ELSE branch that would update the last account is unreachable in CBACT04C.
        cat.WriteSequential(ctx.Dsn("TRANSACT"), systran);
        cat.CommitAll();
        ctx.Display("END OF EXECUTION OF PROGRAM CBACT04C");
        return 0;
    }

    /// <summary>COMPUTE WS-MONTHLY-INT = (TRAN-CAT-BAL * DIS-INT-RATE) / 1200 - no ROUNDED, so truncated (ADR-003).</summary>
    [Trace("CBACT04C 1300-COMPUTE-INTEREST", "BR-INT-001")]
    public static decimal MonthlyInterest(decimal balance, decimal rate) => CobolCodec.Truncate(balance * rate / 1200m, 2);

    [Trace("CBACT04C 1200-GET-INTEREST-RATE", "BR-INT-003")]
    private static decimal InterestRate(StepContext ctx, Data.KeyedStore discgrp, string groupId, FixedRecord bal)
    {
        var suffix = bal.Raw("TRANCAT-TYPE-CD") + bal.Raw("TRANCAT-CD");
        if (discgrp.TryRead(groupId + suffix, out var dis))
        {
            return dis.Number("DIS-INT-RATE");
        }
        ctx.Display("DISCLOSURE GROUP RECORD MISSING");
        ctx.Display("TRY WITH DEFAULT GROUP CODE");
        if (!discgrp.TryRead("DEFAULT".PadRight(10) + suffix, out dis))
        {
            Abend(ctx, "ERROR READING DEFAULT DISCLOSURE GROUP");
        }
        return dis.Number("DIS-INT-RATE");
    }

    [Trace("CBACT04C 1050-UPDATE-ACCOUNT", "BR-INT-004,BR-INT-005")]
    private static void UpdateAccount(FixedRecord account, decimal totalInt, Data.KeyedStore accounts)
    {
        account.Set("ACCT-CURR-BAL", account.Number("ACCT-CURR-BAL") + totalInt);
        account.Set("ACCT-CURR-CYC-CREDIT", 0m);
        account.Set("ACCT-CURR-CYC-DEBIT", 0m);
        accounts.Rewrite(account);
    }

    [Trace("CBACT04C 1300-B-WRITE-TX", "BR-INT-007..010")]
    private static FixedRecord InterestTransaction(StepContext ctx, string parmDate, int suffix, FixedRecord account, FixedRecord xref, decimal amount) =>
        new FixedRecord(Layouts.Transaction)
            .Set("TRAN-ID", parmDate + suffix.ToString("000000", System.Globalization.CultureInfo.InvariantCulture))
            .Set("TRAN-TYPE-CD", "01")
            .Set("TRAN-CAT-CD", "05")
            .Set("TRAN-SOURCE", "System")
            .Set("TRAN-DESC", "Int. for a/c " + account.Raw("ACCT-ID"))
            .Set("TRAN-AMT", amount)
            .Set("TRAN-MERCHANT-ID", 0m)
            .Set("TRAN-CARD-NUM", xref.Raw("XREF-CARD-NUM"))
            .Set("TRAN-ORIG-TS", ctx.Db2Timestamp)
            .Set("TRAN-PROC-TS", ctx.Db2Timestamp);

    private static void Abend(StepContext ctx, string message)
    {
        ctx.Display(message);
        ctx.Display("ABENDING PROGRAM");
        throw new AbendException(999, message);
    }
}
