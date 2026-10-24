# docker/: the images, and the stacks that run on this PC

> **Reference.** What each file here is. **Elsewhere:** the commands and settings in the
> [development guide](../docs/CLAUDE.md#quick-start) and [Setup](../docs/SETUP.md); where each service runs in the
> [deployment view](../docs/architecture/07-deployment-view.md); the cloud in [deploy/](../deploy/README.md).

| File | What it is |
|---|---|
| `docker-compose.yml` | The code-review app and Rider's servers: `api` (5080), `acp-server`, `mcp-server`, `local-agent`; Ollama under the `ollama`, `ollama-nvidia` and `ollama-amd` profiles |
| `docker-compose.puls.yml` | The Puls stack: `puls` (5070), `puls-mongo` (the benchmark store), `puls-searxng` and `puls-browser` (the web it reads, on their own network). Its browser and Mongo also serve the Azure app, through `tailscale serve` on this PC ([the tunnel](../deploy/azure/README.md#the-tunnel)) |
| `images/Dockerfile.*` | One per host: `api`, `puls` (the same API serving Puls's web app), `acp-server`, `mcp-server`, `agent` (the CLI), `orchestrator`. All build from the repo root |
| `images/ollama/` | Ollama with its model pulled on first start: CPU, NVIDIA and AMD variants, and their entrypoint |
| `searxng/settings.yml` | The SearXNG settings, mounted read-only into `puls-searxng` here and uploaded to the Azure one |

Every compose command takes `--env-file config/.env`, and every port binds to `127.0.0.1`:

```bash
docker compose -f docker/docker-compose.yml --env-file config/.env up -d --build api
docker compose -f docker/docker-compose.puls.yml --env-file config/.env up -d --build
```
