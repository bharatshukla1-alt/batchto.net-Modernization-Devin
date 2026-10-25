# Mainframe Batch Modernization Workbench (.NET 8 Batch Target)

You orchestrate an end-to-end modernization pipeline designed strictly for **Mainframe Batch Modernization**. This workbench transforms legacy Mainframe Batch workloads (JCL, COBOL Batch, Copybooks, VSAM datasets, Sequential/GDG datasets, and EBCDIC files) into a high-throughput, cloud-native **.NET 8 / C# 12 Batch Engine** with enterprise checkpoint-restart, deterministic decimal precision, and 5-level reconciliation verification.

- **Workload Domain**: **Pure Mainframe Batch Processing** (`jcl/`, `cbl/`, `cpy/`, `proc/`, `data/`). No CICS, BMS, screens, or frontend UI components.
- **Source Workloads**: Mainframe CardDemo Batch Suite (`INTCALC.jcl`, `READACCT.jcl`, `READCARD.jcl`).
- **Target Architecture**: High-performance .NET 8 Batch Host (`BackgroundService` / `IHostedService`), Quartz.NET batch job scheduler, and `System.Threading.Channels` chunk stream pipelines.
- **Data Architecture**: Relational database storage (SQL Server / PostgreSQL / SQLite) for VSAM KSDS; high-throughput Span-based I/O for Sequential (PS) / Generation Data Groups (GDG).
- **Reliability & Resilience**: Chunk-oriented batch processing with database-backed restart checkpoints (`BATCH_STEP_CHECKPOINT`) and operational execution ledger (`MMP_RUN_LEDGER`).
- **Specification & Design Baseline**: Aligned with `LLD_Target_Agnostic_Modernization_Platform.docx` and tailored specifically for batch workloads.

---

## 1. Repository Architecture & Layout

