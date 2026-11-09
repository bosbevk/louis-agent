#!/usr/bin/env bash
# Records one real triage of the order service (F106.3.4), for the site's /try page, until F106.3.3's workflow runs it
# in code. One paid run on Haiku, a few rand: ask Louis first. Run from the repo root, with the API image built from main:
#   docker compose -f demos/louis-agent/docker/docker-compose.yml --env-file demos/louis-agent/config/.env build api
#   bash demos/louis-agent/recordings/record-triage.sh
# The script does what the workflow will: it collects the error, asks each agent in turn with the brief the workflow
# will hand it, checks the fix in code, and records those steps of its own in the run beside the agents' typed ones. The
# agents see only a fresh git clone of the sample; the container gets the model key and nothing else from config/.
set -euo pipefail

work="$(mktemp -d)"
work="$(cygpath -m "$work" 2>/dev/null || echo "$work")"
port="${RECORD_PORT:-5078}"
api="http://127.0.0.1:$port"
out="demos/louis-agent/web/louis-agent.code-review/wwwroot/recordings"
run="triage-order-total"
branch="fix/order-total-retired-code"

# The department's repository: a fresh clone of the sample, one commit, its error made by the service itself.
mkdir -p "$work/repo" "$work/config" "$work/runs" "$work/people" "$work/skills" "$work/logs" "$work/streams"
cp -r demos/louis-agent/samples/order-service/. "$work/repo/"
printf "bin/\nobj/\nlogs/\n" > "$work/repo/.gitignore"
git -C "$work/repo" init -q -b main
git -C "$work/repo" -c user.name=Sample -c user.email=sample@example.com add -A
git -C "$work/repo" -c user.name=Sample -c user.email=sample@example.com commit -q -m "The order service"
# The service reads its data relative to where it runs, so it runs from the repository.
(cd "$work/repo" && ERROR_LOG_PATH="$work/errors.jsonl" dotnet run --project src/OrderService -- order-total 1003 \
  >/dev/null) || true
# The error as the log wrote it, its stack's paths made the repository's own, so no path of this machine travels.
error="$(python -I -c '
import json, re, sys
row = json.loads(open(sys.argv[1], encoding="utf-8").readline())
row.pop("timestamp", None)
row["stack_trace"] = re.sub(r"in \S*?order-service[\\/]", "in ", row["stack_trace"]).replace("\\", "/").replace("\r", "")
print(json.dumps(row))' "$work/errors.jsonl")"
runbook="$(cat demos/louis-agent/hosts/louis-agent.orchestrator/Skills/order-service/order-total.md)"
cp demos/louis-agent/config/prices.json demos/louis-agent/config/rates.json "$work/config/"
printf "departments:\n  - name: code\n    repository: /workspace\n    error_log: /workspace/logs/errors.jsonl\n    people: /people\n  - name: ops\n    repository: /ops\n    error_log: /ops/logs/errors.jsonl\n    people: /people\n" > "$work/config/departments.yaml"

# A step the code takes, saved in the triage pack's own format beside the agents' typed steps.
step() { # number agent kind summary why [outcome]
  python -I - "$work/runs/$run.jsonl" "$run" "$@" <<'PY'
import json, sys, datetime
path, run, number, agent, kind, summary, why = sys.argv[1:8]
outcome = sys.argv[8] if len(sys.argv) > 8 else None
row = {"run_id": run, "number": int(number), "agent": agent, "kind": kind, "summary": summary, "why": why,
       "at": datetime.datetime.now(datetime.timezone.utc).isoformat().replace("+00:00", "Z")}
if outcome: row["outcome"] = outcome
open(path, "a", encoding="utf-8").write(json.dumps(row) + "\n")
PY
}
steps() { wc -l < "$work/runs/$run.jsonl" | tr -d ' '; }

