#!/usr/bin/env bash
# ==============================================================================
# run_pipeline.sh — Master CLI Pipeline Runner for Devin IDE
# ==============================================================================
set -e

PHASE="${1:-all}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
export REPO_ROOT="$SCRIPT_DIR"

echo "================================================================================"
echo "    Mainframe Batch to .NET 8 Modernization Pipeline Runner                      "
echo "    Phase requested: $PHASE                                                      "
echo "================================================================================"

case "$PHASE" in
    discovery|01)
        echo ">>> [Phase 1] Running Discovery & Business Rules Extraction..."
        python3 "$SCRIPT_DIR/run_pipeline.py" --phase discovery
        ;;
    gate1|02)
        echo ">>> [Phase 2] Checking Discovery Review Gate (GATE 1)..."
        python3 "$SCRIPT_DIR/run_pipeline.py" --phase gate1
        ;;
    planning|03)
        echo ">>> [Phase 3] Generating Canonical Batch Intermediate Representation (IR)..."
        python3 "$SCRIPT_DIR/run_pipeline.py" --phase planning
        ;;
    db|04)
        echo ">>> [Phase 4] Verifying Database DDL & Schema Provisioning..."
        python3 "$SCRIPT_DIR/run_pipeline.py" --phase db
        ;;
    build|05)
        echo ">>> [Phase 5] Compiling Modernized .NET 8 Batch Solution..."
        dotnet build "$SCRIPT_DIR/dotnet/CardDemo.Modernized.sln"
        ;;
    gate2|06)
        echo ">>> [Phase 6] Checking Build Review Gate (GATE 2)..."
        dotnet build "$SCRIPT_DIR/dotnet/CardDemo.Modernized.sln" --warnaserror
        echo "[GATE 2 PASS] Clean compilation with 0 warnings and 0 errors."
        ;;
    test|validation|07)
        echo ">>> [Phase 7] Executing Test Pyramid & 5-Level Batch Reconciliation..."
        dotnet run --project "$SCRIPT_DIR/dotnet/tests/CardDemo.UnitTests/"
        dotnet run --project "$SCRIPT_DIR/dotnet/tests/CardDemo.ContractTests/"
        dotnet run --project "$SCRIPT_DIR/dotnet/tests/CardDemo.Reconciliation/"
        ;;
    audit|gate3|08)
        echo ">>> [Phase 8] Running Quality, Security & Traceability Audit (GATE 3)..."
        python3 "$SCRIPT_DIR/run_pipeline.py" --phase audit
        ;;
    run|host)
        echo ">>> Executing All 3 Modernized Batch Jobs on .NET Host..."
        dotnet run --project "$SCRIPT_DIR/dotnet/src/CardDemo.Host/"
        ;;
    all)
        echo ">>> Executing Full End-to-End Modernization Pipeline in Devin..."
        dotnet build "$SCRIPT_DIR/dotnet/CardDemo.Modernized.sln"
        dotnet run --project "$SCRIPT_DIR/dotnet/tests/CardDemo.UnitTests/"
        dotnet run --project "$SCRIPT_DIR/dotnet/tests/CardDemo.ContractTests/"
        dotnet run --project "$SCRIPT_DIR/dotnet/tests/CardDemo.Reconciliation/"
        dotnet run --project "$SCRIPT_DIR/dotnet/src/CardDemo.Host/"
        echo "================================================================================"
        echo "    [SUCCESS] FULL BATCH MODERNIZATION PIPELINE COMPLETED SUCCESSFULLY!        "
        echo "================================================================================"
        ;;
    *)
        echo "Unknown phase: $PHASE"
        echo "Usage: ./run_pipeline.sh [discovery|gate1|planning|db|build|gate2|test|audit|run|all]"
        exit 1
        ;;
esac
