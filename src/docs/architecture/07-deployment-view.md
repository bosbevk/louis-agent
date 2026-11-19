# 7. Deployment view

> **Explanation.** Where each part runs: the Docker stacks on a developer's machine, what each one mounts, and the two
> websites in Azure. **Elsewhere:** the settings in [Setup](../SETUP.md); the Azure how-to in
> [Deploy the sites](../guides/deploy-the-sites.md).

![The deployment view: four stacks on a developer's machine, and the two sites in Azure](diagrams/07-deployment-view.svg)

## On a developer's machine

Everything runs in Docker, in a few separate stacks, each with its own compose file. The code-review stack is the
coding agent in all its doors. Puls has its own stack beside it. The framework's two websites have a third. And one
dashboard collects the logs and traces of all of them. Every port is bound to `127.0.0.1` only, because these agents
can run shell commands and write files.

### The code-review stack

The coding agent's images all build and run on the .NET 10 SDK image, because the `DotNet*` tools shell out to
`dotnet`. Each one installs `git` and Python. Its services are in `demos/louis-agent/docker/docker-compose.yml`, and
their Dockerfiles in `demos/louis-agent/docker/images/`.

| Service | Dockerfile | What it is |
|---|---|---|
| `api` | `Dockerfile.api` | The HTTP API with the code-review app, on `127.0.0.1:${API_PORT:-5082}`, with a health check on `/health`. The image also carries the coding agent's eval runner and a copy of the docs the guide reads |
| `acp-server` | `Dockerfile.acp-server` | Rider's door, run once per Rider chat with `docker compose run --rm -T acp-server` |
| `mcp-server` | `Dockerfile.mcp-server` | The tools over MCP. Over stdio it runs once per client with `run --rm -T mcp-server`; with `MCP_TRANSPORT=http` it serves on `127.0.0.1:${MCP_PORT:-5090}` |
| `local-agent` | `Dockerfile.agent` | The CLI |
| `ollama` | `ollama/Dockerfile.ollama` | A local model, only with `--profile ollama` |
| `ollama-nvidia` | `ollama/Dockerfile.ollama-nvidia` | The same on an NVIDIA GPU with a 32k-token context, only with `--profile ollama-nvidia` |
| `ollama-amd` | `ollama/Dockerfile.ollama-amd` | The same on an AMD GPU (ROCm, Linux hosts) with a 32k-token context, only with `--profile ollama-amd` |

Every agent service mounts the repo at `/workspace`, the demo's `skills/` at `/skills`, its `logs/` at `/logs` and
`REPOSITORIES_PATH` at `/repositories`. It reads the model's settings file that `LLM_PROFILE` picks, then
`config/.env` and `config/.env.secrets`. Git is told `core.autocrlf=true`, because the repo is a Windows checkout.
Always pass the env file:

```bash
docker compose -f demos/louis-agent/docker/docker-compose.yml --env-file demos/louis-agent/config/.env up -d --build api
```

The API opens the guide to visitors without a sign-in (`OPEN_IDENTITIES`), up to 50 sessions a day. It refuses to
start without `AGENT_API_KEY`, unless `AGENT_AUTH=off` says so on purpose.

`Dockerfile.orchestrator` builds the orchestrator on the SDK image too, since it replays requests with `dotnet run`,
runs the watched service's tests and checks commits with git. No compose file starts it.

### Puls's stack

Puls runs in its own stack, `demos/puls/docker/docker-compose.puls.yml`, on `127.0.0.1:${PULS_PORT:-5070}`. It is one
container: a chat answers from the web itself, and hands cooking to a specialist on the same instance. It mounts its
own `logs/` and `data/puls`, and waits for its stores to be healthy before it starts.

Beside it run what it needs: `puls-sql` for the accounts, `puls-mongo` for its stores, `puls-searxng` for web search
and `puls-browser` for rendering pages. Each is on `127.0.0.1` only. The browser's DevTools port has no
authentication, so it is never opened wider.

### The framework's two sites

`src/site/docker/docker-compose.yml` runs the framework's two websites. The `site` service is louis-agent.dev on
`127.0.0.1:${SITE_PORT:-5080}`, built by `src/site/docker/Dockerfile`, and it keeps its logs and its saved data on the
host. The `framework` service is the framework's own site on `127.0.0.1:${FRAMEWORK_PORT:-5084}`, built by
`Dockerfile.framework`. Both run on the smaller ASP.NET image, since neither runs `dotnet` tools.

### The dashboard

`demos/louis-agent/docker/docker-compose.observability.yml` starts the Aspire dashboard, once, beside the other
stacks. The code-review stack, Puls and the two sites send their logs and traces to it over OTLP on port 4317, and so do Rider's
runs when `config/.env` sets `OTEL_EXPORTER_OTLP_ENDPOINT`. Its page is at
`http://127.0.0.1:18888`. It keeps everything in memory, so a restart forgets it.

## In Azure

Two websites built on the framework run in Azure: louis-agent.dev, which sells websites with agents, and
framework.louis-agent.dev, which teaches the framework. Each is one Azure Container App that sleeps when nobody
visits. Puls isn't deployed: `mypuls.co.za` is a "coming soon" page on a free Static Web App.

A merge to `main` releases both sites through `.github/workflows/deploy-sites.yml`, signed in by OpenID Connect with no
password. It runs `src/site/deploy/azure/deploy.ps1 release` once per app, louis-agent.dev first, so a failure in
the second leaves the site that sells released. Each release builds the app's image in the registry and updates the
app as its next revision. Each name points at its app by DNS-only records in Cloudflare, with a managed certificate.

| Where | What runs | Reaches |
|---|---|---|
| `rg-puls-za`, `puls-env` | `louis-agent-app`: louis-agent.dev's host on the image `src/site/docker/Dockerfile` builds, the public Orbi on the docs, leads and site packs | the vault with `louis-agent-app-identity`; its logs in `logs/` and its saved CV in `data/` on `louis-agent-workspace`; the model; `puls-insights` through the environment's OpenTelemetry agent |
| `rg-puls-za`, `puls-env` | `louis-agent-framework-app`: framework.louis-agent.dev's host on the image `Dockerfile.framework` builds, the framework Orbi on the docs and site packs | the vault with the same identity; its logs in `framework/logs/` on the same share; the model; `puls-insights` |

The how-to is [Deploy the sites](../guides/deploy-the-sites.md), and the pictures of what runs where, a release, a
secret's path and a chat are in [the sites in Azure](deployment-in-azure.md), beside this page.

*Decided in:* FX122, [ADR 0095](../adr/0095-puls-runs-from-azure-and-nothing-points-at-the-pc.md), [ADR 0097](../adr/0097-puls-deploys-from-github-without-a-password.md).
