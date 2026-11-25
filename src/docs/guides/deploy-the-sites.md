# Deploying the framework's two sites to Azure Container Apps

> **How-to.** How louis-agent.dev and framework.louis-agent.dev get to Azure, what runs there, where their secrets
> come from, and what to do the first time an app goes live. **Elsewhere:** the pictures in
> [the sites in Azure](../architecture/deployment-in-azure.md), the local stacks in
> [the site's docker/](../../site/docker/docker-compose.yml), every setting in [Setup](../SETUP.md), the two sites
> in [LouisAgent.Site](../projects/LouisAgent.Site.md) and
> [LouisAgent.SiteFramework](../projects/LouisAgent.SiteFramework.md).

![Deploy the sites: every merge releases both, the same verbs from the PC, an app's secrets, its first release, its domain, rolling back](diagrams/deploy-the-sites.svg)

Two websites run in Azure, and both are built on the framework. louis-agent.dev sells websites with agents, and its
agent is the public Orbi. framework.louis-agent.dev teaches the framework, and its agent is the framework Orbi. Each
is one Azure Container App that sleeps when nobody visits, so the first visitor after a quiet spell waits a few
seconds for it to wake.

One script, `src/site/deploy/azure/deploy.ps1`, beside the two apps' yaml, does every job, and one GitHub workflow runs it on every merge. So in
practice you merge a pull request and both sites go out. Beside the script, one Bicep file, `main.bicep`, describes
everything the two apps stand on in Azure, so that can be made again from code too. This guide first says what is in
Azure, then how a release works, then the jobs you do by hand: the first time an app goes live, its domain, its
secrets, rebuilding everything from nothing, and rolling back.

## What is in Azure

Everything is in one resource group, `rg-puls-za`, in South Africa North. The names start with *puls* because Puls
was deployed here first. Since FX122 Puls isn't deployed: `mypuls.co.za` is a "coming soon" page on a free Static
Web App, published by a workflow of Puls's own repository, [`puls`](https://github.com/bosbevk/puls), which holds the
page. The VM, the Azure SQL server and Puls's app went
then, and their story is in git, FX122 and ADRs 0095 and 0098.

| Resource | Name | What it does today |
|---|---|---|
| Environment | `puls-env` | The Container Apps environment both apps run in, on the Consumption profile, with its static IP and its OpenTelemetry agent |
| louis-agent.dev | `louis-agent-app` | The site's host on the image `louis-agent-site`, from `src/site/deploy/azure/louis-agent-app.yaml` |
| framework.louis-agent.dev | `louis-agent-framework-app` | The framework site's host on the image `louis-agent-site-framework`, from `src/site/deploy/azure/louis-agent-framework-app.yaml` |
| Identity | `louis-agent-app-identity` | What both apps read the vault with: Key Vault Secrets User on `puls-keys` |
| Vault | `puls-keys` | Every secret value; the apps hold only references to them |
| Registry | `acrpulsza` | The images; the environment pulls them as itself |
| Storage | `pulsstorage`, the share `louis-agent-workspace` | Mounted on the environment as `louis-agent-vol`; each app's logs and louis-agent.dev's saved CV |
| Logs and traces | `puls-logs`, `puls-insights` | Each app's logs, and its traces through the environment's agent (F52) |

Everything in the table but the two apps is described in `main.bicep`. It takes every name as a parameter, and our
own names, the ones above, are in `main.bicepparam` beside it, so another business deploys the same file with a
parameters file of its own. Most parts come from Microsoft's Azure Verified Modules, each pinned to a version. The
network and the environment are plain Bicep, since the environment's module has no setting for the telemetry agent
and the network's would rename the subnet's delegation. Each part is told what stands wherever a module's default
differs, so a deploy over the running sites changes nothing. The file also gives the rights a release needs: the
apps' identity reads the vault, the environment pulls from the registry, the release identity updates the apps and
lists the vault, and the person who fills the vault manages its secrets. CI builds and lints the file on every pull
request, and deploys nothing.

The two apps share one identity and one share on purpose. Microsoft lets one user-assigned
identity serve several resources, and lets several apps mount one share, each in a folder of its own, so a second
site needed nothing new in Azure but the app.

## How a release works

`deploy.ps1 release -App <app>` does three things, in this order. It makes the folders the app's entry names on the
share, so a first release finds them. It builds the image with Docker where it runs, tagged with the date and time,
and pushes it to the registry: on the release's runner that build costs nothing, where a build in the registry is
paid by the minute. So a release from your PC needs Docker running. And it fills in the app's yaml and creates or updates the app as the next
revision, `vNNN`, one more than the highest it has ever had (FX123).

![A release: the folders its entry names, the image built in the registry, then the yaml filled in and the app updated as its next revision, until /health answers](diagrams/deploy-the-sites-flow.svg)

What makes an app an app is its entry in the script's table, `$Apps`: its image and Dockerfile, its yaml, its config
folder and its secrets file, the identity it reads the vault with, its share and the folders on it, its hostname, and
the suffix on the vault secrets that are its own. A second site was one more entry, not new code.

Filling in the yaml is where the secrets come in. `update` lists the names the vault holds, writes a `secrets:` block
of references to them, and adds an environment line for a key the app can run without only once the vault has it, so
a missing key never stops a revision (FX86, FX87). It also passes this checkout's commit to the build, so commit
before you build. An app is up when `/health` answers 200.

## Every merge releases both sites

A merge to `main` runs `.github/workflows/deploy-sites.yml`, and *Actions → Deploy the apps → Run workflow* runs it any
time from `main`. It releases louis-agent.dev first, then framework.louis-agent.dev, one job on one queue, so two
merges never release at once and one never stops another half-way. If louis-agent.dev's release fails, the job stops
there and the framework's isn't tried. If the framework's fails, louis-agent.dev is already out. The run's summary
lists each app's running revisions.

The workflow signs in to Azure by OpenID Connect. GitHub signs a short-lived token for the run, and Azure trusts it
only for this repo's `main` branch, so GitHub stores no password. The identity it signs in as, `github-puls-deploy`,
is Contributor on `rg-puls-za`, which lets it push to the registry, read the storage key to make a folder, and
create or update an app. It is also Key Vault Reader on `puls-keys`, so it can read the secrets' *names*, never a
value. The verbs that read files kept off git, `secrets` and `domain`, stay on the PC. The why is
[ADR 0097](../adr/0097-puls-deploys-from-github-without-a-password.md).

That identity was made once, from the PC, signed in with `az login` as the subscription's owner, in PowerShell:

```powershell
$sub = az account show --query id -o tsv
$tenant = az account show --query tenantId -o tsv
$appId = az ad app create --display-name github-puls-deploy --query appId -o tsv
az ad sp create --id $appId -o none
# The trust: tokens GitHub signs for bosbevk/louis-agent's main branch, and nothing else. A file, since az is a
# batch file on Windows and cmd mangles quotes in JSON on a command line.
$fic = Join-Path $env:TEMP 'github-puls-deploy.json'
'{"name":"louis-agent-main","issuer":"https://token.actions.githubusercontent.com","subject":"repo:bosbevk/louis-agent:ref:refs/heads/main","audiences":["api://AzureADTokenExchange"]}' |
    Set-Content $fic -Encoding ascii
az ad app federated-credential create --id $appId --parameters "@$fic" -o none
Remove-Item $fic
az role assignment create --assignee $appId --role Contributor --scope "/subscriptions/$sub/resourceGroups/rg-puls-za" -o none
az role assignment create --assignee $appId --role 'Key Vault Reader' --scope (az keyvault show -n puls-keys --query id -o tsv) -o none
"AZURE_CLIENT_ID=$appId"; "AZURE_TENANT_ID=$tenant"; "AZURE_SUBSCRIPTION_ID=$sub"
```

The three printed values are repository **variables** in GitHub, under *Settings → Secrets and variables → Actions →
Variables*, since they are ids, not secrets. Until they are set, a run stops at its first step and says which are
missing.

## Releasing from the PC

The same verbs run from the PC, after `az login`, from `src\site`:

```powershell
deploy\azure\deploy.ps1 release                                  # louis-agent.dev, the default app
deploy\azure\deploy.ps1 release -App louis-agent-framework-app   # framework.louis-agent.dev
deploy\azure\deploy.ps1 logs -App louis-agent-framework-app      # its last lines; -Follow to tail
```

`build` and `update` run on their own too, `update` with the `-Tag` of an image already built and a `-Revision`.
`workspace` copies an app's price tables into `config/` on its share, though both images now carry their own.

`infra` deploys `main.bicep`. Azure shows its what-if first, every resource it would create, change or delete, and
deploys only when you answer yes, under the deployment name `louis-agent-infra`. Every other verb reads the
environment, the identity and the vault from the outputs Azure recorded for that deployment. A release identity can't
give rights, and the file gives them, so `infra` is run by a person, signed in as someone who can, never by the
workflow:

```powershell
deploy\azure\deploy.ps1 infra    # what-if, then a deploy on your yes
```

## An app's secrets

Every value lives in the vault, and no value is ever in a file under git or on an app. `deploy.ps1 secrets -App
<app>` reads the app's git-ignored secrets file, puts one vault secret per value, prints only the names, and skips a
name whose value is missing. A running revision keeps the values it read, so a changed key needs `secrets`, then a
release. No value may hold `&` or `|`: az is a batch file on Windows, and cmd would cut it there.

| Vault secret | From | Read by |
|---|---|---|
| `anthropic-api-key` | `ANTHROPIC_API_KEY` in `src/site/config/.env.secrets` | Both sites' model calls; the framework's file leaves it out, so its `secrets` keeps this one as it is |
| `agent-api-key-louis-agent`, `admin-api-key-louis-agent` | `AGENT_API_KEY`, `ADMIN_API_KEY` in `src/site/config/.env.secrets` | louis-agent.dev's key gate and admin |
| `resend-api-key` | `RESEND_API_KEY` in `src/site/config/.env.secrets` | louis-agent.dev's lead mail (F112); named in its yaml only once the vault holds it |
| `agent-api-key-framework`, `admin-api-key-framework` | `AGENT_API_KEY`, `ADMIN_API_KEY` in `src/site/config/.env.secrets.framework` | framework.louis-agent.dev's key gate and admin |

Both sites read the same config folder, `src/site/config/`, so the framework keeps its own keys in a file of their
own, `.env.secrets.framework`, copied from `.env.secrets.framework.example`. A release needs both of an app's own keys
in the vault, and stops and says so when one is missing.

## The first time an app goes live

A new app comes to life on its first release, which creates it. framework.louis-agent.dev is created by the first
merge after F164.3. Before that merge:

1. Copy `src/site/config/.env.secrets.framework.example` to `.env.secrets.framework` and fill in its two keys.
2. Run `deploy\azure\deploy.ps1 secrets -App louis-agent-framework-app`, so the vault holds them.

Then the merge releases it, and it answers on its own Azure address. Its domain comes next.

## A domain and its certificate

`deploy.ps1 domain -App <app>` prints the records Cloudflare needs, adds the hostname to the app, and asks Azure for a
managed certificate. The records go in Cloudflare as DNS-only, the grey cloud, since a proxied record blocks the
certificate. Run it, add the records, wait until they resolve, and run it again to bind the certificate.

What it prints depends on the name. A name of two parts, such as `louis-agent.dev`, is an apex. A longer one, such as
`framework.louis-agent.dev` or `www.louis-agent.dev`, is a subdomain, and its first label takes the records.

| For | Type | Name | Value |
|---|---|---|---|
| An apex | A | `@` | The environment's static IP, as printed |
| An apex | TXT | `asuid` | The app's domain verification id, as printed |
| An apex | TXT | `_dnsauth` | The certificate's validation token, printed once the certificate is asked for; without it the certificate stays Pending |
| A subdomain | CNAME | its first label, such as `framework` | The app's own address, `<app>.<env>.southafricanorth.azurecontainerapps.io` |
| A subdomain | TXT | `asuid.` and its first label, such as `asuid.framework` | The same verification id; a subdomain validates by its CNAME, with no token |

`-Hostname` names another one for the same app, as `-Hostname www.louis-agent.dev` did for louis-agent.dev's `www`.
When louis-agent.dev's zone was first added to Cloudflare, Cloudflare had imported the old host's records, a proxied
wildcard and apex and a mail setup, which had to go first.

## The share's layout

Each app mounts only its own logs folder from `louis-agent-workspace`, at `/logs`, and louis-agent.dev also mounts
`data/`, at `/data`. The images carry the rest: the docs each Orbi reads, at `/app/docs`, laid out as the repo with
the films left out, and the price tables, at `/app/config`. A new revision carries them as they were at its commit.

| Folder | Holds |
|---|---|
| `logs/` | louis-agent.dev's run files, its usage ledger (`usage-YYYY-MM.jsonl`) and its tool-result records |
| `data/` | What louis-agent.dev keeps of its own: the CV as the admin last saved it (F142) |
| `framework/logs/` | The same as `logs/`, for framework.louis-agent.dev; its release makes the folder |
| `framework/data/` | framework.louis-agent.dev's evals: their runs and what a live run records; its release makes the folder |
| `config/` | The price tables `deploy.ps1 workspace` copies, which neither image needs now |

## Rebuilding from nothing

If the resource group were lost, the sites come back from code in four steps, each one a command this guide already
has. First `deploy.ps1 infra`, which makes the network, the environment, the registry, the vault, the identity, the
storage and the rights. Then, for each app, `deploy.ps1 secrets -App <app>` puts its keys in the new vault from its
git-ignored file, `deploy.ps1 release -App <app>` builds its image and makes the app, and `deploy.ps1 domain -App <app>`
prints the DNS records for Cloudflare and binds the certificate once they resolve, as *A domain and its certificate*
says. The registry, the vault and the storage account have names the whole of Azure shares, and a deleted vault
keeps its name for 90 days, so a rebuild beside the old group needs a parameters file with new names.

## Rolling back

Activate the revision before, by its name from the list, or re-run an older *Deploy the apps* run, which deploys
the commit it ran:

```powershell
az containerapp revision list -n louis-agent-app -g rg-puls-za -o table
az containerapp revision activate -n louis-agent-app -g rg-puls-za --revision louis-agent-app--v051
```

## Cost

The environment bills by the second while a replica runs, and each app scales to nothing when nobody is on it. The
registry's Basic tier, the share's first 5 GB a month and the logs are the rest. A spend cap on the Anthropic key, in
its console, is the other half of the bill, and both sites' turns are priced in their own usage ledgers. Each
month's figure is written here after the first.

---
[Docs index](../README.md) · Related: [The sites in Azure](../architecture/deployment-in-azure.md) · [Deployment view](../architecture/07-deployment-view.md) · [Setup](../SETUP.md)
