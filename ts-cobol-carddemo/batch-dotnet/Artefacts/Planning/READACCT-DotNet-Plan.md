# READACCT - .NET plan

Extract every account to three sequential formats and list it on SYSOUT.

## Steps
- PREDEL (IEFBR14)
- STEP05 CBACT01C

## Inputs
- ACCTFILE: ACCTDATA.VSAM.KSDS (CVACT01Y, 300)

## Outputs
- OUTFILE: ACCTDATA.PSCOMP (107, COMP-3 cycle debit)
- ARRYFILE: ACCTDATA.ARRYPS (110, OCCURS 5)
- VBRCFILE: ACCTDATA.VBPS (VB 12/39 + RDW)

## Target components
- Program: `CardDemo.Batch.Programs.Cbact01c` (`src/CardDemo.Batch/Programs/ReadPrograms.cs`)
- Job definition: `JobCatalog.Jobs["READACCT"]`
- Data: `KeyedStore`, `SequentialFile`, generated `Layouts`

## Business rules
| Rule | Source | Statement |
|---|---|---|
| BR-RAC-001 | CBACT01C.cbl:201 | Each account is displayed field by field on SYSOUT. |
| BR-RAC-002 | CBACT01C.cbl:237 | OUTFILE carries the account with CURR-CYC-DEBIT packed (COMP-3); 2525.00 is moved only when the debit is zero. |
| BR-RAC-003 | CBACT01C.cbl:231 | The reissue date is reformatted by the assembler routine COBDATFT (type 2 in, type 2 out: YYYY-MM-DD -> YYYYMMDD). |
| BR-RAC-004 | CBACT01C.cbl:259 | ARRYFILE: occurrence 1 and 2 copy the balance with debits 1005.00 / 1525.00; occurrence 3 is -1025.00 / -2500.00. |
| BR-RAC-005 | CBACT01C.cbl:303 | VBRCFILE receives a 12-byte and a 39-byte variable record per account. |

## Proof
- No legacy golden run on the harness (GAP-001 for READACCT); unit tests + sample run
