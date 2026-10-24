<#
.SYNOPSIS
Deploys Puls to Azure Container Apps (rg-puls-za): five verbs, each one az command with the names filled in (CL19).

.DESCRIPTION
  deploy.ps1 build   [-Tag 2026-10-24-1732]       builds louis-agent-puls and ts-forwarder in the registry, tagged alike
  deploy.ps1 secrets [-App puls-app|puls-searxng] copies the secrets from config/.env.secrets, .env.mongo and .env.searxng
                                                  onto the app (never printed); a missing value is skipped, by name
  deploy.ps1 update  -Revision v018 [-Tag ...] [-App ...]
                                                  fills TAG and REVISION into deploy/azure/<App>.yaml and updates the app
  deploy.ps1 searxng-settings                     uploads docker/searxng/settings.yml to the searxng-config share
  deploy.ps1 logs    [-App ...] [-Container ...] [-Follow]
  deploy.ps1 lock    [-Ip 1.2.3.4]                 allows only this PC's public IP (or -Ip) on puls-app's ingress: the
                                                  app answers without a key (AGENT_AUTH=off), so nobody else may reach it
  deploy.ps1 release [-Tag ...]                   the every-day deploy in one go: build (date-and-time tag), then update as the
                                                  next revision after the one Azure runs (v018 -> v019)
The how-to, and what each piece is, is README.md beside this file. Run `az login` first.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet('build', 'secrets', 'update', 'searxng-settings', 'logs', 'lock', 'release')]
    [string]$Verb,
    [string]$Ip,
    # Date and time, so two releases on one day keep both images and either can be rolled back to (review, 2026-10-24).
    [string]$Tag = (Get-Date -Format 'yyyy-MM-dd-HHmm'),
    [string]$Revision,
    [ValidateSet('puls-app', 'puls-searxng')]
    [string]$App = 'puls-app',
    [string]$Container,
    [switch]$Follow
)
$ErrorActionPreference = 'Stop'
$ResourceGroup = 'rg-puls-za'
$Registry = 'acrpulsza'
$StorageAccount = 'pulsstorage'
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
        Write-Host "Building louis-agent-puls:$Tag from the repo root (docker/images/Dockerfile.puls)..."
        Invoke-Az acr build -r $Registry -t "louis-agent-puls:$Tag" -f (Join-Path $Repo 'docker\images\Dockerfile.puls') $Repo
        Write-Host "Building ts-forwarder:$Tag from deploy/azure/ts-forwarder..."
        Invoke-Az acr build -r $Registry -t "ts-forwarder:$Tag" (Join-Path $Here 'ts-forwarder')
    }
    'secrets' {
        $secretsFile = Join-Path $Repo 'config\.env.secrets'
        $mongoFile = Join-Path $Repo 'config\.env.mongo'
        $searxngFile = Join-Path $Repo 'config\.env.searxng'
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
        $settings = Join-Path $Repo 'docker\searxng\settings.yml'
        Write-Host "Uploading $settings to share searxng-config (the SearXNG app reads it at /etc/searxng/settings.yml)..."
        Invoke-Az storage file upload --account-name $StorageAccount --account-key $key --share-name 'searxng-config' --source $settings --path 'settings.yml' --only-show-errors
        Write-Host 'Restart puls-searxng (a new revision, or az containerapp revision restart) for it to read the file again.'
    }
    'logs' {
        if (-not $Container) { $Container = $App }
        $followArgs = @()
        if ($Follow) { $followArgs = @("--follow") }
        Invoke-Az containerapp logs show -n $App -g $ResourceGroup --container $Container --tail 60 @followArgs
    }
}
