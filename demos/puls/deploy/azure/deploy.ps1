<#
.SYNOPSIS
Deploys the louis-agent demo's app to Azure (rg-puls-za): its revisions, as verbs that are each a few az commands with
the names filled in (CL19, F61.4). Since FX122 (Louis's option A, 2026-11-09) it is the only app: Puls's app is gone,
and the site runs alone, sleeping when unused, with no VM and no database. infra, vm and sql-access are the old Bicep
build's (the VM, Mongo and SQL that A removed): don't run them, they would create those again.

.DESCRIPTION
  deploy.ps1 infra   [-WhatIf] [-Ip 1.2.3.4]     deploys demos/puls/config/azure/main.bicep: the network, the VM, the environment,
                                                  Application Insights, the SQL server and database, the app's identity on the
                                                  vault; creates what is missing, updates what exists. -WhatIf prints Azure's
                                                  own what-if and writes nothing. -Ip opens SSH to the VM from that one address.
  deploy.ps1 vm                                   copies the VM's compose, SearXNG's settings and the Mongo and SearXNG env
                                                  files to the VM through run-command, and restarts its services
  deploy.ps1 build   [-App ...] [-Tag 2026-11-08-1200]   builds the app's image (louis-agent-puls, louis-agent-coding) in the registry
  deploy.ps1 workspace [-App ...]                 copies config/prices.json and rates.json from the app's config into its share
  deploy.ps1 secrets [-App ...]                   puts the app's secrets in the vault puls-keys, from its config/.env.secrets;
                                                  never printed, a missing value skipped by name
  deploy.ps1 update  [-App ...] -Tag ... -Revision v021   fills TAG, REVISION, ENVIRONMENT_ID, the app's identity, the
                                                  secrets the vault holds, the providers and the keys it runs without into the app's yaml and creates or updates it
  deploy.ps1 release [-App ...] [-Tag ...]        build, then update as the next revision after the one Azure runs
  deploy.ps1 domain  [-App ...] [-Hostname ...]   prints the DNS records Cloudflare needs, then adds the hostname (the app's
                                                  apex by default) with a managed certificate once they resolve
  deploy.ps1 sql-access [-Ip 1.2.3.4]             lets this PC's public IPv4 (or -Ip) through the SQL server's firewall, for
                                                  SQL Server Management Studio
  deploy.ps1 logs    [-App ...] [-Follow]
The how-to, and what each piece is, is README.md beside this file. Run `az login` first; `az bicep install` once.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet('infra', 'vm', 'build', 'workspace', 'secrets', 'update', 'release', 'domain', 'sql-access', 'logs')]
    [string]$Verb,
    # Which app a verb works on: the louis-agent demo's app, the only one since FX122.
    [ValidateSet('louis-agent-app')]
    [string]$App = 'louis-agent-app',
    [string]$Ip,
    # build and release make one from the date and time when none is given, so two releases on one day keep both images
    # and either can be rolled back to; update needs the tag of an image already built (review, 2026-10-24).
    [string]$Tag,
    [string]$Revision,
    [string]$Hostname,
    [switch]$Follow
)
$ErrorActionPreference = 'Stop'
if (-not $Tag -and $Verb -in @('build', 'release')) { $Tag = Get-Date -Format 'yyyy-MM-dd-HHmm' }
$ResourceGroup = 'rg-puls-za'
$Registry = 'acrpulsza'
$Environment = 'puls-env'
$Vm = 'puls-services'
$Vault = 'puls-keys'
$Deployment = 'puls-build'
$Repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
$Here = $PSScriptRoot
$Config = Join-Path $Repo 'demos\puls\config'
$Bicep = Join-Path $Config 'azure\main.bicep'
$BicepParams = Join-Path $Config 'azure\main.bicepparam'
$Providers = @('google', 'microsoft', 'linkedin')
# What is each app's: its image and Dockerfile, its config folder, its yaml, the Bicep output that names its identity,
# its share, its domain, the suffix on the vault secrets that are its own (the rest both apps share), and WhenSet: the
# keys the app runs without, each env var with its vault secret, named in the yaml only once the vault holds it (FX87).
$Apps = @{
    'louis-agent-app' = @{
        # The framework's site since F124: its own host, image, settings and yaml, all under src/site.
        Image = 'louis-agent-site'; Dockerfile = 'src\site\docker\Dockerfile'
        Config = 'src\site\config'; Yaml = 'src\site\deploy\azure\louis-agent-app.yaml'; IdentityOutput = 'codingAppIdentityId'
        Share = 'louis-agent-workspace'; Hostname = 'louis-agent.dev'; Suffix = '-louis-agent'
        WhenSet = @{ RESEND_API_KEY = 'resend-api-key' }
    }
}
$AppInfo = $Apps[$App]
$AppConfig = Join-Path $Repo $AppInfo.Config
if (-not $Hostname) { $Hostname = $AppInfo.Hostname }

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

