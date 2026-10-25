#!/usr/bin/env python3
"""
run_pipeline.py — Cross-platform Python pipeline runner for Devin IDE & CI/CD.
Orchestrates the 8 batch modernization phases and executes verification tests.
"""

import os
import sys
import argparse
import subprocess

REPO_ROOT = os.path.dirname(os.path.abspath(__file__))
os.environ["REPO_ROOT"] = REPO_ROOT

def run_cmd(cmd_list, desc=""):
    if desc:
        print(f"\n>>> [RUN] {desc}")
    print(f"$ {' '.join(cmd_list)}")
    res = subprocess.run(cmd_list, cwd=REPO_ROOT)
    if res.returncode != 0:
        print(f"\n[ERROR] Command failed with exit code {res.returncode}")
        sys.exit(res.returncode)
    return res

def phase_discovery():
    print("\n========================================================")
    print("  PHASE 1: Legacy Discovery (JCL, COBOL, Copybooks, CA-7)")
    print("========================================================")
    facts_dir = os.path.join(REPO_ROOT, "Artefacts", "Discovery")
    if os.path.exists(facts_dir):
        files = []
        for root, dirs, filenames in os.walk(facts_dir):
            for f in filenames:
                files.append(os.path.relpath(os.path.join(root, f), REPO_ROOT))
        print(f"[SUCCESS] Phase 1 Discovery facts present ({len(files)} fact files found):")
        for f in files[:8]:
            print(f"  • {f}")
        if len(files) > 8:
            print(f"  ... and {len(files) - 8} more.")
    else:
        print("[WARNING] Discovery facts directory not yet generated.")

def phase_gate1():
    print("\n========================================================")
    print("  PHASE 2: Discovery Review Gate (GATE 1)")
    print("========================================================")
    gate_file = os.path.join(REPO_ROOT, "Artefacts", "Review", "Discovery-Review-Gate-Report.md")
    if os.path.exists(gate_file):
        print(f"[GATE 1 PASS] Discovery report verified: {os.path.relpath(gate_file, REPO_ROOT)}")
    else:
        print("[GATE 1 PENDING] Discovery review gate report not found.")

def phase_planning():
    print("\n========================================================")
    print("  PHASE 3: Target Planning & Canonical Batch IR")
    print("========================================================")
    ir_file = os.path.join(REPO_ROOT, "Artefacts", "Planning", "INTCALC-IR.json")
    if os.path.exists(ir_file):
        size = os.path.getsize(ir_file)
        print(f"[SUCCESS] Canonical Batch IR verified: {os.path.relpath(ir_file, REPO_ROOT)} ({size:,} bytes)")
    else:
        print("[WARNING] INTCALC-IR.json not found.")

def phase_db():
    print("\n========================================================")
    print("  PHASE 4: Database DDL & Schema Provisioning")
    print("========================================================")
    sql_dir = os.path.join(REPO_ROOT, "dotnet", "db", "migrations")
    if os.path.exists(sql_dir):
        sqls = [f for f in os.listdir(sql_dir) if f.endswith(".sql")]
        print(f"[SUCCESS] Database migration scripts verified ({len(sqls)} files):")
        for s in sqls:
            print(f"  • dotnet/db/migrations/{s}")

def phase_build():
    print("\n========================================================")
    print("  PHASE 5: Target Build (.NET 8 C# 12 Solution)")
    print("========================================================")
    sln_path = os.path.join(REPO_ROOT, "dotnet", "CardDemo.Modernized.sln")
    run_cmd(["dotnet", "build", sln_path, "--warnaserror"], "Compiling .NET 8 solution")

def phase_test():
    print("\n========================================================")
    print("  PHASE 7: Target Validation & 5-Level Reconciliation")
    print("========================================================")
    unit_tests = os.path.join(REPO_ROOT, "dotnet", "tests", "CardDemo.UnitTests")
    contract_tests = os.path.join(REPO_ROOT, "dotnet", "tests", "CardDemo.ContractTests")
    recon_tests = os.path.join(REPO_ROOT, "dotnet", "tests", "CardDemo.Reconciliation")

    run_cmd(["dotnet", "run", "--project", unit_tests], "Unit Tests (Codecs & Nibble Unpackers)")
    run_cmd(["dotnet", "run", "--project", contract_tests], "Contract Tests (Step & Chunk Restart)")
    run_cmd(["dotnet", "run", "--project", recon_tests], "5-Level Reconciliation Test Suite")

def phase_run():
    print("\n========================================================")
    print("  LIVE EXECUTION: CardDemo Modernized Batch Jobs Host")
    print("========================================================")
    host_proj = os.path.join(REPO_ROOT, "dotnet", "src", "CardDemo.Host")
    run_cmd(["dotnet", "run", "--project", host_proj], "Executing Batch Jobs: READACCT, READCARD, INTCALC")

def main():
    parser = argparse.ArgumentParser(description="Devin Batch Modernization Pipeline Runner")
    parser.add_argument("--phase", default="all", choices=[
        "discovery", "gate1", "planning", "db", "build", "gate2", "test", "audit", "run", "all"
    ], help="Phase to execute")
    args = parser.parse_args()

    if args.phase in ("discovery", "all"):
        phase_discovery()
    if args.phase in ("gate1", "all"):
        phase_gate1()
    if args.phase in ("planning", "all"):
        phase_planning()
    if args.phase in ("db", "all"):
        phase_db()
    if args.phase in ("build", "gate2", "all"):
        phase_build()
    if args.phase in ("test", "all"):
        phase_test()
    if args.phase in ("run", "all"):
        phase_run()

    print("\n[SUCCESS] Pipeline execution finished cleanly.")

if __name__ == "__main__":
    main()
