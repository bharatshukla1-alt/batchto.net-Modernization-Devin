using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CardDemo.Domain.Attributes;

namespace CardDemo.Batch.Restart
{
    [Trace("Mmp.Runtime.Batch.Restart", Ir = "runner:ChunkStepRunner")]
    public class ChunkStepRunner<TRecord>
    {
        private readonly IBatchCheckpointStore _store;
        private readonly int _commitInterval;

        public ChunkStepRunner(IBatchCheckpointStore store, int commitInterval = 1000)
        {
            _store = store;
            _commitInterval = commitInterval;
        }

        public async Task RunStepAsync(
            string jobId,
            string stepName,
            IAsyncEnumerable<TRecord> inputStream,
            Func<TRecord, string> keySelector,
            Func<List<TRecord>, CancellationToken, Task> processAndWriteChunkAsync,
            CancellationToken ct = default)
        {
            var checkpoint = await _store.GetCheckpointAsync(jobId, stepName, ct);

            if (checkpoint?.Status == StepStatus.Completed)
            {
                return; // Already completed
            }

            string? resumeKey = checkpoint?.LastProcessedKey;
            long processedCount = checkpoint?.ProcessedCount ?? 0;
            bool skipping = !string.IsNullOrEmpty(resumeKey);

            var chunkBuffer = new List<TRecord>(_commitInterval);

            try
            {
                await foreach (var item in inputStream.WithCancellation(ct))
                {
                    string currentKey = keySelector(item);

                    if (skipping)
                    {
                        if (string.Equals(currentKey, resumeKey, StringComparison.Ordinal))
                        {
                            skipping = false; // Checkpoint reached
                        }
                        continue;
                    }

                    chunkBuffer.Add(item);

                    if (chunkBuffer.Count >= _commitInterval)
                    {
                        await processAndWriteChunkAsync(chunkBuffer, ct);
                        processedCount += chunkBuffer.Count;

                        await _store.SaveCheckpointAsync(new BatchCheckpoint(
                            JobId: jobId,
                            StepName: stepName,
                            Status: StepStatus.Running,
                            LastProcessedKey: currentKey,
                            ProcessedCount: processedCount,
                            LastCheckpointUtc: DateTime.UtcNow
                        ), ct);

                        chunkBuffer.Clear();
                    }
                }

                if (chunkBuffer.Count > 0)
                {
                    string lastKey = keySelector(chunkBuffer[^1]);
                    await processAndWriteChunkAsync(chunkBuffer, ct);
                    processedCount += chunkBuffer.Count;

                    await _store.SaveCheckpointAsync(new BatchCheckpoint(
                        JobId: jobId,
                        StepName: stepName,
                        Status: StepStatus.Running,
                        LastProcessedKey: lastKey,
                        ProcessedCount: processedCount,
                        LastCheckpointUtc: DateTime.UtcNow
                    ), ct);

                    chunkBuffer.Clear();
                }

                await _store.MarkStepCompletedAsync(jobId, stepName, processedCount, ct);
            }
            catch (Exception ex)
            {
                string? failedKey = chunkBuffer.Count > 0 ? keySelector(chunkBuffer[0]) : resumeKey;
                await _store.SaveCheckpointAsync(new BatchCheckpoint(
                    JobId: jobId,
                    StepName: stepName,
                    Status: StepStatus.Failed,
                    LastProcessedKey: failedKey,
                    ProcessedCount: processedCount,
                    LastCheckpointUtc: DateTime.UtcNow,
                    ErrorMessage: ex.Message
                ), ct);
                throw;
            }
        }
    }
}
