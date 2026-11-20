# Reconciliation Report - POSTTRAN

Five-level reconciliation of the .NET 8 target against the legacy GnuCOBOL golden files (`fixtures/posting/*/expected`).

| Case | L1 counts | L2 keys | L3 fields | L4 control totals | L5 RC + SYSOUT | Result |
|---|---|---|---|---|---|---|
| posting/p01-baseline-mixed | 4/4 | 4/4 | 4/4 | 12/12 | 2/2 | PASS |
| posting/p02-invalid-card | 4/4 | 4/4 | 4/4 | 16/16 | 2/2 | PASS |
| posting/p03-account-missing | 4/4 | 4/4 | 4/4 | 16/16 | 2/2 | PASS |
| posting/p04-overlimit-reject | 4/4 | 4/4 | 4/4 | 16/16 | 2/2 | PASS |
| posting/p05-expired-account | 4/4 | 4/4 | 4/4 | 16/16 | 2/2 | PASS |
| posting/p06-credits-new-category | 4/4 | 4/4 | 4/4 | 12/12 | 2/2 | PASS |
| posting/p07-empty-day | 4/4 | 4/4 | 4/4 | 9/9 | 2/2 | PASS |

Verdict: **PASS** (7 cases)
