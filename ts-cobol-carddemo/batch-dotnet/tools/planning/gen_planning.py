#!/usr/bin/env python3
"""Phase 3 - target planning: per-workload IR + .NET plan, ADRs, architecture plan, schedule graph view,
and the Spec Review gate. Inputs are the Phase 1 discovery artefacts only."""
from __future__ import annotations

import json
import re
from pathlib import Path

BD = Path(__file__).resolve().parents[2]
ART = BD / "Artefacts"
DISC = ART / "Discovery"
PLAN = ART / "Planning"

WORKLOADS = {
    "READACCT": dict(program="CBACT01C", cls="Cbact01c", file="Programs/ReadPrograms.cs", steps=["PREDEL (IEFBR14)", "STEP05 CBACT01C"],
                     inputs=["ACCTFILE: ACCTDATA.VSAM.KSDS (CVACT01Y, 300)"],
                     outputs=["OUTFILE: ACCTDATA.PSCOMP (107, COMP-3 cycle debit)", "ARRYFILE: ACCTDATA.ARRYPS (110, OCCURS 5)", "VBRCFILE: ACCTDATA.VBPS (VB 12/39 + RDW)"],
                     purpose="Extract every account to three sequential formats and list it on SYSOUT."),
    "READCARD": dict(program="CBACT02C", cls="Cbact02c", file="Programs/ReadPrograms.cs", steps=["STEP05 CBACT02C"],
                     inputs=["CARDFILE: CARDDATA.VSAM.KSDS (CVACT02Y, 150)"], outputs=["SYSOUT listing"],
                     purpose="List the card master in key order."),
    "INTCALC": dict(program="CBACT04C", cls="Cbact04c", file="Programs/Cbact04c.cs", steps=["STEP15 CBACT04C PARM=2022071800"],
                    inputs=["TCATBALF (CVTRA01Y)", "XREFFIL1: CARDXREF AIX path (CVACT03Y)", "ACCTFILE I-O (CVACT01Y)", "DISCGRP (CVTRA02Y)"],
                    outputs=["TRANSACT: SYSTRAN(+1) (CVTRA05Y, 350)", "ACCTFILE rewritten"],
                    purpose="Monthly interest per transaction-category balance; interest transactions and account balance update."),
    "POSTTRAN": dict(program="CBTRN02C", cls="Cbtrn02c", file="Programs/Cbtrn02c.cs", steps=["STEP15 CBTRN02C"],
                     inputs=["DALYTRAN.PS (CVTRA06Y, 350)", "XREFFILE (CVACT03Y)", "ACCTFILE I-O", "TCATBALF I-O"],
                     outputs=["TRANFILE: TRANSACT.VSAM.KSDS (OPEN OUTPUT)", "DALYREJS(+1) (REJTRAN, 430)", "RC 4 when rejects"],
                     purpose="Validate and post the daily transactions; rejects with reason codes 100-103."),
    "TRANREPT": dict(program="CBTRN03C", cls="Cbtrn03c", file="Programs/Cbtrn03c.cs", steps=["STEP05R.PRC001 REPRO -> TRANSACT.BKUP(+1)", "STEP05R SORT (263,16,ZD,A) INCLUDE 305,10", "STEP10R CBTRN03C"],
                     inputs=["TRANSACT.DALY(+1)", "CARDXREF", "TRANTYPE", "TRANCATG", "DATEPARM"], outputs=["TRANREPT(+1) (133-byte report, CVTRA07Y)"],
                     purpose="Back up, filter/sort and print the daily transaction detail report with page/account/grand totals."),
    "MNTTRDB2": dict(program="COBTUPDT", cls="Cobtupdt", file="Programs/Db2Programs.cs", src="app/app-transaction-type-db2/cbl",
                     steps=["STEP1 IKJEFT01: DSN RUN PROGRAM(COBTUPDT) PLAN(CARDDEMO)"],
                     inputs=["INPFILE (FB 53: type, code, description)", "CARDDEMO.TRANSACTION_TYPE (Db2 -> PostgreSQL)"],
                     outputs=["CARDDEMO.TRANSACTION_TYPE inserted/updated/deleted", "SYSTSPRT messages", "RC 4 on any SQL error"],
                     purpose="Weekly maintenance of the transaction-type reference table from an A/U/D file."),
    "MNTTRDYN": dict(program="CBTRNDYN", cls="Cbtrndyn", file="Programs/Db2Programs.cs", src="app/app-transaction-type-db2/cbl",
                     steps=["STEP1 IKJEFT01: DSN RUN PROGRAM(CBTRNDYN) PLAN(CARDDEMO) PARMS('COBTUPDT')"],
                     inputs=["DYNSQL (FB 80: one ad-hoc SQL statement)", "INPFILE (FB 53)", "CARDDEMO.TRANSACTION_TYPE"],
                     outputs=["Dynamic statement applied, then COBTUPDT's changes, committed together", "RC 8 when EXECUTE fails (lost after COBTUPDT, GAP-007)"],
                     purpose="Ad-hoc dynamic SQL (PREPARE/EXECUTE) followed by a dynamic CALL of COBTUPDT in the same unit of work."),
}

