# Debug and logs

> **How-to.** Find what the agent did: the tool log, the run files, the usage ledger, and the Docker habits that keep
> a rebuild honest. **Elsewhere:** how the messages and logs are built in
> [core's guide](../../engine/LouisAgent.Core/CLAUDE.md#messages-and-logging); the ledger's design in
> [ADR 0007](../adr/0007-measure-every-request.md).

![Finding what the agent did: the tool log on stderr, the run files, the traces and the dashboard, the usage ledger, and the Docker habits](diagrams/debug-and-logs.svg)

## Logs

Every tool call is logged to stderr, as `[TOOL] -> Name(args)` when it starts and `[TOOL] <- Name: result` (or
`failed: …`) when it ends. By default the arguments, the result and a failure's quoted reason show only as hashes and
lengths (`area=#3f9c0a1b2d4e (10)`). So a log tells you what ran and how it ended, but never what the user typed.
`LOG_CONTENT=full` shows the values, and is for your own machine only.

In Docker the logs are kept in `logs/`, and `docker logs louis_agent_api` shows the API's. With `LOG_DIRECTORY` set,
each process writes its run file there. The orchestrator's lines are tagged `[orchestrator]`, and louis-agent's
progress under it `[louis-agent]`.

Set `OTEL_EXPORTER_OTLP_ENDPOINT`, the standard OpenTelemetry setting, and every log line also goes to that collector.
Each template's holes arrive as fields, and the host is named by `OTEL_SERVICE_NAME`, or else its project's name.
`OTEL_EXPORTER_OTLP_PROTOCOL` picks gRPC (the default) or `http/protobuf`. The JSON-lines records stay out of it.

The traces go to the same collector. Every turn is a span, `invoke_agent {agent}`, named for the agent (its role, or
the default mode) and tagged with its session, host, turn, run and task. Inside it is a span for each model request
and each tool call, and no span carries a prompt or an answer. A crash is one `[FATAL]` line with its stack, and the
last lines and spans still get out. A failed task that nothing awaited is one `[ERROR]`.

The ACP and MCP servers talk to their client over stdout. That's why every log goes through `ILogger` to stderr.

*Decided in:* F18 step 4f, [ADR 0009](../adr/0009-message-text-as-enum-templates.md),
[ADR 0064](../adr/0064-observability-through-opentelemetry.md).

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

Log an exception once, where it's handled, with the exception object itself, never `ex.Message` alone. Every line is
structured: a template with named holes, which the message enums already are. A file's levels are put right when the
file is touched, with no sweep across the code, the Boy Scout Rule, the way ADR 0057 does it.

*Decided in:* F52.

## The usage ledger

Every model request is in `{LOG_DIRECTORY}/usage-YYYY-MM.jsonl` (tokens, cost, host, session, turn, fix and run; no
prompt text). Total one with:

```bash
dotnet run --project src/tools/LouisAgent.UsageProbe -- --ledger logs/usage-YYYY-MM.jsonl
```

## Docker

After changing the web app or the API, rebuild the `api` image. After changing core, rebuild every image you use. A
Rider chat keeps its container until you start a **New Chat**.

After changing `config/.env.secrets`, recreate the containers with `docker compose … up -d`. A `restart` keeps the old
environment, so the new key never arrives. A changed prompt is a change to whatever project embeds it, so rebuild the
images that use it.

Every image rebuild leaves build cache behind. One day of rebuilds filled Docker Desktop's disk with 120 GB of cache
and stopped its engine. `docker system df` shows how much there is, and `docker builder prune -af` clears it safely;
the next build simply caches again. If the daemon stops answering, restart Docker Desktop first.

---
[Guides](README.md) · [Docs index](../README.md)
