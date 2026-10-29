# Debug and logs

> **How-to.** Find what the agent did: the tool log, the run files, the usage ledger, and the Docker habits that keep
> a rebuild honest. **Elsewhere:** how the messages and logs are built in
> [core's guide](../../louis-agent.core/CLAUDE.md#messages-and-logging); the ledger's design in
> [ADR 0007](../adr/0007-measure-every-request.md).

## Logs

- Tool calls are logged to stderr: `[TOOL] -> Name(args)` and `[TOOL] <- Name: result` (or `failed: …`). By default
  the arguments, the result and a failure's quoted reason show as hashes and lengths (`area=#3f9c0a1b2d4e (10)`), so
  a log shows what ran and how it ended, never what the user typed; `LOG_CONTENT=full` shows the values, on your own
  machine only (F18 step 4f).
- In Docker, logs persist in `logs/`; `docker logs louis_agent_api` shows the API.
- With `LOG_DIRECTORY` set, each process's run file is there; the orchestrator's lines are tagged `[orchestrator]`,
  louis-agent's progress under it `[louis-agent]`.
- stdout is a protocol for the ACP and MCP servers, so every log goes through `ILogger` to stderr
  ([ADR 0009](../adr/0009-message-text-as-enum-templates.md)).

## The usage ledger

Every model request is in `{LOG_DIRECTORY}/usage-YYYY-MM.jsonl` (tokens, cost, host, session, turn, fix and run; no
prompt text). Total one with:

```bash
dotnet run --project tools/louis-agent.usage-probe -- --ledger logs/usage-YYYY-MM.jsonl
```

## Docker

- After changing the web app or API, rebuild the `api` image; after changing core, rebuild every image you use. Rider
  chats keep their container until you start a **New Chat**.
- After changing `config/.env.secrets`, recreate the containers with `docker compose … up -d`: `restart` keeps the old
  environment, so the new key never arrives. A changed prompt (`louis-agent.core/Prompts/`) is a change to core:
  rebuild the images that use it.
- Every image rebuild leaves build cache behind; a day of rebuilds filled Docker Desktop's disk (120 GB of cache) and
  stopped its engine (2026-10-15). `docker system df` shows it; `docker builder prune -af` clears it, safely, and
  the next build re-caches. If the daemon stops answering, restart Docker Desktop first.

---
[Guides](README.md) · [Docs index](../README.md)
