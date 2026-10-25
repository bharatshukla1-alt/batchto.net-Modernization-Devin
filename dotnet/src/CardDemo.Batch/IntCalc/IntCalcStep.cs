using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CardDemo.Batch.Restart;
using CardDemo.Data.Stores;
using CardDemo.Domain.Attributes;
using CardDemo.Domain.Models;

namespace CardDemo.Batch.IntCalc
{
    [Trace("cbl/CBACT04C.cbl", Ir = "st:INTCALC.STEP15")]
    public class IntCalcStep
    {
        private readonly ChunkStepRunner<TransCatBalanceRecord> _chunkRunner;
        private readonly IKeyedStore<long, AccountRecord> _accountStore;
        private readonly IKeyedStore<string, DisclosureGroupRecord> _disclosureStore;
        private readonly IKeyedStore<long, CardXrefRecord> _xrefStore;
        private readonly List<TransactionRecord> _outputTransactions = new();

        public IReadOnlyList<TransactionRecord> OutputTransactions => _outputTransactions;

        public IntCalcStep(
            ChunkStepRunner<TransCatBalanceRecord> chunkRunner,
            IKeyedStore<long, AccountRecord> accountStore,
            IKeyedStore<string, DisclosureGroupRecord> disclosureStore,
            IKeyedStore<long, CardXrefRecord> xrefStore)
        {
            _chunkRunner = chunkRunner;
            _accountStore = accountStore;
            _disclosureStore = disclosureStore;
            _xrefStore = xrefStore;
        }

        public async Task ExecuteAsync(
            string jobId,
            string parmDate,
            IAsyncEnumerable<TransCatBalanceRecord> tcatStream,
            CancellationToken ct = default)
        {
            long currentAccountId = -1;
            decimal totalAccruedInterest = 0m;
            AccountRecord? currentAccount = null;
            long tranSequence = 0;

            await _chunkRunner.RunStepAsync(
                jobId: jobId,
                stepName: "STEP15_CBACT04C",
                inputStream: tcatStream,
                keySelector: rec => $"{rec.AccountId:D11}_{rec.TypeCd}_{rec.CatCd:D4}",
                processAndWriteChunkAsync: async (chunk, token) =>
                {
                    foreach (var record in chunk)
                    {
                        // Control break: account changed
                        if (record.AccountId != currentAccountId)
                        {
                            if (currentAccountId != -1 && currentAccount != null)
                            {
                                await CommitAccountInterestAsync(currentAccount, totalAccruedInterest, token);
                            }

                            currentAccountId = record.AccountId;
                            totalAccruedInterest = 0m;
                            currentAccount = await _accountStore.GetAsync(currentAccountId, token);
                        }

                        if (currentAccount == null) continue;

                        // Disclosure Group Rate Lookup with Fallback
                        string primaryKey = $"{currentAccount.GroupId}_{record.TypeCd}_{record.CatCd}";
                        var discRecord = await _disclosureStore.GetAsync(primaryKey, token);
                        if (discRecord == null)
                        {
                            string fallbackKey = $"DEFAULT_{record.TypeCd}_{record.CatCd}";
                            discRecord = await _disclosureStore.GetAsync(fallbackKey, token);
                        }

                        decimal rate = discRecord?.InterestRate ?? 0m;

                        // Interest calculation formula
                        if (rate > 0 && record.Balance > 0)
                        {
                            decimal monthlyInterest = Math.Round((record.Balance * rate) / 1200m, 2, MidpointRounding.AwayFromZero);
                            totalAccruedInterest += monthlyInterest;

                            tranSequence++;
                            string tranId = $"{parmDate}{tranSequence:D6}";

                            var xref = await _xrefStore.GetAsync(currentAccountId, token);
                            var tx = new TransactionRecord(
                                TransId: tranId,
                                TypeCd: record.TypeCd,
                                CatCd: record.CatCd,
                                Source: "INTCALC",
                                Description: "MONTHLY INTEREST CHARGE",
                                Amount: monthlyInterest,
                                CardNumber: xref?.CardNumber ?? "UNKNOWN",
                                MerchantId: 0,
                                MerchantName: "SYSTEM GENERATED",
                                MerchantCity: "INTERNAL",
                                MerchantZip: "00000",
                                OrigTs: DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                                ProcTs: DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                            );
                            _outputTransactions.Add(tx);
                        }
                    }
                },
                ct: ct
            );

            // Flush final group at EOF
            if (currentAccountId != -1 && currentAccount != null)
            {
                await CommitAccountInterestAsync(currentAccount, totalAccruedInterest, ct);
            }
        }

        private async ValueTask CommitAccountInterestAsync(AccountRecord account, decimal interest, CancellationToken ct)
        {
            var updated = account with
            {
                CurrentBalance = account.CurrentBalance + interest,
                CurrCycCredit = 0.00m,
                CurrCycDebit = 0.00m
            };
            await _accountStore.UpsertAsync(account.AccountId, updated, ct);
        }
    }
}
