# CBACT02C - business rules (BRE)

Every rule cites `app/cbl/CBACT02C.cbl`; the line is resolved by `tools/discovery/extract_facts.py`.

| Rule | Statement | Evidence |
|---|---|---|
| BR-RCD-001 | Every card record is read sequentially by card number and displayed on SYSOUT. | `CBACT02C.cbl` L78: `DISPLAY CARD-RECORD` |

## Gaps

* **GAP-004** (High - resolved by ADR): Plugin numeric convention prescribes MidpointRounding.AwayFromZero; the legacy source has no ROUNDED and golden fixtures prove truncation (i02-tie-half-cent). Legacy truncation is kept (ADR-003).
