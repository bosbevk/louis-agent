<#
.SYNOPSIS
Deploys Puls to Azure (rg-puls-za): the Bicep build in config/azure and the app's revisions, as verbs that are each a
few az commands with the names filled in (CL19, F61.4).

.DESCRIPTION
  deploy.ps1 infra   [-WhatIf] [-Ip 1.2.3.4]     deploys demos/puls/config/azure/main.bicep: the network, the VM, the environment,
                                                  Application Insights, the SQL server and database; creates what is missing,
                                                  updates what exists. -WhatIf prints Azure's own what-if and writes nothing.
                                                  -Ip opens SSH to the VM from that one address (none by default).
  deploy.ps1 vm                                   copies the VM's compose, SearXNG's settings and the Mongo and SearXNG env
                                                  files to the VM through run-command, and restarts its services
  deploy.ps1 build   [-Tag 2026-11-08-1200]       builds louis-agent-puls in the registry
  deploy.ps1 secrets                              sets the app's secrets from config/.env.secrets, .env.mongo and .env.sql,
                                                  the VM's address and the SQL server's from the deployment; never printed,
                                                  a missing value skipped by name
  deploy.ps1 update  -Tag ... -Revision v021      fills TAG, REVISION, VM_IP, ENVIRONMENT_ID and the providers into
                                                  puls-app.yaml and creates or updates the app
  deploy.ps1 release [-Tag ...]                   build, then update as the next revision after the one Azure runs
  deploy.ps1 domain  [-Hostname mypuls.co.za]     prints the DNS records Cloudflare needs, then adds the hostname with a
                                                  managed certificate once they resolve
  deploy.ps1 logs    [-Follow]
The how-to, and what each piece is, is README.md beside this file. Run `az login` first; `az bicep install` once.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet('infra', 'vm', 'build', 'secrets', 'update', 'release', 'domain', 'logs')]
    [string]$Verb,
    [string]$Ip,
    # build and release make one from the date and time when none is given, so two releases on one day keep both images
    # and either can be rolled back to; update needs the tag of an image already built (review, 2026-10-24).
    [string]$Tag,
    [string]$Revision,
    [string]$Hostname = 'mypuls.co.za',
    [switch]$Follow
)
$ErrorActionPreference = 'Stop'
if (-not $Tag -and $Verb -in @('build', 'release')) { $Tag = Get-Date -Format 'yyyy-MM-dd-HHmm' }
$ResourceGroup = 'rg-puls-za'
$Registry = 'acrpulsza'
$Environment = 'puls-env'
$App = 'puls-app'
$Vm = 'puls-services'
$Deployment = 'puls-build'
$Repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
$Here = $PSScriptRoot
$Config = Join-Path $Repo 'demos\puls\config'
$Bicep = Join-Path $Config 'azure\main.bicep'
$BicepParams = Join-Path $Config 'azure\main.bicepparam'

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

function Get-Output([string]$Name) {
    # One output of the last Bicep deployment: the VM's address, the environment's id, the SQL server's name.
    $value = (& az deployment group show -n $Deployment -g $ResourceGroup --query "properties.outputs.$Name.value" -o tsv)
    if ($LASTEXITCODE -ne 0 -or -not $value) { throw "The deployment $Deployment has no output $Name yet: run deploy.ps1 infra first." }
    return $value
}

