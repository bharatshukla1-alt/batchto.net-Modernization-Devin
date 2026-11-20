# CBTRN02C - business rules (BRE)

Every rule cites `app/cbl/CBTRN02C.cbl`; the line is resolved by `tools/discovery/extract_facts.py`.

| Rule | Statement | Evidence |
|---|---|---|
| BR-PST-001 | A daily transaction whose card number is not in XREF is rejected with reason 100 'INVALID CARD NUMBER FOUND'. | `CBTRN02C.cbl` L385: `MOVE 100 TO WS-VALIDATION-FAIL-REASON` |
| BR-PST-002 | A transaction whose account is missing is rejected with reason 101 'ACCOUNT RECORD NOT FOUND'. | `CBTRN02C.cbl` L397: `MOVE 101 TO WS-VALIDATION-FAIL-REASON` |
| BR-PST-003 | Credit limit check uses cycle amounts: limit >= cycle credit - cycle debit + amount, else reason 102 'OVERLIMIT TRANSACTION'. | `CBTRN02C.cbl` L403: `COMPUTE WS-TEMP-BAL = ACCT-CURR-CYC-CREDIT` |
| BR-PST-004 | Expiry check: account expiration date >= origin date (first 10 chars), else reason 103; 103 overwrites 102 when both fail. | `CBTRN02C.cbl` L414: `IF ACCT-EXPIRAION-DATE >= DALYTRAN-ORIG-TS (1:10)` |
| BR-PST-005 | Rejected transactions are written to DALYREJS as the 350-byte record plus an 80-byte trailer (reason 9(4) + description X(76)). | `CBTRN02C.cbl` L448: `MOVE WS-VALIDATION-TRAILER TO VALIDATION-TRAILER` |
| BR-PST-006 | Missing (account, type, category) balance records are created with the transaction amount. | `CBTRN02C.cbl` L478: `MOVE 'Y' TO WS-CREATE-TRANCAT-REC` |
| BR-PST-007 | Existing category balances are incremented by the transaction amount. | `CBTRN02C.cbl` L508: `ADD DALYTRAN-AMT TO TRAN-CAT-BAL` |
| BR-PST-008 | The amount is added to ACCT-CURR-BAL; non-negative amounts to cycle credit, negative amounts to cycle debit. | `CBTRN02C.cbl` L548: `IF DALYTRAN-AMT >= 0` |
| BR-PST-009 | Posted transactions get PROC-TS = current DB2-format timestamp. | `CBTRN02C.cbl` L438: `MOVE  DB2-FORMAT-TS          TO    TRAN-PROC-TS` |
| BR-PST-010 | TRANSACT is opened OUTPUT: the posted-transaction file is replaced by this run's postings. | `CBTRN02C.cbl` L256: `OPEN OUTPUT TRANSACT-FILE` |
| BR-PST-011 | Return code 4 when at least one transaction was rejected. | `CBTRN02C.cbl` L230: `MOVE 4 TO RETURN-CODE` |

## Gaps

* **GAP-004** (High - resolved by ADR): Plugin numeric convention prescribes MidpointRounding.AwayFromZero; the legacy source has no ROUNDED and golden fixtures prove truncation (i02-tie-half-cent). Legacy truncation is kept (ADR-003).