ADRS = [
    ("ADR-001", "Target stack: .NET 8 / C# 12 console host, no framework batch runtime",
     "The plugin prescribes .NET 8 / C# 12. Jobs are plain C# classes behind `IBatchProgram`, run by `JobRunner` (JES semantics: return codes, abend stops the job). No Spring-Batch-like framework: the five programs are small and a framework would hide the COBOL control flow that parity depends on."),
    ("ADR-002", "VSAM KSDS -> `KeyedStore` with PostgreSQL or file persistence",
     "Each cluster is an ordinal-ordered keyed store with optional non-unique alternate index (CARDXREF account AIX). Persistence is pluggable: `PostgresPersistence` (tables generated from the copybooks, `db/migrations/001_create_vsam_tables.sql`) or `FilePersistence` (the repository's unloaded-dataset format, used by the parity harness). Records stay fixed-format bytes so COMP-3 / zoned / overpunch round-trip exactly."),
    ("ADR-003", "Decimal arithmetic with legacy truncation (deviation from AwayFromZero)",
     "All money is `decimal`. CBACT04C has no ROUNDED; golden case i02-tie-half-cent proves truncation, so `CobolCodec.Truncate` is used instead of the plugin default `MidpointRounding.AwayFromZero` (GAP-004)."),
    ("ADR-004", "Reproduce legacy quirks and runner semantics",
     "CBACT04C never updates the last account; CBTRN02C checks the limit on cycle amounts and lets reason 103 overwrite 102; CBTRN03C NEXT SENTENCE exits the loop and EOF re-adds the last amount; CBACT01C carries the previous packed debit (GAP-002). COBDATFT (HLASM, not in the harness loadlib) is ported to `CobDatFt` from app/asm/COBDATFT.asm and proven by unit tests (GAP-001). rc.json keeps the harness behaviour of merging setup-job step codes."),
    ("ADR-005", "Restartability: step state, chunk checkpoints, batch run ledger",
     "Every step is appended to `ledger/batch_run_ledger.jsonl` (run id, job, step, program, RC, status, counts). An abended job stores its committed steps and pinned GDG generations in `state/<JOB>.json`; `--restart` skips them. POSTTRAN commits every `ChunkSize` records as one unit: the KSDS changes and the checkpoint (position, counters, SYSOUT, reject-file length) are written in one PostgreSQL transaction, or through a redo journal in file mode (the journal rename is the commit point; a restart replays a left-over journal). Rejects are appended before the commit and cut back to the checkpointed length on restart, and a step's (+n) generations are pinned before it starts, so a crash at any point neither loses nor replays a posting or a reject (fault-injection tests cover each boundary). INTCALC/TRANREPT commit at step end, so a rerun is idempotent."),
    ("ADR-006", "Deterministic clock and GDG catalog",
     "The frozen chain clock (2022-07-05 10:00:00.00) is injected into `StepContext`; `Db2Timestamp` reproduces Z-GET-DB2-FORMAT-TIMESTAMP. `DatasetCatalog` resolves GDG relative generations per job like the z/OS catalog."),
]


