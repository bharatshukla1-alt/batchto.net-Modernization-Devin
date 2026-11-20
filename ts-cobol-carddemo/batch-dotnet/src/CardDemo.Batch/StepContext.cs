using System.Globalization;
using CardDemo.Batch.Restart;
using CardDemo.Data;

namespace CardDemo.Batch;

public interface IBatchProgram
{
    /// <summary>Runs the program; the return value is the step return code.</summary>
    int Execute(StepContext ctx);
}

/// <summary>CEE3ABD equivalent: the step ends abnormally and the job stops.</summary>
public sealed class AbendException(int code, string message) : Exception(message)
{
    public int Code { get; } = code;
}

public sealed class StepContext(string job, string step, DatasetCatalog catalog, IReadOnlyDictionary<string, string> dd, DateTime clock)
{
    public string Job { get; } = job;
    public string Step { get; } = step;
    public string? Parm { get; init; }
    public DatasetCatalog Catalog { get; } = catalog;
    public IReadOnlyDictionary<string, string> Dd { get; } = dd;
    public DateTime Clock { get; } = clock;
    public ICheckpointStore? Checkpoints { get; init; }
    public int ChunkSize { get; init; } = 1000;
    public List<string> Sysout { get; } = [];
    public int RecordsRead { get; set; }
    public int RecordsWritten { get; set; }

    public string Dsn(string ddName) => Dd.TryGetValue(ddName, out var dsn) ? dsn : throw new AbendException(999, $"DD {ddName} missing in {Job}.{Step}");

    public void Display(string line) => Sysout.Add(line);

    /// <summary>Z-GET-DB2-FORMAT-TIMESTAMP: YYYY-MM-DD-HH.MM.SS.hh0000 from FUNCTION CURRENT-DATE.</summary>
    public string Db2Timestamp => Clock.ToString("yyyy-MM-dd-HH.mm.ss.ff", CultureInfo.InvariantCulture) + "0000";
}
