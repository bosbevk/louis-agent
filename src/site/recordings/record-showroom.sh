#!/usr/bin/env bash
# Records the showroom's three conversations (F128.2) for the site's demo pages: a restaurant, a law practice and a
# doctor's practice, each a few turns with its made-up business's agent. A few cents a business on the site's model.
# Run from the repo root, with the site's image built:
#   docker compose -f src/site/docker/docker-compose.yml build
#   bash src/site/recordings/record-showroom.sh            # a dry run: no mail goes out, nothing is saved
#   SEND_MAIL=1 bash src/site/recordings/record-showroom.sh  # the recording: each booking mails LEADS_TO, once
# The site's own image runs on a spare port, open on localhost only, its settings files handed to Docker as they are,
# so no key is read here. Each page's questions are word for word the ones below (ShowroomPagesTests).
set -euo pipefail

port="${RECORD_PORT:-5079}"
out="src/site/louis-agent.site/wwwroot/recordings/showroom"
config="src/site/config"

# A dry run sends no mail (the lead goes to the log) and keeps what it heard in a scratch folder.
mail=(-e RESEND_API_KEY=)
if [ "${SEND_MAIL:-}" = "1" ]; then mail=(); else out="$(mktemp -d)"; fi
mkdir -p "$out"

env_files=(--env-file "$config/.env.anthropic")
for file in "$config/.env" "$config/.env.secrets"; do
  if [ -f "$file" ]; then env_files+=(--env-file "$file"); fi
done

MSYS_NO_PATHCONV=1 docker run -d --rm --name louis_agent_showroom -p "127.0.0.1:$port:8080" "${env_files[@]}" \
  -e AGENT_AUTH=off -e HOST_TIME_ZONE=Africa/Johannesburg -e LEADS_TO=louismarais0@gmail.com \
  -e LEADS_FROM=guide@louis-agent.dev "${mail[@]}" louis-agent-site:latest >/dev/null
trap 'docker stop louis_agent_showroom >/dev/null 2>&1 || true' EXIT
# Ready within two minutes, or say so: a host that stops at start-up is gone, and would be waited on for ever.
for _ in $(seq 60); do
  if curl -fs "http://127.0.0.1:$port/health" >/dev/null; then break; fi
  sleep 2
done
curl -fs "http://127.0.0.1:$port/health" >/dev/null || { echo "The site's host did not start." >&2; exit 1; }

# One session as the business, each turn's stream saved as it came, as {business}-{n}.sse.
record() {
  local business="$1"
  shift
  local session
  session=$(curl -s -X POST "http://127.0.0.1:$port/v1/sessions" -H "content-type: application/json" \
    -d "{\"identity\":\"$business\",\"time_zone\":\"Africa/Johannesburg\"}" |
    sed -n 's/.*"session_id":"\([^"]*\)".*/\1/p')
  local turn=0
  for question in "$@"; do
    turn=$((turn + 1))
    curl -s -N -X POST "http://127.0.0.1:$port/v1/sessions/$session/messages" -H "content-type: application/json" \
      -d "{\"message\":\"$question\"}" > "$out/$business-$turn.sse"
    echo "$business $turn: $(grep -o '"text_delta","text":"[^"]*"' "$out/$business-$turn.sse" | sed 's/.*"text":"//; s/"$//' | tr -d '\n')"
  done
}

record wheatfield-kitchen \
  'Hi, are you open for lunch on Sunday? We are a family of five, with two small kids.' \
  'Lovely. Could we book a table for Sunday at half past twelve?' \
  'Sam Botha, and my email is sam@example.com' \
  'Yes please, send it.'

record koringland-attorneys \
  'Good morning. I need to change my will after my divorce. What should I put in it?' \
  'Okay, I would like a consultation then, on a Thursday morning if possible.' \
  'Anna de Villiers, anna@example.com' \
  'Yes, please send it.'

record graanveld-family-practice \
  'Hi, my son has had a fever since yesterday. What medicine should I give him?' \
  'Can he see a doctor tomorrow morning?' \
  'Jacques Louw, jacques@example.com, it is for my son Pieter.' \
  'Yes please.'

echo "Saved to $out. Read each for anything private before committing."
