# READCARD - .NET plan

List the card master in key order.

## Steps
- STEP05 CBACT02C

## Inputs
- CARDFILE: CARDDATA.VSAM.KSDS (CVACT02Y, 150)

## Outputs
- SYSOUT listing

## Target components
- Program: `CardDemo.Batch.Programs.Cbact02c` (`src/CardDemo.Batch/Programs/ReadPrograms.cs`)
- Job definition: `JobCatalog.Jobs["READCARD"]`
- Data: `KeyedStore`, `SequentialFile`, generated `Layouts`

## Business rules
| Rule | Source | Statement |
|---|---|---|
| BR-RCD-001 | CBACT02C.cbl:78 | Every card record is read sequentially by card number and displayed on SYSOUT. |

## Proof
- No legacy golden run on the harness (GAP-001 for READACCT); unit tests + sample run