```
testRepo/
├── AGENTS.md                                # Root orchestration: batch pipeline phases, golden rules, artifact contracts
├── CLAUDE.md                                # Compatibility mirror for AGENTS.md
├── .github/agents/                          # Legacy PySpark agents (Reference baseline)
├── .Claude/
│   ├── Agents/                              # 14 Dedicated Batch Modernization Subagents
│   │   ├── Jcl-Analyzer-Agent.md             # Phase 1: JCL step sequence, DD datasets, and execution conditions
│   │   ├── Program-Analyzer-Agent.md         # Phase 1: Batch COBOL AST, file controls (SELECT/ASSIGN), and copybooks
│   │   ├── Bre-Analyzer-Agent.md             # Phase 1: Batch business rules, interest math formulas, control breaks
│   │   ├── ca7-scheduler-analyzer.agent.md   # Phase 1: Parses CA-7 / OPC 1LJOB blocks into job DAGs & run cycles
│   │   ├── Discovery-Review-Gate-Agent.md    # Phase 2: Audits Discovery facts vs source code before Gate 1
│   │   ├── Target-Planning-Agent.md          # Phase 3: Canonical Batch IR synthesis & .NET Batch ADR formulation
│   │   ├── Schema-Store-Provisioner-Agent.md # Phase 4: Relational VSAM schemas & BATCH_STEP_CHECKPOINT DDL
│   │   ├── Dotnet-Batch-Engineer-Agent.md    # Phase 5: Production .NET 8 / C# 12 batch generation with restart
│   │   ├── quartz-orchestrator-creator.agent.md # Phase 5: Quartz.NET jobs & step sequence coordinator
│   │   ├── Spec-Review-Gate-Agent.md         # Phase 6: Deep-reviews .NET batch code against plan before Gate 2
│   │   ├── Reconciliation-Test-Agent.md      # Phase 7: 5-Level batch reconciliation & xUnit contract tests
│   │   ├── Test-Failure-Remediator-Agent.md  # Phase 7: Automated test failure & batch reconciliation remediator
│   │   ├── Quality-Security-Auditor-Agent.md # Phase 8: Code quality, [Trace] coverage, and zero-allocation span audit
│   │   └── Quality-Security-Remediator-Agent.md # Phase 8: Automated code & security remediator
│   ├── Commands/                            # 8 Executable Batch Pipeline Commands
│   │   ├── 01-Legacy-Discovery.md            # Phase 1: JCL, Batch COBOL, Copybook, CA-7 & BRE extraction
│   │   ├── 02-Discovery-Review-Gate.md       # Phase 2: Audit discovery completeness (GATE 1)
│   │   ├── 03-Target-Planning.md             # Phase 3: Canonical Batch IR, ADRs, and Lowering Plan
│   │   ├── 04-Database-Provisioning.md       # Phase 4: VSAM DDL, Checkpoint, and Ledger tables
│   │   ├── 05-Target-Build.md                # Phase 5: .NET 8 batch solution, codecs, and batch steps
│   │   ├── 06-Target-Review-Gate.md          # Phase 6: Compile & IR implementation review (GATE 2)
│   │   ├── 07-Target-Validation.md           # Phase 7: xUnit tests, contract tests & batch reconciliation
│   │   └── 08-Quality-Security-Audit.md      # Phase 8: Traceability, memory, and security audit (GATE 3)
│   ├── Prompts/
│   │   └── Common/
│   │       ├── Anti-Hallucination.md         # Strict evidence anchoring & no fabrication rules
│   │       └── Target-Neutrality.md          # Target-neutral extraction rules (no target bias)
│   └── Specs/
│       └── INTCALC-Modernization-Spec.md     # Detailed batch specification for INTCALC (CBACT04C)
├── input/                                   # READ-ONLY source legacy mainframe assets
│   ├── jcl/                                 # JCL jobs (INTCALC, READACCT, READCARD)
│   ├── cbl/                                 # COBOL batch programs (CBACT01C, CBACT02C, CBACT04C)
│   ├── cpy/                                 # Copybooks (CVACT01Y, CVTRA01Y, etc.)
│   ├── proc/ & asm/                         # Cataloged batch procedures and assembler routines
│   └── data/                                # EBCDIC batch files & test datasets
├── Artefacts/                               # All analysis, specifications, and reports produced by agents
│   ├── Discovery/                           # Jcl-Facts, Program-Facts, CA-7 schedule, and BRE extractions
│   ├── Review/                              # Review-Gate1.md, ReviewGate2.md audits
│   ├── Planning/                            # Canonical Batch IR (*-IR.json) and Architecture Decision Records (ADRs)
│   ├── Database/                            # Generated DDL, SQL migrations, and schema mappings
│   ├── Reports/                             # Reconciliation reports, parity matrices
│   └── Audits/                              # Code quality, security, and [Trace] coverage reports
├── dotnet/                                  # Modernized .NET 8 Batch Solution and Target Code
│   ├── CardDemo.Modernized.sln
│   ├── Directory.Build.props                # net8.0, Nullable enable, TreatWarningsAsErrors
│   ├── Directory.Packages.props             # Central Package Management (CPM)
│   ├── src/
│   │   ├── CardDemo.Domain/                 # Strongly typed records generated from batch copybooks
│   │   ├── CardDemo.Data/                   # Binary codecs (COMP-3, EBCDIC), layout readers, keyed stores
│   │   ├── CardDemo.Batch/                  # Batch steps, control breaks, business rule engines, restart logic
│   │   ├── CardDemo.Orchestration/          # Multi-step batch coordinator, Quartz.NET jobs, restart logic
│   │   └── CardDemo.Host/                   # Generic Host console entry point, DI, CLI options
│   ├── db/
│   │   └── migrations/                      # SQL Server / PostgreSQL / SQLite table scripts
│   └── tests/
│       ├── CardDemo.UnitTests/              # Batch calculation and rule unit tests
│       ├── CardDemo.ContractTests/          # Contract scenarios (edge cases, overflow, negative balances)
│       └── CardDemo.Reconciliation/         # 5-level legacy batch output vs .NET output comparison runner
└── output/                                  # Modernized batch execution outputs for reconciliation
```

---

## 2. Golden Rules for Batch Modernization

