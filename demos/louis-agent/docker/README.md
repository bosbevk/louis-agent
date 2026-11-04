# docker/: the coding agent's images and the stacks that run on this PC

> **Reference.** What each file here is. **Elsewhere:** the commands and settings in the
> [development guide](../../../src/docs/guides/get-started.md) and [Setup](../docs/SETUP.md); where each service runs
> in the [deployment view](../../../src/docs/architecture/07-deployment-view.md); Puls's own stack in
> [its docker/](../../puls/docker/README.md).

| File | What it is |
|---|---|
| `docker-compose.yml` | The code-review app and Rider's servers: `api` (5080), `acp-server`, `mcp-server`, `local-agent`; Ollama under the `ollama`, `ollama-nvidia` and `ollama-amd` profiles |
| `docker-compose.observability.yml` | The dashboard both stacks and Rider send their logs and traces to (F52): the standalone Aspire dashboard, its page on 18888 and OTLP over gRPC on 4317, kept in memory only. Started once with `docker compose -f demos/louis-agent/docker/docker-compose.observability.yml up -d`, the one command that takes no `--env-file` |
| `images/Dockerfile.*` | One per host: `api`, `acp-server`, `mcp-server`, `agent` (the CLI), `orchestrator`, `site`. All build from the repo root |
| `images/ollama/` | Ollama with its model pulled on first start: CPU, NVIDIA and AMD variants, and their entrypoint |

Every compose command takes `--env-file demos/louis-agent/config/.env`, and every port binds to `127.0.0.1`:

```bash
docker compose -f demos/louis-agent/docker/docker-compose.yml --env-file demos/louis-agent/config/.env up -d --build api
```
