---
description: Run Stage 4 Target Build to generate the .NET 8 solution, Domain records, codecs, batch processors, step orchestrators, and restart/checkpoint mechanisms.
---

## Step 1: Initialize .NET Solution Structure
Create the following solution layout:
```
dotnet/
├── CardDemo.Modernized.sln
├── Directory.Build.props
├── Directory.Packages.props
├── src/
│   ├── CardDemo.Domain/
│   ├── CardDemo.Data/
│   ├── CardDemo.Batch/
│   ├── CardDemo.Orchestration/
│   └── CardDemo.Host/
└── tests/
    ├── CardDemo.UnitTests/
    ├── CardDemo.ContractTests/
    └── CardDemo.Reconciliation/
```

## Step 2: Invoke Dotnet-Batch-Engineer-Agent
Run the `Dotnet-Batch-Engineer-Agent` to generate code based on:
- `Artefacts/Planning/DotNet-Batch-Architecture-Plan.md`
- `Artefacts/Planning/*-IR.json`
- `Artefacts/Discovery/Rules/*-BRE.md`

## Step 3: Implement Core Modules
1. **CardDemo.Domain**:
   - Generate strongly typed C# records corresponding to copybooks (`AccountRecord`, `CardXrefRecord`, `TransCatBalanceRecord`, `DisclosureGroupRecord`, `TransactionRecord`).
   - Add `[Trace]` attributes mapping each record and property to source copybooks.
2. **CardDemo.Data**:
   - `Mmp.Runtime.Codecs`: `EbcdicCodec`, `Comp3Codec` (Packed Decimal bitwise unpacker), `BinaryCodec`.
   - File stream layouts for sequential input/output files.
3. **CardDemo.Batch**:
   - `ChunkStepRunner<TRecord>` with `IBatchCheckpointStore` for chunk commits and restart recovery.
   - `IntCalcStep` (`CBACT04C` interest calculation, control break, group totals, rate fallback, and account balance update).
   - `ReadAcctStep` (`CBACT01C`) and `ReadCardStep` (`CBACT02C`).
4. **CardDemo.Orchestration**:
   - `BatchJobCoordinator`: Sequential step execution pipeline with `forceRestartFromStep` support.
   - Quartz.NET / BackgroundService job wrappers.
5. **CardDemo.Host**:
   - `Program.cs` configured with `Host.CreateDefaultBuilder()`, DI service registrations, logging, and CLI command options.

## Step 4: Build Verification
Execute build via command:
```bash
dotnet build dotnet/CardDemo.Modernized.sln -c Release
```
Verify that compilation passes with 0 warnings (`TreatWarningsAsErrors=true`).
