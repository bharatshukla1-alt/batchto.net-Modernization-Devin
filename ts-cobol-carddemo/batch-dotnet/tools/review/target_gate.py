#!/usr/bin/env python3
"""Phase 6 - Target Review gate (GATE 2): build with warnings as errors, tests, JCL/DD fidelity of the
job catalog, numeric safety and traceability of every program class."""
from __future__ import annotations

import json
import os
import re
import subprocess
from pathlib import Path

BD = Path(__file__).resolve().parents[2]
ROOT = BD.parent
DOTNET = os.environ.get("DOTNET", str(Path.home() / ".dotnet" / "dotnet"))
IGNORED_DD = {"STEPLIB", "SYSOUT", "SYSPRINT", "SYSIN", "SYMNAMES", "SYSUDUMP", "CEEDUMP", "SYSTSPRT", "SYSTSIN", "DBRMLIB", "DD01", "DD02", "DD03"}
JOBS = {"READACCT": "CBACT01C", "READCARD": "CBACT02C", "INTCALC": "CBACT04C", "POSTTRAN": "CBTRN02C", "TRANREPT": "CBTRN03C", "MNTTRDB2": "IKJEFT01", "MNTTRDYN": "IKJEFT01"}
DB2_JCL = ROOT / "app" / "app-transaction-type-db2" / "jcl"


def run(cmd: list[str]) -> tuple[int, str]:
    p = subprocess.run(cmd, cwd=BD, capture_output=True, text=True)
    return p.returncode, p.stdout + p.stderr


def jcl_dds(job: str, program: str) -> set[str]:
    dds, inside = set(), False
    path = DB2_JCL / f"{job}.jcl" if (DB2_JCL / f"{job}.jcl").exists() else ROOT / "app" / "jcl" / f"{job}.jcl"
    for line in path.read_text().splitlines():
        m = re.match(r"^//(\S+)\s+(EXEC|DD)\b", line)
        if not m:
            continue
        if m.group(2) == "EXEC":
            inside = f"PGM={program}" in line
        elif inside and m.group(1) not in IGNORED_DD:
            dds.add(m.group(1))
    return dds


def main() -> int:
    findings: list[tuple[str, str, str]] = []
    rc, out = run([DOTNET, "build", "CardDemo.Batch.sln", "-c", "Release", "--nologo", "-warnaserror"])
    build = "PASS" if rc == 0 else "FAIL"
    if rc:
        findings.append(("CRITICAL", "build", out[-400:]))
    rc, out = run([DOTNET, "test", "CardDemo.Batch.sln", "-c", "Release", "--nologo", "--no-build"])
    m = re.search(r"Failed:\s+(\d+), Passed:\s+(\d+)", out)
    tests = f"{m.group(2)} passed, {m.group(1)} failed" if m else "not run"
    if rc or not m:
        findings.append(("CRITICAL", "tests", tests))
    rc, out = run([DOTNET, f"src/CardDemo.Host/bin/Release/net8.0/carddemo-batch.dll", "describe"])
    catalog = json.loads(out)["jobs"]
    for job, program in JOBS.items():
        steps = [s for s in catalog.get(job, []) if s["program"] == program]
        if not steps:
            findings.append(("CRITICAL", job, f"no step runs {program}"))
            continue
        missing = jcl_dds(job, program) - set(steps[0]["dd"])
        for dd in sorted(missing):
            findings.append(("HIGH", f"{job}/{steps[0]['step']}", f"JCL DD {dd} not mapped"))
    src = list((BD / "src").rglob("*.cs"))
    for f in src:
        text = f.read_text()
        for i, line in enumerate(text.splitlines(), 1):
            if re.search(r"\b(double|float)\b", line):
                findings.append(("HIGH", f"{f.relative_to(BD)}:{i}", "binary floating point in target code"))
            if re.search(r"DateTime\.(Now|Today)\b", line):
                findings.append(("HIGH", f"{f.relative_to(BD)}:{i}", "wall clock instead of the injected chain clock"))
    for f in (BD / "src" / "CardDemo.Batch" / "Programs").glob("*.cs"):
        for cls in re.findall(r"public sealed class (\w+)", f.read_text()):
            if not re.search(r"\[Trace\([^)]*\)\]\s*public sealed class " + cls + r"\b", f.read_text()):
                findings.append(("MEDIUM", cls, "program class without [Trace]"))
    verdict = "PASS" if not any(s in ("CRITICAL", "HIGH") for s, _, _ in findings) else "FAIL"
    rep = ["# Target Review Gate (GATE 2)", "", f"Verdict: **{verdict}**", "",
           f"- Build (`-warnaserror`, Release): {build}", f"- Tests: {tests}",
           f"- Workloads checked against JCL DDs: {', '.join(JOBS)}", f"- C# files scanned: {len(src)}", "",
           "| Severity | Item | Finding |", "|---|---|---|",
           *([f"| {s} | {i} | {m} |" for s, i, m in findings] or ["| - | - | none |"]), ""]
    (BD / "Artefacts" / "Review" / "Target-Review-Gate-Report.md").write_text("\n".join(rep))
    print(f"GATE 2: {verdict} (build {build}; tests {tests}; {len(findings)} findings)")
    for f in findings:
        print(" ", *f)
    return 0 if verdict == "PASS" else 1


if __name__ == "__main__":
    raise SystemExit(main())
