# COBTUPDT - program facts

Source: `app/app-transaction-type-db2/cbl/COBTUPDT.cbl` (read-only estate).

## Files

| Logical file | DD | Organisation | Access | Key | Alt key | Line |
|---|---|---|---|---|---|---|
| TR-RECORD | INPFILE | SEQUENTIAL | SEQUENTIAL |  |  | L31 |

## Copybooks


## External calls

* none

## Paragraphs

* `FILE-CONTROL` (L30)
* `0001-OPEN-FILES` (L82)
* `1001-READ-NEXT-RECORDS` (L91)
* `1002-READ-RECORDS` (L100)
* `1003-TREAT-RECORD` (L109)
* `10031-INSERT-DB` (L132)
* `10032-UPDATE-DB` (L166)
* `10033-DELETE-DB` (L196)
* `9999-ABEND` (L230)
* `2001-CLOSE-STOP` (L234)

## I/O statements

| Verb | Target | Line |
|---|---|---|
| OPEN INPUT | TR-RECORD | L83 |
| READ | TR-RECORD | L101 |
| DELETE | FROM | L202 |

DISPLAY statements: 11 (SYSOUT is part of the parity contract).
