#!/usr/bin/env python3
"""Phase 7 - five-level reconciliation of the .NET target against the legacy golden files.

L1 record counts, L2 key sets, L3 field-level values (repository comparator),
L4 control totals of every numeric field, L5 run control (return codes + SYSOUT).
Reads work/parity-dotnet/<chain>/<case> (written by tools/parity_dotnet.sh).
"""
from __future__ import annotations

import json
import sys
from collections import defaultdict
from decimal import Decimal
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT))
from tools.parity.compare import compare_case  # noqa: E402
from tools.parity.copybook import decode_record, parse_copybook  # noqa: E402

OUT = ROOT / "batch-dotnet" / "Artefacts" / "Reports"
WORKLOADS = {"POSTTRAN": "posting", "TRANREPT": "posting", "INTCALC": "interest"}
OWNED = {"TRANREPT": ("TRANREPT.G", "TRANREPT."), "INTCALC": ("", "INTCALC."), "POSTTRAN": ("", "POSTTRAN.")}


def records(path: Path, lrecl: int) -> list[bytes]:
    if not path.exists():
        return []
    return [line.encode("latin-1").ljust(lrecl) for line in path.read_text(encoding="latin-1").splitlines()]


def owns(workload: str, name: str) -> bool:
    if workload == "TRANREPT":
        return "TRANREPT" in name
    return "TRANREPT" not in name


def reconcile_case(workload: str, chain: str, case: str) -> dict:
    expected = ROOT / "fixtures" / chain / case / "expected"
    actual = ROOT / "work" / "parity-dotnet" / chain / case
    manifest = json.loads((expected / "manifest.json").read_text())
    diffs, _ = compare_case(chain, case, actual)
    levels = {f"L{i}": {"checks": 0, "failures": 0} for i in range(1, 6)}
    totals = {}
    for dsn, meta in sorted(manifest.items()):
        if not owns(workload, dsn):
            continue
        exp = records(expected / "datasets" / f"{dsn}.txt", meta["lrecl"])
        act = records(actual / "datasets" / f"{dsn}.txt", meta["lrecl"])
        levels["L1"]["checks"] += 1
        levels["L1"]["failures"] += len(exp) != len(act)
        if meta["layout"] == "text":
            continue
        fields = [f for f in parse_copybook(meta["layout"]) if not f.is_filler]
        key = fields[0].name
        levels["L2"]["checks"] += 1
        ek = [decode_record(meta["layout"], r)[key] for r in exp]
        ak = [decode_record(meta["layout"], r)[key] for r in act]
        levels["L2"]["failures"] += ek != ak
        sums = {"expected": defaultdict(Decimal), "actual": defaultdict(Decimal)}
        for side, recs in (("expected", exp), ("actual", act)):
            for r in recs:
                for name, value in decode_record(meta["layout"], r).items():
                    if isinstance(value, (Decimal, int)) and not isinstance(value, bool):
                        sums[side][name] += Decimal(value)
        for name in sums["expected"]:
            levels["L4"]["checks"] += 1
            levels["L4"]["failures"] += sums["expected"][name] != sums["actual"][name]
        totals[dsn] = {k: str(v) for k, v in sums["actual"].items() if k.endswith(("AMT", "BAL", "CREDIT", "DEBIT"))}
    mine = [d for d in diffs if owns(workload, d.get("where", "") + d.get("dataset", ""))]
    levels["L3"]["checks"] = sum(1 for d, m in manifest.items() if owns(workload, d))
    levels["L3"]["failures"] = sum(1 for d in mine if "SYSOUT" not in str(d) and "rc.json" not in str(d))
    sysouts = [p for p in (expected / "sysout").glob("*.txt") if p.stem.startswith(workload)]
    levels["L5"]["checks"] = len(sysouts) + 1
    levels["L5"]["failures"] = sum(1 for d in diffs if "SYSOUT " + workload in str(d) or "rc.json" in str(d))
    return {"case": f"{chain}/{case}", "levels": levels, "control_totals": totals,
            "passed": all(v["failures"] == 0 for v in levels.values())}


def main() -> int:
    OUT.mkdir(parents=True, exist_ok=True)
    ok = True
    for workload, chain in WORKLOADS.items():
        cases = sorted(p.name for p in (ROOT / "fixtures" / chain).iterdir() if p.is_dir())
        results = [reconcile_case(workload, chain, c) for c in cases]
        (OUT / f"Reconciliation-Report-{workload}.json").write_text(json.dumps(results, indent=1) + "\n")
        lines = [f"# Reconciliation Report - {workload}", "",
                 "Five-level reconciliation of the .NET 8 target against the legacy GnuCOBOL golden files "
                 "(`fixtures/{chain}/*/expected`).".format(chain=chain), "",
                 "| Case | L1 counts | L2 keys | L3 fields | L4 control totals | L5 RC + SYSOUT | Result |",
                 "|---|---|---|---|---|---|---|"]
        for r in results:
            cells = [f"{v['checks'] - v['failures']}/{v['checks']}" for v in r["levels"].values()]
            lines.append(f"| {r['case']} | " + " | ".join(cells) + f" | {'PASS' if r['passed'] else 'FAIL'} |")
            ok &= r["passed"]
        verdict = "PASS" if all(r["passed"] for r in results) else "FAIL"
        lines += ["", f"Verdict: **{verdict}** ({len(results)} cases)", ""]
        (OUT / f"Reconciliation-Report-{workload}.md").write_text("\n".join(lines))
        print(f"{workload}: {verdict} ({len(results)} cases)")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
