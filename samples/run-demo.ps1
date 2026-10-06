<#
.SYNOPSIS
    End-to-end demo: an orchestrator agent watches order-service, triages its exceptions with one runbook per API
    method, has louis-agent fix the ten auto-fixable ones (one branch each), and verifies every commit itself.

.DESCRIPTION
    1. Copies samples/order-service to .demo/order-service and makes it a fresh git repository (branch main).
    2. Replays data/requests.txt against the service: thirteen calls fail and are logged to logs/errors.jsonl.
    3. Starts louis-agent.api on http://127.0.0.1:5081 with that repository as its workspace.
    4. Runs the orchestrator once over the new errors. Every message between the agents goes to .demo/agent-comms.md.
    5. Leaves louis-agent.api running: open http://127.0.0.1:5081, go to Branches, review each fix and merge it.

    Commits only - nothing is pushed (the demo repository has no remote).
    Needs the .NET 10 SDK, git, and ANTHROPIC_API_KEY in config/.env.secrets (or the environment).
    Works in Windows PowerShell 5.1 and PowerShell 7. Takes about 10-15 minutes.

.PARAMETER Port
    Port for louis-agent.api and its web app (default 5081, so it doesn't clash with the Docker api on 5080).

.PARAMETER StopApi
    Stop louis-agent.api when the orchestrator is done instead of leaving it running for the web app.

.PARAMETER Resume
    Keep .demo as it is (repository, branches, logs) and only (re)start louis-agent.api and run the orchestrator over
    the errors it hasn't triaged yet, e.g. after a run was cut short. The comms log gets a new run section.

.PARAMETER ApiOnly
    Keep .demo as it is and only (re)start louis-agent.api for the web app; the orchestrator doesn't run.
#>
param(
    [int]$Port = 5081,
    [switch]$StopApi,
    [switch]$Resume,
    [switch]$ApiOnly
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$demo = Join-Path $repo '.demo'
$service = Join-Path $demo 'order-service'
$state = Join-Path $demo 'orchestrator-state'
$comms = Join-Path $demo 'agent-comms.md'
$apiLog = Join-Path $demo 'louis-agent-api.log'
$apiPidFile = Join-Path $demo 'louis-agent-api.pid'
$url = "http://127.0.0.1:$Port"

function Step([string]$text) { Write-Host "`n=== $text ===" -ForegroundColor Cyan }

function Invoke-Native([string]$exe, [string[]]$arguments) {
    & $exe @arguments
    if ($LASTEXITCODE -ne 0) { throw "$exe $($arguments -join ' ') failed with exit code $LASTEXITCODE" }
}

# An API left running by the previous demo would hold the port.
if (Test-Path $apiPidFile) {
    $previous = Get-Content $apiPidFile
    if (Get-Process -Id $previous -ErrorAction SilentlyContinue) {
        Write-Host "Stopping the louis-agent.api left running by the previous demo (process $previous)"
        & taskkill /PID $previous /T /F | Out-Null
        Start-Sleep -Seconds 1
    }
}

$reuse = $Resume -or $ApiOnly
if ($reuse -and -not (Test-Path (Join-Path $service '.git'))) { throw 'Nothing to resume: run the demo once without -Resume/-ApiOnly first.' }

if (-not $reuse) {
Step "1. Fresh copy of order-service in .demo/order-service"
if (Test-Path $demo) {
    # git marks its objects read-only
    Get-ChildItem $demo -Recurse -Force | Where-Object { -not $_.PSIsContainer } | ForEach-Object { $_.IsReadOnly = $false }
    Remove-Item $demo -Recurse -Force
}
New-Item -ItemType Directory -Force $service | Out-Null
$source = Join-Path $PSScriptRoot 'order-service'
Get-ChildItem $source -Recurse -File |
    Where-Object { $_.FullName.Substring($source.Length) -notmatch '[\\/](bin|obj|logs)[\\/]' } |
    ForEach-Object {
        $target = Join-Path $service $_.FullName.Substring($source.Length + 1)
        New-Item -ItemType Directory -Force (Split-Path $target) | Out-Null
        Copy-Item $_.FullName $target
    }

Push-Location $service
try {
    Invoke-Native git @('init', '-q', '-b', 'main')
    # Commits in this repository are louis-agent's; the orchestrator shows the author when it verifies one.
    Invoke-Native git @('config', 'user.name', 'louis-agent')
    Invoke-Native git @('config', 'user.email', 'louis-agent@localhost')
    Invoke-Native git @('config', 'core.autocrlf', 'false')
    Invoke-Native git @('add', '.')
    Invoke-Native git @('-c', 'user.name=demo-setup', '-c', 'user.email=demo@localhost', 'commit', '-q', '-m', 'order-service as deployed')

    Step "2. Production traffic (data/requests.txt)"
    & dotnet run --project src/OrderService -- serve data/requests.txt
    if (-not (Test-Path 'logs/errors.jsonl')) { throw 'The traffic run logged no errors; nothing to demo.' }
}
finally {
    Pop-Location
}
}

Step "3. Starting louis-agent.api on $url (workspace: .demo/order-service)"
# Child processes inherit these; variables already set win over config/.env, so the API works on the demo repository.
$env:WORKSPACE_ROOT = $service
$env:ASPNETCORE_URLS = $url
# An unpublished build only serves the web app's files in Development (the API is still only on 127.0.0.1).
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:GIT_AUTHOR_NAME = 'louis-agent'; $env:GIT_AUTHOR_EMAIL = 'louis-agent@localhost'
$env:GIT_COMMITTER_NAME = 'louis-agent'; $env:GIT_COMMITTER_EMAIL = 'louis-agent@localhost'
& dotnet build (Join-Path $repo 'src/louis-agent.api') -nologo -v q | Out-Null
$api = Start-Process dotnet -ArgumentList @('run', '--no-build', '--no-launch-profile', '--project', 'src/louis-agent.api') `
    -WorkingDirectory $repo -RedirectStandardError $apiLog -RedirectStandardOutput "$apiLog.out" -PassThru -WindowStyle Hidden
Set-Content $apiPidFile $api.Id

$finished = $false
try {
    $deadline = (Get-Date).AddSeconds(90)
    do {
        Start-Sleep -Seconds 2
        try { $up = (Invoke-RestMethod "$url/health" -TimeoutSec 2).status -eq 'ok' } catch { $up = $false }
        if ($api.HasExited) { throw "louis-agent.api exited; see $apiLog" }
    } until ($up -or (Get-Date) -gt $deadline)
    if (-not $up) { throw "louis-agent.api did not come up on $url; see $apiLog" }
    Write-Host "louis-agent.api is up (log: $apiLog)"

    if ($ApiOnly) {
        $finished = $true
        return
    }

    Step "4. Orchestrator: triage every new error"
    $env:ORCHESTRATOR_SERVICE = 'order-service'
    $env:ORCHESTRATOR_SERVICE_ROOT = $service
    $env:ORCHESTRATOR_STATE_DIRECTORY = $state
    $env:ORCHESTRATOR_COMMS_LOG = $comms
    $env:LOUIS_AGENT_URL = $url
    Push-Location $repo
    try { & dotnet run --project src/louis-agent.orchestrator -- --once }
    finally { Pop-Location }

    Step "Result"
    Write-Host "Fix branches in .demo/order-service (each made from main, committed, not pushed):"
    & git -C $service for-each-ref refs/heads/fix --format='  %(refname:short)  %(objectname:short) %(authorname): %(subject)'
    Write-Host "`nDecisions:"
    Get-Content (Join-Path $state 'decisions.jsonl') -ErrorAction SilentlyContinue | ForEach-Object {
        $d = $_ | ConvertFrom-Json
        Write-Host ("  {0,-14} {1,-17} {2,-10} {3}" -f $d.error_id, $d.Method, $d.Outcome, $d.Reason)
    }
    Write-Host "`nEverything the agents said to each other: $comms"
    $finished = $true
}
finally {
    if ($StopApi -or -not $finished) {
        if (-not $api.HasExited) { & taskkill /PID $api.Id /T /F | Out-Null }
        Remove-Item $apiPidFile -ErrorAction SilentlyContinue
    }
    else {
        Write-Host "`nlouis-agent.api is still running: open $url, go to the Branches tab, review each fix and merge it." -ForegroundColor Green
        Write-Host "Stop it with: taskkill /PID $($api.Id) /T /F   (the next demo run stops it too)"
    }
}
