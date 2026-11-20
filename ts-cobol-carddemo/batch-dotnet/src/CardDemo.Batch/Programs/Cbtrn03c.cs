using CardDemo.Domain;

namespace CardDemo.Batch.Programs;

/// <summary>TRANREPT STEP10R - daily transaction detail report (CBTRN03C, layout CVTRA07Y).</summary>
[Trace("app/cbl/CBTRN03C.cbl")]
public sealed class Cbtrn03c : IBatchProgram
{
    public const string Name = "CBTRN03C";
    private const int PageSize = 20;
    private static readonly string Dashes = new('-', 133);

    public int Execute(StepContext ctx)
    {
        var cat = ctx.Catalog;
        var trans = cat.ReadSequential(ctx.Dsn("TRANFILE"), Layouts.Transaction.Length);
        var xref = cat.Cluster(ctx.Dsn("CARDXREF"));
        var types = cat.Cluster(ctx.Dsn("TRANTYPE"));
        var cats = cat.Cluster(ctx.Dsn("TRANCATG"));
        var parm = cat.ReadSequential(ctx.Dsn("DATEPARM"), 80).FirstOrDefault() ?? Enumerable.Repeat((byte)' ', 80).ToArray();
        // 0550-DATEPARM-READ [BR-RPT-001]
        var startDate = CobolCodec.Text(parm.AsSpan(0, 10));
        var endDate = CobolCodec.Text(parm.AsSpan(11, 10));
        var report = new List<string>();

        ctx.Display("START OF EXECUTION OF PROGRAM CBTRN03C");
        ctx.Display($"Reporting from {startDate} to {endDate}");

        var tran = new FixedRecord(Layouts.Transaction, Enumerable.Repeat((byte)' ', Layouts.Transaction.Length).ToArray());
        var firstTime = true;
        var currCard = new string(' ', 16);
        long lineCounter = 0;
        decimal pageTotal = 0, accountTotal = 0, grandTotal = 0;
        FixedRecord xrefRec = new(Layouts.CardXref);
        var index = 0;
        var eof = false;

        void Write(string line)
        {
            report.Add(line);
            lineCounter++;
        }
        void Headers()
        {
            Write("DALYREPT".PadRight(38) + "Daily Transaction Report".PadRight(41) + "Date Range: " + startDate + " to " + endDate);
            Write("");
            Write("Transaction ID".PadRight(17) + "Account ID".PadRight(12) + "Transaction Type".PadRight(19) + "Tran Category".PadRight(35) + "Tran Source".PadRight(14) + " " + "        Amount".PadRight(16));
            Write(Dashes);
        }
        // BR-RPT-005: page totals roll into the grand total; account totals reset independently
        void PageTotals()
        {
            Write("Page Total".PadRight(11) + new string('.', 86) + CobolCodec.Edit(pageTotal, "+ZZZ,ZZZ,ZZZ.ZZ"));
            grandTotal += pageTotal;
            pageTotal = 0;
            Write(Dashes);
        }
        void AccountTotals()
        {
            Write("Account Total".PadRight(13) + new string('.', 84) + CobolCodec.Edit(accountTotal, "+ZZZ,ZZZ,ZZZ.ZZ"));
            accountTotal = 0;
            Write(Dashes);
        }

        while (!eof)
        {
            // 1000-TRANFILE-GET-NEXT: READ INTO leaves TRAN-RECORD unchanged at end of file.
            if (index < trans.Count)
            {
                tran = new FixedRecord(Layouts.Transaction, trans[index++]);
                ctx.RecordsRead++;
            }
            else
            {
                eof = true;
            }
            var procDate = tran.Raw("TRAN-PROC-TS")[..10];
            if (!(string.CompareOrdinal(procDate, startDate) >= 0 && string.CompareOrdinal(procDate, endDate) <= 0))
            {
                break; // BR-RPT-002: NEXT SENTENCE leaves the PERFORM loop
            }
            if (!eof)
            {
                ctx.Display(tran.ToString());
                var card = tran.Raw("TRAN-CARD-NUM");
                if (card != currCard)
                {
                    if (!firstTime) // BR-RPT-003
                    {
                        AccountTotals();
                    }
                    currCard = card;
                    if (!xref.TryRead(card, out xrefRec))
                    {
                        Abend(ctx, "INVALID CARD NUMBER : " + card);
                    }
                }
                if (!types.TryRead(tran.Raw("TRAN-TYPE-CD"), out var type))
                {
                    Abend(ctx, "INVALID TRANSACTION TYPE : " + tran.Raw("TRAN-TYPE-CD"));
                }
                var catKey = tran.Raw("TRAN-TYPE-CD") + tran.Raw("TRAN-CAT-CD");
                if (!cats.TryRead(catKey, out var category))
                {
                    Abend(ctx, "INVALID TRAN CATG KEY : " + catKey);
                }
                // 1100-WRITE-TRANSACTION-REPORT [BR-RPT-004]
                if (firstTime)
                {
                    firstTime = false;
                    Headers();
                }
                if (lineCounter % PageSize == 0)
                {
                    PageTotals();
                    Headers();
                }
                var amt = tran.Number("TRAN-AMT");
                pageTotal += amt;
                accountTotal += amt;
                Write(Detail(tran, xrefRec, type, category));
            }
            else
            {
                // BR-RPT-006: end of file re-adds the last amount; the final account total is never written.
                var amt = tran.Number("TRAN-AMT");
                ctx.Display("TRAN-AMT " + CobolCodec.DisplaySigned(amt, 11, 2));
                ctx.Display("WS-PAGE-TOTAL" + CobolCodec.DisplaySigned(pageTotal, 11, 2));
                pageTotal += amt;
                accountTotal += amt;
                PageTotals();
                report.Add("Grand Total".PadRight(11) + new string('.', 86) + CobolCodec.Edit(grandTotal, "+ZZZ,ZZZ,ZZZ.ZZ"));
            }
        }
        ctx.Catalog.WriteSequential(ctx.Dsn("TRANREPT"), report.Select(l => CobolCodec.Latin1.GetBytes(l.PadRight(133))));
        ctx.RecordsWritten = report.Count;
        ctx.Display("END OF EXECUTION OF PROGRAM CBTRN03C");
        return 0;
    }

    [Trace("CBTRN03C 1120-WRITE-DETAIL", "BR-RPT-008")]
    private static string Detail(FixedRecord tran, FixedRecord xref, FixedRecord type, FixedRecord category) =>
        tran.Raw("TRAN-ID") + " " + xref.Raw("XREF-ACCT-ID") + " " + tran.Raw("TRAN-TYPE-CD") + "-" + type.Raw("TRAN-TYPE-DESC")[..15] + " "
        + tran.Raw("TRAN-CAT-CD") + "-" + category.Raw("TRAN-CAT-TYPE-DESC")[..29] + " " + tran.Raw("TRAN-SOURCE") + "    "
        + CobolCodec.Edit(tran.Number("TRAN-AMT"), "-ZZZ,ZZZ,ZZZ.ZZ") + "  ";

    [Trace("CBTRN03C 9999-ABEND-PROGRAM", "BR-RPT-007")]
    private static void Abend(StepContext ctx, string message)
    {
        ctx.Display(message);
        ctx.Display("ABENDING PROGRAM");
        throw new AbendException(999, message);
    }
}
