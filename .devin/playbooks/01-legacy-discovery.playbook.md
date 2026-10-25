# Devin Playbook: 01-Legacy-Discovery
> **Execution Shell**: Linux Bash / Devin Terminal
> **Orchestrator Command**: `python3 run_pipeline.py --phase ...`

## Playbook Objective
---
description: Run Stage 1 Discovery to analyze JCL jobs, COBOL batch programs, copybooks, and extract pure business rules into target-neutral artifacts.
---

## Step 1: Initialize Discovery Environment
Verify that the following directories exist under the workspace root:
- `Artefacts/Discovery/Workloads/`
- `Artefacts/Discovery/Programs/`
- `Artefacts/Discovery/Rules/`

If they do not exist, create them.

## Step 2: Analyze JCL Workloads
Invoke the `Jcl-Analyzer-Agent` for each workload in `jcl/`:
- `jcl/INTCALC.jcl`
- `jcl/READACCT.jcl`
- `jcl/READCARD.jcl`

Save the extracted steps, condition logic, and dataset bindings to:
- `Artefacts/Discovery/Workloads/INTCALC-Jcl-Facts.md`
- `Artefacts/Discovery/Workloads/READACCT-Jcl-Facts.md`
- `Artefacts/Discovery/Workloads/READCARD-Jcl-Facts.md`

## Step 3: Analyze COBOL Programs & Copybooks
Invoke the `Program-Analyzer-Agent` for each program in `cbl/` and its referenced copybooks in `cpy/`:
- `cbl/CBACT04C.cbl` (Interest Calculation)
- `cbl/CBACT01C.cbl` (Account File Reader)
- `cbl/CBACT02C.cbl` (Card Data Reader)

Save the structural program facts to:
- `Artefacts/Discovery/Programs/CBACT04C-Program-Facts.md`
- `Artefacts/Discovery/Programs/CBACT01C-Program-Facts.md`
- `Artefacts/Discovery/Programs/CBACT02C-Program-Facts.md`

## Step 4: Extract Business Rules & Calculations
Invoke the `Bre-Analyzer-Agent` to extract all mathematical formulas, decision trees, control breaks, and validation checks.
Ensure every rule has line-level evidence anchors in the source code.

Save output to:
- `Artefacts/Discovery/Rules/CBACT04C-BRE.md`
- `Artefacts/Discovery/Rules/CBACT01C-BRE.md`
- `Artefacts/Discovery/Rules/CBACT02C-BRE.md`

## Step 5: Validation Check
Verify that:
1. No absolute local paths (`C:\Users\...`) appear in any generated markdown file.
2. No target-specific references (PySpark, Python, etc.) exist in the discovery output.
3. Every business rule has documented evidence.


---

## Automated Devin Run Procedure
Devin should execute the following terminal commands to validate this phase:
```bash
# 1. Ensure environment is ready
chmod +x ./setup_devin_env.sh && ./setup_devin_env.sh

# 2. Execute pipeline verification for this phase
python3 run_pipeline.py --phase all
```
