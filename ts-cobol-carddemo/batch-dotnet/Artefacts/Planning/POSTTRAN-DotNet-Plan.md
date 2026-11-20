# POSTTRAN - .NET plan

Validate and post the daily transactions; rejects with reason codes 100-103.

## Steps
- STEP15 CBTRN02C

## Inputs
- DALYTRAN.PS (CVTRA06Y, 350)
- XREFFILE (CVACT03Y)
- ACCTFILE I-O
- TCATBALF I-O

## Outputs
- TRANFILE: TRANSACT.VSAM.KSDS (OPEN OUTPUT)
- DALYREJS(+1) (REJTRAN, 430)
- RC 4 when rejects

## Target components
- Program: `CardDemo.Batch.Programs.Cbtrn02c` (`src/CardDemo.Batch/Programs/Cbtrn02c.cs`)
- Job definition: `JobCatalog.Jobs["POSTTRAN"]`
- Data: `KeyedStore`, `SequentialFile`, generated `Layouts`

## Business rules
| Rule | Source | Statement |
|---|---|---|
| BR-PST-001 | CBTRN02C.cbl:385 | A daily transaction whose card number is not in XREF is rejected with reason 100 'INVALID CARD NUMBER FOUND'. |
| BR-PST-002 | CBTRN02C.cbl:397 | A transaction whose account is missing is rejected with reason 101 'ACCOUNT RECORD NOT FOUND'. |
| BR-PST-003 | CBTRN02C.cbl:403 | Credit limit check uses cycle amounts: limit >= cycle credit - cycle debit + amount, else reason 102 'OVERLIMIT TRANSACTION'. |
| BR-PST-004 | CBTRN02C.cbl:414 | Expiry check: account expiration date >= origin date (first 10 chars), else reason 103; 103 overwrites 102 when both fail. |
| BR-PST-005 | CBTRN02C.cbl:448 | Rejected transactions are written to DALYREJS as the 350-byte record plus an 80-byte trailer (reason 9(4) + description X(76)). |
| BR-PST-006 | CBTRN02C.cbl:478 | Missing (account, type, category) balance records are created with the transaction amount. |
| BR-PST-007 | CBTRN02C.cbl:508 | Existing category balances are incremented by the transaction amount. |
| BR-PST-008 | CBTRN02C.cbl:548 | The amount is added to ACCT-CURR-BAL; non-negative amounts to cycle credit, negative amounts to cycle debit. |
| BR-PST-009 | CBTRN02C.cbl:438 | Posted transactions get PROC-TS = current DB2-format timestamp. |
| BR-PST-010 | CBTRN02C.cbl:256 | TRANSACT is opened OUTPUT: the posted-transaction file is replaced by this run's postings. |
| BR-PST-011 | CBTRN02C.cbl:230 | Return code 4 when at least one transaction was rejected. |

## Proof
- Golden fixtures via `batch-dotnet/tools/parity_dotnet.sh`
