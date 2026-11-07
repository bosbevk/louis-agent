<#
.SYNOPSIS
Deploys the framework's site to Azure (rg-louis-agent-za): the Bicep build in config/azure and the site's revisions,
as verbs that are each a few az commands with the names filled in (F61.4; the shape of demos/puls/deploy/azure/deploy.ps1).

.DESCRIPTION
  deploy.ps1 infra   [-WhatIf]                   makes the resource group and deploys demos/louis-agent/config/azure/main.bicep:
                                                  the registry, the logs, the environment; creates what is missing, updates
                                                  what exists. -WhatIf prints Azure's own what-if and writes nothing.
  deploy.ps1 build   [-Tag 2026-11-08-1200]       builds louis-agent-site in the registry
  deploy.ps1 update  -Tag ... -Revision v002      fills TAG, REVISION and ENVIRONMENT_ID into site.yaml and creates or updates the app
  deploy.ps1 release [-Tag ...]                   build, then update as the next revision after the one Azure runs
  deploy.ps1 domain  [-Hostname louis-agent.dev]  prints the DNS records Cloudflare needs, then adds the hostname with a
                                                  managed certificate once they resolve
  deploy.ps1 logs    [-Follow]
The how-to, and what each piece is, is README.md beside this file. Run `az login` first; `az bicep install` once.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet('infra', 'build', 'update', 'release', 'domain', 'logs')]
    [string]$Verb,
    [string]$Tag,
    [string]$Revision,
    [string]$Hostname = 'louis-agent.dev',
    [switch]$Follow
)
$ErrorActionPreference = 'Stop'
if (-not $Tag -and $Verb -in @('build', 'release')) { $Tag = Get-Date -Format 'yyyy-MM-dd-HHmm' }
$ResourceGroup = 'rg-louis-agent-za'
$Location = 'southafricanorth'
$Registry = 'acrlouisagentza'
$Environment = 'louis-agent-env'
$App = 'louis-agent-site'
$Deployment = 'louis-agent-build'
$Repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
$Here = $PSScriptRoot
$Bicep = Join-Path $Repo 'demos\louis-agent\config\azure\main.bicep'

function Invoke-Az {
    # az is a native command: its progress spinner and warnings go to stderr, which Windows PowerShell turns into errors
    # when the output is redirected, so judge it by its exit code alone (its own stderr has already said why).
    $ErrorActionPreference = 'Continue'
    & az @args
    $ErrorActionPreference = 'Stop'
    if ($LASTEXITCODE -ne 0) { throw "az $($args[0]) $($args[1]) failed ($LASTEXITCODE)" }
}

function Get-Output([string]$Name) {
    # One output of the last Bicep deployment: the environment's id or address.
    $value = (& az deployment group show -n $Deployment -g $ResourceGroup --query "properties.outputs.$Name.value" -o tsv)
    if ($LASTEXITCODE -ne 0 -or -not $value) { throw "The deployment $Deployment has no output $Name yet: run deploy.ps1 infra first." }
    return $value
}

switch ($Verb) {
    'infra' {
        if ($WhatIfPreference) {
            Write-Host "The resource group $ResourceGroup would be made in $Location if missing; then what the deployment $Deployment would change (nothing is written):"
            if ((& az group exists -n $ResourceGroup) -eq 'true') { Invoke-Az deployment group what-if -n $Deployment -g $ResourceGroup -f $Bicep }
            else { Write-Host '  (the group does not exist yet, so what-if can only run after it is made)' }
            return
        }
        Write-Host "Making $ResourceGroup in $Location if missing, then deploying $Bicep as $Deployment..."
        Invoke-Az group create -n $ResourceGroup -l $Location --query 'properties.provisioningState' -o tsv
        Invoke-Az deployment group create -n $Deployment -g $ResourceGroup -f $Bicep --query 'properties.outputs' -o json
        Write-Host 'Next: deploy.ps1 release, then deploy.ps1 domain.'
    }
    'build' {
        Write-Host "Building louis-agent-site:$Tag from the repo root (demos/louis-agent/docker/images/Dockerfile.site)..."
        Invoke-Az acr build -r $Registry -t "louis-agent-site:$Tag" -f (Join-Path $Repo 'demos\louis-agent\docker\images\Dockerfile.site') $Repo
    }
    'update' {
        if (-not $Revision) { throw 'update needs -Revision (vNNN, one more than the running revision; Single mode refuses a repeat).' }
        if (-not $Tag) { throw 'update needs -Tag: the tag of an image already built (deploy.ps1 build; or release, which does both).' }
        $environmentId = Get-Output 'environmentId'
        $template = Join-Path $Here 'site.yaml'
        $resolved = Join-Path ([System.IO.Path]::GetTempPath()) "$App.$Revision.yaml"
        (Get-Content $template -Raw).Replace(':TAG', ":$Tag").Replace("'REVISION'", "'$Revision'").Replace('ENVIRONMENT_ID', $environmentId) |
            Set-Content $resolved -Encoding utf8 -NoNewline
        $exists = (& az containerapp show -n $App -g $ResourceGroup --query 'name' -o tsv 2>$null)
        if ($LASTEXITCODE -eq 0 -and $exists) {
            Write-Host "Updating $App to revision $Revision (tag $Tag) from $template..."
            Invoke-Az containerapp update -n $App -g $ResourceGroup --yaml $resolved --query '{revision:properties.latestRevisionName, ready:properties.latestReadyRevisionName, state:properties.provisioningState}' -o json
        } else {
            Write-Host "Creating $App in $Environment as revision $Revision (tag $Tag) from $template..."
            Invoke-Az containerapp create -n $App -g $ResourceGroup --yaml $resolved --query '{fqdn:properties.configuration.ingress.fqdn, state:properties.provisioningState}' -o json
        }
        Remove-Item $resolved -ErrorAction SilentlyContinue
        Write-Host 'Watch it come up with: deploy.ps1 logs'
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
