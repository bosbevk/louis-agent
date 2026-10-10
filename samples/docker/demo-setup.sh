#!/usr/bin/env bash
# Runs in the demo-setup container (docker/docker-compose.demo.yml): a fresh order-service repository in
# /demo/order-service, then one round of production traffic whose exceptions land in logs/errors.jsonl.
# DEMO_RESET=0 keeps /demo as it is, so a cut-short run can be resumed.
set -euo pipefail

src=/samples/order-service
repo=/demo/order-service

if [ "${DEMO_RESET:-1}" != "1" ]; then
    [ -d "$repo/.git" ] || { echo "Nothing to resume: run the demo once without -Resume first." >&2; exit 1; }
    echo "Resuming: /demo is kept as it is."
    exit 0
fi

echo "=== 1. Fresh copy of order-service in /demo/order-service ==="
rm -rf "$repo" /demo/orchestrator-state /demo/agent-comms.md /demo/logs
mkdir -p "$repo"
cp -r "$src/." "$repo/"
find "$repo" -type d \( -name bin -o -name obj -o -name logs \) -prune -exec rm -rf {} +
# The host checkout may have Windows line endings; the demo repository is LF throughout so every diff shows only
# what an agent changed.
find "$repo" -type f \( -name '*.cs' -o -name '*.csproj' -o -name '*.slnx' -o -name '*.csv' -o -name '*.txt' \
    -o -name '*.md' -o -name '.gitignore' \) -exec sed -i 's/\r$//' {} +

cd "$repo"
git init -q -b main
git config core.autocrlf false
git add .
git -c user.name=demo-setup -c user.email=demo@localhost commit -q -m "order-service as deployed"

# DEMO_REQUESTS picks the traffic file in data/ (run-demo.ps1 -Short: requests-short.txt, five errors).
requests="${DEMO_REQUESTS:-requests.txt}"
case "$requests" in
    *[!A-Za-z0-9._-]* | .*) echo "DEMO_REQUESTS must be a file name in data/, got '$requests'." >&2; exit 1 ;;
esac
[ -f "data/$requests" ] || { echo "No traffic file data/$requests." >&2; exit 1; }

echo "=== 2. Production traffic (data/$requests) ==="
dotnet run --project src/OrderService -- serve "data/$requests"
[ -s logs/errors.jsonl ] || { echo "The traffic run logged no errors; nothing to demo." >&2; exit 1; }
echo "$(wc -l < logs/errors.jsonl) exceptions logged to logs/errors.jsonl"
