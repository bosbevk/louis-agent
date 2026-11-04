<#
.SYNOPSIS
Deploys Puls to Azure Container Apps (rg-puls-za): eight verbs, each a few az commands with the names filled in (CL19).

.DESCRIPTION
  deploy.ps1 build   [-Tag 2026-10-24-1732]       builds louis-agent-puls and ts-forwarder in the registry, tagged alike
  deploy.ps1 secrets [-App puls-app|puls-searxng] copies the secrets from demos/puls/config/.env.secrets, .env.mongo and .env.searxng
                                                  onto the app (never printed); a missing value is skipped, by name
  deploy.ps1 update  -Tag 2026-10-24-1732 -Revision v018 [-App ...]
                                                  fills TAG (an image already built) and REVISION into deploy/azure/<App>.yaml
                                                  and updates the app
  deploy.ps1 searxng-settings                     uploads demos/puls/docker/searxng/settings.yml to the searxng-config share
  deploy.ps1 logs    [-App ...] [-Container ...] [-Follow]
  deploy.ps1 lock    [-Ip 1.2.3.4]                 allows only this PC's public IP (or -Ip) on puls-app's ingress: the
                                                  app answers without a key (AGENT_AUTH=off), so nobody else may reach it
  deploy.ps1 release [-Tag ...]                   the every-day deploy in one go: build (date-and-time tag), then update as the
                                                  next revision after the one Azure runs (v018 -> v019)
  deploy.ps1 telemetry [-WhatIf]                  creates Application Insights puls-insights on the environment's Log
                                                  Analytics workspace and turns on the environment's OpenTelemetry agent
                                                  (F52); -WhatIf runs the reads and prints the two writes
The how-to, and what each piece is, is README.md beside this file. Run `az login` first.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet('build', 'secrets', 'update', 'searxng-settings', 'logs', 'lock', 'release', 'telemetry')]
    [string]$Verb,
    [string]$Ip,
    # build and release make one from the date and time when none is given, so two releases on one day keep both images
    # and either can be rolled back to; update needs the tag of an image already built (review, 2026-10-24).
    [string]$Tag,
    [string]$Revision,
    [ValidateSet('puls-app', 'puls-searxng')]
    [string]$App = 'puls-app',
    [string]$Container,
    [switch]$Follow
)
$ErrorActionPreference = 'Stop'
if (-not $Tag -and $Verb -in @('build', 'release')) { $Tag = Get-Date -Format 'yyyy-MM-dd-HHmm' }
$ResourceGroup = 'rg-puls-za'
$Registry = 'acrpulsza'
$StorageAccount = 'pulsstorage'
$Environment = 'puls-container-env'
$Location = 'southafricanorth'
$Insights = 'puls-insights'
$Repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$Here = $PSScriptRoot

function Get-EnvValue([string]$File, [string]$Key) {
    # The first uncommented KEY=value line of a .env file; $null when the file or the key is missing.
    if (-not (Test-Path $File)) { return $null }
    $line = Get-Content $File | Where-Object { $_ -match "^$Key=" } | Select-Object -First 1
    if ($null -eq $line) { return $null }
    $value = $line.Substring($Key.Length + 1).Trim()
    if ($value.Length -eq 0) { return $null }
    return $value
}

function Invoke-Az {
    # az is a native command: its progress spinner and warnings go to stderr, which Windows PowerShell turns into errors
    # when the output is redirected, so judge it by its exit code alone (its own stderr has already said why).
    $ErrorActionPreference = 'Continue'
    & az @args
    $ErrorActionPreference = 'Stop'
    if ($LASTEXITCODE -ne 0) { throw "az $($args[0]) $($args[1]) failed ($LASTEXITCODE)" }
}