function Set-BuildParameters {
    # The two values the parameters file reads from the environment, so neither is ever on a command line or in a file
    # under git: the SQL admin password from config/.env.sql, the SSH public key from the PC's own key.
    $password = Get-EnvValue (Join-Path $Config '.env.sql') 'MSSQL_SA_PASSWORD'
    if (-not $password) { throw 'MSSQL_SA_PASSWORD is missing in demos/puls/config/.env.sql (copy .env.sql.example); it is the SQL server''s admin password too.' }
    $keyFile = Join-Path $env:USERPROFILE '.ssh\id_ed25519.pub'
    if (Test-Path $keyFile) { $key = (Get-Content $keyFile -Raw).Trim() }
    elseif ($WhatIfPreference) {
        # A preview may run before the key exists; the placeholder is plainly one, and a real deploy refuses it.
        Write-Host "No SSH key at ${keyFile} yet: the what-if uses a placeholder key; run ssh-keygen -t ed25519 before deploy.ps1 infra."
        $key = 'ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIPlaceholderKeyForWhatIfOnly0000000000000000 what-if'
    }
    else { throw "No SSH key at ${keyFile}: run ssh-keygen -t ed25519 once; the VM takes its public half." }
    $env:PULS_SQL_ADMIN_PASSWORD = $password
    $env:PULS_SSH_PUBLIC_KEY = $key
    $env:PULS_SSH_FROM_IP = if ($Ip) { $Ip } else { '' }
}

function ConvertTo-UploadScript([hashtable]$Files) {
    # A shell script that writes each file from base64, so no quoting of the content is ever needed; the env files
    # are readable by root only.
    $lines = @('set -e', 'mkdir -p /data/compose /data/env', 'chmod 700 /data/env')
    foreach ($target in $Files.Keys) {
        $bytes = [System.IO.File]::ReadAllBytes($Files[$target])
        $lines += "echo '$([Convert]::ToBase64String($bytes))' | base64 -d > $target"
    }
    $lines += 'chmod 600 /data/env/.env.*'
    $lines += 'systemctl restart puls-services'
    $lines += 'docker ps --format "{{.Names}} {{.Status}}"'
    return ($lines -join "`n")
}

