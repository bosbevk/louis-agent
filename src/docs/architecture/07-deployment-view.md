# 7. Deployment view

> **Explanation.** Where each part runs: the Docker images and services, and what they mount. **Elsewhere:** the
> settings in [Setup](../SETUP.md); the orchestrator demo was removed in CL10 (its last run at the [`pre-cl10` tag](https://github.com/bosbevk/louis-agent/blob/pre-cl10/samples/README.md)).

## Deployment

All images build and run on the .NET 10 SDK image (the `DotNet*` tools shell out to `dotnet`), with `git` and Python
installed and the demo's `skills/` copied in.

| Service (`demos/louis-agent/docker/docker-compose.yml`) | Dockerfile (`demos/louis-agent/docker/images/`) | Notes |
|---|---|---|
| `acp-server` | `Dockerfile.acp-server` | Run per Rider chat with `docker compose run --rm -T acp-server` |
| `api` | `Dockerfile.api` | API + web app on `127.0.0.1:${API_PORT:-5080}`; health check on `/health` |
| Puls: `puls` (`docker-compose.puls.yml`) | `Dockerfile.puls` | The general assistant on `127.0.0.1:${PULS_PORT:-5070}`; its specialists run on the same instance (CL19); logs in `logs/puls`. Its browser and Mongo also serve `puls-app` on Azure, through `tailscale serve` on this PC ([demos/puls/deploy/azure](../../../demos/puls/deploy/azure/README.md)) |
| `mcp-server` | `Dockerfile.mcp-server` | stdio: run per client with `run --rm -T mcp-server`; with `MCP_TRANSPORT=http`, on `127.0.0.1:${MCP_PORT:-5090}` |
| `local-agent` | `Dockerfile.agent` | The CLI |
| `ollama` | `ollama/Dockerfile.ollama` | Only with `--profile ollama` |
| `ollama-nvidia` | `ollama/Dockerfile.ollama-nvidia` | Only with `--profile ollama-nvidia`: on an NVIDIA GPU, 32k-token context |
| `ollama-amd` | `ollama/Dockerfile.ollama-amd` | Only with `--profile ollama-amd`: on an AMD GPU (ROCm, Linux hosts), 32k-token context |

Every agent service above mounts the repo at `/workspace`, the demo's `skills/` at `/skills`, `logs/` at `/logs` and
`REPOSITORIES_PATH` at `/repositories`, reads `config/.env` and `config/.env.secrets`, and sets `core.autocrlf=true`
for git (the repo is a Windows checkout). Always pass the env file:

```bash
docker compose -f demos/louis-agent/docker/docker-compose.yml --env-file demos/louis-agent/config/.env up -d --build api
```

The orchestrator demo (its own compose file, a demo service with ten bugs) was removed in CL10 and comes back with
the rewritten benchmarks; `Dockerfile.orchestrator` still builds the orchestrator.

## In Azure (F61.4)

Each demo's `config/azure/main.bicep` is the record of what Azure holds for it, deployed by its `deploy/azure/deploy.ps1
infra`; the app itself is a yaml the script's `release` deploys as a new revision. Built on 2026-11-08, live once Louis
runs it; until then the CL19 deploy (Puls reaching the PC's browser and Mongo through a Tailscale sidecar) and the F58
tunnels (both domains served from the PC) keep running, and go when the new build answers.

| Where | What runs | Reaches |
|---|---|---|
| `rg-puls-za`, `puls-env` (a Container Apps environment on `puls-vnet`) | `puls-app`: the Puls image, one container, behind its key and its sign-in | the VM on private IPs; Azure SQL over TLS; `puls-insights` through the environment's OpenTelemetry agent |
| `rg-puls-za`, the VM `puls-services` (Ubuntu, Docker, no public IP) | `mongo:8` on 27017, `headless-shell` on 9222, `searxng` on 8080, from `deploy/azure/vm/docker-compose.services.yml`; Mongo's files on an XFS data disk at `/data` | nothing: the security group lets only the apps' subnet in |
| `rg-puls-za`, Azure SQL `puls-sql-za` | `puls_accounts`, the free offer, paused when idle | — |
| `rg-louis-agent-za`, `louis-agent-env` | `louis-agent-site`: the static site, scales to zero | nothing: it spends and keeps nothing |

The domains point at the environments' static IPs with Container Apps' managed certificates, DNS-only in Cloudflare.
The how-to for each is its deploy README: [Puls](../../../demos/puls/deploy/azure/README.md),
[the site](../../../demos/louis-agent/deploy/azure/README.md).
