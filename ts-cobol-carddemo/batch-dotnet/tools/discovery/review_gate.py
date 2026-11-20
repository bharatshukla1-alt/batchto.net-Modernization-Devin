#!/usr/bin/env python3
"""Phase 2 (Discovery Review Gate, GATE 1).

Cross-checks the Phase 1 artefacts against the estate:
  * every program DD (SELECT ... ASSIGN TO) is bound by a DD in the job step;
  * every copybook a program copies exists in app/cpy;
  * every business rule still resolves to its cited line;
  * every in-scope program has JCL facts, program facts and a BRE file.
Writes Artefacts/Review/Discovery-Review-Gate-Report.md and exits 1 on a
critical finding.
"""
from __future__ import annotations

import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
import extract_facts as ef  # noqa: E402

UTILITY_DDS = {"SYSOUT", "SYSPRINT", "STEPLIB", "SYSIN", "DBRMLIB", "SYSTSPRT", "SYSTSIN", "SYSUDUMP"}


def main() -> int:
    findings: list[tuple[str, str, str]] = []
    rows = []
    for job, pgms in ef.WORKLOADS.items():
        jcl = ef.parse_jcl(job)
        for pgm in pgms:
            step = next((s for s in jcl["steps"] if pgm in (s["target"], s.get("dsn_program"))), None)
            if step is None:
                findings.append(("CRITICAL", job, f"no step executes {pgm}"))
                continue
            facts = ef.parse_program(pgm)
            bound = {d["dd"] for d in step["dds"]}
            needed = {f["dd"] for f in facts["files"]}
            for dd in sorted(needed - bound):
                findings.append(("CRITICAL", f"{job}/{step['step']}", f"{pgm} assigns {dd} but the step has no DD"))
            for dd in sorted(bound - needed - UTILITY_DDS):
                findings.append(("INFO", f"{job}/{step['step']}", f"DD {dd} is not used by {pgm}"))
            for cpy in facts["copybooks"]:
                if not (ef.APP / "cpy" / f"{cpy['name']}.cpy").exists():
                    findings.append(("CRITICAL", pgm, f"copybook {cpy['name']} (L{cpy['line']}) missing"))
            for art in (ef.OUT / "Workloads" / f"{job}-Jcl-Facts.md", ef.OUT / "Programs" / f"{pgm}-Program-Facts.md", ef.OUT / "Rules" / f"{pgm}-BRE.md"):
                if not art.exists():
                    findings.append(("CRITICAL", pgm, f"artefact {art.name} missing"))
            for call in facts["calls"]:
                if call["target"] != "CEE3ABD" and not (ef.APP / "cbl" / f"{call['target']}.cbl").exists():
                    sev = "MEDIUM" if (ef.APP / "asm" / f"{call['target']}.asm").exists() else "CRITICAL"
                    findings.append((sev, pgm, f"CALL {call['target']} (L{call['line']}) resolves to {'HLASM source - see GAP-001' if sev == 'MEDIUM' else 'nothing'}"))
            rows.append(f"| {job} | {step['step']} | {pgm} | {len(needed)} | {len(needed & bound)} | {len(facts['copybooks'])} | {len(ef.RULES.get(pgm, []))} |")
    rules = json.loads((ef.OUT / "rules.json").read_text())
    for r in rules:
        src = ef.lines(ef.cbl_path(r["program"]))
        if r["line"] > len(src):
            findings.append(("CRITICAL", r["id"], "cited line out of range"))
    critical = [f for f in findings if f[0] == "CRITICAL"]
    verdict = "PASS - ready for GATE 1 approval" if not critical else "FAIL - critical gaps"
    out = ["# Discovery Review Gate (GATE 1)", "", f"Verdict: **{verdict}**", "",
           "| Job | Step | Program | Program DDs | Bound in JCL | Copybooks | Rules |", "|---|---|---|---|---|---|---|", *rows, "",
           f"Business rules with line evidence: {len(rules)}", "", "## Findings", "", "| Severity | Where | Finding |", "|---|---|---|",
           *[f"| {s} | {w} | {t} |" for s, w, t in findings], "", "## Open gaps carried forward", "",
           (ef.OUT / "Gaps.md").read_text().split("\n", 2)[2]]
    report = ef.ROOT / "batch-dotnet" / "Artefacts" / "Review" / "Discovery-Review-Gate-Report.md"
    report.write_text("\n".join(out))
    print(f"GATE 1: {verdict} ({len(findings)} findings, {len(critical)} critical)")
    return 1 if critical else 0


if __name__ == "__main__":
    sys.exit(main())
