#!/usr/bin/env bash
# Runs the batch-to-.NET modernization pipeline phase by phase (one agent persona per phase).
# Each phase is deterministic and re-runnable; a failing gate stops the pipeline.
set -euo pipefail
BD="$(cd "$(dirname "$0")" && pwd)"; ROOT="$(dirname "$BD")"
export DOTNET="${DOTNET:-$(command -v dotnet || echo "$HOME/.dotnet/dotnet")}"
phase() { printf '\n=== %s ===\n' "$1"; }
phase "Phase 1 - Legacy discovery (Discovery Analyst)";        python3 "$BD/tools/discovery/extract_facts.py"
phase "Phase 2 - Discovery review gate, GATE 1 (Reviewer)";      python3 "$BD/tools/discovery/review_gate.py"
phase "Phase 3 - Target planning (Architect)";                   python3 "$BD/tools/planning/gen_planning.py"
phase "Phase 4 - Data layouts + PostgreSQL DDL (Data Engineer)"; python3 "$BD/tools/codegen/gen_layouts.py"
phase "Phase 5 - Target build (.NET Developer)";                 "$DOTNET" build "$BD/CardDemo.Batch.sln" -c Release --nologo -v q -warnaserror
phase "Phase 6 - Target review gate, GATE 2 (Reviewer)";         python3 "$BD/tools/review/target_gate.py"
phase "Phase 7 - Validation: parity + reconciliation (QA)";      "$BD/tools/parity_dotnet.sh"; python3 "$BD/tools/validation/reconcile.py"
phase "Phase 8 - Quality & security audit, GATE 3 (Auditor)";    python3 "$BD/tools/audit/audit.py"
