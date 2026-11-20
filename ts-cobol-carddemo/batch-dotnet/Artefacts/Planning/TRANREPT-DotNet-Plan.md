# TRANREPT - .NET plan

Back up, filter/sort and print the daily transaction detail report with page/account/grand totals.

## Steps
- STEP05R.PRC001 REPRO -> TRANSACT.BKUP(+1)
- STEP05R SORT (263,16,ZD,A) INCLUDE 305,10
- STEP10R CBTRN03C

## Inputs
- TRANSACT.DALY(+1)
- CARDXREF
- TRANTYPE
- TRANCATG
- DATEPARM

## Outputs
- TRANREPT(+1) (133-byte report, CVTRA07Y)

## Target components
- Program: `CardDemo.Batch.Programs.Cbtrn03c` (`src/CardDemo.Batch/Programs/Cbtrn03c.cs`)
- Job definition: `JobCatalog.Jobs["TRANREPT"]`
- Data: `KeyedStore`, `SequentialFile`, generated `Layouts`

## Business rules
| Rule | Source | Statement |
|---|---|---|
| BR-RPT-001 | CBTRN03C.cbl:221 | Report period is read from DATEPARM (start date, filler, end date). |
| BR-RPT-002 | CBTRN03C.cbl:177 | A transaction outside the period triggers NEXT SENTENCE, which leaves the whole PERFORM loop (no totals written) - legacy quirk. |
| BR-RPT-003 | CBTRN03C.cbl:183 | A change of card number writes the account total of the previous card (not before the first). |
| BR-RPT-004 | CBTRN03C.cbl:282 | Every line counter multiple of the page size (20) writes page totals and new headers. |
| BR-RPT-005 | CBTRN03C.cbl:297 | Page totals roll into the grand total and reset; account totals reset but do not reset the page total. |
| BR-RPT-006 | CBTRN03C.cbl:198 | At end-of-file the last TRAN-AMT is added again to page and account totals before page and grand totals are written; the final account total is never written - legacy quirk. |
| BR-RPT-007 | CBTRN03C.cbl:487 | XREF, TRANTYPE and TRANCATG lookups that fail abend the program (U999). |
| BR-RPT-008 | CBTRN03C.cbl:370 | The detail line carries id, account, type code/desc, category code/desc, source and the edited amount (-ZZZ,ZZZ,ZZZ.ZZ). |

## Proof
- Golden fixtures via `batch-dotnet/tools/parity_dotnet.sh`
