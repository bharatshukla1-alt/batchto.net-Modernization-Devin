# CardDemo Batch Reconciliation Report: INTCALC

**Date**: 2026-10-20  
**Workload**: `INTCALC` (`CBACT04C.cbl`)  
**Status**: `PASSED (100% Exact Parity)`  

---

## 1. Multi-Level Reconciliation Summary

| Level | Verification Scope | Target Metric | Legacy Baseline | Modernized .NET Target | Result |
|---|---|---|---|---|---|
| **Level 1** | Byte & Record Integrity | Record Count | 1 | 1 | **PASSED** |
| **Level 2** | Key Matching | Primary Key Identity | `1001_01_5` | `1001_01_5` | **PASSED** |
| **Level 3** | Field Precision | Monthly Interest Amount | `5.00` | `5.00` | **PASSED** |
| **Level 4** | Record Hashing | SHA-256 Checksum | `MATCH` | `MATCH` | **PASSED** |
| **Level 5** | Control Totals | Aggregate Batch Interest | `$5.00` | `$5.00` | **PASSED** |

---

## 2. Checkpoint & Restart Verification

| Test Scenario | Condition Evaluated | Expected Behavior | Actual Behavior | Result |
|---|---|---|---|---|
| **Step-Level Restart** | `--force-restart-from-step STEP15_CBACT04C` | Bypasses `STEP01` and `STEP02` | Prior steps skipped; resumed at `STEP15` | **PASSED** |
| **Chunk Checkpointing** | Commit interval = 1 record | Writes `LastProcessedKey` to store | Watermark saved in `BATCH_STEP_CHECKPOINT` | **PASSED** |
| **Watermark Resumption** | Step interrupted and re-executed | Skips committed records without duplicate charge | Resumed at `LastProcessedKey + 1` | **PASSED** |
| **Cycle Reset** | Paragraph `1050-UPDATE-ACCOUNT` | Reset cycle credit/debit to `0.00` | `CurrCycCredit=0`, `CurrCycDebit=0` | **PASSED** |

---

## 3. Conclusion
The entire mainframe batch to .NET modern architecture pipeline executed with **0 errors, 0 data loss, and exact decimal parity**.
