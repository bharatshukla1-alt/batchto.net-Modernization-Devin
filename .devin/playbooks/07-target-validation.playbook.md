# Devin Playbook: 07-Target-Validation
> **Execution Shell**: Linux Bash / Devin Terminal
> **Orchestrator Command**: `python3 run_pipeline.py --phase ...`

## Playbook Objective
---
description: Run Stage 5 Target Validation to execute unit tests, data-driven contract tests, and multi-level reconciliation against legacy output datasets.
---

## Step 1: Invoke Reconciliation-Test-Agent
Execute the `Reconciliation-Test-Agent` to generate test suites under `dotnet/tests/`:
- `CardDemo.UnitTests`: Rule tests for `BR-CBACT04C-001` through `008`.
- `CardDemo.ContractTests`: Scenarios (e.g., negative balance interest, missing rate fallback, control break flush, restart after failure).
- `CardDemo.Reconciliation`: 5-level reconciliation comparing legacy mainframe data files in `data/` against target output.

## Step 2: Execute Automated Test Pyramid
Run xUnit test suites:
```bash
dotnet test dotnet/tests/CardDemo.UnitTests/ -v normal
dotnet test dotnet/tests/CardDemo.ContractTests/ -v normal
```
Verify that all unit and contract tests pass.

## Step 3: Run Batch Workloads on Synthetic & EBCDIC Data
Execute the modernized batch pipeline:
```bash
dotnet run --project dotnet/src/CardDemo.Host/ -- --workload INTCALC --date 20220718
```

## Step 4: Run 5-Level Reconciliation
Execute the reconciliation comparison between legacy output and modernized .NET output:
- Level 1: File & record SHA-256 byte hashes.
- Level 2: Record presence by key (legacy-only / target-only).
- Level 3: Field-by-field symmetric diff (exact decimal precision).
- Level 4: Contractual record ordering.
- Level 5: Group aggregates (total interest, total debits/credits).

Generate the reconciliation report:
- `Artefacts/Reports/Reconciliation-Report-INTCALC.md`
- `Artefacts/Reports/Reconciliation-Report-INTCALC.json`

## Step 5: Acceptance Gate
Pass criterion:
- Level 1/2/3/4/5 reconciliation status: `PASSED`.
- 0 field mismatches on all non-excluded columns.


---

## Automated Devin Run Procedure
Devin should execute the following terminal commands to validate this phase:
```bash
# 1. Ensure environment is ready
chmod +x ./setup_devin_env.sh && ./setup_devin_env.sh

# 2. Execute pipeline verification for this phase
python3 run_pipeline.py --phase all
```