# The API's own image, open on localhost only, with the model key passed through and nothing else.
ANTHROPIC_API_KEY="$(grep '^ANTHROPIC_API_KEY=' demos/louis-agent/config/.env.secrets | cut -d= -f2-)"
export ANTHROPIC_API_KEY
MSYS_NO_PATHCONV=1 docker run -d --rm --name louis_agent_triage_recording -p "127.0.0.1:$port:8080" \
  -e ANTHROPIC_API_KEY -e LLM_PROVIDER=anthropic -e LLM_MODEL=claude-haiku-5-5 -e AGENT_AUTH=off \
  -e USAGE_IN_CHAT=true -e HOST_TIME_ZONE=Africa/Johannesburg -e HOST_CULTURE=en-ZA \
  -e WORKSPACE_ROOT=/workspace -e CONFIG_DIRECTORY=/config -e PRICES_FILE=/config/prices.json \
  -e RATES_FILE=/config/rates.json -e SKILLS_DIRECTORY=/skills -e LOG_DIRECTORY=/logs \
  -e TRIAGE_RUNS=/runs -e TRIAGE_PEOPLE=/people -e TRIAGE_DEPARTMENTS=/config/departments.yaml \
  -e GIT_CONFIG_COUNT=1 -e GIT_CONFIG_KEY_0=core.autocrlf -e GIT_CONFIG_VALUE_0=true \
  -e GIT_AUTHOR_NAME=Coder -e GIT_AUTHOR_EMAIL=coder@example.com \
  -e GIT_COMMITTER_NAME=Coder -e GIT_COMMITTER_EMAIL=coder@example.com \
  -v "$work/repo:/workspace" -v "$work/runs:/runs" -v "$work/people:/people" -v "$work/skills:/skills" \
  -v "$work/logs:/logs" -v "$work/config:/config:ro" louis-agent-api:latest >/dev/null
unset ANTHROPIC_API_KEY
trap 'docker stop louis_agent_triage_recording >/dev/null' EXIT
until curl -fs "$api/health" >/dev/null; do sleep 2; done

# One agent's turn: a session as that agent, one message, the stream saved; prints the answer's text.
ask() { # name body message
  local session
  session=$(curl -s -X POST "$api/v1/sessions" -H "content-type: application/json" -d "$2" |
    sed -n 's/.*"session_id":"\([^"]*\)".*/\1/p')
  python -I -c 'import json,sys; print(json.dumps({"message": sys.argv[1]}))' "$3" > "$work/body.json"
  curl -s -N -X POST "$api/v1/sessions/$session/messages" -H "content-type: application/json" \
    --data-binary "@$work/body.json" > "$work/streams/$1.sse"
  python -I -c '
import json, sys
text = []
for line in open(sys.argv[1], encoding="utf-8"):
    if line.startswith("data: ") and "\"text_delta\"" in line:
        text.append(json.loads(line[6:])["delta"]["text"])
print("".join(text).strip())' "$work/streams/$1.sse"
}
role() { printf '{"identity":"triage","role":"triage-%s","depth":1,"toolsets":[%s],"time_zone":"Africa/Johannesburg"}' "$1" "$2"; }
manager='{"identity":"triage","toolsets":["triage"],"time_zone":"Africa/Johannesburg"}'

step 1 code collected "Error 1 of the order service read from its log." "order-total 1003 failed with a KeyNotFoundException."
brief="Run id: $run.

The error, as the order service reported it:

$error"

ask sorter "$(role sorter '"triage"')" "$brief

The departments:
- code: the order service's code (this repository). The default when nothing else fits.
- ops: the hosting configuration the services run on.

Sort it." >/dev/null
ask manager-1 "$manager" "$brief

The sorter says it is the code department's. The method's runbook:

$runbook

Decide what this case gets." >/dev/null
ask investigator "$(role investigator '"triage","files","git","dotnet"')" "$brief

The department's repository is your workspace. The method's runbook:

$runbook

Find the cause and report it." >/dev/null
# The investigator's typed report, as the workflow will read it from the run: where, the cause, the area, how sure.
investigation="$(python -I -c '
import json, sys
rows = [json.loads(l) for l in open(sys.argv[1], encoding="utf-8")]
found = [r for r in rows if r["kind"] == "investigated"]
r = json.loads(found[-1]["result"]) if found and found[-1].get("result") else {}
print(" ".join(f"{k}: {v}." for k, v in r.items()))' "$work/runs/$run.jsonl")"
ask manager-2 "$manager" "$brief

The method's runbook:

$runbook

The investigator reported: $investigation

Decide what this case gets now." >/dev/null

# The coder is the coding identity, on its own branch; the code then checks its work and records both.
coder="$(ask coder '{"toolsets":["files","git","dotnet"],"time_zone":"Africa/Johannesburg"}' "In the repository in your workspace, the order service's GET /orders/{id}/total fails for order 1003: $investigation

The runbook's expected behaviour: an unknown or retired discount code gives no discount (the total is the subtotal) instead of failing. Totals for orders with WELCOME10, VIP20 or no code must not change.

