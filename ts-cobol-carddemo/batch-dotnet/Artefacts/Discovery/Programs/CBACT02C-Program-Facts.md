# CBACT02C - program facts

Source: `app/cbl/CBACT02C.cbl` (read-only estate).

## Files

| Logical file | DD | Organisation | Access | Key | Alt key | Line |
|---|---|---|---|---|---|---|
| CARDFILE-FILE | CARDFILE | INDEXED | SEQUENTIAL | FD-CARD-NUM |  | L29 |

## Copybooks

* `CVACT02Y` (L45)

## External calls

* `CEE3ABD` (L158)

## Paragraphs

* `FILE-CONTROL` (L28)
* `1000-CARDFILE-GET-NEXT` (L92)
* `0000-CARDFILE-OPEN` (L118)
* `9000-CARDFILE-CLOSE` (L136)
* `9999-ABEND-PROGRAM` (L154)
* `9910-DISPLAY-IO-STATUS` (L161)

## I/O statements

| Verb | Target | Line |
|---|---|---|
| START | OF | L71 |
| READ | CARDFILE-FILE | L93 |
| OPEN INPUT | CARDFILE-FILE | L120 |

DISPLAY statements: 13 (SYSOUT is part of the parity contract).
