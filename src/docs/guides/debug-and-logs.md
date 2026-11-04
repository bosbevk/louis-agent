# Debug and logs

> **How-to.** Find what the agent did: the tool log, the run files, the usage ledger, and the Docker habits that keep
> a rebuild honest. **Elsewhere:** how the messages and logs are built in
> [core's guide](../../engine/louis-agent.core/CLAUDE.md#messages-and-logging); the ledger's design in
> [ADR 0007](../adr/0007-measure-every-request.md).

## Logs

- Tool calls are logged to stderr: `[TOOL] -> Name(args)` and `[TOOL] <- Name: result` (or `failed: …`). By default
  the arguments, the result and a failure's quoted reason show as hashes and lengths (`area=#3f9c0a1b2d4e (10)`), so
  a log shows what ran and how it ended, never what the user typed; `LOG_CONTENT=full` shows the values, on your own
  machine only (F18 step 4f).
- In Docker, logs persist in `logs/`; `docker logs louis_agent_api` shows the API.
- With `LOG_DIRECTORY` set, each process's run file is there; the orchestrator's lines are tagged `[orchestrator]`,
  louis-agent's progress under it `[louis-agent]`.
- With `OTEL_EXPORTER_OTLP_ENDPOINT` set (the standard OpenTelemetry setting), every log line also goes to that
  collector, each template's holes as fields and the host named by `OTEL_SERVICE_NAME` (else its project's name);
  `OTEL_EXPORTER_OTLP_PROTOCOL` picks gRPC (the default) or `http/protobuf`. The JSON-lines records stay out. The
  traces go to the same collector: every turn is a span, `invoke_agent {agent}` named for the agent (its role, or
  the default mode) and tagged with its session, host, turn, run and task, holding a span per model request and tool
  call, with no prompt or answer on any of them. A crash is one `[FATAL]` line with its stack, and the last lines
  and spans still leave; a failed task nothing awaited is one `[ERROR]`.
- stdout is a protocol for the ACP and MCP servers, so every log goes through `ILogger` to stderr
  ([ADR 0009](../adr/0009-message-text-as-enum-templates.md)).

## Traces and the dashboard

Start it once, beside the stacks: `docker compose -f demos/louis-agent/docker/docker-compose.observability.yml up -d`, then open
`http://127.0.0.1:18888`. Every container already sends to it. One Puls question shows as one trace: the request
span, `invoke_agent puls`, a `chat` span per model request and an `execute_tool` per tool call, and nested under the
tool call that handed it over, the cook's own `invoke_agent`. The *Structured logs* tab has the same turn's lines,
each template hole a field, filtered by `service.name`. It keeps everything in memory: a restart forgets it. For
Rider and `dotnet run`, set `OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317` in `config/.env`. With the dashboard
down, everything works as before and no error line appears.

## What to log at which level

The why is [ADR 0064](../adr/0064-observability-through-opentelemetry.md).

| Level | Means | Ours, examples |
|---|---|---|
| **Critical** | The process can't go on | an unhandled exception at the top of a host, a store that won't open |
| **Error** | One operation failed; the process goes on | a turn failed on the API, a run exited non-zero, a request 500, a task nobody awaited |
| **Warning** | Unexpected, but handled | a retry, a tool result cut to size, a price missing, a check that threw (Unknown) |
| **Information** | A milestone a person wants later | session opened, run started, turn done in N s, the file logging to |
| **Debug** | A decision, for the developer | which toolset, a cache hit, which provider strategy |
| **Trace** | Everything, never on in production | the raw request |

- Log an exception once, where it is handled, with the exception object, never `ex.Message` alone.
- Structured: a template with named holes, which the message enums are already.
- Levels are fixed as a file is touched, no sweep (F52's pick F1, the Boy Scout Rule, as ADR 0057 does it).

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
  environment, so the new key never arrives. A changed prompt (`src/engine/louis-agent.core/Prompts/`) is a change to core:
  rebuild the images that use it.
- Every image rebuild leaves build cache behind; a day of rebuilds filled Docker Desktop's disk (120 GB of cache) and
  stopped its engine (2026-10-15). `docker system df` shows it; `docker builder prune -af` clears it, safely, and
  the next build re-caches. If the daemon stops answering, restart Docker Desktop first.

---
[Guides](README.md) · [Docs index](../README.md)
