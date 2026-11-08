#!/usr/bin/env bash
# Records the coding agent fixing the invoice matcher (F120), for the site's /coding-agent page.
# One paid run on Haiku, a few cents: ask Louis first. Run from the repo root, with the API image built:
#   docker compose -f demos/louis-agent/docker/docker-compose.yml --env-file demos/louis-agent/config/.env build api
#   bash demos/louis-agent/recordings/record.sh
# The agent sees only a fresh git copy of the sample, never this repo; the container gets the model key and nothing
# else from config/ (the host loads every .env file in CONFIG_DIRECTORY, so config/ itself is never mounted).
set -euo pipefail

# A Windows path under Git Bash, so Docker Desktop can mount it; as it is on Linux.
work="$(mktemp -d)"
work="$(cygpath -m "$work" 2>/dev/null || echo "$work")"
port="${RECORD_PORT:-5078}"
out="demos/louis-agent/web/louis-agent.code-review/wwwroot/recordings/coding-session.sse"
question="The bank payments are not matching their invoices, and the tests show it. Find out why, fix the matcher, and run the tests again."

# The sample as its own repository, so the agent's git tools see one clean commit.
mkdir -p "$work/workspace" "$work/config" "$work/skills" "$work/logs"
cp -r demos/louis-agent/samples/invoice-matcher/. "$work/workspace/"
printf "bin/\nobj/\n" > "$work/workspace/.gitignore"
git -C "$work/workspace" init -q -b main
git -C "$work/workspace" -c user.name=Sample -c user.email=sample@example.com add -A
git -C "$work/workspace" -c user.name=Sample -c user.email=sample@example.com commit -q -m "Match bank payments to invoices"
cp demos/louis-agent/config/prices.json demos/louis-agent/config/rates.json "$work/config/"

# The API's own image, open on localhost only, with the model key passed through and nothing else.
ANTHROPIC_API_KEY="$(grep '^ANTHROPIC_API_KEY=' demos/louis-agent/config/.env.secrets | cut -d= -f2-)"
export ANTHROPIC_API_KEY
MSYS_NO_PATHCONV=1 docker run -d --rm --name louis_agent_recording -p "127.0.0.1:$port:8080" \
  -e ANTHROPIC_API_KEY -e LLM_PROVIDER=anthropic -e LLM_MODEL=claude-haiku-5-5 -e AGENT_AUTH=off \
  -e USAGE_IN_CHAT=true -e HOST_TIME_ZONE=Africa/Johannesburg -e HOST_CULTURE=en-ZA \
  -e WORKSPACE_ROOT=/workspace -e CONFIG_DIRECTORY=/config -e PRICES_FILE=/config/prices.json \
  -e RATES_FILE=/config/rates.json -e SKILLS_DIRECTORY=/skills -e LOG_DIRECTORY=/logs \
  -e GIT_AUTHOR_NAME=Agent -e GIT_AUTHOR_EMAIL=agent@example.com \
  -e GIT_COMMITTER_NAME=Agent -e GIT_COMMITTER_EMAIL=agent@example.com \
  -v "$work/workspace:/workspace" -v "$work/skills:/skills" -v "$work/logs:/logs" -v "$work/config:/config:ro" \
  louis-agent-api:latest >/dev/null
unset ANTHROPIC_API_KEY
trap 'docker stop louis_agent_recording >/dev/null' EXIT
until curl -fs "http://127.0.0.1:$port/health" >/dev/null; do sleep 2; done

# One session with the tools a coding task needs, one message, the stream saved as it came.
session=$(curl -s -X POST "http://127.0.0.1:$port/v1/sessions" -H "content-type: application/json" \
  -d '{"toolsets":["files","git","dotnet"],"time_zone":"Africa/Johannesburg"}' |
  sed -n 's/.*"session_id":"\([^"]*\)".*/\1/p')
curl -s -N -X POST "http://127.0.0.1:$port/v1/sessions/$session/messages" -H "content-type: application/json" \
  -d "{\"message\":\"$question\"}" > "$out"

echo "Recorded $(grep -c '^event:' "$out") events to $out. Read it for anything private before committing."
git -C "$work/workspace" diff --stat
