# Batch-to-.NET Modernization Specification: CardDemo INTCALC

## 1. Workload Overview
- **Job Name**: `INTCALC`
- **Mainframe Sources**:
  - JCL: `jcl/INTCALC.jcl`
  - Program: `cbl/CBACT04C.cbl`
  - Copybooks: `CVACT01Y.cpy` (Account), `CVTRA01Y.cpy` (Trans Cat Balance), `CVTRA02Y.cpy` (Disclosure Group), `CVACT03Y.cpy` (Card Xref), `CVTRA05Y.cpy` (Transaction)
- **Target .NET Project**: `CardDemo.Batch.IntCalc`

## 2. Dataset & Storage Lowering Plan

| Legacy DD / DSN | Construct | Direction | Modernized .NET Target | Key / Schema |
|---|---|---|---|---|
| `TCATBALF` (`AWS.M2.CARDDEMO.TCATBALF.VSAM.KSDS`) | VSAM KSDS / PS EBCDIC | Input (Stream) | `IAsyncEnumerable<TransCatBalanceRecord>` | `(TRANCAT_ACCT_ID, TRANCAT_TYPE_CD, TRANCAT_CD)` |
| `ACCTFILE` (`AWS.M2.CARDDEMO.ACCTDATA.VSAM.KSDS`) | VSAM KSDS | In-Out (Update) | `IKeyedStore<long, AccountRecord>` (SQL Table / SQLite) | `ACCT_ID` |
| `DISCGRP` (`AWS.M2.CARDDEMO.DISCGRP.VSAM.KSDS`) | VSAM KSDS | Input (Lookup) | `IKeyedStore<DisclosureGroupKey, DisclosureGroupRecord>` | `(GROUP_ID, TYPE_CD, CAT_CD)` |
| `XREFFIL1` (`AWS.M2.CARDDEMO.CARDXREF.VSAM.AIX.PATH`) | VSAM AIX | Input (Lookup) | `IKeyedStore<long, CardXrefRecord>` (Alternate Index) | `XREF_ACCT_ID` |
| `TRANSACT` (`AWS.M2.CARDDEMO.SYSTRAN(+1)`) | GDG (+1) / Sequential | Output | `ISequentialWriter<TransactionRecord>` / SQL Insert | `TRAN_ID` |

## 3. Core Business Rules & Formulas

### 3.1 Control Break (BR-CBACT04C-001)
- Records in `TCATBALF` are ordered by `TRANCAT-ACCT-ID`.
- When `TRANCAT-ACCT-ID != WS-LAST-ACCT-NUM`:
  1. Trigger account update for prior account:
     - `ACCT-CURR-BAL = ACCT-CURR-BAL + WS-TOTAL-INT`
     - `ACCT-CURR-CYC-CREDIT = 0`
     - `ACCT-CURR-CYC-DEBIT = 0`
  2. Commit transaction.
  3. Reset `WS-TOTAL-INT = 0`.
  4. Fetch new account attributes for current `TRANCAT-ACCT-ID`.
- At End-of-File (EOF), perform final group flush for the last account processed.

### 3.2 Disclosure Group Rate Lookup with Fallback (BR-CBACT04C-002)
- Primary Key: `(ACCT-GROUP-ID, TRANCAT-TYPE-CD, TRANCAT-CD)`
- If record not found:
  - Fallback Key: `("DEFAULT", TRANCAT-TYPE-CD, TRANCAT-CD)`
- If still not found:
  - Interest Rate = `0.00`

### 3.3 Interest Calculation (BR-CBACT04C-003)
- If `DIS-INT-RATE > 0` and `TRAN-CAT-BAL > 0`:
  $$\text{WS-MONTHLY-INT} = \text{Math.Round}\left(\frac{\text{TRAN-CAT-BAL} \times \text{DIS-INT-RATE}}{1200\text{m}}, 2, \text{MidpointRounding.AwayFromZero}\right)$$
  - Accumulate: `WS-TOTAL-INT += WS-MONTHLY-INT`
  - Emit record to `TRANSACT` output file with `TRAN-AMT = WS-MONTHLY-INT`.

### 3.4 Transaction ID Generation (BR-CBACT04C-004)
- `TRAN-ID` format: `PARM-DATE` (10 chars, e.g. `2022-07-18`) concatenated with 6-digit zero-padded counter (`WS-TRANID-SUFFIX`). Example: `2022-07-18000001`.

## 4. Checkpoint & Restart Specification

- **Control Table**: `BATCH_STEP_CHECKPOINT`
- **Checkpoint Watermark**: `LastProcessedKey = $"{AccountId:D11}_{TypeCd}_{CatCd}"`
- **Commit Interval**: 1,000 records.
- **Restart Semantics**:
  - Read `BATCH_STEP_CHECKPOINT` for `JobId="INTCALC"`, `StepName="STEP15_CBACT04C"`.
  - If status == `Completed`, skip step.
  - If status == `Failed` or `Running`:
    - Fast-forward sequential reader to `LastProcessedKey`.
    - Resume processing from `LastProcessedKey + 1`.
    - Preserve `WS-TRANID-SUFFIX` counter and `WS-TOTAL-INT` state.
