# MNTTRDYN - .NET plan

Ad-hoc dynamic SQL (PREPARE/EXECUTE) followed by a dynamic CALL of COBTUPDT in the same unit of work.

## Steps
- STEP1 IKJEFT01: DSN RUN PROGRAM(CBTRNDYN) PLAN(CARDDEMO) PARMS('COBTUPDT')

## Inputs
- DYNSQL (FB 80: one ad-hoc SQL statement)
- INPFILE (FB 53)
- CARDDEMO.TRANSACTION_TYPE

## Outputs
- Dynamic statement applied, then COBTUPDT's changes, committed together
- RC 8 when EXECUTE fails (lost after COBTUPDT, GAP-007)

## Target components
- Program: `CardDemo.Batch.Programs.Cbtrndyn` (`src/CardDemo.Batch/Programs/Db2Programs.cs`)
- Job definition: `JobCatalog.Jobs["MNTTRDYN"]`
- Data: `KeyedStore`, `SequentialFile`, generated `Layouts`

## Business rules
| Rule | Source | Statement |
|---|---|---|
| BR-DYN-001 | CBTRNDYN.cbl:73 | DYNSQL lines starting with '--' are skipped; columns 1-72 of the other lines are concatenated, each followed by one blank, into an 800-byte statement. |
| BR-DYN-002 | CBTRNDYN.cbl:96 | The statement is PREPAREd and EXECUTEd; the EXECUTE SQLCODE is displayed (statement errors surface at EXECUTE under ocesql). |
| BR-DYN-003 | CBTRNDYN.cbl:93 | A failed PREPARE or EXECUTE sets RETURN-CODE 8 and processing continues. |
| BR-DYN-004 | CBTRNDYN.cbl:62 | The program named in PARMS is CALLed dynamically (CALL identifier); COBTUPDT's STOP RUN ends the run unit, so 'CBTRNDYN: END' is not reached. |

## Proof
- Golden fixtures via `batch-dotnet/tools/parity_dotnet.sh`
