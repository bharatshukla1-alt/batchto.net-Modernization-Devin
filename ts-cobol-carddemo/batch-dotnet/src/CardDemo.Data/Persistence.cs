using System.Globalization;
using CardDemo.Domain;
using Npgsql;

namespace CardDemo.Data;

public interface IKsdsPersistence
{
    IEnumerable<byte[]> Load(string dsn, RecordLayout layout);
    void Save(string dsn, RecordLayout layout, IEnumerable<byte[]> records);
}

/// <summary>Fixed-length records in the repository's unloaded-dataset format: one record per line, trailing spaces stripped.</summary>
public sealed class FilePersistence(string datasetDir) : IKsdsPersistence
{
    public string PathOf(string dsn) => Path.Combine(datasetDir, dsn + ".txt");

    public IEnumerable<byte[]> Load(string dsn, RecordLayout layout) => SequentialFile.Read(PathOf(dsn), layout.Length);

    public void Save(string dsn, RecordLayout layout, IEnumerable<byte[]> records) => SequentialFile.Write(PathOf(dsn), records, append: false);
}

/// <summary>KSDS rows in PostgreSQL tables generated from the copybooks (db/migrations/001_create_vsam_tables.sql).</summary>
public sealed class PostgresPersistence(string connectionString, IReadOnlyDictionary<string, string> tables) : IKsdsPersistence
{
    public IEnumerable<byte[]> Load(string dsn, RecordLayout layout)
    {
        var fields = layout.Fields.Where(f => !f.IsFiller).ToList();
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        using var cmd = new NpgsqlCommand($"SELECT {string.Join(", ", fields.Select(Column))} FROM {tables[dsn]}", conn);
        using var reader = cmd.ExecuteReader();
        var rows = new List<byte[]>();
        while (reader.Read())
        {
            var rec = new FixedRecord(layout);
            for (var i = 0; i < fields.Count; i++)
            {
                if (fields[i].Kind == FieldKind.Text)
                {
                    rec.Set(fields[i].Name, reader.GetString(i));
                }
                else
                {
                    rec.Set(fields[i].Name, reader.GetDecimal(i));
                }
            }
            rows.Add(rec.Bytes);
        }
        return rows;
    }

    public void Save(string dsn, RecordLayout layout, IEnumerable<byte[]> records) => SaveAll([(dsn, layout, records)], null, null);

    private const string CheckpointDdl = "CREATE TABLE IF NOT EXISTS batch_checkpoint (checkpoint_key VARCHAR(64) PRIMARY KEY, payload TEXT NOT NULL)";

    /// <summary>
    /// One unit of work: every store and the restart checkpoint commit in a single transaction, so a
    /// crash leaves either the previous chunk or this one. A null checkpointJson deletes the checkpoint.
    /// </summary>
    public void SaveAll(IReadOnlyList<(string Dsn, RecordLayout Layout, IEnumerable<byte[]> Records)> stores, string? checkpointKey, string? checkpointJson, Action<string>? hook = null)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        using var tx = conn.BeginTransaction();
        foreach (var (dsn, layout, records) in stores)
        {
            Write(conn, tx, dsn, layout, records);
        }
        if (checkpointKey is not null)
        {
            Exec(conn, tx, CheckpointDdl);
            Exec(conn, tx, "DELETE FROM batch_checkpoint WHERE checkpoint_key = $1", checkpointKey);
            if (checkpointJson is not null)
            {
                Exec(conn, tx, "INSERT INTO batch_checkpoint (checkpoint_key, payload) VALUES ($1, $2)", checkpointKey, checkpointJson);
            }
        }
        hook?.Invoke("pg-before-commit");
        tx.Commit();
    }

    public string? LoadCheckpoint(string checkpointKey)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        Exec(conn, null, CheckpointDdl);
        using var cmd = new NpgsqlCommand("SELECT payload FROM batch_checkpoint WHERE checkpoint_key = $1", conn);
        cmd.Parameters.Add(new NpgsqlParameter { Value = checkpointKey });
        return cmd.ExecuteScalar() as string;
    }

    /// <summary>Deletes the checkpoints of the given jobs only; other chains' restart points survive.</summary>
    public void ClearCheckpoints(IEnumerable<string> jobs)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        Exec(conn, null, CheckpointDdl);
        foreach (var job in jobs)
        {
            Exec(conn, null, "DELETE FROM batch_checkpoint WHERE split_part(checkpoint_key, '.', 1) = $1", job);
        }
    }

    private static void Exec(NpgsqlConnection conn, NpgsqlTransaction? tx, string sql, params object[] args)
    {
        using var cmd = new NpgsqlCommand(sql, conn, tx);
        foreach (var a in args)
        {
            cmd.Parameters.Add(new NpgsqlParameter { Value = a });
        }
        cmd.ExecuteNonQuery();
    }

    private void Write(NpgsqlConnection conn, NpgsqlTransaction tx, string dsn, RecordLayout layout, IEnumerable<byte[]> records)
    {
        var fields = layout.Fields.Where(f => !f.IsFiller).ToList();
        using (var del = new NpgsqlCommand($"DELETE FROM {tables[dsn]}", conn, tx))
        {
            del.ExecuteNonQuery();
        }
        var sql = $"INSERT INTO {tables[dsn]} ({string.Join(", ", fields.Select(Column))}) VALUES ({string.Join(", ", fields.Select((_, i) => "$" + (i + 1).ToString(CultureInfo.InvariantCulture)))})";
        foreach (var bytes in records)
        {
            var rec = new FixedRecord(layout, bytes);
            using var ins = new NpgsqlCommand(sql, conn, tx);
            foreach (var f in fields)
            {
                ins.Parameters.Add(new NpgsqlParameter { Value = f.Kind == FieldKind.Text ? rec.Text(f.Name) : rec.Number(f.Name) });
            }
            ins.ExecuteNonQuery();
        }
    }

    private static string Column(FieldSpec f) => f.Name.ToLowerInvariant().Replace('-', '_');
}
