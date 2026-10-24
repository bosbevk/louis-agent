# 7. Deployment view

> **Explanation.** Where each part runs: the Docker images and services, and what they mount. **Elsewhere:** the
> settings in [Setup](../SETUP.md); the orchestrator demo was removed in CL10 (its last run at the [`pre-cl10` tag](https://github.com/bosbevk/louis-agent/blob/pre-cl10/samples/README.md)).

## Deployment

All images build and run on the .NET 10 SDK image (the `DotNet*` tools shell out to `dotnet`), with `git` and Python
installed and `Skills/` copied in.

| Service (`docker/docker-compose.yml`) | Dockerfile (`docker/images/`) | Notes |
|---|---|---|
| `acp-server` | `Dockerfile.acp-server` | Run per Rider chat with `docker compose run --rm -T acp-server` |
| `api` | `Dockerfile.api` | API + web app on `127.0.0.1:${API_PORT:-5080}`; health check on `/health` |
| Puls: `puls`, `puls-agent` (`docker-compose.puls.yml`) | `Dockerfile.puls` | The general assistant on `127.0.0.1:${PULS_PORT:-5070}` and the specialist it delegates to on `${PULS_CODER_PORT:-5071}`; logs in `logs/puls` |
| `mcp-server` | `Dockerfile.mcp-server` | stdio: run per client with `run --rm -T mcp-server`; with `MCP_TRANSPORT=http`, on `127.0.0.1:${MCP_PORT:-5090}` |
| `local-agent` | `Dockerfile.agent` | The CLI |
| `ollama` | `ollama/Dockerfile.ollama` | Only with `--profile ollama` |
| `ollama-nvidia` | `ollama/Dockerfile.ollama-nvidia` | Only with `--profile ollama-nvidia`: on an NVIDIA GPU, 32k-token context |
| `ollama-amd` | `ollama/Dockerfile.ollama-amd` | Only with `--profile ollama-amd`: on an AMD GPU (ROCm, Linux hosts), 32k-token context |

Every agent service above mounts the repo at `/workspace`, `Skills/` at `/skills`, `logs/` at `/logs` and
`REPOSITORIES_PATH` at `/repositories`, reads `config/.env` and `config/.env.secrets`, and sets `core.autocrlf=true`
for git (the repo is a Windows checkout). Always pass the env file:

```bash
docker compose -f docker/docker-compose.yml --env-file config/.env up -d --build api
```

The orchestrator demo (its own compose file, a demo service with ten bugs) was removed in CL10 and comes back with
the rewritten benchmarks; `Dockerfile.orchestrator` still builds the orchestrator.
