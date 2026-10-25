# DEVIN.md — Autonomous Mainframe Batch to .NET 8 Modernization Engine

This repository contains an end-to-end modernization workbench designed for **Mainframe Batch Modernization**. It transforms legacy IBM z/OS Batch workloads (JCL, COBOL Batch, Copybooks, VSAM KSDS, QSAM datasets, and binary EBCDIC/COMP-3 files) into an enterprise cloud-native **.NET 8 / C# 12 Batch Engine** with chunk checkpointing, 96-bit base-10 decimal math, and 5-level reconciliation verification.

---

## 1. Operating Instructions for Devin

When operating on this repository, Devin acts as the **Lead Modernization Systems Architect & .NET Batch Engineer**.

### Core Environment Setup
Upon opening the repository sandbox in Devin, immediately verify or run the environment setup script:
```bash
chmod +x ./setup_devin_env.sh
./setup_devin_env.sh
```
This ensures the .NET 8 SDK (`dotnet-sdk-8.0`) and internationalization libraries (`libicu-dev`) are active in Devin's Ubuntu Linux container.

### Fast Pipeline Runner
Devin can execute individual phases or the entire end-to-end pipeline using the master CLI runner:
```bash
# Run full batch pipeline with 5-level verification
python3 run_pipeline.py --phase all

# Or run individual phases:
python3 run_pipeline.py --phase discovery
python3 run_pipeline.py --phase build
python3 run_pipeline.py --phase test
python3 run_pipeline.py --phase run
```

---

## 2. Non-Negotiable Golden Rules for Devin

1. **Strictly Mainframe Batch Processing**: The workload scope is strictly backend batch (`input/jcl`, `input/cbl`, `input/cpy`, `input/data`). **DO NOT** generate CICS, BMS maps, 3270 terminal handlers, or web frontends.
2. **Source Immutability**: The directory `input/` (`input/jcl/`, `input/cbl/`, `input/cpy/`, `input/data/`, `input/asm/`) is **READ-ONLY**. Never alter, rename, or reformat legacy source files.
3. **No Hallucinations / Strict Grounding**: Every extracted business rule, formula, and step constraint MUST cite direct line numbers in `input/`. Never invent missing logic; record unknowns as `GAP-XXX`.
4. **Deterministic Decimal Arithmetic**: All financial calculations (interest, balances, ledgers) MUST use C# `decimal` (96-bit base-10) with `MidpointRounding.AwayFromZero`. **Never use floating-point types (`float`, `double`)**.
5. **Zero-Allocation Binary Codecs**: EBCDIC parsing and COMP-3 bitwise packed decimal unpacking must utilize `ReadOnlySpan<byte>` without managed heap allocation.
6. **Two-Level Restartability**: High-volume batch jobs must support step bypass (`--force-restart-from-step`) and chunk checkpointing (`BATCH_STEP_CHECKPOINT`).
7. **Traceability**: Every public C# class, record, and rule method must carry a `[Trace(Source = "...", Rule = "...")]` attribute.
8. **Human Review Gates**: Devin must pause at designated review gates (**GATE 1**, **GATE 2**, **GATE 3**) and request confirmation before advancing.

---

## 3. The 8 Phased Modernization Playbooks

Devin executes modernizations following the 8 playbooks defined in `.devin/playbooks/`:

