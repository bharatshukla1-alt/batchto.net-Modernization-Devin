#!/usr/bin/env bash
# Prove the .NET target against the legacy golden files with the repository's comparator.
# usage: batch-dotnet/tools/parity_dotnet.sh [chain] [case]
# CARDDEMO_PG="Host=...;Username=...;Password=...;Database=..." runs the KSDS stores on PostgreSQL instead of files.
set -uo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
DOTNET="${DOTNET:-$(command -v dotnet || echo "$HOME/.dotnet/dotnet")}"
REPORT="$ROOT/work/parity/report-dotnet.md"
mkdir -p "$ROOT/work/parity"; rm -f "$REPORT"
"$DOTNET" build "$ROOT/batch-dotnet/src/CardDemo.Host" -c Release --nologo -v q >/dev/null || exit 2
HOST_DLL="$ROOT/batch-dotnet/src/CardDemo.Host/bin/Release/net8.0/carddemo-batch.dll"
fail=0; n=0
for chain in ${1:-posting interest${CARDDEMO_DB2:+ trantype-update trantype-dynamic}}; do
  for dir in "$ROOT"/fixtures/$chain/${2:-*}/; do
    case=$(basename "$dir"); n=$((n+1))
    out="$ROOT/work/parity-dotnet/$chain/$case"
    "$DOTNET" "$HOST_DLL" run-chain "$chain" "$dir" "$out" --quiet ${CARDDEMO_PG:+--postgres "$CARDDEMO_PG"} ${CARDDEMO_DB2:+--db2 "$CARDDEMO_DB2"} || { echo "$chain/$case: run failed"; fail=1; continue; }
    python3 "$ROOT/tools/parity/compare.py" "$chain" "$case" --actual "$out" --report "$REPORT" --label "${LABEL:-.NET 8 target}" || fail=1
  done
done
echo "cases: $n  report: $REPORT"; exit $fail
