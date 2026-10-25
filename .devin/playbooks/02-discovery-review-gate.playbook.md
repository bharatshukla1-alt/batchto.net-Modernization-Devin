# Devin Playbook: 02-Discovery-Review-Gate
> **Execution Shell**: Linux Bash / Devin Terminal
> **Orchestrator Command**: `python3 run_pipeline.py --phase ...`

## Playbook Objective
---
description: Run Stage 2 Discovery Review Gate to verify that all JCL jobs, COBOL programs, copybooks, and business rules were completely and accurately extracted before planning begins.
---

## Step 1: Locate Discovery Artifacts
Verify that the following artifacts exist under `Artefacts/Discovery/`:
- `Workloads/INTCALC-Jcl-Facts.md`
- `Workloads/READACCT-Jcl-Facts.md`
- `Workloads/READCARD-Jcl-Facts.md`
- `Programs/CBACT04C-Program-Facts.md`
- `Programs/CBACT01C-Program-Facts.md`
- `Programs/CBACT02C-Program-Facts.md`
- `Rules/CBACT04C-BRE.md`
- `Rules/CBACT01C-BRE.md`
- `Rules/CBACT02C-BRE.md`

If any file is missing, stop immediately and report the missing workload discovery.

## Step 2: Invoke Discovery-Review-Gate-Agent
Run the `Discovery-Review-Gate-Agent` to cross-check:
1. Every JCL EXEC step against `cbl/` source files.
2. Every `COPY` statement in the COBOL programs against files in `cpy/`.
3. Every DD statement in JCL against program file assignments (`SELECT ... ASSIGN`).
4. Evidence anchors in every BRE rule against line numbers in `cbl/`.

## Step 3: Write Review Gate Report
Generate `Artefacts/Review/Discovery-Review-Gate-Report.md` containing:
- Component Completeness Checklist
- Unresolved DD statements or unmapped copybooks
- Categorized Gaps (`Critical`, `Warning`, `Informational`)
- Audit Verdict: `APPROVED` or `BLOCKED`

## Step 4: Gate 1 Decision
If any **Critical Gap** is found:
- STOP the pipeline.
- Prompt the user with the exact missing component or evidence mismatch.
- Do NOT proceed to Target Planning until resolved and approved.


---

## Automated Devin Run Procedure
Devin should execute the following terminal commands to validate this phase:
```bash
# 1. Ensure environment is ready
chmod +x ./setup_devin_env.sh && ./setup_devin_env.sh

# 2. Execute pipeline verification for this phase
python3 run_pipeline.py --phase all
```
