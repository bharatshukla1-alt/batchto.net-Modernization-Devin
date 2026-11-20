# COBTUPDT - business rules (BRE)

Every rule cites `app/app-transaction-type-db2/cbl/COBTUPDT.cbl`; the line is resolved by `tools/discovery/extract_facts.py`.

| Rule | Statement | Evidence |
|---|---|---|
| BR-TUP-001 | OPEN FILE OK / NOT OK is displayed and the read loop runs either way (0001-OPEN-FILES falls through). | `COBTUPDT.cbl` L85: `DISPLAY 'OPEN FILE OK'` |
| BR-TUP-002 | Record type A inserts, U updates, D deletes, '*' is a comment; any other type is an error. | `COBTUPDT.cbl` L110: `EVALUATE INPUT-REC-TYPE` |
| BR-TUP-003 | INSERT/UPDATE bind the 2-character type code and the full 50-character description (trailing spaces stored). | `COBTUPDT.cbl` L146: `:INPUT-REC-DESC` |
| BR-TUP-004 | SQLCODE +100 on UPDATE/DELETE would report 'No records found.', but Open COBOL ESQL returns 0 when no row matches, so a missing row reports success (fixture t05). | `COBTUPDT.cbl` L180: `WHEN SQLCODE = +100` |
| BR-TUP-005 | An error DISPLAYs WS-RETURN-MSG (never cleared), sets RETURN-CODE 4 and processing continues with the next record. | `COBTUPDT.cbl` L232: `MOVE 4 TO RETURN-CODE` |
| BR-TUP-006 | Negative SQLCODE message: 'Error accessing: TRANSACTION_TYPE table. SQLCODE:' followed by SQLCODE edited ----9. | `COBTUPDT.cbl` L157: `' TRANSACTION_TYPE table. SQLCODE:'` |

## Gaps

* **GAP-004** (High - resolved by ADR): Plugin numeric convention prescribes MidpointRounding.AwayFromZero; the legacy source has no ROUNDED and golden fixtures prove truncation (i02-tie-half-cent). Legacy truncation is kept (ADR-003).
* **GAP-006** (High - decision needed): SQLCODEs come from Open COBOL ESQL over PostgreSQL, not Db2: unique violation -403, any other error -400, no row found 0 (+100 never raised). Under PostgreSQL a failed statement aborts the whole unit of work (d03: COBTUPDT's later UPDATE also fails); Db2 would roll back only the failed statement. The port reproduces the golden (PostgreSQL) behaviour; a savepoint-per-statement policy is a migration decision (ADR-007).