switch ($Verb) {
    'build' {
        # The commit the eval runner stamps on a run (F18): this checkout's, or "unknown" where git can't say.
        # git's stderr would stop the script under 'Stop' (as az's would, see Invoke-Az), and a PC without git throws.
        $ErrorActionPreference = 'Continue'
        try { $commit = git -C $Repo rev-parse --short HEAD 2>$null } catch { $commit = $null }
        $ErrorActionPreference = 'Stop'
        if ($LASTEXITCODE -ne 0 -or -not $commit) { $commit = 'unknown' }
        Write-Host "Building louis-agent-puls:$Tag (commit $commit) from the repo root (demos/puls/docker/images/Dockerfile.puls)..."
        Invoke-Az acr build -r $Registry -t "louis-agent-puls:$Tag" --build-arg "EVAL_COMMIT=$commit" `
            -f (Join-Path $Repo 'demos\puls\docker\images\Dockerfile.puls') $Repo
        Write-Host "Building ts-forwarder:$Tag from deploy/azure/ts-forwarder..."
        Invoke-Az acr build -r $Registry -t "ts-forwarder:$Tag" (Join-Path $Here 'ts-forwarder')
    }
    'secrets' {
        $secretsFile = Join-Path $Repo 'demos\puls\config\.env.secrets'
        $mongoFile = Join-Path $Repo 'demos\puls\config\.env.mongo'
        $searxngFile = Join-Path $Repo 'demos\puls\config\.env.searxng'
        $wanted = @()
        if ($App -eq 'puls-app') {
            $wanted += @{ name = 'anthropic-api-key'; value = Get-EnvValue $secretsFile 'ANTHROPIC_API_KEY' }
            $wanted += @{ name = 'admin-api-key'; value = Get-EnvValue $secretsFile 'ADMIN_API_KEY' }
            $wanted += @{ name = 'ts-authkey'; value = Get-EnvValue $secretsFile 'TS_AUTHKEY' }
            $user = Get-EnvValue $mongoFile 'MONGO_INITDB_ROOT_USERNAME'
            $password = Get-EnvValue $mongoFile 'MONGO_INITDB_ROOT_PASSWORD'
            $url = $null
            # The home PC's Mongo, at the loopback port the ts-forwarder sidecar forwards. No `&` in the URL: az is a
            # batch file on Windows, and cmd would cut the value there (connectTimeoutMS was lost that way on 2026-10-24);
            # the same holds for any secret value, so none of ours may contain & or |.
            if ($user -and $password) { $url = "mongodb://${user}:${password}@127.0.0.1:27017/?authSource=admin" }
            $wanted += @{ name = 'benchmarks-mongo-url'; value = $url }
        } else {
            $wanted += @{ name = 'searxng-secret'; value = Get-EnvValue $searxngFile 'SEARXNG_SECRET' }
        }
        $set = @()
        foreach ($s in $wanted) {
            if ($null -eq $s.value) { Write-Host "  skipped $($s.name): no value in its file" }
            else { $set += "$($s.name)=$($s.value)" }
        }
        if ($set.Count -eq 0) { throw 'No secret has a value to set.' }
        $names = ($set | ForEach-Object { $_.Split('=')[0] }) -join ', '
        Write-Host "Setting $($set.Count) secret(s) on ${App}: $names"
        Invoke-Az containerapp secret set -n $App -g $ResourceGroup --secrets @set --query '[].name' -o tsv
        Write-Host 'A running revision keeps its old values until the next update.'
    }
    'update' {
        if (-not $Revision) { throw 'update needs -Revision (vNNN, one more than the running revision; Single mode refuses a repeat).' }
        if (-not $Tag) { throw 'update needs -Tag: the tag of an image already built (deploy.ps1 build; or release, which does both).' }
        $template = Join-Path $Here "$App.yaml"
        $resolved = Join-Path ([System.IO.Path]::GetTempPath()) "$App.$Revision.yaml"
        (Get-Content $template -Raw).Replace(':TAG', ":$Tag").Replace("'REVISION'", "'$Revision'") | Set-Content $resolved -Encoding utf8 -NoNewline
        Write-Host "Updating $App to revision $Revision (tag $Tag) from $template..."
        Invoke-Az containerapp update -n $App -g $ResourceGroup --yaml $resolved --query '{revision:properties.latestRevisionName, ready:properties.latestReadyRevisionName, state:properties.provisioningState}' -o json
        Remove-Item $resolved -ErrorAction SilentlyContinue
        if ($App -eq 'puls-app') {
            # The allow-list lives in Azure, not in the yaml: show it after every update, so a wiped rule is seen at once.
            Write-Host 'Ingress allow-list after the update (empty means open to the internet: run deploy.ps1 lock):'
            Invoke-Az containerapp ingress access-restriction list -n $App -g $ResourceGroup --query '[].{name:name, ip:ipAddressRange, action:action}' -o table
        }
        Write-Host "Watch it come up with: deploy.ps1 logs -App $App"
    }
    'release' {
        $latest = (& az containerapp show -n 'puls-app' -g $ResourceGroup --query 'properties.latestRevisionName' -o tsv)
        if ($LASTEXITCODE -ne 0 -or $latest -notmatch '--v(\d+)$') { throw "Could not read puls-app's latest revision ($latest)." }
        $next = 'v{0:D3}' -f ([int]$Matches[1] + 1)
        Write-Host "Release: images tagged $Tag, then puls-app revision $next (after $latest)."
        & $PSCommandPath build -Tag $Tag
        & $PSCommandPath update -Tag $Tag -Revision $next -App 'puls-app'
    }
    'lock' {
        # An IPv4-only lookup: the restriction takes IPv4 ranges, and a dual-stack PC answers IPv6 first elsewhere.
        if (-not $Ip) { $Ip = (Invoke-RestMethod -Uri 'https://ipv4.icanhazip.com' -TimeoutSec 10).Trim() }
        if ($Ip -notmatch '^\d{1,3}(\.\d{1,3}){3}$') { throw "Not an IPv4 address: $Ip. Pass -Ip." }
        Write-Host "Allowing only $Ip on puls-app's ingress (rule 'home'); every other address gets a 403."
        Invoke-Az containerapp ingress access-restriction set -n 'puls-app' -g $ResourceGroup --rule-name 'home' --ip-address "$Ip/32" --action Allow --description 'The home PC: the app answers without a key' --query '[].{name:name, ip:ipAddressRange, action:action}' -o table
    }
    'searxng-settings' {
        $key = (& az storage account keys list -g $ResourceGroup -n $StorageAccount --query '[0].value' -o tsv)
        if ($LASTEXITCODE -ne 0) { throw 'Could not read the storage account key.' }
        $settings = Join-Path $Repo 'demos\puls\docker\searxng\settings.yml'
        Write-Host "Uploading $settings to share searxng-config (the SearXNG app reads it at /etc/searxng/settings.yml)..."
        Invoke-Az storage file upload --account-name $StorageAccount --account-key $key --share-name 'searxng-config' --source $settings --path 'settings.yml' --only-show-errors
        Write-Host 'Restart puls-searxng (a new revision, or az containerapp revision restart) for it to read the file again.'
    }
    'telemetry' {
        # The Log Analytics workspace the environment already logs to (Louis's pick K1): App Insights goes on it, so its
        # traces and logs sit beside the container logs. Reads, so -WhatIf runs them too.
        $customerId = Invoke-Az containerapp env show -n $Environment -g $ResourceGroup --query 'properties.appLogsConfiguration.logAnalyticsConfiguration.customerId' -o tsv
        if (-not $customerId) { throw "$Environment logs to no Log Analytics workspace: see Monitoring > Logging options on the environment in the portal." }
        $workspace = Invoke-Az monitor log-analytics workspace list -g $ResourceGroup --query "[?customerId=='$customerId'].id" -o tsv
        if (-not $workspace) { throw "No Log Analytics workspace in $ResourceGroup has the environment's id ($customerId): it logs to one in another resource group, which this verb doesn't expect; find it in the portal (the environment's Monitoring > Logging options) before going on." }
        Write-Host "The environment logs to $workspace."

        # Application Insights on that workspace: create is an upsert, so a second run answers the same resource. The
        # connection string isn't a secret (Microsoft says so), but it isn't printed either: the environment holds it.
        $connection = $null
        if ($PSCmdlet.ShouldProcess($ResourceGroup, "az monitor app-insights component create --app $Insights -l $Location --workspace <the workspace above> --kind web --application-type web")) {
            $connection = Invoke-Az monitor app-insights component create --app $Insights -g $ResourceGroup -l $Location --workspace $workspace --kind web --application-type web --query connectionString -o tsv
            $key = ($connection -split ';' | Where-Object { $_ -like 'InstrumentationKey=*' }) -replace '^InstrumentationKey=', ''
            Write-Host "Application Insights ${Insights}: instrumentation key $($key.Substring(0, 8))... (the rest stays in Azure)"
        }

        # The environment's managed OpenTelemetry agent (preview), sending traces and logs to it: every app of the
        # environment gets OTEL_EXPORTER_OTLP_ENDPOINT and _PROTOCOL (gRPC) injected, which both exporters read.
        if ($PSCmdlet.ShouldProcess($ResourceGroup, "az containerapp env telemetry app-insights set -n $Environment --connection-string <the string above> --enable-open-telemetry-traces true --enable-open-telemetry-logs true")) {
            Invoke-Az containerapp env telemetry app-insights set -n $Environment -g $ResourceGroup --connection-string $connection --enable-open-telemetry-traces true --enable-open-telemetry-logs true -o none
            # show answers the whole environment; the agent's destinations are two lists that should both hold appInsights.
            Invoke-Az containerapp env telemetry app-insights show -n $Environment -g $ResourceGroup --query 'properties.openTelemetryConfiguration.{traces:tracesConfiguration.destinations, logs:logsConfiguration.destinations}' -o json
        }

        Write-Host 'Once set, every app in the environment gets the agent''s OTEL_EXPORTER_OTLP_ENDPOINT and _PROTOCOL; a running'
        Write-Host 'revision picks them up on its next one (deploy.ps1 release). Then ask Puls a question and look in the portal:'
        Write-Host "$Insights > Transaction search, a few minutes later."
    }
    'logs' {
        if (-not $Container) { $Container = $App }
        $followArgs = @()
        if ($Follow) { $followArgs = @("--follow") }
        Invoke-Az containerapp logs show -n $App -g $ResourceGroup --container $Container --tail 60 @followArgs
    }
}
