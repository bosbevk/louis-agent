# Deploying Puls to Azure Container Apps

> **How-to.** What runs in the cloud, how to build and deploy it, where its secrets come from, and how it reaches the
> browser and the benchmark store on the home PC. **Elsewhere:** the local stacks in [docker/](../../docker/README.md),
> every setting in [Setup](../../docs/SETUP.md), why the layout is what it is in the
> [CL19 plan](../../docs/plans/CL19-config-by-place.md).

Everything here is in resource group `rg-puls-za` (South Africa North). This folder is the whole footprint: two app
definitions, the tunnel's sidecar image and one script. No secret value is in any file here.

## What exists in Azure

| Resource | Name | What it is |
|---|---|---|
| Container App | `puls-app` | Puls: the `louis-agent-puls` image (the same one `docker/images/Dockerfile.puls` builds) and its `ts-forwarder` sidecar, in one pod. Public address `puls-app.greenwater-cdd33adc.southafricanorth.azurecontainerapps.io` |
| Container App | `puls-searxng` | The SearXNG it searches with (development and benchmarks only, ADR 0023) |
| Environment | `puls-container-env` | The Container Apps environment both run in; its storages name the file shares below |
| Registry | `acrpulsza` | `louis-agent-puls`, `ts-forwarder` (ours, built by `deploy.ps1 build`) and `searxng/searxng` (SearXNG's own image, copied in) |
| Storage account | `pulsstorage` | File shares: `puls-app-workspace` (storage `puls-app-vol`: the app's `config/`, `logs/` and `data/`), `searxng-config` (storage `searxngvol`: SearXNG's `settings.yml`) |
| Unused | `mongo-data` share (storage `mongo-data-vol`), `mongo` and `headless-shell` images | From an earlier design with Mongo and the browser in Azure; both run on the home PC now. Deleting them is a decision for Louis |

The benchmark store (Mongo) and the headless browser are **not** in Azure: they run on the home PC in the local Puls
stack, and `puls-app` reaches them over Tailscale (*The tunnel* below).

## First time on a new PC

1. `az login`, with access to `rg-puls-za`; Docker is not needed, the registry builds the images.
2. Fill `config/.env.secrets` (as for the local stack) and add `TS_AUTHKEY`, a Tailscale auth key made **reusable and
   ephemeral** in the Tailscale admin console; `config/.env.mongo` and `config/.env.searxng` as for the local stack.
3. Put the secrets on the apps: `deploy.ps1 secrets` and `deploy.ps1 secrets -App puls-searxng`.
4. Start the home end of the tunnel (*The tunnel*); a new PC has a new Tailscale IP, so `FORWARD_TO_HOST` in
   `puls-app.yaml` changes with it.
5. `deploy.ps1 lock`: the ingress allows one public IP, and a new PC or network has a new one (*Locked to one
   address*), so until this runs the app answers 403 from here.
6. `deploy.ps1 release` for the first deploy from this PC, then `deploy.ps1 logs` to watch it come up.

## Every-day deploy

```powershell
deploy\azure\deploy.ps1 release                      # build both images (today's tag) and deploy the next revision, in one go
deploy\azure\deploy.ps1 logs                         # the app; -Container ts-forwarder for the sidecar; -Follow to tail
```

`release` is `build` then `update` with the revision after the one Azure runs (`v018` becomes `v019`); the two verbs
stay for a deploy of an image already built (`update -Tag 2026-10-22b -Revision v017`, as the first sidecar deploy was).

`update` fills `TAG` and `REVISION` into `puls-app.yaml` and runs `az containerapp update --yaml`. The yaml sets no
`secrets:` block and names every secret by `secretRef`, and the update is a patch, so the secrets set on the app stay.
The running revision is in `az containerapp revision list -n puls-app -g rg-puls-za -o table`; the app is up when
`https://puls-app.…/health` answers 200. The same for SearXNG with `-App puls-searxng`; its settings file is the local
stack's `docker/searxng/settings.yml`, uploaded with `deploy.ps1 searxng-settings` and read on its next restart.

## Secrets

| App secret | From | Read by |
|---|---|---|
| `anthropic-api-key` | `config/.env.secrets` `ANTHROPIC_API_KEY` | `puls-app` |
| `admin-api-key` | `config/.env.secrets` `ADMIN_API_KEY` | `puls-app` (the admin portal) |
| `benchmarks-mongo-url` | built from `config/.env.mongo`'s user and password, at `127.0.0.1:27017` (the tunnel) | `puls-app` |
| `ts-authkey` | `config/.env.secrets` `TS_AUTHKEY` | the `ts-forwarder` sidecar |
| `searxng-secret` | `config/.env.searxng` `SEARXNG_SECRET` | `puls-searxng` |

`deploy.ps1 secrets` copies them all, prints only their names, and skips one whose value is missing. To rotate one:
change it locally, run `secrets`, then `update` with a new revision (a running revision keeps its old values).

## The tunnel

`puls-app` needs the home PC's headless browser (9222) and Mongo (27017). Azure Container Apps has no `/dev/net/tun`,
so the `ts-forwarder` sidecar runs Tailscale in userspace mode and, for each port in `FORWARD_PORTS`, a `socat`
listener on the pod's loopback that dials `FORWARD_TO_HOST` (the PC's Tailscale IP) with `tailscale nc`. The app
container shares that loopback, hence `BROWSER_URL=http://127.0.0.1:9222` and Mongo at `127.0.0.1:27017`.