1. **Source Immutability**: `input/` (`input/jcl/`, `input/cbl/`, `input/cpy/`, `input/proc/`, and `input/data/`) is **READ-ONLY**. Never alter, reformat, or write to legacy source folders.
2. **Scope Constraint**: **STRICTLY BATCH WORKLOADS**. No online transaction processing (CICS/IMS TM), 3270 screens, BMS maps, or web frontend code. The target is an automated batch pipeline.
3. **Target Neutrality in Early Phases**: Phases 1, 2, and 3 must remain strictly target-agnostic. No target syntax (`Spark`, `PySpark`, `Java`, `.NET`) in extraction facts or Canonical IR.
4. **No Fabrication (Anti-Hallucination)**: Every extracted business rule, data structure, and step constraint MUST cite direct line-level evidence in the source repository (`cbl/`, `cpy/`, `jcl/`). Missing information must be recorded as a `GAP-XXX` rather than assumed.
5. **Deterministic Decimal Precision**: All financial amounts, interest calculations, and balances MUST use C# `decimal` (96-bit base-10). Never use floating-point types (`float`, `double`).
6. **Zero-Allocation Binary Codecs**: EBCDIC decoding and COMP-3 (Packed Decimal) bitwise unpacking must use `ReadOnlySpan<byte>` and `Span<byte>` to guarantee low GC pressure during batch processing.
7. **Mandatory Restartability**: All high-volume batch steps must implement chunk-level checkpointing (`BATCH_STEP_CHECKPOINT`) and step-level restart capability (`forceRestartFromStep`).
8. **Traceability Guarantee**: Every public C# class, record, and rule method must carry a `[Trace(Source="...", Rule="...", Ir="...")]` attribute. Public members without trace attributes fail quality gates.
9. **Human Review Gates**: STOP at each designated review gate (**GATE 1**, **GATE 2**, **GATE 3**). Do not advance past an unconfirmed gate until explicit user approval is received.

---

## 3. Batch Pipeline Order & Artifact Contract

| Phase | Command | Owning Agent(s) | Primary Outputs / Writes | Review Gate |
|---|---|---|---|---|
| **Phase 1: Legacy Discovery** | `/01-Legacy-Discovery` | `Jcl-Analyzer-Agent`<br>`Program-Analyzer-Agent`<br>`Bre-Analyzer-Agent`<br>`ca7-scheduler-analyzer.agent` | `Artefacts/Discovery/Workloads/<WORKLOAD>-Jcl-Facts.md`<br>`Artefacts/Discovery/Programs/<PROGRAM>-Program-Facts.md`<br>`Artefacts/Discovery/Rules/<PROGRAM>-BRE.md`<br>`Artefacts/Discovery/Workloads/Batch-Schedule-Graph.json` | — |
| **Phase 2: Discovery Review Gate** | `/02-Discovery-Review-Gate` | `Discovery-Review-Gate-Agent` | `Artefacts/Review/Discovery-Review-Gate-Report.md` | **GATE 1: Discovery Approval** |
| **Phase 3: Target Planning** | `/03-Target-Planning` | `Target-Planning-Agent` | `Artefacts/Planning/<WORKLOAD>-IR.json`<br>`Artefacts/Planning/DotNet-Batch-Architecture-Plan.md`<br>`Artefacts/Planning/ADRs/*.md` | — |
| **Phase 4: Database Provisioning** | `/04-Database-Provisioning` | `Schema-Store-Provisioner-Agent` | `dotnet/db/migrations/001_Create_CardDemo_VSAM_Tables.sql`<br>`dotnet/db/migrations/002_Create_Batch_Operational_Tables.sql`<br>`dotnet/src/CardDemo.Data/Stores/*.cs` | — |
| **Phase 5: Target Build** | `/05-Target-Build` | `Dotnet-Batch-Engineer-Agent`<br>`quartz-orchestrator-creator.agent` | `dotnet/src/CardDemo.Domain/*.cs`<br>`dotnet/src/CardDemo.Data/Codecs/*.cs`<br>`dotnet/src/CardDemo.Batch/*.cs`<br>`dotnet/src/CardDemo.Orchestration/*.cs`<br>`dotnet/src/CardDemo.Host/Program.cs` | — |
| **Phase 6: Target Review Gate** | `/06-Target-Review-Gate` | `Spec-Review-Gate-Agent` | `Artefacts/Review/Target-Review-Gate-Report.md` | **GATE 2: Code Review & Build Gate** |
| **Phase 7: Target Validation** | `/07-Target-Validation` | `Reconciliation-Test-Agent`<br>`Test-Failure-Remediator-Agent` | `dotnet/tests/CardDemo.UnitTests/*.cs`<br>`dotnet/tests/CardDemo.ContractTests/*.cs`<br>`Artefacts/Reports/Reconciliation-Report-<WORKLOAD>.md` | — |
| **Phase 8: Quality & Security Audit** | `/08-Quality-Security-Audit` | `Quality-Security-Auditor-Agent`<br>`Quality-Security-Remediator-Agent` | `Artefacts/Audits/Quality-Security-Audit-Report.md`<br>`Artefacts/Audits/Traceability-Matrix.html` | **GATE 3: Final Acceptance Gate** |

---

## 4. Specialized Batch Subagents (14 Subagents)

