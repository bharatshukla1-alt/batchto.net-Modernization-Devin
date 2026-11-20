# CBACT04C - business rules (BRE)

Every rule cites `app/cbl/CBACT04C.cbl`; the line is resolved by `tools/discovery/extract_facts.py`.

| Rule | Statement | Evidence |
|---|---|---|
| BR-INT-001 | Interest is computed per TCATBALF record as (TRAN-CAT-BAL * DIS-INT-RATE) / 1200 with no ROUNDED phrase: the result is truncated to 2 decimals. | `CBACT04C.cbl` L465: `= ( TRAN-CAT-BAL * DIS-INT-RATE) / 1200` |
| BR-INT-002 | Interest is only computed when the disclosure-group rate is non-zero. | `CBACT04C.cbl` L214: `IF DIS-INT-RATE NOT = 0` |
| BR-INT-003 | When the account's disclosure group is missing (status 23) the DEFAULT group is read. | `CBACT04C.cbl` L437: `MOVE 'DEFAULT' TO FD-DIS-ACCT-GROUP-ID` |
| BR-INT-004 | The accumulated interest is added to ACCT-CURR-BAL when the account changes. | `CBACT04C.cbl` L352: `ADD WS-TOTAL-INT  TO ACCT-CURR-BAL` |
| BR-INT-005 | Cycle credit and cycle debit are reset to zero on account update. | `CBACT04C.cbl` L353: `MOVE 0 TO ACCT-CURR-CYC-CREDIT` |
| BR-INT-006 | The account update is triggered only by an account break (first record suppressed); the last account at end-of-file is never updated (legacy quirk). | `CBACT04C.cbl` L195: `IF WS-FIRST-TIME NOT = 'Y'` |
| BR-INT-007 | Interest transaction id = PARM date (10) + 6-digit running suffix. | `CBACT04C.cbl` L476: `STRING PARM-DATE,` |
| BR-INT-008 | Interest transactions carry type 01, category 05, source 'System'. | `CBACT04C.cbl` L484: `MOVE 'System'             TO TRAN-SOURCE` |
| BR-INT-009 | Description is 'Int. for a/c ' followed by the account id. | `CBACT04C.cbl` L485: `STRING 'Int. for a/c ' ,` |
| BR-INT-010 | Origin and processing timestamps are the DB2-format current timestamp (frozen clock in the harness). | `CBACT04C.cbl` L497: `MOVE DB2-FORMAT-TS        TO TRAN-ORIG-TS` |
| BR-INT-011 | The card number on the interest transaction comes from the XREF alternate index keyed by account id. | `CBACT04C.cbl` L38: `ALTERNATE RECORD KEY IS FD-XREF-ACCT-ID` |
| BR-INT-012 | 1400-COMPUTE-FEES is a placeholder with no logic (fees not implemented). | `CBACT04C.cbl` L518: `1400-COMPUTE-FEES.` |

## Gaps

* **GAP-003** (Low): XREF alternate index (account id) with duplicates: VSAM returns the first card in AIX order; the port returns the lowest card number for the account.
* **GAP-004** (High - resolved by ADR): Plugin numeric convention prescribes MidpointRounding.AwayFromZero; the legacy source has no ROUNDED and golden fixtures prove truncation (i02-tie-half-cent). Legacy truncation is kept (ADR-003).