| Phase | Playbook File | Agent Personas Adopted | Primary Outputs | Gate |
|---|---|---|---|---|
| **Phase 1** | `.devin/playbooks/01-legacy-discovery.playbook.md` | `jcl-analyzer`<br>`program-analyzer`<br>`bre-analyzer`<br>`ca7-scheduler-analyzer` | `Artefacts/Discovery/Workloads/*-Jcl-Facts.md`<br>`Artefacts/Discovery/Programs/*-Program-Facts.md`<br>`Artefacts/Discovery/Rules/*-BRE.md`<br>`Artefacts/Discovery/Workloads/Batch-Schedule-Graph.json` | — |
| **Phase 2** | `.devin/playbooks/02-discovery-review-gate.playbook.md` | `discovery-review-gate` | `Artefacts/Review/Discovery-Review-Gate-Report.md` | **GATE 1: Discovery Approval** |
| **Phase 3** | `.devin/playbooks/03-target-planning.playbook.md` | `target-planning` | `Artefacts/Planning/*-IR.json`<br>`Artefacts/Planning/ADRs/*.md` | — |
| **Phase 4** | `.devin/playbooks/04-database-provisioning.playbook.md` | `schema-store-provisioner` | `dotnet/db/migrations/001_Create_CardDemo_VSAM_Tables.sql`<br>`dotnet/db/migrations/002_Create_Batch_Operational_Tables.sql`<br>`dotnet/src/CardDemo.Data/Stores/*.cs` | — |
| **Phase 5** | `.devin/playbooks/05-target-build.playbook.md` | `dotnet-batch-engineer`<br>`quartz-orchestrator-creator` | `dotnet/src/CardDemo.Domain/*.cs`<br>`dotnet/src/CardDemo.Data/Codecs/*.cs`<br>`dotnet/src/CardDemo.Batch/*.cs`<br>`dotnet/src/CardDemo.Orchestration/*.cs`<br>`dotnet/src/CardDemo.Host/Program.cs` | — |
| **Phase 6** | `.devin/playbooks/06-target-review-gate.playbook.md` | `spec-review-gate` | `Artefacts/Review/Target-Review-Gate-Report.md` | **GATE 2: Code Review & Build Gate** |
| **Phase 7** | `.devin/playbooks/07-target-validation.playbook.md` | `reconciliation-test`<br>`test-failure-remediator` | `dotnet/tests/CardDemo.UnitTests/*.cs`<br>`dotnet/tests/CardDemo.ContractTests/*.cs`<br>`dotnet/tests/CardDemo.Reconciliation/Program.cs`<br>`Artefacts/Reports/Reconciliation-Report-*.md` | — |
| **Phase 8** | `.devin/playbooks/08-quality-security-audit.playbook.md` | `quality-security-auditor`<br>`quality-security-remediator` | `Artefacts/Audits/Quality-Security-Audit-Report.md` | **GATE 3: Final Acceptance Gate** |

---

## 4. The 14 Specialized Agent Personas for Devin

Devin adopts these 14 specialized agent personas (detailed in `.devin/agents/`) based on the task:
1. **`ca7-scheduler-analyzer`**: Parses CA-7 scheduler reports (`1LJOB` blocks) into dependency graphs and triggers.
2. **`jcl-analyzer`**: Analyzes JCL step flow, DD statements, condition codes, and dataset lifecycles.
3. **`program-analyzer`**: Constructs COBOL ASTs, `SELECT...ASSIGN` file controls, and working-storage layouts.
4. **`bre-analyzer`**: Isolates business math formulas and fallback lookup rules with exact source line citations.
5. **`discovery-review-gate`**: Cross-audits extracted facts against source code to verify 100% coverage (**GATE 1**).
6. **`target-planning`**: Synthesizes facts into Canonical Batch IR JSON and emits Architecture Decision Records.
7. **`schema-store-provisioner`**: Generates SQL DDL for VSAM tables, checkpoint tables, and C# `IKeyedStore` implementations.
8. **`dotnet-batch-engineer`**: Emits production C# 12 / .NET 8 code, zero-allocation COMP-3/EBCDIC span codecs, and chunk step runners.
9. **`quartz-orchestrator-creator`**: Generates Quartz.NET job configurations, batch coordinators, and step bypass restart logic.
10. **`spec-review-gate`**: Executes clean builds (`dotnet build --warnaserror`), verifies 0 warnings/errors, and audits `[Trace]` attributes (**GATE 2**).
11. **`reconciliation-test`**: Executes 5-level batch reconciliation tests across row count, schema typing, SHA-256 hashes, arithmetic, and restart recovery.
12. **`test-failure-remediator`**: Diagnoses test failures or rounding variances and applies surgical code edits.
13. **`quality-security-auditor`**: Inspects memory allocations, SAST security standards, and traceability matrices (**GATE 3**).
14. **`quality-security-remediator`**: Automatically patches security, code quality, and missing annotation issues.

---

## 5. Standard Test & Execution Commands for Devin

```bash
# 1. Compile the entire .NET 8 solution
dotnet build dotnet/CardDemo.Modernized.sln

# 2. Run unit tests
dotnet run --project dotnet/tests/CardDemo.UnitTests/

# 3. Run contract & restartability tests
dotnet run --project dotnet/tests/CardDemo.ContractTests/

# 4. Run 5-level batch reconciliation suite
dotnet run --project dotnet/tests/CardDemo.Reconciliation/

# 5. Execute all 3 batch jobs live
dotnet run --project dotnet/src/CardDemo.Host/
```
