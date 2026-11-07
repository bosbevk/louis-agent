# Deploying Puls to Azure Container Apps

> **How-to.** What runs in the cloud, how to build and deploy it, where its secrets come from, and how it reaches its
> stores, its browser and its search. **Elsewhere:** the local stacks in [docker/](../../docker/README.md), every
> setting in [Setup](../../../../src/docs/SETUP.md), the build's record in `demos/puls/config/azure/main.bicep`, why
> the layout is what it is in the F61.4 plan until its ADR.

Everything here is in resource group `rg-puls-za` (South Africa North). This folder is the app's definition, the VM's
files and one script; the Bicep build in `config/azure/` is everything else. No secret value is in any file here.

Live since 2026-11-07 on `https://mypuls.co.za` and `https://www.mypuls.co.za`. It replaced the CL19 deploy, which
reached the PC's browser and Mongo through a Tailscale sidecar with SearXNG as its own app and the ingress locked to
one IP, and the F58 tunnel that served a placeholder from the PC; both are gone, in Azure and in the repo.

## What the Bicep build makes (`deploy.ps1 infra`)

| Resource | Name | What it is |
|---|---|---|
| Network | `puls-vnet` | `10.0.0.0/16`: the apps' subnet `10.0.0.0/23`, delegated to Container Apps, and the services' subnet `10.0.2.0/24` |
| Security group | `puls-services-nsg` | On the services' subnet: 27017, 9222 and 8080 from the apps' subnet, everything else denied; SSH from one address only with `infra -Ip` |
| VM | `puls-services` | Ubuntu 24.04, `Standard_B2s`, no public IP, a 32 GB Standard SSD data disk as XFS at `/data`; cloud-init from `vm/cloud-init.yaml` installs Docker and a unit that runs `vm/docker-compose.services.yml`: `mongo:9` on 27017 (not the PC's 8: the image's 7.0 kernel refuses MongoDB 8, SERVER-121912), `headless-shell` on 9222, `searxng` on 8080, all on the private IP |
| App identity | `puls-app-identity` | A user-assigned identity with Key Vault Secrets User on `puls-keys`; the app reads its secrets with it |
| Environment | `puls-env` | A Container Apps environment on the apps' subnet with a Consumption profile; logs to `puls-logs`, traces and logs to `puls-insights` through its OpenTelemetry agent; its identity pulls from `acrpulsza`; the app's share `puls-app-workspace` as storage `puls-app-vol` |
| Application Insights | `puls-insights` | Puls's logs and traces (F52), on `puls-logs` |
| SQL | `puls-sql-za`, database `puls_accounts` | Azure SQL's free offer: serverless General Purpose, 32 GB, 100,000 vCore-seconds a month, paused when idle and when the month's seconds run out, never billed; "Allow Azure services" on, TLS 1.2 |
| Kept from CL19 | `acrpulsza`, `puls-logs`, `pulsstorage`, `puls-keys` | The registry, the workspace, the storage account (the share `puls-app-workspace`), the vault: referenced, never redeclared |

The deployment is `puls-build`; it creates what is missing and updates what exists, and its outputs (the VM's private
IP, the environment's id and static IP, the SQL server's address) are what the verbs read back, never a value copied
by hand. The app itself is `puls-app.yaml`, deployed by `deploy.ps1 release` into `puls-env`.

## First time

1. `az login`, and `az bicep install` once; `ssh-keygen -t ed25519` once (the VM takes `~/.ssh/id_ed25519.pub`);
   Docker is not needed, the registry builds the image.
