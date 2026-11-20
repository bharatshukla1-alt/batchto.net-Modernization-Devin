# CBTRNDYN - program facts

Source: `app/app-transaction-type-db2/cbl/CBTRNDYN.cbl` (read-only estate).

## Files

| Logical file | DD | Organisation | Access | Key | Alt key | Line |
|---|---|---|---|---|---|---|
| DYNSQL-FILE | DYNSQL | SEQUENTIAL | SEQUENTIAL |  |  | L34 |

## Copybooks


## External calls

* none

## Paragraphs

* `FILE-CONTROL` (L33)
* `1000-READ-STATEMENT` (L67)
* `2000-RUN-DYNAMIC-SQL` (L87)

## I/O statements

| Verb | Target | Line |
|---|---|---|
| OPEN INPUT | DYNSQL-FILE | L68 |
| READ | DYNSQL-FILE | L70 |

DISPLAY statements: 5 (SYSOUT is part of the parity contract).
