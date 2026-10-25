---
description: Run Stage 6 Target Review Gate to audit the generated .NET code, Domain models, and solution structure before entering validation and execution.
---

## Step 1: Locate Target Solution
Verify that `dotnet/CardDemo.Modernized.sln` and its constituent projects exist:
- `dotnet/src/CardDemo.Domain/`
- `dotnet/src/CardDemo.Data/`
- `dotnet/src/CardDemo.Batch/`
- `dotnet/src/CardDemo.Orchestration/`
- `dotnet/src/CardDemo.Host/`

## Step 2: Invoke Spec-Review-Gate-Agent
Run the `Spec-Review-Gate-Agent` to verify:
1. Every planned operation in `Artefacts/Planning/*-IR.json` is implemented in `dotnet/src/`.
2. Traceability: `[Trace(...)]` attribute presence on all public classes, records, and rule methods.
3. Codec implementation: verify `Comp3Codec` and `EbcdicCodec` zero-allocation span operations.
4. Checkpoint restart logic: verify `ChunkStepRunner<T>` and `BATCH_STEP_CHECKPOINT` integration.

## Step 3: Compile Verification
Run build check:
```bash
dotnet build dotnet/CardDemo.Modernized.sln -c Release
```
Ensure 0 compilation errors and 0 compiler warnings.

## Step 4: Write Review Gate Report
Generate `Artefacts/Review/Target-Review-Gate-Report.md` containing:
- Code generation completeness matrix
- Traceability coverage percentage
- Compiler status
- Audit Verdict: `APPROVED` or `BLOCKED`

## Step 5: Gate 2 Decision
If any blocker gaps or build failures exist:
- STOP the pipeline.
- Require remediation before advancing to test execution and reconciliation.