1. **`Jcl-Analyzer-Agent`**: Extracts batch execution steps, cataloged procedures, condition logic (`COND/IF-THEN-ELSE`), DD statements, and dataset lifecycles.
2. **`Program-Analyzer-Agent`**: Analyzes COBOL batch programs and copybooks to extract data divisions, file control (`SELECT ... ASSIGN`), working-storage, and call graphs.
3. **`Bre-Analyzer-Agent`**: Extracts pure business rules, calculations, decision tables, control breaks, and fallback lookups with line evidence.
4. **`ca7-scheduler-analyzer.agent`**: Parses CA-7 / OPC scheduler report text (`1LJOB` blocks) into ordered job dependencies, predecessor-successor graphs, triggers, and calendar rules.
5. **`Discovery-Review-Gate-Agent`**: Cross-checks Discovery facts against source files to detect missing programs, unresolved DDs, or orphaned copybooks before Gate 1.
6. **`Target-Planning-Agent`**: Synthesizes discovery facts into the Canonical Batch Intermediate Representation (IR) and produces .NET Architecture Decision Records (ADRs).
7. **`Schema-Store-Provisioner-Agent`**: Generates relational DDL, Dapper mappings, and data access layers replacing VSAM KSDS files and batch control tables.
8. **`Dotnet-Batch-Engineer-Agent`**: Generates production-grade .NET 8 / C# 12 batch applications, Domain DTOs, binary codecs, chunk processors, and DI configurations.
9. **`quartz-orchestrator-creator.agent`**: Generates Quartz.NET job classes, sequential pipeline steps, execution dependency graphs, and Quartz scheduling registrations.
10. **`Spec-Review-Gate-Agent`**: Deep-reviews generated .NET batch code against the plan and IR before execution, verifying compiler status and `[Trace]` attributes.
11. **`Reconciliation-Test-Agent`**: Generates automated xUnit contract tests, synthetic test vectors, and multi-level batch reconciliation tests (row count, hash, field-diff, aggregates).
12. **`Test-Failure-Remediator-Agent`**: Analyzes batch test failures or reconciliation discrepancies and applies targeted code fixes in `dotnet/src/`.
13. **`Quality-Security-Auditor-Agent`**: Audits the .NET 8 batch codebase for coding standards, trace attributes, security vulnerabilities, memory efficiency, and compliance.
14. **`Quality-Security-Remediator-Agent`**: Automatically remediates code quality, memory, security, and traceability findings flagged by the auditor.

---

## 5. Mainframe Batch to .NET 8 Construct Mapping Matrix

| Mainframe / JCL Construct | Legacy PySpark Equivalent (`.github/agents`) | **Target .NET 8 Architecture** |
|---|---|---|
| **Sequential (PS) dataset** | `spark.read.csv()` / `df.write.csv()` | `FileStream` + `System.IO.Pipelines` or CsvHelper / FlatFiles |
| **FB/VB/VBS EBCDIC dataset** | Cobrix (`spark.read.format("cobol")`) | Zero-allocation `Mmp.Runtime.Codecs.EbcdicCodec` + `RecordReader` |
| **VSAM KSDS file** | Delta / CSV / Parquet | `IKeyedStore<TKey, TRecord>` backed by SQL Server / PostgreSQL (Dapper) or SQLite |
| **Packed Decimal (`COMP-3`)** | Decimal UDF via Cobrix | `Comp3Codec.ReadDecimal(ReadOnlySpan<byte>, scale)` bitwise unpacker |
| **Zoned Decimal** | Long / Decimal | `ZonedCodec.ReadDecimal(ReadOnlySpan<byte>)` with overpunch sign |
| **Binary (`COMP` / `COMP-4`)** | Integer / Long | `BinaryPrimitives.ReadInt16BigEndian` / `ReadInt32BigEndian` |
| **OCCURS structure** | Array column, `explode()` | C# arrays (`T[]`) or `ReadOnlyMemory<T>` |
| **Multiple record layouts (01)** | Multi-layout DataFrame unions | Discriminated union records or `[StructLayout(LayoutKind.Explicit)]` |
| **SORT / SYNCSORT / DFSORT** | `df.orderBy(...)` | `Mmp.Runtime.Sort.ExternalMergeSort` or LINQ `OrderBy()` |
| **INCLUDE COND= in SORT** | `df.filter(...)` | LINQ `Where(predicate)` / compiled expressions |
| **IDCAMS REPRO** | Read source and write to output | Stream copy / Bulk insert using `IKeyedStore.BulkInsertAsync` |
| **GDG (`(+1)` / `(0)`)** | Partitioned output directory | Partitioned generation path `output/<workload>/G{gen:D4}V00` |
| **DISPLAY in COBOL** | `print()` call | `ILogger.LogInformation(...)` with matching message text |
| **COBSWAIT / WAIT step** | `time.sleep()` | `await Task.Delay(...)` using CancellationToken |
| **CA-7 Schedule Graph** | `prefect_spark_flow.py` | `Quartz.NET` Job + `BatchJobCoordinator.cs` |

