<#
.SYNOPSIS
    End-to-end demo: an orchestrator agent watches order-service, triages its exceptions with one runbook per API
    method, has louis-agent fix the auto-fixable one, then verifies louis-agent's commit and the fix itself.

.DESCRIPTION
    1. Copies samples/order-service to .demo/order-service and makes it a fresh git repository (branch main).
    2. Replays data/requests.txt against the service: four calls fail and are logged to logs/errors.jsonl.
    3. Starts louis-agent.api on http://127.0.0.1:5081 with that repository as its workspace.
    4. Runs the orchestrator once over the new errors, then stops louis-agent.api.

    Needs the .NET 10 SDK, git, and ANTHROPIC_API_KEY in config/.env.secrets (or the environment).
    Works in Windows PowerShell 5.1 and PowerShell 7.

.PARAMETER Port
    Port for louis-agent.api (default 5081, so it doesn't clash with the Docker api on 5080).

.PARAMETER KeepApi
    Leave louis-agent.api running afterwards (e.g. to look at the session in the web app).
#>
param(
    [int]$Port = 5081,
    [switch]$KeepApi
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$demo = Join-Path $repo '.demo'
$service = Join-Path $demo 'order-service'
$state = Join-Path $demo 'orchestrator-state'
$apiLog = Join-Path $demo 'louis-agent-api.log'
$url = "http://127.0.0.1:$Port"

function Step([string]$text) { Write-Host "`n=== $text ===" -ForegroundColor Cyan }

function Invoke-Native([string]$exe, [string[]]$arguments) {
    & $exe @arguments
    if ($LASTEXITCODE -ne 0) { throw "$exe $($arguments -join ' ') failed with exit code $LASTEXITCODE" }
}

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

Step "3. Starting louis-agent.api on $url (workspace: .demo/order-service)"
# Child processes inherit these; variables already set win over config/.env, so the API works on the demo repository.
$env:WORKSPACE_ROOT = $service
$env:ASPNETCORE_URLS = $url
$env:GIT_AUTHOR_NAME = 'louis-agent'; $env:GIT_AUTHOR_EMAIL = 'louis-agent@localhost'
$env:GIT_COMMITTER_NAME = 'louis-agent'; $env:GIT_COMMITTER_EMAIL = 'louis-agent@localhost'
& dotnet build (Join-Path $repo 'src/louis-agent.api') -nologo -v q | Out-Null
$api = Start-Process dotnet -ArgumentList @('run', '--no-build', '--no-launch-profile', '--project', 'src/louis-agent.api') `
    -WorkingDirectory $repo -RedirectStandardError $apiLog -RedirectStandardOutput "$apiLog.out" -PassThru -WindowStyle Hidden

try {
    $deadline = (Get-Date).AddSeconds(90)
    do {
        Start-Sleep -Seconds 2
        try { $up = (Invoke-RestMethod "$url/health" -TimeoutSec 2).status -eq 'ok' } catch { $up = $false }
        if ($api.HasExited) { throw "louis-agent.api exited; see $apiLog" }
    } until ($up -or (Get-Date) -gt $deadline)
    if (-not $up) { throw "louis-agent.api did not come up on $url; see $apiLog" }
    Write-Host "louis-agent.api is up (log: $apiLog)"

    Step "4. Orchestrator: triage every new error"
    $env:ORCHESTRATOR_SERVICE = 'order-service'
    $env:ORCHESTRATOR_SERVICE_ROOT = $service
    $env:ORCHESTRATOR_STATE_DIRECTORY = $state
    $env:LOUIS_AGENT_URL = $url
    Push-Location $repo
    try { & dotnet run --project src/louis-agent.orchestrator -- --once }
    finally { Pop-Location }

    Step "Result"
    Write-Host "Branches and commits in .demo/order-service:"
    & git -C $service log --all --format='  %h %an  %d %s'
    Write-Host "`nDecisions ($state/decisions.jsonl):"
    Get-Content (Join-Path $state 'decisions.jsonl') -ErrorAction SilentlyContinue | ForEach-Object {
        $d = $_ | ConvertFrom-Json
        Write-Host ("  {0,-14} {1,-10} {2}" -f $d.error_id, $d.Outcome, $d.Reason)
    }
    $escalations = Join-Path $state 'escalations.jsonl'
    if (Test-Path $escalations) {
        Write-Host "`nEscalations:"
        Get-Content $escalations | ForEach-Object { $e = $_ | ConvertFrom-Json; Write-Host "  -> $($e.team): $($e.reason)" }
    }
}
finally {
    if ($KeepApi) {
        Write-Host "`nlouis-agent.api is still running on $url (process $($api.Id))."
    }
    elseif (-not $api.HasExited) {
        & taskkill /PID $api.Id /T /F | Out-Null
    }
}
