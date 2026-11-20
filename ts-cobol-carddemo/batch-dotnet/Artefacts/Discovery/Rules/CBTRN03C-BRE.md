# CBTRN03C - business rules (BRE)

Every rule cites `app/cbl/CBTRN03C.cbl`; the line is resolved by `tools/discovery/extract_facts.py`.

| Rule | Statement | Evidence |
|---|---|---|
| BR-RPT-001 | Report period is read from DATEPARM (start date, filler, end date). | `CBTRN03C.cbl` L221: `READ DATE-PARMS-FILE INTO WS-DATEPARM-RECORD` |
| BR-RPT-002 | A transaction outside the period triggers NEXT SENTENCE, which leaves the whole PERFORM loop (no totals written) - legacy quirk. | `CBTRN03C.cbl` L177: `NEXT SENTENCE` |
| BR-RPT-003 | A change of card number writes the account total of the previous card (not before the first). | `CBTRN03C.cbl` L183: `PERFORM 1120-WRITE-ACCOUNT-TOTALS` |
| BR-RPT-004 | Every line counter multiple of the page size (20) writes page totals and new headers. | `CBTRN03C.cbl` L282: `IF FUNCTION MOD(WS-LINE-COUNTER, WS-PAGE-SIZE) = 0` |
| BR-RPT-005 | Page totals roll into the grand total and reset; account totals reset but do not reset the page total. | `CBTRN03C.cbl` L297: `ADD WS-PAGE-TOTAL TO WS-GRAND-TOTAL` |
| BR-RPT-006 | At end-of-file the last TRAN-AMT is added again to page and account totals before page and grand totals are written; the final account total is never written - legacy quirk. | `CBTRN03C.cbl` L198: `DISPLAY 'TRAN-AMT ' TRAN-AMT` |
| BR-RPT-007 | XREF, TRANTYPE and TRANCATG lookups that fail abend the program (U999). | `CBTRN03C.cbl` L487: `DISPLAY 'INVALID CARD NUMBER : '  FD-XREF-CARD-NUM` |
| BR-RPT-008 | The detail line carries id, account, type code/desc, category code/desc, source and the edited amount (-ZZZ,ZZZ,ZZZ.ZZ). | `CBTRN03C.cbl` L370: `MOVE TRAN-AMT TO TRAN-REPORT-AMT` |

## Gaps

* **GAP-004** (High - resolved by ADR): Plugin numeric convention prescribes MidpointRounding.AwayFromZero; the legacy source has no ROUNDED and golden fixtures prove truncation (i02-tie-half-cent). Legacy truncation is kept (ADR-003).
