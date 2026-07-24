#!/usr/bin/env bash
# Launch the API and the Vite dev server together in one terminal.
#
#   ./scripts/start.sh
#
# Both processes run as children of this script. Ctrl+C (or the terminal
# closing) stops both — nothing is left running in the background.
set -euo pipefail
cd "$(dirname "$0")/.."

# Job control: each background job gets its own process group, so we can
# kill an entire tree (dotnet run -> API, npm -> vite) with one signal.
set -m

pids=()

cleanup() {
  trap - INT TERM EXIT
  echo
  echo "Shutting down..."
  for pid in "${pids[@]}"; do
    kill -TERM -- -"$pid" 2>/dev/null || true
  done
  wait 2>/dev/null || true
  echo "All stopped."
  exit 0
}
trap cleanup INT TERM EXIT

if [ ! -d web/node_modules ]; then
  echo "web/node_modules missing — running npm install..."
  (cd web && npm install)
fi

dotnet run --project src/DmAssist.Api &
pids+=($!)

npm --prefix web run dev &
pids+=($!)

echo
echo "  UI:  http://localhost:5173   <- open this one"
echo "  API: http://localhost:5178   (no page at /, API only)"
echo
echo "  Ctrl+C stops both."
echo

# If either process dies on its own, take the other down too so the
# script never sits there half-alive.
while true; do
  for pid in "${pids[@]}"; do
    if ! kill -0 "$pid" 2>/dev/null; then
      echo "One of the processes exited — stopping the other."
      exit 1
    fi
  done
  sleep 2
done