---

## 6. Detailed End-to-End Batch Restart Architecture

Modernized .NET batch systems require two distinct levels of restartability:

### Level 1: Step-Level Restart (JCL `RESTART=STEP02` Parity)
- The orchestrator (`BatchJobCoordinator`) evaluates step status in the database before starting each step.
- If a step is already marked `COMPLETED` for the given `BusinessDate`, it is bypassed automatically.
- Operators can pass an explicit restart parameter:
  ```bash
  dotnet run --project dotnet/src/CardDemo.Host/ -- --workload INTCALC --date 20220718 --force-restart-from-step STEP15_CBACT04C
  ```
- Any step preceding the target restart step is skipped; the pipeline resumes directly from the target step.

### Level 2: Chunk-Level Checkpointing (`ChunkStepRunner<T>`)
- High-volume batch streams (such as `INTCALC` processing millions of transaction category balance records) operate in chunks of `commitInterval` (default 1,000 records).
- **Execution Flow**:
  1. Input reader reads records sequentially.
  2. If a checkpoint exists with `Status == 'Running'` or `Status == 'Failed'`, the reader fast-forwards through the stream until `currentKey == LastProcessedKey`.
  3. For each batch of 1,000 records:
     - Output records (e.g. `TRANSACT`) are inserted into the target store.
     - Account balances (`ACCTDATA`) are updated.
     - `BATCH_STEP_CHECKPOINT` is updated with `LastProcessedKey`, `ProcessedCount`, and `LastCheckpointUtc`.
     - The database transaction commits.
  4. **On Unhandled Failure**: The active transaction rolls back. The last committed checkpoint remains intact.
  5. **On Recovery**: Resuming the batch job reads the last committed checkpoint and picks up precisely at `LastProcessedKey + 1` without calculating interest twice or generating duplicate transactions.

### Operational Database Schema
```sql
CREATE TABLE BATCH_STEP_CHECKPOINT (
    JobId NVARCHAR(64) NOT NULL,
    StepName NVARCHAR(64) NOT NULL,
    Status NVARCHAR(20) NOT NULL,            -- 'Pending', 'Running', 'Completed', 'Failed'
    LastProcessedKey NVARCHAR(128) NULL,     -- e.g. '000000450000_01_0005'
    ProcessedCount BIGINT NOT NULL DEFAULT 0,
    LastCheckpointUtc DATETIME2 NOT NULL,
    ErrorMessage NVARCHAR(MAX) NULL,
    CONSTRAINT PK_BatchStepCheckpoint PRIMARY KEY (JobId, StepName)
);

CREATE TABLE MMP_RUN_LEDGER (
    RunId NVARCHAR(64) NOT NULL,
    Workload NVARCHAR(64) NOT NULL,
    BusinessDate NVARCHAR(10) NOT NULL,       -- 'YYYY-MM-DD'
    Status NVARCHAR(20) NOT NULL,            -- 'Running', 'Completed', 'Failed'
    StartedAt DATETIME2 NOT NULL,
    CompletedAt DATETIME2 NULL,
    CONSTRAINT PK_MmpRunLedger PRIMARY KEY (RunId)
);
```

---

## 7. Command Execution Quick Reference

```bash
# Phase 1: Run Legacy Discovery on JCL, COBOL, Copybooks, and CA-7
/01-Legacy-Discovery

# Phase 2: Audit Discovery Completeness (GATE 1)
/02-Discovery-Review-Gate

# Phase 3: Synthesize Canonical Batch IR and generate .NET Architecture Decision Records
/03-Target-Planning

# Phase 4: Provision Database DDL and C# Keyed Stores
/04-Database-Provisioning

# Phase 5: Generate .NET 8 C# Solution, Codecs, and Batch Steps with Restart
/05-Target-Build

# Phase 6: Code Review & Build Compilation Check (GATE 2)
/06-Target-Review-Gate

# Phase 7: Execute Test Pyramid & 5-Level Batch Reconciliation
/07-Target-Validation

# Phase 8: Run Quality, Security, Memory, and Traceability Audits (GATE 3)
/08-Quality-Security-Audit
```
