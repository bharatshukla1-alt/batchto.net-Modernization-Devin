# INTCALC - .NET plan

Monthly interest per transaction-category balance; interest transactions and account balance update.

## Steps
- STEP15 CBACT04C PARM=2022071800

## Inputs
- TCATBALF (CVTRA01Y)
- XREFFIL1: CARDXREF AIX path (CVACT03Y)
- ACCTFILE I-O (CVACT01Y)
- DISCGRP (CVTRA02Y)

## Outputs
- TRANSACT: SYSTRAN(+1) (CVTRA05Y, 350)
- ACCTFILE rewritten

## Target components
- Program: `CardDemo.Batch.Programs.Cbact04c` (`src/CardDemo.Batch/Programs/Cbact04c.cs`)
- Job definition: `JobCatalog.Jobs["INTCALC"]`
- Data: `KeyedStore`, `SequentialFile`, generated `Layouts`

## Business rules
| Rule | Source | Statement |
|---|---|---|
| BR-INT-001 | CBACT04C.cbl:465 | Interest is computed per TCATBALF record as (TRAN-CAT-BAL * DIS-INT-RATE) / 1200 with no ROUNDED phrase: the result is truncated to 2 decimals. |
| BR-INT-002 | CBACT04C.cbl:214 | Interest is only computed when the disclosure-group rate is non-zero. |
| BR-INT-003 | CBACT04C.cbl:437 | When the account's disclosure group is missing (status 23) the DEFAULT group is read. |
| BR-INT-004 | CBACT04C.cbl:352 | The accumulated interest is added to ACCT-CURR-BAL when the account changes. |
| BR-INT-005 | CBACT04C.cbl:353 | Cycle credit and cycle debit are reset to zero on account update. |
| BR-INT-006 | CBACT04C.cbl:195 | The account update is triggered only by an account break (first record suppressed); the last account at end-of-file is never updated (legacy quirk). |
| BR-INT-007 | CBACT04C.cbl:476 | Interest transaction id = PARM date (10) + 6-digit running suffix. |
| BR-INT-008 | CBACT04C.cbl:484 | Interest transactions carry type 01, category 05, source 'System'. |
| BR-INT-009 | CBACT04C.cbl:485 | Description is 'Int. for a/c ' followed by the account id. |
| BR-INT-010 | CBACT04C.cbl:497 | Origin and processing timestamps are the DB2-format current timestamp (frozen clock in the harness). |
| BR-INT-011 | CBACT04C.cbl:38 | The card number on the interest transaction comes from the XREF alternate index keyed by account id. |
| BR-INT-012 | CBACT04C.cbl:518 | 1400-COMPUTE-FEES is a placeholder with no logic (fees not implemented). |

## Proof
- Golden fixtures via `batch-dotnet/tools/parity_dotnet.sh`
