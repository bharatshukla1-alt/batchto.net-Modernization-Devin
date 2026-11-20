# CardDemo batch -> .NET 8 (from TCS-Devin-Sandbox/ts-cobol-carddemo)

`batch-dotnet/` is the .NET 8 / C# 12 port of the CardDemo batch estate produced with this workbench's 8 phases and 3 gates.
It was developed in `TCS-Devin-Sandbox/ts-cobol-carddemo` (PRs #19, #23 and #26, since reverted there) and is kept here instead.

| Workload | Legacy | .NET class |
|---|---|---|
| READACCT | CBACT01C (+ HLASM COBDATFT) | `Cbact01c`, `CobDatFt` |
| READCARD | CBACT02C | `Cbact02c` |
| INTCALC | CBACT04C | `Cbact04c` |
| POSTTRAN | CBTRN02C | `Cbtrn02c` (atomic chunk checkpoints) |
| TRANREPT | SORT + CBTRN03C | `SortStep`, `Cbtrn03c` |
| MNTTRDB2 | IKJEFT01 -> COBTUPDT (Db2) | `Dsnrun`, `Cobtupdt` |
| MNTTRDYN | IKJEFT01 -> CBTRNDYN (dynamic SQL + dynamic CALL) | `Dsnrun`, `Cbtrndyn` |

Results at hand-over: 24/24 golden fixtures pass the estate's field-level comparator (KSDS on files or PostgreSQL; Db2 on PostgreSQL),
56 xUnit tests, GATE 1 / Spec Review / GATE 2 / GATE 3 PASS, 47/47 business rules traced. Open decisions: `Artefacts/Discovery/Gaps.md`
(GAP-006: PostgreSQL aborts the unit of work after a failed dynamic statement, Db2 would not).

## Demo video

`demo/carddemo-batch-modernization-full.mp4` (about 5.5 minutes) has three parts: the legacy JCL/COBOL/Db2 runs on the GnuCOBOL estate, the conversion phase by phase, and the same flows on .NET 8, including parity, crash tests and gates. Every screen is captured command output.

## Running

The discovery, parity and reconciliation tools read the legacy sources, fixtures and comparator of the ts-cobol-carddemo estate, so copy
`batch-dotnet/` into the root of a `ts-cobol-carddemo` checkout and run from there:

```sh
cp -r batch-dotnet /path/to/ts-cobol-carddemo/
cd /path/to/ts-cobol-carddemo && make up && make build
batch-dotnet/run_modernization.sh                      # all phases and gates
CARDDEMO_DB2="Host=...;Username=...;Password=...;Database=..." batch-dotnet/tools/parity_dotnet.sh   # adds the Db2 chains
```

See `batch-dotnet/README.md` for the architecture, commands and artefact layout.
