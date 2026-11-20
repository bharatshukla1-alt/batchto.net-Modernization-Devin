# Discovery Review Gate (GATE 1)

Verdict: **PASS - ready for GATE 1 approval**

| Job | Step | Program | Program DDs | Bound in JCL | Copybooks | Rules |
|---|---|---|---|---|---|---|
| READACCT | STEP05 | CBACT01C | 4 | 4 | 2 | 5 |
| READCARD | STEP05 | CBACT02C | 1 | 1 | 1 | 1 |
| INTCALC | STEP15 | CBACT04C | 5 | 5 | 5 | 12 |
| POSTTRAN | STEP15 | CBTRN02C | 6 | 6 | 5 | 11 |
| TRANREPT | STEP10R | CBTRN03C | 6 | 6 | 5 | 8 |
| MNTTRDB2 | STEP1 | COBTUPDT | 1 | 1 | 0 | 6 |
| MNTTRDYN | STEP1 | CBTRNDYN | 1 | 1 | 0 | 4 |

Business rules with line evidence: 47

## Findings

| Severity | Where | Finding |
|---|---|---|
| MEDIUM | CBACT01C | CALL COBDATFT (L231) resolves to HLASM source - see GAP-001 |
| INFO | INTCALC/STEP15 | DD XREFFIL1 is not used by CBACT04C |
| INFO | MNTTRDYN/STEP1 | DD INPFILE is not used by CBTRNDYN |

## Open gaps carried forward

| Gap | Scope | Description | Severity |
|---|---|---|---|
| GAP-001 | CBACT01C | COBDATFT is HLASM (app/asm/COBDATFT.asm) and is not built into the GnuCOBOL loadlib, so READACCT cannot run on the harness; the .NET port follows the assembler source (VALIDIN2 path) and is proven by unit tests, not by golden files. | Medium |
| GAP-002 | CBACT01C | OUT-ACCT-CURR-CYC-DEBIT is only moved when the debit is zero; otherwise the FD buffer keeps the previous record's value. The .NET port reproduces 'carry previous value' (initial value 0). | Low |
| GAP-003 | CBACT04C | XREF alternate index (account id) with duplicates: VSAM returns the first card in AIX order; the port returns the lowest card number for the account. | Low |
| GAP-004 | ALL | Plugin numeric convention prescribes MidpointRounding.AwayFromZero; the legacy source has no ROUNDED and golden fixtures prove truncation (i02-tie-half-cent). Legacy truncation is kept (ADR-003). | High - resolved by ADR |
| GAP-006 | COBTUPDT,CBTRNDYN | SQLCODEs come from Open COBOL ESQL over PostgreSQL, not Db2: unique violation -403, any other error -400, no row found 0 (+100 never raised). Under PostgreSQL a failed statement aborts the whole unit of work (d03: COBTUPDT's later UPDATE also fails); Db2 would roll back only the failed statement. The port reproduces the golden (PostgreSQL) behaviour; a savepoint-per-statement policy is a migration decision (ADR-007). | High - decision needed |
| GAP-007 | CBTRNDYN | After CALL COBTUPDT the run unit ends at COBTUPDT's STOP RUN with COBTUPDT's RETURN-CODE, so CBTRNDYN's RC 8 from a failed EXECUTE is lost (d03 ends RC 4). Kept as observed (ADR-007). | Medium |
| GAP-005 | TRANREPT | SORT is DFSORT on z/OS; ordering of equal keys is not stated (no EQUALS). The harness and port use a stable sort. | Low |
