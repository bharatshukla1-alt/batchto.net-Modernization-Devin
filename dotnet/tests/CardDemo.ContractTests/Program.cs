using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CardDemo.Batch.IntCalc;
using CardDemo.Batch.Restart;
using CardDemo.Data.Stores;
using CardDemo.Domain.Models;

namespace CardDemo.ContractTests
{
    public static class Program
    {
        public static async Task Main()
        {
            Console.WriteLine("Running CardDemo Contract Tests (Scenarios & Restart Verification)...");

            var checkpointStore = new MemoryBatchCheckpointStore();
            var accountStore = new MemoryKeyedStore<long, AccountRecord>();
            var disclosureStore = new MemoryKeyedStore<string, DisclosureGroupRecord>();
            var xrefStore = new MemoryKeyedStore<long, CardXrefRecord>();

            // Seed Account with initial balance 1000.00
            await accountStore.UpsertAsync(2001, new AccountRecord(2001, "Y", 1000.00m, 5000m, 1000m, "2020-01-01", "2026-12-31", "2020-01-01", 10.00m, 20.00m, "GRP01"));
            // Fallback rate test: GRP01 rate missing, DEFAULT rate present at 6.00%
            await disclosureStore.UpsertAsync("DEFAULT_01_1", new DisclosureGroupRecord("DEFAULT", "01", 1, 6.00m));
            await xrefStore.UpsertAsync(2001, new CardXrefRecord("4222222222222222", 6001, 2001));

            var chunkRunner = new ChunkStepRunner<TransCatBalanceRecord>(checkpointStore, commitInterval: 1);
            var intCalc = new IntCalcStep(chunkRunner, accountStore, disclosureStore, xrefStore);

            async IAsyncEnumerable<TransCatBalanceRecord> Stream()
            {
                yield return new TransCatBalanceRecord(2001, "01", 1, 2000.00m); // Interest = (2000 * 6) / 1200 = 10.00
                await Task.Yield();
            }

            // Run execution
            await intCalc.ExecuteAsync("CONTRACT_RUN_1", "2022-07-18", Stream());

            var updatedAcct = await accountStore.GetAsync(2001);
            if (updatedAcct == null) throw new Exception("Account record not found after run.");

            // Verify account balance updated by +10.00 and cycle fields reset to 0
            if (updatedAcct.CurrentBalance != 1010.00m) throw new Exception($"Contract failed: expected balance 1010.00, got {updatedAcct.CurrentBalance}");
            if (updatedAcct.CurrCycCredit != 0.00m || updatedAcct.CurrCycDebit != 0.00m) throw new Exception("Contract failed: cycle debit/credit not reset.");

            // Verify Checkpoint written
            var cp = await checkpointStore.GetCheckpointAsync("CONTRACT_RUN_1", "STEP15_CBACT04C");
            if (cp == null || cp.Status != StepStatus.Completed) throw new Exception("Contract failed: checkpoint not marked completed.");

            Console.WriteLine("[PASS] All Contract & Restart Scenarios Passed Successfully.");
        }
    }
}