2. `config/.env.secrets`, `config/.env.mongo`, `config/.env.searxng` and `config/.env.sql` as for the local stack
   (the SQL password in `.env.sql` is the Azure SQL admin's too).
3. `deploy.ps1 infra -WhatIf` to see what would be made, then `deploy.ps1 infra` (the VM and the environment take
   some minutes; the first boot runs cloud-init, another few).
4. `deploy.ps1 vm`: the compose, SearXNG's settings and the Mongo and SearXNG env files go up through run-command and
   the services start; the command prints `docker ps` from the VM.
5. `deploy.ps1 secrets`: the app's secrets, with the Mongo and SQL connection strings built from the outputs.
6. `deploy.ps1 release`: builds the image and creates `puls-app` in `puls-env` as revision `v001` (the old app of the
   same name in the old environment is untouched: a Container App belongs to one environment).
7. `deploy.ps1 domain -WhatIf` prints the DNS records; put them in Cloudflare, DNS-only (grey cloud: a proxied record
   blocks the certificate), then `deploy.ps1 domain` for the apex and `deploy.ps1 domain -Hostname www.mypuls.co.za`:

   | Type | Name | Value |
   |---|---|---|
   | A | `@` | the environment's static IP, as printed |
   | TXT | `asuid` | the app's domain verification id, as printed |
   | TXT | `_dnsauth` | the apex certificate's validation token, which `domain` prints once it has asked for the certificate (the `asuid` record proves the hostname is ours; the certificate stays Pending without this one) |
   | CNAME | `www` | the app's own address (`puls-app.<env>.southafricanorth.azurecontainerapps.io`) |
   | TXT | `asuid.www` | the same verification id (`www` validates by its CNAME, no token) |

8. Done on 2026-11-07, for the record: the old environment `puls-container-env` with its `puls-app` and
   `puls-searxng`, the `ts-forwarder` and `searxng` images in the registry and the `searxng-config` and `mongo-data`
   shares were deleted, with the earlier design's `mongo`, `headless-shell` and `searxng` images and the backups of
   the two shares in the recovery vault `vault-mvihj8bq`; that vault keeps its soft-deleted backups 14 days and
   refuses to shorten that, so it is deleted after 2026-11-22 (`az backup vault delete -n vault-mvihj8bq -g rg-puls-za --yes`),
   at no cost meanwhile. On the PC, `tailscale serve reset` and `docker compose -p mypuls-production down` end the
   old paths, and the tunnel `mypuls-placeholder` goes in Cloudflare Zero Trust.

## Every-day deploy

```powershell
deploy\azure\deploy.ps1 release     # build (tagged date and time) and deploy the next revision, in one go
deploy\azure\deploy.ps1 logs        # -Follow to tail
deploy\azure\deploy.ps1 vm          # after a change to the VM's compose or SearXNG's settings
deploy\azure\deploy.ps1 infra       # after a change to main.bicep (what-if first)
deploy\azure\deploy.ps1 sql-access  # once per home IP, for Management Studio
```

`build` passes this checkout's commit to the Puls image as `EVAL_COMMIT`, so an eval run in the cloud says which
commit it ran; commit before you build. `release` is `build` then `update` with the revision after the one Azure runs;
`update` fills `TAG`, `REVISION`, the VM's address and the environment's id into `puls-app.yaml`, adds a line pair for
each sign-in provider whose two secrets are set, and creates or updates the app. The running revision is in
`az containerapp revision list -n puls-app -g rg-puls-za -o table`; the app is up when `/health` answers 200.

## Secrets

Every value is in the vault `puls-keys` (Louis's pick, 2026-11-08): `deploy.ps1 secrets` writes it there, the app's
secrets are references to it by URL read with `puls-app-identity`, and `update` writes the yaml's `secrets:` block
from the names the vault holds, so no value is ever on the app or in a file under git.

| Vault secret | From | Read by |
|---|---|---|
| `anthropic-api-key` | `config/.env.secrets` `ANTHROPIC_API_KEY` | the model |
| `agent-api-key` | `config/.env.secrets` `AGENT_API_KEY` | the key gate: a `/v1` call needs it or a signed-in person (F61.2); the peer call sends it |
| `admin-api-key` | `config/.env.secrets` `ADMIN_API_KEY` | the admin portal |
| `mongo-url` | `config/.env.mongo`'s user and password at the VM's private IP | the eval store, the shop prices, the recipe library |
| `accounts-sql-url` | `config/.env.sql`'s password, the admin `pulsadmin`, the server from the outputs | the accounts (Identity's tables, F61.1) |
| `auth-google-client-id` and `-secret`, `auth-microsoft-…`, `auth-linkedin-…` | `config/.env.secrets` `AUTH_*` | the sign-in providers (F61.3); a pair is set together or not at all |

`deploy.ps1 secrets` copies them all, prints only their names, and skips one whose value is missing. No value may hold
`&` or `|` (az is a batch file on Windows, and cmd would cut it there). To rotate one: change it locally, run
`secrets`, then `release` (a running revision keeps its old values).

## The VM

Nothing on `puls-services` is reachable from the internet: no public IP, and the security group lets only the apps'
subnet in on the three ports. Files go up with `deploy.ps1 vm` through `az vm run-command` (Azure's agent runs the
script as root; the script, env files included, passes through Azure's control plane, which keeps the last one in the
VM's run-command history, so the values there are the same ones the app carries as secrets). SSH is off unless
`infra -Ip <your IPv4>` opened it from one address; `infra` without `-Ip` closes it again. Mongo's files are on the
data disk, which outlives the VM: `az vm delete` keeps it unless told otherwise.

Checks from inside the app, when something doesn't answer (the VM's IP from `az deployment group show -n puls-build -g rg-puls-za --query properties.outputs.vmPrivateIp.value -o tsv`):

```powershell
az containerapp exec -n puls-app -g rg-puls-za --command "curl -s -m 10 http://<vm-ip>:9222/json/version"    # Chrome's version
az containerapp exec -n puls-app -g rg-puls-za --command "curl -s -m 10 http://<vm-ip>:8080/healthz"         # SearXNG
az vm run-command invoke -n puls-services -g rg-puls-za --command-id RunShellScript --scripts "docker ps"   # the three containers
```

**A look at the collections** (the VM is private, so no Studio 3T; Louis, 2026-11-08: "fine for now"): `mongosh`
inside the container, with the user from `config/.env.mongo`, lists what each database holds:

```powershell
az vm run-command invoke -n puls-services -g rg-puls-za --command-id RunShellScript --scripts "docker exec puls_mongo mongosh -u <user> -p <password> --quiet --eval 'db.adminCommand({listDatabases:1}).databases.forEach(d => print(d.name, db.getSiblingDB(d.name).getCollectionNames()))'" --query "value[0].message" -o tsv
```

**SQL Server Management Studio** reaches Azure SQL once `deploy.ps1 sql-access` has let this PC's IPv4 through the
server's firewall: server `puls-sql-za.database.windows.net`, port 1433, login `pulsadmin` with `config/.env.sql`'s
password, database `puls_accounts`, encryption on. A new home IP means `sql-access` again.

## The share's layout

`puls-app-workspace` is mounted at `/workspace`, with `logs/` also at `/logs` and `data/` at `/data`:

| Folder | Holds |
|---|---|
| `config/` | `prices.json` and `rates.json`, as `PRICES_FILE` and `RATES_FILE` expect; copy them from the repo's `config/` when they change |
| `logs/` | The run files (`agent-api-*.log`), the usage ledger (`usage-YYYY-MM.jsonl`) and the tool-result records |
| `data/` | `shop-stores.json`, the shops' store-per-town cache (F34), the pages an eval run saves |

## What the browser says it is

The VM's browser reads shops' pages for the app's users, and tells every site so: `BROWSER_USER_AGENT` in
`puls-app.yaml` is `Puls/1.0 (+https://github.com/bosbevk/louis-agent/blob/main/demos/puls/docs/SITE_OWNERS.md; reads a page on a user's behalf)`, who reads, a public page to learn more, and
why (FX21). Never a person's name or place: the string reaches every site and tracker a page touches.

## Cost

Written here after the first month, against the parent plan's estimate: the VM and its two disks are the fixed part
(a `Standard_B2s` and two Standard SSDs), the environment bills by the second for the one replica, the database is
free within its offer, the registry's Basic tier and the workspace's first 5 GB a month are the rest. A spend cap on
the Anthropic key, in the console, is the other half of the bill.

---
[Docs index](../../../../src/docs/README.md) · Related: [Setup](../../../../src/docs/SETUP.md) · [Deployment view](../../../../src/docs/architecture/07-deployment-view.md) · [The site on Azure](../../../louis-agent/deploy/azure/README.md)
