using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using CardDemo.Domain.Attributes;

namespace CardDemo.Batch.Restart
{
    public enum StepStatus
    {
        Pending,
        Running,
        Completed,
        Failed
    }

    [Trace("Mmp.Runtime.Batch.Restart", Ir = "table:BATCH_STEP_CHECKPOINT")]
    public record BatchCheckpoint(
        string JobId,
        string StepName,
        StepStatus Status,
        string? LastProcessedKey,
        long ProcessedCount,
        DateTime LastCheckpointUtc,
        string? ErrorMessage = null
    );

    [Trace("Mmp.Runtime.Batch.Restart", Ir = "service:IBatchCheckpointStore")]
    public interface IBatchCheckpointStore
    {
        ValueTask<BatchCheckpoint?> GetCheckpointAsync(string jobId, string stepName, CancellationToken ct = default);
        ValueTask SaveCheckpointAsync(BatchCheckpoint checkpoint, CancellationToken ct = default);
        ValueTask MarkStepCompletedAsync(string jobId, string stepName, long totalCount, CancellationToken ct = default);
    }

    [Trace("Mmp.Runtime.Batch.Restart", Ir = "service:MemoryBatchCheckpointStore")]
    public class MemoryBatchCheckpointStore : IBatchCheckpointStore
    {
        private readonly ConcurrentDictionary<string, BatchCheckpoint> _checkpoints = new();

        public ValueTask<BatchCheckpoint?> GetCheckpointAsync(string jobId, string stepName, CancellationToken ct = default)
        {
            _checkpoints.TryGetValue($"{jobId}_{stepName}", out var cp);
            return ValueTask.FromResult(cp);
        }

        public ValueTask SaveCheckpointAsync(BatchCheckpoint checkpoint, CancellationToken ct = default)
        {
            _checkpoints[$"{checkpoint.JobId}_{checkpoint.StepName}"] = checkpoint;
            return ValueTask.CompletedTask;
        }

        public ValueTask MarkStepCompletedAsync(string jobId, string stepName, long totalCount, CancellationToken ct = default)
        {
            var cp = new BatchCheckpoint(
                JobId: jobId,
                StepName: stepName,
                Status: StepStatus.Completed,
                LastProcessedKey: null,
                ProcessedCount: totalCount,
                LastCheckpointUtc: DateTime.UtcNow
            );
            return SaveCheckpointAsync(cp, ct);
        }
    }
}
