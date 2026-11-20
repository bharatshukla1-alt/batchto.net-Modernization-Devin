# Reconciliation Report - INTCALC

Five-level reconciliation of the .NET 8 target against the legacy GnuCOBOL golden files (`fixtures/interest/*/expected`).

| Case | L1 counts | L2 keys | L3 fields | L4 control totals | L5 RC + SYSOUT | Result |
|---|---|---|---|---|---|---|
| interest/i01-baseline | 2/2 | 2/2 | 2/2 | 9/9 | 2/2 | PASS |
| interest/i02-tie-half-cent | 2/2 | 2/2 | 2/2 | 9/9 | 2/2 | PASS |
| interest/i03-zero-rate-skipped | 2/2 | 2/2 | 2/2 | 9/9 | 2/2 | PASS |
| interest/i04-default-group-fallback | 2/2 | 2/2 | 2/2 | 9/9 | 2/2 | PASS |
| interest/i05-multi-category | 2/2 | 2/2 | 2/2 | 9/9 | 2/2 | PASS |
| interest/i06-negative-balance | 2/2 | 2/2 | 2/2 | 9/9 | 2/2 | PASS |
| interest/i07-nontie-rounding | 2/2 | 2/2 | 2/2 | 9/9 | 2/2 | PASS |

Verdict: **PASS** (7 cases)