Fix it on a new branch named $branch, with a test that proves it, run the tests, and commit on that branch. Never push.")"
# The coding agent leaves commits to a person, so the code commits the coder's change on its branch, as the workflow
# will. Without a change on the branch there is no fix to record, so the recording stops rather than tell a visitor
# something that didn't happen.
current="$(git -C "$work/repo" rev-parse --abbrev-ref HEAD)"
if [ "$current" = "$branch" ] && [ -n "$(git -C "$work/repo" status --porcelain)" ]; then
  git -C "$work/repo" add -A
  git -C "$work/repo" -c user.name=Coder -c user.email=coder@example.com commit -q \
    -m "Fix order-total for a retired discount code" -m "The coder's change, committed by the triage workflow."
fi
ahead="$(git -C "$work/repo" rev-list --count "main..$branch" 2>/dev/null || echo 0)"
if [ "$ahead" -lt 1 ]; then
  echo "The coder left no change on $branch, so there is nothing to record. Its answer:" >&2
  echo "$coder" | tail -c 800 >&2
  exit 1
fi
commit="$(git -C "$work/repo" log -1 --format='%h %s' "$branch")"
# Its reason is its own words about its tests: the sentence where the test reproduced the bug and the one where they
# all pass, so a visitor reads what it proved, not where the answer happened to stop. The answer streams sentences back
# to back, sometimes with no space after the full stop ("Now the fix:The diff"), so it is split after . ! ? or : before
# a capital letter; of the sentences about its tests, the first and the last are kept, word for word.
proved="$(sed -E 's/([.!?:])[[:space:]]*([A-Z])/\1\n\2/g' <<< "$coder" \
  | grep -E 'reproduces|fails with|tests? pass' \
  | awk '{ line[NR] = $0 } END { if (NR == 1) printf "%s", line[1]; if (NR > 1) printf "%s %s", line[1], line[NR] }')"
step "$(( $(steps) + 1 ))" coder fixed "The coder fixed it on $branch, with a test, and the code committed it (${commit%% *})." \
  "$proved"
git -C "$work/repo" checkout -q "$branch"
tests="$(dotnet test "$work/repo/OrderService.slnx" 2>&1 | grep -E 'Passed!|Failed!' | tail -1 | sed 's/ - .*//')"
replay="$(cd "$work/repo" && ERROR_LOG_PATH="$work/replay-errors.jsonl" dotnet run --project src/OrderService -- \
  order-total 1003 2>&1 | tail -1)"
step "$(( $(steps) + 1 ))" code checked "The code checked the fix: the commit is on $branch, the tests and the request." "$tests; order-total 1003 now answers: $replay"
# The reviewer reads the fix where it is: the branch stays checked out, as the workflow will hand it over.

ask reviewer "$(role reviewer '"triage","files","git"')" "$brief

The cause the investigator found: $investigation

The method's runbook:

$runbook

The coder's fix is the commit $commit on the branch $branch, which is checked out in your workspace; its diff against main is \`git diff main\`. The code's checks passed: $tests; the request now answers.

Review the fix." >/dev/null

# The close follows the reviewer's verdict: happy leaves the fix on its branch for a person to merge; not happy, after
# this one round, goes to a person with the reviewer's reasons (the workflow will give the coder another round first).
happy="$(python -I -c '
import json, sys
rows = [json.loads(l) for l in open(sys.argv[1], encoding="utf-8")]
found = [r for r in rows if r["kind"] == "reviewed"]
print("yes" if found and json.loads(found[-1]["result"]).get("happy") else "no")' "$work/runs/$run.jsonl")"
if [ "$happy" = yes ]; then
  step "$(( $(steps) + 1 ))" code finished "The case is closed: fixed on $branch, waiting for a person to merge it." \
    "The reviewer is happy, and a person merges; nothing is ever pushed." fixed-on-branch
else
  step "$(( $(steps) + 1 ))" code finished "The case goes to a person: the reviewer wasn't happy with the fix." \
    "One round is all this recording runs, so the reviewer's reasons go to a person." raised-to-person
fi

cp "$work/runs/$run.jsonl" "$out/triage-run.jsonl"
mkdir -p "$out/triage-streams" && cp "$work/streams/"*.sse "$out/triage-streams/"
echo "Recorded $(steps) steps of run $run to $out/triage-run.jsonl. Read it, and the streams, for anything private."
git -C "$work/repo" diff --stat main "$branch"
