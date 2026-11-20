# CBTRNDYN - business rules (BRE)

Every rule cites `app/app-transaction-type-db2/cbl/CBTRNDYN.cbl`; the line is resolved by `tools/discovery/extract_facts.py`.

| Rule | Statement | Evidence |
|---|---|---|
| BR-DYN-001 | DYNSQL lines starting with '--' are skipped; columns 1-72 of the other lines are concatenated, each followed by one blank, into an 800-byte statement. | `CBTRNDYN.cbl` L73: `IF DYNSQL-REC(1:2) NOT = '--'` |
| BR-DYN-002 | The statement is PREPAREd and EXECUTEd; the EXECUTE SQLCODE is displayed (statement errors surface at EXECUTE under ocesql). | `CBTRNDYN.cbl` L96: `EXECUTE DYNSTMT` |
| BR-DYN-003 | A failed PREPARE or EXECUTE sets RETURN-CODE 8 and processing continues. | `CBTRNDYN.cbl` L93: `MOVE 8 TO RETURN-CODE` |
| BR-DYN-004 | The program named in PARMS is CALLed dynamically (CALL identifier); COBTUPDT's STOP RUN ends the run unit, so 'CBTRNDYN: END' is not reached. | `CBTRNDYN.cbl` L62: `CALL WS-PGM-NAME` |

## Gaps

* **GAP-004** (High - resolved by ADR): Plugin numeric convention prescribes MidpointRounding.AwayFromZero; the legacy source has no ROUNDED and golden fixtures prove truncation (i02-tie-half-cent). Legacy truncation is kept (ADR-003).
* **GAP-006** (High - decision needed): SQLCODEs come from Open COBOL ESQL over PostgreSQL, not Db2: unique violation -403, any other error -400, no row found 0 (+100 never raised). Under PostgreSQL a failed statement aborts the whole unit of work (d03: COBTUPDT's later UPDATE also fails); Db2 would roll back only the failed statement. The port reproduces the golden (PostgreSQL) behaviour; a savepoint-per-statement policy is a migration decision (ADR-007).
* **GAP-007** (Medium): After CALL COBTUPDT the run unit ends at COBTUPDT's STOP RUN with COBTUPDT's RETURN-CODE, so CBTRNDYN's RC 8 from a failed EXECUTE is lost (d03 ends RC 4). Kept as observed (ADR-007).
