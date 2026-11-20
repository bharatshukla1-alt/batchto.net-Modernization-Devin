using CardDemo.Domain;

namespace CardDemo.Batch.Programs;

/// <summary>COBDATFT (HLASM) date conversion - type 1 = YYYYMMDD, type 2 = YYYY-MM-DD (GAP-001: ported from app/asm/COBDATFT.asm).</summary>
[Trace("app/asm/COBDATFT.asm", "BR-RAC-003")]
public static class CobDatFt
{
    public static string Convert(string input, char inType, char outType)
    {
        var s = input.PadRight(20);
        var (y, m, d) = inType == '2' ? (s[..4], s.Substring(5, 2), s.Substring(8, 2)) : (s[..4], s.Substring(4, 2), s.Substring(6, 2));
        return (outType == '2' ? y + m + d : y + "-" + m + "-" + d).PadRight(20);
    }
}

/// <summary>READACCT STEP05 - account extract to PSCOMP (COMP-3), ARRYPS (OCCURS) and VBPS (variable) files (CBACT01C).</summary>
[Trace("app/cbl/CBACT01C.cbl")]
public sealed class Cbact01c : IBatchProgram
{
    public const string Name = "CBACT01C";

    public int Execute(StepContext ctx)
    {
        var accounts = ctx.Catalog.Cluster(ctx.Dsn("ACCTFILE"));
        var outRecs = new List<byte[]>();
        var arrRecs = new List<byte[]>();
        var vbRecs = new List<byte[]>();
        var outDebit = new byte[7];
        CobolCodec.EncodePacked(outDebit, 0m, 2, true);
        ctx.Display("START OF EXECUTION OF PROGRAM CBACT01C");
        foreach (var a in accounts.Scan())
        {
            ctx.RecordsRead++;
            // 1100-DISPLAY-ACCT-RECORD [BR-RAC-001]
            foreach (var f in new[] { "ACCT-ID", "ACCT-ACTIVE-STATUS", "ACCT-CURR-BAL", "ACCT-CREDIT-LIMIT", "ACCT-CASH-CREDIT-LIMIT", "ACCT-OPEN-DATE", "ACCT-EXPIRAION-DATE", "ACCT-REISSUE-DATE", "ACCT-CURR-CYC-CREDIT", "ACCT-CURR-CYC-DEBIT", "ACCT-GROUP-ID" })
            {
                var spec = a.Layout[f];
                var value = spec.Kind == FieldKind.Zoned && spec.Signed ? CobolCodec.DisplaySigned(a.Number(f), spec.Digits, spec.Scale) : a.Raw(f);
                ctx.Display((f + " ").PadRight(25) + ":" + value);
            }
            ctx.Display(new string('-', 49));

            // 1300-POPUL-ACCT-RECORD: 107-byte record, cycle debit packed [BR-RAC-002, GAP-002]
            var o = new byte[107];
            Array.Fill(o, (byte)' ');
            Copy(a, "ACCT-ID", o, 0, 11);
            Copy(a, "ACCT-ACTIVE-STATUS", o, 11, 1);
            CobolCodec.EncodeZoned(o.AsSpan(12, 12), a.Number("ACCT-CURR-BAL"), 2, true);
            CobolCodec.EncodeZoned(o.AsSpan(24, 12), a.Number("ACCT-CREDIT-LIMIT"), 2, true);
            CobolCodec.EncodeZoned(o.AsSpan(36, 12), a.Number("ACCT-CASH-CREDIT-LIMIT"), 2, true);
            Copy(a, "ACCT-OPEN-DATE", o, 48, 10);
            Copy(a, "ACCT-EXPIRAION-DATE", o, 58, 10);
            CobolCodec.Latin1.GetBytes(CobDatFt.Convert(a.Raw("ACCT-REISSUE-DATE"), '2', '2')[..10], o.AsSpan(68, 10));
            CobolCodec.EncodeZoned(o.AsSpan(78, 12), a.Number("ACCT-CURR-CYC-CREDIT"), 2, true);
            if (a.Number("ACCT-CURR-CYC-DEBIT") == 0)
            {
                CobolCodec.EncodePacked(outDebit, 2525.00m, 2, true);
            }
            outDebit.CopyTo(o, 90);
            Copy(a, "ACCT-GROUP-ID", o, 97, 10);
            outRecs.Add(o);

            // 1400-POPUL-ARRAY-RECORD [BR-RAC-004]
            var r = new byte[110];
            Array.Fill(r, (byte)' ');
            Copy(a, "ACCT-ID", r, 0, 11);
            var occ = new (decimal Bal, decimal Debit)[] { (a.Number("ACCT-CURR-BAL"), 1005.00m), (a.Number("ACCT-CURR-BAL"), 1525.00m), (-1025.00m, -2500.00m), (0, 0), (0, 0) };
            for (var i = 0; i < 5; i++)
            {
                CobolCodec.EncodeZoned(r.AsSpan(11 + 19 * i, 12), occ[i].Bal, 2, true);
                CobolCodec.EncodePacked(r.AsSpan(23 + 19 * i, 7), occ[i].Debit, 2, true);
            }
            arrRecs.Add(r);

            // 1500-POPUL-VBRC-RECORD [BR-RAC-005]
            var v1 = CobolCodec.Latin1.GetBytes(a.Raw("ACCT-ID") + a.Raw("ACCT-ACTIVE-STATUS"));
            var v2 = new byte[39];
            Copy(a, "ACCT-ID", v2, 0, 11);
            CobolCodec.EncodeZoned(v2.AsSpan(11, 12), a.Number("ACCT-CURR-BAL"), 2, true);
            CobolCodec.EncodeZoned(v2.AsSpan(23, 12), a.Number("ACCT-CREDIT-LIMIT"), 2, true);
            CobolCodec.Latin1.GetBytes(a.Raw("ACCT-REISSUE-DATE")[..4], v2.AsSpan(35, 4));
            ctx.Display("VBRC-REC1:" + CobolCodec.Text(v1));
            ctx.Display("VBRC-REC2:" + CobolCodec.Text(v2));
            vbRecs.Add(v1);
            vbRecs.Add(v2);
            ctx.Display(a.ToString());
            ctx.RecordsWritten += 4;
        }
        Data.SequentialFile.WriteBinary(ctx.Catalog.PathOf(ctx.Dsn("OUTFILE")).Replace(".txt", ".bin", StringComparison.Ordinal), outRecs);
        Data.SequentialFile.WriteBinary(ctx.Catalog.PathOf(ctx.Dsn("ARRYFILE")).Replace(".txt", ".bin", StringComparison.Ordinal), arrRecs);
        Data.SequentialFile.WriteBinary(ctx.Catalog.PathOf(ctx.Dsn("VBRCFILE")).Replace(".txt", ".bin", StringComparison.Ordinal),
            vbRecs.Select(v => new byte[] { (byte)((v.Length + 4) >> 8), (byte)(v.Length + 4), 0, 0 }.Concat(v).ToArray())); // RDW + data
        ctx.Display("END OF EXECUTION OF PROGRAM CBACT01C");
        return 0;
    }

    private static void Copy(FixedRecord a, string field, byte[] target, int offset, int length)
    {
        var f = a.Layout[field];
        Array.Copy(a.Bytes, f.Offset, target, offset, Math.Min(length, f.Length));
    }
}

/// <summary>READCARD STEP05 - list the card master in key order (CBACT02C).</summary>
[Trace("app/cbl/CBACT02C.cbl", "BR-RCD-001")]
public sealed class Cbact02c : IBatchProgram
{
    public const string Name = "CBACT02C";

    public int Execute(StepContext ctx)
    {
        ctx.Display("START OF EXECUTION OF PROGRAM CBACT02C");
        foreach (var card in ctx.Catalog.Cluster(ctx.Dsn("CARDFILE")).Scan())
        {
            ctx.RecordsRead++;
            ctx.Display(card.ToString());
        }
        ctx.Display("END OF EXECUTION OF PROGRAM CBACT02C");
        return 0;
    }
}
