<#
.SYNOPSIS
    End-to-end demo, entirely in Docker: an orchestrator agent watches order-service, triages its exceptions with one
    runbook per API method, has louis-agent fix the ten auto-fixable ones (one branch each), and verifies every commit.

.DESCRIPTION
    Runs docker/docker-compose.demo.yml; nothing but docker runs on the host.
    1. demo-setup: a fresh order-service repository in .demo/order-service (branch main), then production traffic
       (data/requests.txt) that logs thirteen exceptions to logs/errors.jsonl.
    2. demo-api: louis-agent.api and the web app on http://127.0.0.1:5081, with that repository as its workspace.
    3. orchestrator: triages every new error once; every message between the agents goes to .demo/agent-comms.md.
    4. Leaves demo-api running: open http://127.0.0.1:5081, go to Branches, review each fix and merge it.

    Commits only - nothing is pushed (the demo repository has no remote).
    Needs Docker Desktop and ANTHROPIC_API_KEY in config/.env.secrets. Takes about 15 minutes; the first run also
    builds the images.

.PARAMETER Port
    Host port for louis-agent.api and its web app (default 5081, so it doesn't clash with the main api on 5080).

.PARAMETER StopApi
    Stop demo-api when the orchestrator is done instead of leaving it running for the web app.

.PARAMETER Resume
    Keep .demo as it is (repository, branches, logs): only (re)start demo-api and run the orchestrator over the errors
    it hasn't triaged yet, e.g. after a run was cut short. The comms log gets a new run section.

.PARAMETER ApiOnly
    Keep .demo as it is and only (re)start demo-api for the web app; the orchestrator doesn't run.

.PARAMETER Short
    Replay data/requests-short.txt instead: five errors (four fixes and one escalation) instead of thirteen, for a
    quicker, cheaper run.
#>
param(
    [int]$Port = 5081,
    [switch]$StopApi,
    [switch]$Resume,
    [switch]$ApiOnly,
    [switch]$Short
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$composeArgs = @('compose', '-f', (Join-Path $repo 'docker/docker-compose.demo.yml'))
$envFile = Join-Path $repo 'config/.env'
if (Test-Path $envFile) { $composeArgs += @('--env-file', $envFile) }
$env:DEMO_PORT = $Port
$env:DEMO_REQUESTS = if ($Short) { 'requests-short.txt' } else { 'requests.txt' }
$url = "http://127.0.0.1:$Port"

function Step([string]$text) { Write-Host "`n=== $text ===" -ForegroundColor Cyan }

function Compose([string[]]$arguments) {
    & docker @composeArgs @arguments
    if ($LASTEXITCODE -ne 0) { throw "docker compose $($arguments -join ' ') failed with exit code $LASTEXITCODE" }
}

Step "Building the images (louis-agent-api, louis-agent-orchestrator)"
Compose @('build', 'demo-api', 'orchestrator')

if ($Resume -or $ApiOnly) {
    $env:DEMO_RESET = '0'
    Compose @('run', '--rm', 'demo-setup')
}
else {
    # demo-api has the old repository as its workspace; stop it before that repository is replaced.
    Compose @('rm', '--stop', '--force', 'demo-api')
    $env:DEMO_RESET = '1'
    Compose @('run', '--rm', 'demo-setup')
}

Step "Starting demo-api: louis-agent.api and the web app on $url"
Compose @('up', '-d', '--wait', 'demo-api')

if (-not $ApiOnly) {
    Step "Orchestrator: triage every new error"
    Compose @('run', '--rm', 'orchestrator')

    Step "Result"
    Compose @('run', '--rm', '--entrypoint', 'bash', 'demo-setup', '/samples/docker/demo-summary.sh')
    Write-Host "`nEverything the agents said to each other: $(Join-Path $repo '.demo/agent-comms.md')"
}

if ($StopApi) {
    Compose @('stop', 'demo-api')
}
else {
    Write-Host "`ndemo-api is running: open $url, go to the Branches tab, review each fix and merge it." -ForegroundColor Green
    Write-Host "Stop it with: docker compose -f docker/docker-compose.demo.yml stop demo-api"
}