switch ($Verb) {
    'infra' {
        Set-BuildParameters
        if ($Ip -and $Ip -notmatch '^\d{1,3}(\.\d{1,3}){3}$') { throw "Not an IPv4 address: $Ip." }
        if ($WhatIfPreference) {
            Write-Host "What the deployment $Deployment would change in ${ResourceGroup} (nothing is written):"
            Invoke-Az deployment group what-if -n $Deployment -g $ResourceGroup -f $Bicep -p $BicepParams
            return
        }
        Write-Host "Deploying $Bicep to $ResourceGroup as $Deployment (creates what is missing, updates what exists)..."
        Invoke-Az deployment group create -n $Deployment -g $ResourceGroup -f $Bicep -p $BicepParams --query 'properties.outputs' -o json
        Write-Host 'Next: deploy.ps1 vm (the services), deploy.ps1 secrets, then deploy.ps1 release.'
    }
    'vm' {
        $files = @{
            '/data/compose/docker-compose.services.yml' = Join-Path $Here 'vm\docker-compose.services.yml'
            '/data/compose/settings.yml' = Join-Path $Repo 'demos\puls\docker\searxng\settings.yml'
            '/data/env/.env.mongo' = Join-Path $Config '.env.mongo'
            '/data/env/.env.searxng' = Join-Path $Config '.env.searxng'
        }
        foreach ($source in $files.Values) { if (-not (Test-Path $source)) { throw "Missing $source (the env files are copied from their .example)." } }
        $script = ConvertTo-UploadScript $files
        Write-Host "Copying the compose, SearXNG's settings and the two env files to $Vm and restarting its services..."
        # run-command runs as root on the VM through Azure's agent: no port on the VM opens for this. The script,
        # env files included, passes through Azure's control plane, which keeps the last script in the VM's
        # run-command history; the values are the same ones the apps carry as secrets.
        Invoke-Az vm run-command invoke -n $Vm -g $ResourceGroup --command-id RunShellScript --scripts $script --query 'value[0].message' -o tsv
    }
    'build' {
        # The commit the eval runner stamps on a run (F18): this checkout's, or "unknown" where git can't say.
        $ErrorActionPreference = 'Continue'
        try { $commit = git -C $Repo rev-parse --short HEAD 2>$null } catch { $commit = $null }
        $ErrorActionPreference = 'Stop'
        if ($LASTEXITCODE -ne 0 -or -not $commit) { $commit = 'unknown' }
        Write-Host "Building louis-agent-puls:$Tag (commit $commit) from the repo root (demos/puls/docker/images/Dockerfile.puls)..."
        Invoke-Az acr build -r $Registry -t "louis-agent-puls:$Tag" --build-arg "EVAL_COMMIT=$commit" `
            -f (Join-Path $Repo 'demos\puls\docker\images\Dockerfile.puls') $Repo
    }
    'secrets' {
        $secretsFile = Join-Path $Config '.env.secrets'
        $mongoFile = Join-Path $Config '.env.mongo'
        $sqlFile = Join-Path $Config '.env.sql'
        $vmIp = Get-Output 'vmPrivateIp'
        $sqlServer = Get-Output 'sqlServerFqdn'
        $wanted = @()
        $wanted += @{ name = 'anthropic-api-key'; value = Get-EnvValue $secretsFile 'ANTHROPIC_API_KEY' }
        $wanted += @{ name = 'agent-api-key'; value = Get-EnvValue $secretsFile 'AGENT_API_KEY' }
        $wanted += @{ name = 'admin-api-key'; value = Get-EnvValue $secretsFile 'ADMIN_API_KEY' }
        # The VM's Mongo, at its private address. No `&` in a value: az is a batch file on Windows, and cmd would cut
        # the value there (connectTimeoutMS was lost that way on 2026-10-24); the same holds for every secret value.
        $user = Get-EnvValue $mongoFile 'MONGO_INITDB_ROOT_USERNAME'
        $password = Get-EnvValue $mongoFile 'MONGO_INITDB_ROOT_PASSWORD'
        $mongo = $null
        if ($user -and $password) { $mongo = "mongodb://${user}:${password}@${vmIp}:27017/?authSource=admin" }
        $wanted += @{ name = 'mongo-url'; value = $mongo }
        # Azure SQL: the admin the Bicep build made, with the same password as the PC's SQL Server (config/.env.sql).
        $sqlPassword = Get-EnvValue $sqlFile 'MSSQL_SA_PASSWORD'
        $sql = $null
        if ($sqlPassword) { $sql = "Server=tcp:${sqlServer},1433;Database=puls_accounts;User Id=pulsadmin;Password=${sqlPassword};Encrypt=True;TrustServerCertificate=False;Connection Timeout=60" }
        $wanted += @{ name = 'accounts-sql-url'; value = $sql }
        # The sign-in providers (F61.3): a provider is on only with both its keys, so a pair is set together or not at all.
        foreach ($provider in @('google', 'microsoft', 'linkedin')) {
            $upper = $provider.ToUpperInvariant()
            $id = Get-EnvValue $secretsFile "AUTH_${upper}_CLIENT_ID"
            $secret = Get-EnvValue $secretsFile "AUTH_${upper}_CLIENT_SECRET"
            if ($id -and $secret) {
                $wanted += @{ name = "auth-$provider-client-id"; value = $id }
                $wanted += @{ name = "auth-$provider-client-secret"; value = $secret }
            } else { $wanted += @{ name = "auth-$provider-client-id and -secret"; value = $null } }
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
        $vmIp = Get-Output 'vmPrivateIp'
        $environmentId = Get-Output 'environmentId'
        # A provider's two lines go in only when both its secrets are on the app: a secretRef to a missing secret fails the update.
        $present = @(& az containerapp secret list -n $App -g $ResourceGroup --query '[].name' -o tsv 2>$null)
        $providers = @()
        foreach ($provider in @('google', 'microsoft', 'linkedin')) {
            if ($present -contains "auth-$provider-client-id" -and $present -contains "auth-$provider-client-secret") {
                $upper = $provider.ToUpperInvariant()
                $providers += "          - name: AUTH_${upper}_CLIENT_ID"
                $providers += "            secretRef: auth-$provider-client-id"
                $providers += "          - name: AUTH_${upper}_CLIENT_SECRET"
                $providers += "            secretRef: auth-$provider-client-secret"
            }
        }
        $template = Join-Path $Here 'puls-app.yaml'
        $resolved = Join-Path ([System.IO.Path]::GetTempPath()) "$App.$Revision.yaml"
        (Get-Content $template -Raw).Replace(':TAG', ":$Tag").Replace("'REVISION'", "'$Revision'").Replace('VM_IP', $vmIp).Replace('ENVIRONMENT_ID', $environmentId).Replace('          # PROVIDERS', ($providers -join "`n")) |
            Set-Content $resolved -Encoding utf8 -NoNewline
        $exists = (& az containerapp show -n $App -g $ResourceGroup --query 'properties.environmentId' -o tsv 2>$null)
        if ($LASTEXITCODE -eq 0 -and $exists -eq $environmentId) {
            Write-Host "Updating $App to revision $Revision (tag $Tag) from $template..."
            Invoke-Az containerapp update -n $App -g $ResourceGroup --yaml $resolved --query '{revision:properties.latestRevisionName, ready:properties.latestReadyRevisionName, state:properties.provisioningState}' -o json
        } else {
            # The first deploy into puls-env: the app of the old environment keeps its name until it is deleted (README).
            Write-Host "Creating $App in $Environment as revision $Revision (tag $Tag) from $template..."
            Invoke-Az containerapp create -n $App -g $ResourceGroup --yaml $resolved --query '{fqdn:properties.configuration.ingress.fqdn, state:properties.provisioningState}' -o json
        }
        Remove-Item $resolved -ErrorAction SilentlyContinue
        Write-Host "Watch it come up with: deploy.ps1 logs"
    }
    'release' {
        $latest = (& az containerapp show -n $App -g $ResourceGroup --query 'properties.latestRevisionName' -o tsv 2>$null)
        if ($LASTEXITCODE -ne 0 -or -not $latest) { $next = 'v001' }
        elseif ($latest -match '--v(\d+)$') { $next = 'v{0:D3}' -f ([int]$Matches[1] + 1) }
        else { throw "Could not read $App's latest revision ($latest)." }
        Write-Host "Release: image tagged $Tag, then $App revision $next."
        & $PSCommandPath build -Tag $Tag
        & $PSCommandPath update -Tag $Tag -Revision $next
    }
    'domain' {
        # The records Cloudflare needs, DNS-only (a proxied record blocks the certificate): the apex points at the
        # environment's address, the TXT proves the domain is ours, www is a CNAME to the app's own address.
        $staticIp = Get-Output 'environmentStaticIp'
        $fqdn = (& az containerapp show -n $App -g $ResourceGroup --query 'properties.configuration.ingress.fqdn' -o tsv)
        $verification = (& az containerapp show -n $App -g $ResourceGroup --query 'properties.customDomainVerificationId' -o tsv)
        if ($LASTEXITCODE -ne 0 -or -not $fqdn) { throw "$App is not deployed yet: deploy.ps1 release first." }
        $apex = $Hostname -notmatch '^www\.'
        Write-Host "In Cloudflare, DNS-only, for ${Hostname}:"
        if ($apex) {
            Write-Host "  A      @           $staticIp"
            Write-Host "  TXT    asuid       $verification"
        } else {
            Write-Host "  CNAME  www         $fqdn"
            Write-Host "  TXT    asuid.www   $verification"
        }
        $method = if ($apex) { 'TXT' } else { 'CNAME' }
        if ($PSCmdlet.ShouldProcess($App, "az containerapp hostname add/bind $Hostname with a managed certificate ($method)")) {
            Write-Host "Adding $Hostname to $App and asking for its managed certificate (the records above must resolve first)..."
            Invoke-Az containerapp hostname add -n $App -g $ResourceGroup --hostname $Hostname -o none
            Invoke-Az containerapp hostname bind -n $App -g $ResourceGroup --hostname $Hostname --environment $Environment --validation-method $method --query '[].{name:name, state:bindingType}' -o table
        }
    }
    'logs' {
        $followArgs = @()
        if ($Follow) { $followArgs = @("--follow") }
        Invoke-Az containerapp logs show -n $App -g $ResourceGroup --container $App --tail 60 @followArgs
    }
}
