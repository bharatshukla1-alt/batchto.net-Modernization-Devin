# Devin Playbook: 08-Quality-Security-Audit
> **Execution Shell**: Linux Bash / Devin Terminal
> **Orchestrator Command**: `python3 run_pipeline.py --phase ...`

## Playbook Objective
---
description: Run Stage 6 Quality & Security Audit to verify traceability coverage, Roslyn analyzer rules, zero-allocation memory safety, and security compliance.
---

## Step 1: Invoke Quality-Security-Auditor-Agent
Run the `Quality-Security-Auditor-Agent` to scan all files in `dotnet/src/` and `dotnet/tests/`.

## Step 2: Traceability Audit
Verify that:
1. Every public C# class, record, and method is decorated with a `[Trace(...)]` attribute.
2. Every business rule in `Artefacts/Discovery/Rules/` has at least one corresponding `[Trace]` in the implementation and one `[Trace]` in a unit/contract test.
3. Compute traceability coverage:
   - Rule coverage target: 100%
   - Statement / paragraph coverage target: >= 95%

## Step 3: Numeric & Financial Safety Audit
Verify that:
1. Zero instances of `float` or `double` exist in balance or financial computation code.
2. Rounding modes explicitly match legacy specifications (`MidpointRounding.AwayFromZero`).

## Step 4: Memory & Performance Audit
Verify that:
1. `ReadOnlySpan<byte>` is used for record binary parsing.
2. No unnecessary string allocations occur during high-volume record reading.

## Step 5: Security & Path Sanitization
Verify that:
1. No absolute personal user paths (e.g. `C:\Users\2571594\...`) exist in code or configs.
2. Database connection strings and secrets use environment variables (`${env:DB_CONN_STR}`).
3. Run ledger checks prevent accidental double runs of the same business date.

## Step 6: Audit Report Generation
Save findings to:
- `Artefacts/Audits/Quality-Security-Audit-Report.md`
- `Artefacts/Audits/Traceability-Matrix.html`


---

## Automated Devin Run Procedure
Devin should execute the following terminal commands to validate this phase:
```bash
# 1. Ensure environment is ready
chmod +x ./setup_devin_env.sh && ./setup_devin_env.sh

# 2. Execute pipeline verification for this phase
python3 run_pipeline.py --phase all
```
