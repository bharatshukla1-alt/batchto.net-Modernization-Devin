# MNTTRDB2 - .NET plan

Weekly maintenance of the transaction-type reference table from an A/U/D file.

## Steps
- STEP1 IKJEFT01: DSN RUN PROGRAM(COBTUPDT) PLAN(CARDDEMO)

## Inputs
- INPFILE (FB 53: type, code, description)
- CARDDEMO.TRANSACTION_TYPE (Db2 -> PostgreSQL)

## Outputs
- CARDDEMO.TRANSACTION_TYPE inserted/updated/deleted
- SYSTSPRT messages
- RC 4 on any SQL error

## Target components
- Program: `CardDemo.Batch.Programs.Cobtupdt` (`src/CardDemo.Batch/Programs/Db2Programs.cs`)
- Job definition: `JobCatalog.Jobs["MNTTRDB2"]`
- Data: `KeyedStore`, `SequentialFile`, generated `Layouts`

## Business rules
| Rule | Source | Statement |
|---|---|---|
| BR-TUP-001 | COBTUPDT.cbl:85 | OPEN FILE OK / NOT OK is displayed and the read loop runs either way (0001-OPEN-FILES falls through). |
| BR-TUP-002 | COBTUPDT.cbl:110 | Record type A inserts, U updates, D deletes, '*' is a comment; any other type is an error. |
| BR-TUP-003 | COBTUPDT.cbl:146 | INSERT/UPDATE bind the 2-character type code and the full 50-character description (trailing spaces stored). |
| BR-TUP-004 | COBTUPDT.cbl:180 | SQLCODE +100 on UPDATE/DELETE would report 'No records found.', but Open COBOL ESQL returns 0 when no row matches, so a missing row reports success (fixture t05). |
| BR-TUP-005 | COBTUPDT.cbl:232 | An error DISPLAYs WS-RETURN-MSG (never cleared), sets RETURN-CODE 4 and processing continues with the next record. |
| BR-TUP-006 | COBTUPDT.cbl:157 | Negative SQLCODE message: 'Error accessing: TRANSACTION_TYPE table. SQLCODE:' followed by SQLCODE edited ----9. |

## Proof
- Golden fixtures via `batch-dotnet/tools/parity_dotnet.sh`
