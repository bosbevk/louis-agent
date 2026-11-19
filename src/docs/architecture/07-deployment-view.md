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
| Puls: `puls` (`docker-compose.puls.yml`) | `Dockerfile.puls` | The general assistant on `127.0.0.1:${PULS_PORT:-5070}`; its specialists run on the same instance (CL19); logs in `logs/puls`. The Azure app has its own browser and Mongo on the VM since F61.4 ([demos/puls/deploy/azure](../../../demos/puls/deploy/azure/README.md)) |
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

## In Azure

Two websites built on the framework run in Azure: louis-agent.dev, which sells websites with agents, and
framework.louis-agent.dev, which teaches the framework. Each is one Azure Container App that sleeps when nobody
visits. Puls isn't deployed since FX122: `mypuls.co.za` is a "coming soon" page on a free Static Web App.

A merge to `main` releases both sites through `.github/workflows/deploy-puls.yml`, signed in by OpenID Connect with
no password ([ADR 0097](../adr/0097-puls-deploys-from-github-without-a-password.md)). It runs
`demos/puls/deploy/azure/deploy.ps1 release` once per app, louis-agent.dev first, and each release builds the app's
image in the registry and updates the app as its next revision. Each name points at its app by DNS-only records in
Cloudflare, with a managed certificate.

| Where | What runs | Reaches |
|---|---|---|
| `rg-puls-za`, `puls-env` | `louis-agent-app`: louis-agent.dev's host on the image `src/site/docker/Dockerfile` builds, the public Orbi on the docs, leads and site packs | the vault with `louis-agent-app-identity`; its logs in `logs/` and its saved CV in `data/` on `louis-agent-workspace`; the model; `puls-insights` through the environment's OpenTelemetry agent |
| `rg-puls-za`, `puls-env` | `louis-agent-framework-app`: framework.louis-agent.dev's host on the image `Dockerfile.framework` builds, the framework Orbi on the docs and site packs | the vault with the same identity; its logs in `framework/logs/` on the same share; the model; `puls-insights` |

The how-to is the [deploy README](../../../demos/puls/deploy/azure/README.md), and the pictures of what runs where, a
release, a secret's path and a chat are in [the Azure architecture](../../../demos/puls/deploy/azure/ARCHITECTURE.md).
