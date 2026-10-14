# 7. Deployment view

> **Explanation.** Where each part runs: the Docker images and services, and what they mount. **Elsewhere:** the
> settings in [Setup](../SETUP.md); running the demo in [samples/README.md](../../samples/README.md).

## Deployment

All images build and run on the .NET 10 SDK image (the `DotNet*` tools shell out to `dotnet`), with `git` and Python
installed and `Skills/` copied in.

| Service (`docker/docker-compose.yml`) | Dockerfile | Notes |
|---|---|---|
| `acp-server` | `Dockerfile.acp-server` | Run per Rider chat with `docker compose run --rm -T acp-server` |
| `api` | `Dockerfile.api` | API + web app on `127.0.0.1:${API_PORT:-5080}`; health check on `/health` |
| Puls: `puls`, `puls-agent` (`docker-compose.puls.yml`) | `Dockerfile.puls` | The general assistant on `127.0.0.1:${PULS_PORT:-5070}` and the specialist it delegates to on `${PULS_CODER_PORT:-5071}`; logs in `logs/puls` |
| `mcp-server` | `Dockerfile.mcp-server` | stdio: run per client with `run --rm -T mcp-server`; with `MCP_TRANSPORT=http`, on `127.0.0.1:${MCP_PORT:-5090}` |
| `local-agent` | `Dockerfile.agent` | The CLI |
| `ollama` | `services/Dockerfile.ollama` | Only with `--profile ollama` |
| `ollama-nvidia` | `services/Dockerfile.ollama-nvidia` | Only with `--profile ollama-nvidia`: on an NVIDIA GPU, 32k-token context |
| `ollama-amd` | `services/Dockerfile.ollama-amd` | Only with `--profile ollama-amd`: on an AMD GPU (ROCm, Linux hosts), 32k-token context |

Every agent service above mounts the repo at `/workspace`, `Skills/` at `/skills`, `logs/` at `/logs` and
`REPOSITORIES_PATH` at `/repositories`, reads `config/.env` and `config/.env.secrets`, and sets `core.autocrlf=true`
for git (the repo is a Windows checkout). Always pass the env file:

```bash
docker compose -f docker/docker-compose.yml --env-file config/.env up -d --build api
```

The orchestrator demo has its own compose file, `docker/docker-compose.demo.yml` (project `louis-agent-demo`):

| Service | Dockerfile | Notes |
|---|---|---|
| `demo-setup` | `Dockerfile.orchestrator` | One-off: fresh demo repository in `.demo/order-service` and its production traffic |
| `demo-api` | `Dockerfile.api` | louis-agent.api and the web app on `127.0.0.1:${DEMO_PORT:-5081}`, workspace `/demo/order-service` |
| `orchestrator` | `Dockerfile.orchestrator` | Triages new errors once and calls `demo-api` for fixes; SDK image, as it replays requests and runs tests |

All three mount the host's `.demo/` at `/demo` (the demo repository is LF, so no `core.autocrlf`) and share a `nuget`
volume. `samples/run-demo.ps1` drives them; see [samples/README.md](../../samples/README.md).
