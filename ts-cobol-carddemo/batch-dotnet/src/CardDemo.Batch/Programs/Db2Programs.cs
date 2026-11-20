using System.Globalization;
using System.Text;
using CardDemo.Data;
using CardDemo.Domain;

namespace CardDemo.Batch.Programs;

/// <summary>A program run under the DSN command processor; it shares the task's unit of work.</summary>
public interface IDb2Program
{
    int Run(StepContext ctx, Db2Session db, string? parm);
}

/// <summary>IKJEFT01 "DSN SYSTEM(..) RUN PROGRAM(x) PARMS(..)": attach, run, commit at end of task.</summary>
[Trace("tools/stubs/DSNRUN.cbl", "BR-DB2-001")]
public sealed class Dsnrun(string program, string? parm) : IBatchProgram
{
    public int Execute(StepContext ctx)
    {
        var cs = ctx.Catalog.Db2ConnectionString ?? throw new AbendException(12, "DSNRUN: DATABASE CONNECT FAILED");
        using var db = new Db2Session(cs);
        ctx.Display("DSNRUN: DSN ATTACHED, RUNNING " + program);
        var rc = Resolve(program).Run(ctx, db, parm);
        ctx.Display("DSNRUN: COMMIT AT END OF TASK");
        db.Commit();
        return rc;
    }

    /// <summary>Dynamic CALL: the load-module name resolved at run time.</summary>
    [Trace("CBTRNDYN CALL WS-PGM-NAME", "BR-DYN-004")]
    public static IDb2Program Resolve(string name) => name.Trim() switch
    {
        "COBTUPDT" => new Cobtupdt(),
        "CBTRNDYN" => new Cbtrndyn(),
        _ => throw new AbendException(999, $"CALL {name.Trim()}: program not found (S806)"),
    };
}

/// <summary>COBTUPDT: apply A/U/D/* maintenance records from INPFILE to CARDDEMO.TRANSACTION_TYPE.</summary>
[Trace("COBTUPDT", "BR-TUP-001..006")]
public sealed class Cobtupdt : IDb2Program
{
    private readonly char[] _returnMsg = Enumerable.Repeat(' ', 80).ToArray();
    private int _rc;

    public int Run(StepContext ctx, Db2Session db, string? parm)
    {
        var dsn = ctx.Dsn("INPFILE");
        var exists = File.Exists(ctx.Catalog.PathOf(dsn));
        ctx.Display(exists ? "OPEN FILE OK" : "OPEN FILE NOT OK"); // [Trace("COBTUPDT 0001-OPEN-FILES", "BR-TUP-001")]
        foreach (var bytes in exists ? ctx.Catalog.ReadSequential(dsn, 53) : [])
        {
            var rec = Encoding.Latin1.GetString(bytes);
            ctx.RecordsRead++;
            ctx.Display("PROCESSING   " + rec);
            string type = rec[..1], number = rec.Substring(1, 2), desc = rec.Substring(3, 50);
            switch (type) // [Trace("COBTUPDT 1003-TREAT-RECORD", "BR-TUP-002")]
            {
                case "A":
                    ctx.Display("ADDING RECORD");
                    Apply(ctx, db, "INSERT INTO CARDDEMO.TRANSACTION_TYPE (TR_TYPE, TR_DESCRIPTION) VALUES ($1, $2)", "RECORD INSERTED SUCCESSFULLY", number, desc);
                    break;
                case "U":
                    ctx.Display("UPDATING RECORD");
                    Apply(ctx, db, "UPDATE CARDDEMO.TRANSACTION_TYPE SET TR_DESCRIPTION = $1 WHERE TR_TYPE = $2", "RECORD UPDATED SUCCESSFULLY", desc, number);
                    break;
                case "D":
                    ctx.Display("DELETING RECORD");
                    Apply(ctx, db, "DELETE FROM CARDDEMO.TRANSACTION_TYPE WHERE TR_TYPE = $1", "RECORD DELETED SUCCESSFULLY", number);
                    break;
                case "*":
                    ctx.Display("IGNORING COMMENTED LINE");
                    break;
                default:
                    Abend(ctx, "ERROR: TYPE NOT VALID");
                    break;
            }
        }
        return _rc;
    }

