using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CardDemo.Batch.Restart;
using CardDemo.Domain.Attributes;

namespace CardDemo.Orchestration
{
    [Trace("Mmp.Runtime.Orchestration", Ir = "workflow:BatchJobCoordinator")]
    public class BatchJobCoordinator
    {
        private readonly IBatchCheckpointStore _store;

        public BatchJobCoordinator(IBatchCheckpointStore store)
        {
            _store = store;
        }

        public async Task ExecutePipelineAsync(
            string jobId,
            string? forceRestartFromStep,
            IEnumerable<(string StepName, Func<CancellationToken, Task> Action)> pipelineSteps,
            CancellationToken ct = default)
        {
            bool canExecute = string.IsNullOrEmpty(forceRestartFromStep);

            foreach (var (name, action) in pipelineSteps)
            {
                if (!canExecute)
                {
                    if (string.Equals(name, forceRestartFromStep, StringComparison.OrdinalIgnoreCase))
                    {
                        canExecute = true; // Match found: start here
                    }
                    else
                    {
                        continue; // Skip prior steps
                    }
                }

                var cp = await _store.GetCheckpointAsync(jobId, name, ct);
                if (cp?.Status == StepStatus.Completed && string.IsNullOrEmpty(forceRestartFromStep))
                {
                    continue; // Step already completed
                }

                await action(ct);
            }
        }
    }
}
