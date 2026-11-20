# .NET Batch Architecture Plan

| Layer | Project | Responsibility |
|---|---|---|
| Domain | CardDemo.Domain | Generated copybook layouts, `CobolCodec` (zoned/COMP-3/COMP/edit), `FixedRecord`, `[Trace]` |
| Data | CardDemo.Data | `KeyedStore` (VSAM KSDS + AIX), `PostgresPersistence` / `FilePersistence`, `SequentialFile`, `DatasetCatalog` (GDG), `Db2Session` (Db2 SQL on PostgreSQL) |
| Batch | CardDemo.Batch | One `IBatchProgram` per COBOL program, SORT/IDCAMS utilities, `StepContext`, chunk checkpoints |
| Orchestration | CardDemo.Orchestration | `JobCatalog` (JCL transcribed), `JobRunner` (RC/abend/restart/ledger), `ChainRunner` (parity contract) |
| Host | CardDemo.Host | `carddemo-batch run-chain <chain> <case> <out> [--postgres] [--db2] [--restart]` |
| Tests | tests/CardDemo.Tests | Codec unit tests, golden-fixture contract tests, restart test |

## Decisions
- [ADR-001](ADRs/ADR-001.md): Target stack: .NET 8 / C# 12 console host, no framework batch runtime
- [ADR-002](ADRs/ADR-002.md): VSAM KSDS -> `KeyedStore` with PostgreSQL or file persistence
- [ADR-003](ADRs/ADR-003.md): Decimal arithmetic with legacy truncation (deviation from AwayFromZero)
- [ADR-004](ADRs/ADR-004.md): Reproduce legacy quirks and runner semantics
- [ADR-005](ADRs/ADR-005.md): Restartability: step state, chunk checkpoints, batch run ledger
- [ADR-006](ADRs/ADR-006.md): Deterministic clock and GDG catalog
- [ADR-007](ADRs/ADR-007.md): Db2 SQL programs -> Npgsql `Db2Session` with Open COBOL ESQL semantics

## Workloads
- [READACCT](READACCT-DotNet-Plan.md) - CBACT01C
- [READCARD](READCARD-DotNet-Plan.md) - CBACT02C
- [INTCALC](INTCALC-DotNet-Plan.md) - CBACT04C
- [POSTTRAN](POSTTRAN-DotNet-Plan.md) - CBTRN02C
- [TRANREPT](TRANREPT-DotNet-Plan.md) - CBTRN03C
- [MNTTRDB2](MNTTRDB2-DotNet-Plan.md) - COBTUPDT
- [MNTTRDYN](MNTTRDYN-DotNet-Plan.md) - CBTRNDYN
