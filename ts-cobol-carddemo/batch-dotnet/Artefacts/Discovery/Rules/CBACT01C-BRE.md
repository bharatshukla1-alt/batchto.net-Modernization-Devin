# CBACT01C - business rules (BRE)

Every rule cites `app/cbl/CBACT01C.cbl`; the line is resolved by `tools/discovery/extract_facts.py`.

| Rule | Statement | Evidence |
|---|---|---|
| BR-RAC-001 | Each account is displayed field by field on SYSOUT. | `CBACT01C.cbl` L201: `DISPLAY 'ACCT-ID                 :'   ACCT-ID` |
| BR-RAC-002 | OUTFILE carries the account with CURR-CYC-DEBIT packed (COMP-3); 2525.00 is moved only when the debit is zero. | `CBACT01C.cbl` L237: `MOVE 2525.00         TO   OUT-ACCT-CURR-CYC-DEBIT` |
| BR-RAC-003 | The reissue date is reformatted by the assembler routine COBDATFT (type 2 in, type 2 out: YYYY-MM-DD -> YYYYMMDD). | `CBACT01C.cbl` L231: `CALL 'COBDATFT'       USING CODATECN-REC.` |
| BR-RAC-004 | ARRYFILE: occurrence 1 and 2 copy the balance with debits 1005.00 / 1525.00; occurrence 3 is -1025.00 / -2500.00. | `CBACT01C.cbl` L259: `MOVE   -1025.00        TO   ARR-ACCT-CURR-BAL(3).` |
| BR-RAC-005 | VBRCFILE receives a 12-byte and a 39-byte variable record per account. | `CBACT01C.cbl` L303: `MOVE 39 TO WS-RECD-LEN.` |

## Gaps

* **GAP-001** (Medium): COBDATFT is HLASM (app/asm/COBDATFT.asm) and is not built into the GnuCOBOL loadlib, so READACCT cannot run on the harness; the .NET port follows the assembler source (VALIDIN2 path) and is proven by unit tests, not by golden files.
* **GAP-002** (Low): OUT-ACCT-CURR-CYC-DEBIT is only moved when the debit is zero; otherwise the FD buffer keeps the previous record's value. The .NET port reproduces 'carry previous value' (initial value 0).
* **GAP-004** (High - resolved by ADR): Plugin numeric convention prescribes MidpointRounding.AwayFromZero; the legacy source has no ROUNDED and golden fixtures prove truncation (i02-tie-half-cent). Legacy truncation is kept (ADR-003).