    /// <summary>SQLCODE 0 (ocesql also reports 0 when no row matched, so +100 never fires: BR-TUP-004) or negative.</summary>
    private void Apply(StepContext ctx, Db2Session db, string sql, string ok, params object[] args)
    {
        var sqlcode = db.Execute(sql, args);
        if (sqlcode == 0)
        {
            ctx.Display(ok);
            ctx.RecordsWritten++;
        }
        else if (sqlcode < 0)
        {
            Abend(ctx, "Error accessing: TRANSACTION_TYPE table. SQLCODE:" + sqlcode.ToString(CultureInfo.InvariantCulture).PadLeft(5)); // PIC ----9
        }
    }

    /// <summary>9999-ABEND: STRING into WS-RETURN-MSG (never cleared), DISPLAY it, RETURN-CODE 4, and carry on.</summary>
    [Trace("COBTUPDT 9999-ABEND", "BR-TUP-005")]
    private void Abend(StepContext ctx, string text)
    {
        text.CopyTo(0, _returnMsg, 0, Math.Min(text.Length, 80));
        ctx.Display(new string(_returnMsg));
        _rc = 4;
    }
}

/// <summary>CBTRNDYN: PREPARE/EXECUTE one ad-hoc statement from DYNSQL, then CALL the program named in PARMS.</summary>
[Trace("CBTRNDYN", "BR-DYN-001..004")]
public sealed class Cbtrndyn : IDb2Program
{
    public int Run(StepContext ctx, Db2Session db, string? parm)
    {
        var stmt = new StringBuilder();
        var lines = 0;
        foreach (var bytes in ctx.Catalog.ReadSequential(ctx.Dsn("DYNSQL"), 80)) // [Trace("CBTRNDYN 1000-READ-STATEMENT", "BR-DYN-001")]
        {
            var rec = Encoding.Latin1.GetString(bytes);
            if (!rec.StartsWith("--", StringComparison.Ordinal))
            {
                stmt.Append(rec.AsSpan(0, 72)).Append(' ');
                lines++;
            }
        }
        var sql = stmt.Length > 800 ? stmt.ToString(0, 800) : stmt.ToString();
        ctx.Display("CBTRNDYN: " + (lines % 100).ToString("00", CultureInfo.InvariantCulture) + " SQL LINES READ");
        var rc = 0;
        // PREPARE + EXECUTE: ocesql reports statement errors at EXECUTE (BR-DYN-002); an empty statement gives -212
        // without touching the unit of work (observed on the estate).
        var sqlcode = string.IsNullOrWhiteSpace(sql) ? -212 : db.Execute(sql.TrimEnd());
        ctx.Display("CBTRNDYN: EXECUTE SQLCODE " + SqlCode(sqlcode));
        if (sqlcode < 0)
        {
            rc = 8; // [Trace("CBTRNDYN 2000-RUN-DYNAMIC-SQL", "BR-DYN-003")]
        }
        if (!string.IsNullOrEmpty(parm))
        {
            var pgm = parm.PadRight(8)[..8];
            ctx.Display("CBTRNDYN: CALLING " + pgm);
            // The callee ends with STOP RUN: the run unit ends there and its RETURN-CODE is the step RC (GAP-007).
            return Dsnrun.Resolve(pgm).Run(ctx, db, null);
        }
        ctx.Display("CBTRNDYN: END");
        return rc;
    }

    /// <summary>DISPLAY of SQLCODE (S9(9) COMP-5): sign and 10 digits.</summary>
    private static string SqlCode(int sqlcode) => (sqlcode < 0 ? "-" : "+") + Math.Abs(sqlcode).ToString("0000000000", CultureInfo.InvariantCulture);
}
