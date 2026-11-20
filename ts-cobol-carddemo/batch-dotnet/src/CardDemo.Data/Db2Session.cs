using Npgsql;

namespace CardDemo.Data;

/// <summary>
/// One DSN task on PostgreSQL: a single unit of work committed at end of task, with SQLCODEs mapped the way
/// Open COBOL ESQL reports them in the golden runs (unique violation -403, any other error -400, no row found 0).
/// </summary>
[CardDemo.Domain.Trace("tools/stubs/DSNRUN.cbl + ocesql", "GAP-006")]
public sealed class Db2Session : IDisposable
{
    private readonly NpgsqlConnection _conn;
    private readonly NpgsqlTransaction _tx;

    public Db2Session(string connectionString)
    {
        _conn = new NpgsqlConnection(connectionString);
        _conn.Open();
        using (var cmd = new NpgsqlCommand("SET search_path TO carddemo, public", _conn))
        {
            cmd.ExecuteNonQuery();
        }
        _tx = _conn.BeginTransaction();
    }

    public int Execute(string sql, params object[] args)
    {
        try
        {
            using var cmd = new NpgsqlCommand(sql, _conn, _tx);
            foreach (var a in args)
            {
                // Untyped, as ocesql sends host variables: the server infers CHAR(n), so trailing blanks compare equal.
                cmd.Parameters.Add(a is string ? new NpgsqlParameter { Value = a, NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Unknown } : new NpgsqlParameter { Value = a });
            }
            cmd.ExecuteNonQuery();
            return 0;
        }
        catch (PostgresException ex)
        {
            return ex.SqlState == PostgresErrorCodes.UniqueViolation ? -403 : -400;
        }
    }

    /// <summary>COMMIT at end of task; after a failed statement PostgreSQL has aborted the unit of work, so this rolls back.</summary>
    public void Commit() => _tx.Commit();

    public void Dispose()
    {
        _tx.Dispose();
        _conn.Dispose();
    }
}

/// <summary>Database side of the parity contract (tools/parity/record.py reset_database / snapshot_database).</summary>
public static partial class Db2Fixture
{
    /// <summary>Table and column lists come from JobCatalog; anything that is not a plain identifier list is refused.</summary>
    private static string Ident(string text) => IdentList().IsMatch(text) ? text : throw new ArgumentException($"not an identifier list: {text}");

    [System.Text.RegularExpressions.GeneratedRegex(@"^[a-z_][a-z0-9_]*(\.[a-z_][a-z0-9_]*)?(, [a-z_][a-z0-9_]*)*$")]
    private static partial System.Text.RegularExpressions.Regex IdentList();

    public static void Reset(string connectionString, IEnumerable<string> tables, string seedFile)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        foreach (var t in tables)
        {
            using var cmd = new NpgsqlCommand($"TRUNCATE TABLE {Ident(t)} CASCADE", conn);
            cmd.ExecuteNonQuery();
        }
        if (File.Exists(seedFile))
        {
            using var seed = new NpgsqlCommand(File.ReadAllText(seedFile), conn);
            seed.ExecuteNonQuery();
        }
    }

    public static void Snapshot(string connectionString, IEnumerable<(string Table, string Order)> tables, string outDir)
    {
        Directory.CreateDirectory(outDir);
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        foreach (var (t, order) in tables)
        {
            using var reader = conn.BeginTextExport($"COPY (SELECT * FROM {Ident(t)} ORDER BY {Ident(order)}) TO STDOUT WITH CSV HEADER");
            File.WriteAllText(Path.Combine(outDir, t + ".csv"), reader.ReadToEnd());
        }
    }
}