**The home end** is `tailscale serve` on the PC, forwarding the two tailnet ports to the loopback ports the local
Puls stack publishes (Docker never binds wider than `127.0.0.1`; Louis, 2026-10-24). Once per PC, in the background so
it survives the terminal and reboots:

```powershell
tailscale serve --bg --tcp 9222 tcp://127.0.0.1:9222
tailscale serve --bg --tcp 27017 tcp://127.0.0.1:27017
tailscale serve status                                # both forwards listed
```

Only tailnet members reach those ports; the DevTools port has no authentication of its own, so never publish it any
other way. The PC's Tailscale IP is pinned in `puls-app.yaml` (`FORWARD_TO_HOST`); if the PC rejoins the tailnet with a
new one, change it there and `update`.

The sidecar's Tailscale state is on ephemeral storage, so every replica joins as a new node (`puls-app`, `puls-app-1`,
…). With an **ephemeral** auth key a dead replica's node disappears by itself; with a plain key they pile up in the
admin console (44 of them after one crash-looping day, 2026-10-24) and have to be deleted by hand.

Checks from inside the pod, when something doesn't answer:

```powershell
az containerapp exec -n puls-app -g rg-puls-za --container puls-app --command "curl -s -m 10 http://127.0.0.1:9222/json/version"   # Chrome's version
az containerapp exec -n puls-app -g rg-puls-za --container puls-app --command "curl -sS -m 8 -o /dev/null -w %{exitcode} http://127.0.0.1:27017"   # 52 = connected (Mongo closes HTTP), 7 = refused
```

## The share's layout

`puls-app-workspace` is mounted at `/workspace`, with `logs/` also at `/logs` and `data/` at `/data`:

| Folder | Holds |
|---|---|
| `config/` | `prices.json` and `rates.json`, as `PRICES_FILE` and `RATES_FILE` expect; copy them from the repo's `config/` when they change |
| `logs/` | The run files (`agent-api-*.log`), the usage ledger (`usage-YYYY-MM.jsonl`) and the tool-result records |
| `data/` | `shop-stores.json`, the shops' store-per-town cache (F34) |

## Locked to one address

`puls-app` serves a public address with `AGENT_AUTH=off`, so anyone who reached it would chat on the Anthropic key, and
this file names the address. Until the TODO's *The Azure app behind a key* closes that, the ingress allows one IP:
the home PC's, as an access restriction on the app in Azure (never in the repo, since a home IP is personal and
changes). `deploy.ps1 lock` sets it to this PC's public IP (or `-Ip`); every other source gets a 403, checked from
inside Azure itself on 2026-10-24. The rule is not in `puls-app.yaml`, so `deploy.ps1 update` lists the allow-list
afterwards: an empty list means the update wiped it, and `lock` puts it back. A new home IP (the ISP's doing) shows
as a 403 from the browser; `lock` again. A spend cap on the Anthropic key, in the console, is the other half.

---
[Docs index](../../docs/README.md) · Related: [Setup](../../docs/SETUP.md) · [Deployment view](../../docs/architecture/07-deployment-view.md)