function Read-Az {
    # A read that may come back empty (an app that doesn't exist yet): az's "not found" goes to stderr, which Windows
    # PowerShell turns into a terminating error under 'Stop' when it is redirected, so the read runs under 'Continue'
    # with stderr dropped, and the caller judges by $LASTEXITCODE and the value.
    $ErrorActionPreference = 'Continue'
    $value = (& az @args 2>$null)
    $ErrorActionPreference = 'Stop'
    return $value
}

function Get-Output([string]$Name) {
    # One output of the last Bicep deployment: the VM's address, the environment's id, the SQL server's name, the identity.
    $value = Read-Az deployment group show -n $Deployment -g $ResourceGroup --query "properties.outputs.$Name.value" -o tsv
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

function Get-Secrets {
    # The app's secrets as name/value pairs, from the git-ignored config files and the deployment's outputs; a missing
    # value is reported by name, never printed. No `&` in a value: az is a batch file on Windows, and cmd would cut
    # the value there (connectTimeoutMS was lost that way on 2026-10-24).
    $secretsFile = Join-Path $AppConfig '.env.secrets'
    $suffix = $AppInfo.Suffix
    $wanted = @()
    # The model key, and this app's own keys, suffixed as they were beside Puls (FX122: no Mongo or SQL secret now).
    $wanted += @{ name = 'anthropic-api-key'; value = Get-EnvValue $secretsFile 'ANTHROPIC_API_KEY' }
    $wanted += @{ name = "agent-api-key$suffix"; value = Get-EnvValue $secretsFile 'AGENT_API_KEY' }
    $wanted += @{ name = "admin-api-key$suffix"; value = Get-EnvValue $secretsFile 'ADMIN_API_KEY' }
    foreach ($provider in $Providers) {
        $upper = $provider.ToUpperInvariant()
        $id = Get-EnvValue $secretsFile "AUTH_${upper}_CLIENT_ID"
        $secret = Get-EnvValue $secretsFile "AUTH_${upper}_CLIENT_SECRET"
        if ($id -and $secret) {
            $wanted += @{ name = "auth-$provider-client-id$suffix"; value = $id }
            $wanted += @{ name = "auth-$provider-client-secret$suffix"; value = $secret }
        } else { $wanted += @{ name = "auth-$provider-client-id$suffix and -secret$suffix"; value = $null } }
    }
    # The keys the app runs without (FX87), each from its env var.
    foreach ($key in $AppInfo.WhenSet.Keys) { $wanted += @{ name = $AppInfo.WhenSet[$key]; value = Get-EnvValue $secretsFile $key } }
    return $wanted
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
        # The script goes to az as a file (@path), whole: on a command line its lines came apart (2026-11-08).
        $script = Join-Path ([System.IO.Path]::GetTempPath()) 'puls-services-upload.sh'
        [System.IO.File]::WriteAllText($script, (ConvertTo-UploadScript $files) + "`n", [System.Text.UTF8Encoding]::new($false))
        Write-Host "Copying the compose, SearXNG's settings and the two env files to $Vm and restarting its services..."
        # run-command runs as root on the VM through Azure's agent: no port on the VM opens for this. The script,
        # env files included, passes through Azure's control plane, which keeps the last script in the VM's
        # run-command history; the values are the same ones the vault holds.
        try { Invoke-Az vm run-command invoke -n $Vm -g $ResourceGroup --command-id RunShellScript --scripts "@$script" --query 'value[0].message' -o tsv }
        finally { Remove-Item $script -ErrorAction SilentlyContinue }
    }
    'build' {
        # The commit the eval runner stamps on a run (F18): this checkout's, or "unknown" where git can't say.
        $ErrorActionPreference = 'Continue'
        try { $commit = git -C $Repo rev-parse --short HEAD 2>$null } catch { $commit = $null }
        $ErrorActionPreference = 'Stop'
        if ($LASTEXITCODE -ne 0 -or -not $commit) { $commit = 'unknown' }
        Write-Host "Building $($AppInfo.Image):$Tag (commit $commit) from the repo root ($($AppInfo.Dockerfile))..."
        Invoke-Az acr build -r $Registry -t "$($AppInfo.Image):$Tag" --build-arg "EVAL_COMMIT=$commit" `
            -f (Join-Path $Repo $AppInfo.Dockerfile) $Repo
    }
    'workspace' {
        # The price tables the app reads from its share (README: the share's layout), copied from its config folder;
        # the storage key is read once and never printed. The share is the Bicep build's (or CL19's, for Puls).
        $key = Read-Az storage account keys list -n 'pulsstorage' -g $ResourceGroup --query '[0].value' -o tsv
        if ($LASTEXITCODE -ne 0 -or -not $key) { throw 'Could not read the storage key of pulsstorage.' }
        foreach ($file in @('prices.json', 'rates.json')) {
            $source = Join-Path $AppConfig $file
            if (-not (Test-Path $source)) { throw "Missing $source." }
            Write-Host "  $($AppInfo.Share)/config/$file"
            Invoke-Az storage directory create --account-name 'pulsstorage' --account-key $key --share-name $AppInfo.Share -n 'config' -o none
            Invoke-Az storage file upload --account-name 'pulsstorage' --account-key $key --share-name $AppInfo.Share --source $source --path "config/$file" -o none
        }
    }
    'secrets' {
        # Every value goes into the vault, one secret each; the app only ever references them (update writes the block).
        $set = 0
        foreach ($s in Get-Secrets) {
            if ($null -eq $s.value) { Write-Host "  skipped $($s.name): no value in its file"; continue }
            Write-Host "  $Vault/$($s.name)"
            Invoke-Az keyvault secret set --vault-name $Vault -n $s.name --value $s.value --query 'name' -o none
            $set++
        }
        if ($set -eq 0) { throw 'No secret has a value to set.' }
        Write-Host "$set secret(s) in $Vault. A running revision keeps the values it read until the next update (deploy.ps1 release)."
    }
    'update' {
        if (-not $Revision) { throw 'update needs -Revision (vNNN, one more than the running revision; Single mode refuses a repeat).' }
        if (-not $Tag) { throw 'update needs -Tag: the tag of an image already built (deploy.ps1 build; or release, which does both).' }
        $environmentId = Get-Output 'environmentId'
        $identityId = Get-Output $AppInfo.IdentityOutput
        $vaultUri = (Get-Output 'vaultUri').TrimEnd('/')
        $exists = Read-Az containerapp show -n $App -g $ResourceGroup --query 'properties.environmentId' -o tsv
        $inPlace = ($LASTEXITCODE -eq 0 -and $exists -eq $environmentId)
        if ($LASTEXITCODE -eq 0 -and $exists -and -not $inPlace) {
            # A Container App's name is unique in its resource group, and an app can't move between environments.
            throw "$App already exists in another environment ($exists). Delete the old one first: az containerapp delete -n $App -g $ResourceGroup"
        }
        # The secrets block: one reference per secret the vault holds among the names the app may use; a reference to
        # a secret the vault lacks would stop the revision, so what the vault lacks stays out, the providers' lines too.
        $inVault = @(Read-Az keyvault secret list --vault-name $Vault --query '[].name' -o tsv)
        if ($LASTEXITCODE -ne 0) { throw "Could not list ${Vault}: deploy.ps1 secrets first, and check your role on the vault." }
        $suffix = $AppInfo.Suffix
        $names = @('anthropic-api-key', "agent-api-key$suffix", "admin-api-key$suffix")
        foreach ($provider in $Providers) { $names += "auth-$provider-client-id$suffix"; $names += "auth-$provider-client-secret$suffix" }
        $names += @($AppInfo.WhenSet.Values)
        $secrets = @('    secrets:')
        foreach ($name in $names) {
            if ($inVault -contains $name) {
                $secrets += "      - name: $name"
                $secrets += "        keyVaultUrl: $vaultUri/secrets/$name"
                $secrets += "        identity: $identityId"
            }
        }
        foreach ($name in @('anthropic-api-key', "agent-api-key$suffix", "admin-api-key$suffix")) {
            if ($inVault -notcontains $name) { throw "The vault $Vault has no $name yet: deploy.ps1 secrets first." }
        }
        $providerLines = @()
        foreach ($provider in $Providers) {
            if ($inVault -contains "auth-$provider-client-id$suffix" -and $inVault -contains "auth-$provider-client-secret$suffix") {
                $upper = $provider.ToUpperInvariant()
                $providerLines += "          - name: AUTH_${upper}_CLIENT_ID"
                $providerLines += "            secretRef: auth-$provider-client-id$suffix"
                $providerLines += "          - name: AUTH_${upper}_CLIENT_SECRET"
                $providerLines += "            secretRef: auth-$provider-client-secret$suffix"
            }
        }
        # The keys the app runs without (FX87): an env line only for a key the vault holds.
        $whenSetLines = @()
        foreach ($key in $AppInfo.WhenSet.Keys) {
            if ($inVault -contains $AppInfo.WhenSet[$key]) {
                $whenSetLines += "          - name: $key"
                $whenSetLines += "            secretRef: $($AppInfo.WhenSet[$key])"
            }
        }
        $template = Join-Path $Repo $AppInfo.Yaml
        $resolved = Join-Path ([System.IO.Path]::GetTempPath()) "$App.$Revision.yaml"
        (Get-Content $template -Raw).Replace(':TAG', ":$Tag").Replace("'REVISION'", "'$Revision'").Replace('ENVIRONMENT_ID', $environmentId).Replace('APP_IDENTITY_ID', $identityId).Replace('    # SECRETS', ($secrets -join "`n")).Replace('          # PROVIDERS', ($providerLines -join "`n")).Replace('          # WHEN SET', ($whenSetLines -join "`n")) |
            Set-Content $resolved -Encoding utf8 -NoNewline
        if ($inPlace) {
            Write-Host "Updating $App to revision $Revision (tag $Tag) from $template..."
            Invoke-Az containerapp update -n $App -g $ResourceGroup --yaml $resolved --query '{revision:properties.latestRevisionName, ready:properties.latestReadyRevisionName, state:properties.provisioningState}' -o json
        } else {
            Write-Host "Creating $App in $Environment as revision $Revision (tag $Tag) from $template..."
            Invoke-Az containerapp create -n $App -g $ResourceGroup --yaml $resolved --query '{fqdn:properties.configuration.ingress.fqdn, state:properties.provisioningState}' -o json
        }
        Remove-Item $resolved -ErrorAction SilentlyContinue
        Write-Host "Watch it come up with: deploy.ps1 logs"
    }
    'release' {
        # The next vNNN after the highest the app has had (FX123): a revision made outside this script carries Azure's
        # own name (louis-agent-app--0000001), so the latest alone can't say; none at all, and it is the first.
        $revisions = @(Read-Az containerapp revision list -n $App -g $ResourceGroup --all --query '[].name' -o tsv)
        $numbers = @($revisions | Where-Object { $_ -match '--v(\d+)$' } | ForEach-Object { [int]($_ -replace '^.*--v', '') })
        $highest = if ($numbers.Count -gt 0) { [int]($numbers | Measure-Object -Maximum).Maximum } else { 0 }
        $next = 'v{0:D3}' -f ($highest + 1)
        Write-Host "Release: image tagged $Tag, then $App revision $next."
        & $PSCommandPath build -App $App -Tag $Tag
        & $PSCommandPath update -App $App -Tag $Tag -Revision $next
    }
    'domain' {
        # The records Cloudflare needs, DNS-only (a proxied record blocks the certificate): the apex points at the
        # environment's address, the TXT proves the domain is ours, www is a CNAME to the app's own address.
        $staticIp = Get-Output 'environmentStaticIp'
        $fqdn = Read-Az containerapp show -n $App -g $ResourceGroup --query 'properties.configuration.ingress.fqdn' -o tsv
        $verification = Read-Az containerapp show -n $App -g $ResourceGroup --query 'properties.customDomainVerificationId' -o tsv
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
            # Added already on an earlier run that stopped at a pending certificate: adding again would refuse.
            $added = Read-Az containerapp hostname list -n $App -g $ResourceGroup --query "[?name=='$Hostname'].name | [0]" -o tsv
            if (-not $added) { Invoke-Az containerapp hostname add -n $App -g $ResourceGroup --hostname $Hostname -o none }
            if ($apex) {
                # An apex certificate validates with a token of its own, in a TXT record named _dnsauth (2026-11-07: the
                # asuid record proves the hostname is ours, the certificate stays Pending without this one). The
                # certificate is asked for first, so the token can be printed before the bind waits on it.
                $token = Read-Az containerapp env certificate create -n $Environment -g $ResourceGroup --hostname $Hostname --validation-method TXT --query 'properties.validationToken' -o tsv
                if (-not $token) {
                    # Asked again while the first request is pending, create refuses and the list leaves the token out
                    # (2026-11-08); the certificate itself, read at the 2023-05-01 API version, still carries it.
                    $certificate = Read-Az containerapp env certificate list -n $Environment -g $ResourceGroup --managed-certificates-only --query "[?properties.subjectName=='$Hostname'].id | [0]" -o tsv
                    if ($certificate) { $token = Read-Az rest --method get --url "https://management.azure.com${certificate}?api-version=2023-05-01" --query 'properties.validationToken' -o tsv }
                }
                Write-Host "  TXT    _dnsauth    $token      (in Cloudflare too, DNS-only; the certificate validates once it resolves)"
                # The bind refuses a certificate still pending on that record: stop here, and run domain again once it resolves.
                $state = Read-Az containerapp env certificate list -n $Environment -g $ResourceGroup --managed-certificates-only --query "[?properties.subjectName=='$Hostname'].properties.provisioningState | [0]" -o tsv
                if ($state -ne 'Succeeded') { Write-Host "The certificate is $state. Once the _dnsauth record resolves, run deploy.ps1 domain -App $App again to bind it."; return }
            }
            Invoke-Az containerapp hostname bind -n $App -g $ResourceGroup --hostname $Hostname --environment $Environment --validation-method $method --query '[].{name:name, state:bindingType}' -o table
        }
    }
    'sql-access' {
        # SQL Server Management Studio from this PC: the server's firewall takes one IPv4 address (the rule 'home';
        # a new home IP, the ISP's doing, means running this again). The server is puls-sql-za.database.windows.net,
        # the login pulsadmin with config/.env.sql's password, the database puls_accounts, encryption on.
        if (-not $Ip) { $Ip = (Invoke-RestMethod -Uri 'https://ipv4.icanhazip.com' -TimeoutSec 10).Trim() }
        if ($Ip -notmatch '^\d{1,3}(\.\d{1,3}){3}$') { throw "Not an IPv4 address: $Ip. Pass -Ip." }
        Write-Host "Allowing $Ip on puls-sql-za's firewall (rule 'home')..."
        Invoke-Az sql server firewall-rule create -g $ResourceGroup -s 'puls-sql-za' -n 'home' --start-ip-address $Ip --end-ip-address $Ip --query '{name:name, from:startIpAddress, to:endIpAddress}' -o table
    }
    'logs' {
        $followArgs = @()
        if ($Follow) { $followArgs = @("--follow") }
        Invoke-Az containerapp logs show -n $App -g $ResourceGroup --container $App --tail 60 @followArgs
    }
}
