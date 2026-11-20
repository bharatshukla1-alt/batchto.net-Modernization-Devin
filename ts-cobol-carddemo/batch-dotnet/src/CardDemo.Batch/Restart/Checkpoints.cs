using System.Text.Json;
using CardDemo.Data;

namespace CardDemo.Batch.Restart;

/// <summary>Chunk-level restart point: input position plus program state committed with the data.</summary>
public sealed record Checkpoint(string Job, string Step, int Position, Dictionary<string, string> State, DateTime CommittedAt)
{
    public static string Key(string job, string step) => $"{job}.{step}";
}

public interface ICheckpointStore
{
    Checkpoint? Load(string job, string step);

    /// <summary>Makes the open stores' changes and the checkpoint durable as one unit.</summary>
    void Commit(Checkpoint checkpoint);

    /// <summary>Final commit of the step: data becomes durable and the checkpoint is removed in the same unit.</summary>
    void Complete(string job, string step);
}

/// <summary>Checkpoints kept by the dataset catalog, so they commit atomically with the KSDS data (files or PostgreSQL).</summary>
public sealed class CatalogCheckpointStore(DatasetCatalog catalog) : ICheckpointStore
{
    public Checkpoint? Load(string job, string step) =>
        catalog.LoadCheckpoint(Checkpoint.Key(job, step)) is { } json ? JsonSerializer.Deserialize<Checkpoint>(json) : null;

    public void Commit(Checkpoint checkpoint) => catalog.CommitUnit(Checkpoint.Key(checkpoint.Job, checkpoint.Step), JsonSerializer.Serialize(checkpoint));

    public void Complete(string job, string step) => catalog.CommitUnit(Checkpoint.Key(job, step), null);
}
