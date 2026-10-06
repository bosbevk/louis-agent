#!/usr/bin/env bash
# Runs in a container after the orchestrator: the fix branches it produced and the latest run summary from the
# agent communications log.
set -euo pipefail

cd /demo/order-service
echo "Fix branches in .demo/order-service (each made from main, committed, not pushed):"
git for-each-ref refs/heads/fix --format='  %(refname:short)  %(objectname:short) %(authorname): %(subject)'

comms=/demo/agent-comms.md
if [ -f "$comms" ]; then
    echo
    # The last "## Run summary" section: one row per error, then the branches ready to merge.
    awk '/^## Run summary/ { buffer = "" } { buffer = buffer $0 "\n" } END { printf "%s", buffer }' "$comms" \
        | sed -n '/^## Run summary/,$p'
fi
