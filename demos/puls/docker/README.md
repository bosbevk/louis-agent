# docker/: Puls's image and the stack that runs on this PC

> **Reference.** What each file here is. **Elsewhere:** the commands and settings in the
> [development guide](../../../src/docs/guides/get-started.md) and [Setup](../docs/SETUP.md); where each service runs
> in the [deployment view](../../../src/docs/architecture/07-deployment-view.md); the cloud in
> [deploy/](../deploy/README.md); the dashboard both stacks send to in
> [the coding agent's docker/](../../louis-agent/docker/README.md).

| File | What it is |
|---|---|
| `docker-compose.puls.yml` | The Puls stack: `puls` (5070), `puls-mongo` (the benchmark store), `puls-searxng` and `puls-browser` (the web it reads, on their own network). Its browser and Mongo also serve the Azure app, through `tailscale serve` on this PC ([the tunnel](../deploy/azure/README.md#the-tunnel)) |
| `images/Dockerfile.puls` | The API serving Puls's web app. Builds from the repo root |
| `searxng/settings.yml` | The SearXNG settings, mounted read-only into `puls-searxng` here and uploaded to the Azure one |

Every compose command takes `--env-file demos/puls/config/.env`, and every port binds to `127.0.0.1`:

```bash
docker compose -f demos/puls/docker/docker-compose.puls.yml --env-file demos/puls/config/.env up -d --build
```
