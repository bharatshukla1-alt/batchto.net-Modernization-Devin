# Reconciliation Report - TRANREPT

Five-level reconciliation of the .NET 8 target against the legacy GnuCOBOL golden files (`fixtures/posting/*/expected`).

| Case | L1 counts | L2 keys | L3 fields | L4 control totals | L5 RC + SYSOUT | Result |
|---|---|---|---|---|---|---|
| posting/p01-baseline-mixed | 1/1 | 0/0 | 1/1 | 0/0 | 2/2 | PASS |
| posting/p02-invalid-card | 1/1 | 0/0 | 1/1 | 0/0 | 2/2 | PASS |
| posting/p03-account-missing | 1/1 | 0/0 | 1/1 | 0/0 | 2/2 | PASS |
| posting/p04-overlimit-reject | 1/1 | 0/0 | 1/1 | 0/0 | 2/2 | PASS |
| posting/p05-expired-account | 1/1 | 0/0 | 1/1 | 0/0 | 2/2 | PASS |
| posting/p06-credits-new-category | 1/1 | 0/0 | 1/1 | 0/0 | 2/2 | PASS |
| posting/p07-empty-day | 1/1 | 0/0 | 1/1 | 0/0 | 2/2 | PASS |

Verdict: **PASS** (7 cases)