ADRS.append(("ADR-007", "Db2 SQL programs -> Npgsql `Db2Session` with Open COBOL ESQL semantics",
             "IKJEFT01 `DSN RUN PROGRAM` becomes `Dsnrun`: one PostgreSQL transaction per task, committed at end of task. Embedded SQL becomes parameterised Npgsql commands; dynamic SQL (PREPARE/EXECUTE) executes the DYNSQL text as is, and dynamic CALL resolves the PARMS program name at run time (`Dsnrun.Resolve`). SQLCODEs follow what the golden runs recorded through ocesql: -403 duplicate key, -400 other errors, 0 when no row matched; a failed statement aborts the unit of work as PostgreSQL does (GAP-006). The callee's STOP RUN ends the task with its return code (GAP-007). A Db2-faithful savepoint per statement is a migration decision, not done silently."))


def main() -> int:
    rules = json.loads((DISC / "rules.json").read_text())
    gaps = re.findall(r"^\| (GAP-\d+) \| ([^|]+) \| ([^|]+) \|", (DISC / "Gaps.md").read_text(), re.M)
    (PLAN / "ADRs").mkdir(parents=True, exist_ok=True)
    for adr, title, body in ADRS:
        (PLAN / "ADRs" / f"{adr}.md").write_text(f"# {adr}: {title}\n\nStatus: Accepted\n\n{body}\n")
    findings = []
    for wl, w in WORKLOADS.items():
        wr = [r for r in rules if r["program"] == w["program"]]
        ir = {"target_design_specification": {
            "document_metadata": {"document_type": "Mainframe Batch To .NET 8 Target Design Specification", "workload": wl,
                                  "source_program": w["program"], "confidence_level": "HIGH"},
            "source_system_summary": {"application_name": "CardDemo", "processing_description": w["purpose"],
                                      "source_evidence": [f"Discovery/Workloads/{wl}-Jcl-Facts.md", f"Discovery/Programs/{w['program']}-Program-Facts.md"]},
            "steps": w["steps"], "inputs": w["inputs"], "outputs": w["outputs"],
            "mainframe_to_dotnet_mapping": [
                {"source_construct": "VSAM KSDS", "target_construct": "CardDemo.Data.KeyedStore (PostgreSQL vsam_* table)", "adr": "ADR-002"},
                {"source_construct": "QSAM FB/VB dataset, GDG", "target_construct": "SequentialFile + DatasetCatalog", "adr": "ADR-006"},
                {"source_construct": "COPYBOOK", "target_construct": "CardDemo.Domain.Layouts (generated) + FixedRecord", "adr": "ADR-002"},
                {"source_construct": "COMP-3 / zoned / COMP", "target_construct": "CobolCodec (decimal)", "adr": "ADR-003"},
                {"source_construct": f"PROGRAM {w['program']}", "target_construct": f"CardDemo.Batch.Programs.{w['cls']}", "adr": "ADR-001"},
                {"source_construct": "JCL job / step / DD", "target_construct": "CardDemo.Orchestration.JobCatalog + JobRunner", "adr": "ADR-005"},
            ],
            "business_rules": [{"id": r["id"], "source": f"{w.get('src', 'app/cbl')}/{r['program']}.cbl:{r['line']}", "statement": r["statement"],
                                "target": f"src/CardDemo.Batch/{w['file']}"} for r in wr],
            "gaps": [{"id": g[0], "description": g[2].strip()} for g in gaps if w["program"] in g[1] or g[1].strip() in ("ALL", wl)],
        }}
        (PLAN / f"{wl}-IR.json").write_text(json.dumps(ir, indent=1) + "\n")
        plan = [f"# {wl} - .NET plan", "", w["purpose"], "", "## Steps", *[f"- {s}" for s in w["steps"]], "",
                "## Inputs", *[f"- {s}" for s in w["inputs"]], "", "## Outputs", *[f"- {s}" for s in w["outputs"]], "",
                "## Target components", f"- Program: `CardDemo.Batch.Programs.{w['cls']}` (`src/CardDemo.Batch/{w['file']}`)",
                f"- Job definition: `JobCatalog.Jobs[\"{wl}\"]`", "- Data: `KeyedStore`, `SequentialFile`, generated `Layouts`", "",
                "## Business rules", "| Rule | Source | Statement |", "|---|---|---|",
                *[f"| {r['id']} | {r['program']}.cbl:{r['line']} | {r['statement']} |" for r in wr], "",
                "## Proof", "- Golden fixtures via `batch-dotnet/tools/parity_dotnet.sh`" if wl in ("POSTTRAN", "TRANREPT", "INTCALC", "MNTTRDB2", "MNTTRDYN")
                else "- No legacy golden run on the harness (GAP-001 for READACCT); unit tests + sample run", ""]
        (PLAN / f"{wl}-DotNet-Plan.md").write_text("\n".join(plan))
        if not wr:
            findings.append(("HIGH", wl, "no business rules mapped"))
    arch = ["# .NET Batch Architecture Plan", "",
            "| Layer | Project | Responsibility |", "|---|---|---|",
            "| Domain | CardDemo.Domain | Generated copybook layouts, `CobolCodec` (zoned/COMP-3/COMP/edit), `FixedRecord`, `[Trace]` |",
            "| Data | CardDemo.Data | `KeyedStore` (VSAM KSDS + AIX), `PostgresPersistence` / `FilePersistence`, `SequentialFile`, `DatasetCatalog` (GDG), `Db2Session` (Db2 SQL on PostgreSQL) |",
            "| Batch | CardDemo.Batch | One `IBatchProgram` per COBOL program, SORT/IDCAMS utilities, `StepContext`, chunk checkpoints |",
            "| Orchestration | CardDemo.Orchestration | `JobCatalog` (JCL transcribed), `JobRunner` (RC/abend/restart/ledger), `ChainRunner` (parity contract) |",
            "| Host | CardDemo.Host | `carddemo-batch run-chain <chain> <case> <out> [--postgres] [--db2] [--restart]` |",
            "| Tests | tests/CardDemo.Tests | Codec unit tests, golden-fixture contract tests, restart test |", "",
            "## Decisions", *[f"- [{a}](ADRs/{a}.md): {t}" for a, t, _ in ADRS], "",
            "## Workloads", *[f"- [{wl}]({wl}-DotNet-Plan.md) - {w['program']}" for wl, w in WORKLOADS.items()], ""]
    (PLAN / "DotNet-Batch-Architecture-Plan.md").write_text("\n".join(arch))
    graph = json.loads((DISC / "Workloads" / "Batch-Schedule-Graph.json").read_text())
    md = ["# Batch schedule graph", "", f"Source: `{graph['source']}`", "", "```mermaid", "flowchart LR"]
    for job, j in sorted(graph["jobs"].items()):
        for cond in j["in"]:
            for other, o in graph["jobs"].items():
                if cond in o["out"]:
                    md.append(f"  {other} --> {job}")
    md += ["```", "", "In scope: " + ", ".join(sorted(k for k, v in graph["jobs"].items() if v.get("in_scope"))), ""]
    (DISC / "Workloads" / "Batch-Schedule-Graph.md").write_text("\n".join(md))
    covered = {g[0] for g in gaps}
    adr_text = " ".join(b for _, _, b in ADRS)
    for g in sorted(covered):
        if g not in adr_text and g not in ("GAP-003", "GAP-005"):
            findings.append(("MEDIUM", g, "gap without an ADR decision"))
    verdict = "PASS" if not any(f[0] in ("CRITICAL", "HIGH") for f in findings) else "FAIL"
    rep = ["# Spec Review Gate", "", f"Verdict: **{verdict}**", "",
           f"- Workloads planned: {len(WORKLOADS)}", f"- Rules mapped to target components: {len(rules)}", f"- Gaps carried: {len(gaps)}", f"- ADRs: {len(ADRS)}", "",
           "| Severity | Item | Finding |", "|---|---|---|", *([f"| {s} | {i} | {m} |" for s, i, m in findings] or ["| - | - | none |"]),
           "", "GAP-003 and GAP-005 are low-severity behavioural choices documented in the IR (lowest card / stable sort).", ""]
    (ART / "Review" / "Spec-Review-Gate-Report.md").write_text("\n".join(rep))
    print(f"planning: {len(WORKLOADS)} IR + plans, {len(ADRS)} ADRs; spec gate {verdict} ({len(findings)} findings)")
    return 0 if verdict == "PASS" else 1


if __name__ == "__main__":
    raise SystemExit(main())
