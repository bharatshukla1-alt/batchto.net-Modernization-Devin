---
description: Run Stage 2 Planning to synthesize discovery facts into the Canonical Intermediate Representation (IR), formulate .NET Architecture Decision Records (ADRs), and produce the lowering plan.
---

## Step 1: Initialize Planning Directory
Ensure `Artefacts/Planning/` exists.

## Step 2: Invoke Target-Planning-Agent
Run the `Target-Planning-Agent` with inputs from Stage 1 (`Artefacts/Discovery/`):
- `Workloads/*-Jcl-Facts.md`
- `Programs/*-Program-Facts.md`
- `Rules/*-BRE.md`

## Step 3: Generate Canonical Workload IR
For each workload (`INTCALC`, `READACCT`, `READCARD`), generate:
- `Artefacts/Planning/INTCALC-IR.json`
- `Artefacts/Planning/READACCT-IR.json`
- `Artefacts/Planning/READCARD-IR.json`

Ensure schemas, resources, operations, rules, and ordering constraints conform to the target-agnostic IR standard.

## Step 4: Generate .NET Architecture Decision Records (ADRs)
Formulate the .NET batch architecture decisions:
1. **Data Access Strategy**:
   - `ADR-001`: Relational table storage (SQL Server / PostgreSQL / SQLite) for VSAM KSDS files (`ACCTDATA`, `CARDXREF`, `TCATBALF`, `DISCGRP`).
   - `ADR-002`: FlatFile streaming via `System.IO.Pipelines` for sequential files.
2. **Numeric Representation**:
   - `ADR-003`: C# `decimal` (96-bit fixed point) for COMP-3 packed fields and monetary interest calculations.
3. **Execution & Checkpoint Model**:
   - `ADR-004`: Step-level restart coordinator + chunk-level checkpointing (`BATCH_STEP_CHECKPOINT`) with 1,000-record commit intervals.
4. **Orchestration**:
   - `ADR-005`: Multi-step dependency execution pipeline (`BatchJobCoordinator`) with Quartz.NET / BackgroundService hosting.

Save the comprehensive plan to:
- `Artefacts/Planning/DotNet-Batch-Architecture-Plan.md`

## Step 5: Review Gate
Review all identified gaps (`GAP-XXX`). Ensure any BLOCKER or HIGH gaps have explicit decisions or SME waivers before proceeding to code generation.
