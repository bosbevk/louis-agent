# 0064. Observability through OpenTelemetry, and what to log at which level

**Status:** Accepted · **Date:** 2026-11-03 (F52, approved on the recommended option of every finding; the picks
of the build are Louis's, on their cards) · **Recorded:** 2026-11-04

## Context

Every host logged through `ILogger` with Serilog behind it ([ADR 0009](0009-message-text-as-enum-templates.md)): the
console on stderr, a run file per process, the JSON-lines records, every value hashed unless `LOG_CONTENT=full`. F18
had put Microsoft.Extensions.AI's spans in the tool loop (a `chat` span per model request, `execute_tool` per tool
call), but nothing exported them, so they died in the process. A crash logged nothing of ours, a sub-session on a
peer was a separate story from the call that opened it, and nothing showed one host's work beside another's.

Louis asked for one error and log store per project (2026-10-30), Exceptionless first, "or whatever the research
says". The research (`research/reports/logging-and-error-tracking.md` in the Claude project) weighed Exceptionless
(a second pipe, its own server or a one-project free plan), Sentry and GlitchTip against OpenTelemetry, the standard
both .NET and Azure speak; Louis said yes to OTLP per host, the Aspire dashboard on the PC and Application Insights
on Azure.

## Decision

**One pipe per signal, on the standard settings.** Nothing of ours names a collector: `OTEL_EXPORTER_OTLP_ENDPOINT`,
`_PROTOCOL`, `_HEADERS` and `OTEL_SERVICE_NAME` are read by the exporters themselves and win over our defaults; with
no endpoint, nothing listens and nothing is sent.

- **Logs through Serilog's OTLP sink, traces through the OpenTelemetry SDK** (A1): the logs keep their one pipeline
  (templates, hashing, tags in `AgentLogging`), the traces the reference SDK every instrumentation plugs into.
- **.NET's own sources beside ours** (B2): `Microsoft.AspNetCore` and `System.Net.Http`, so every request is a span
  and an `HttpClient` call carries the W3C `traceparent`; a sub-session's turn nests under the parent's tool call
  across the HTTP hop with nothing of ours.
- **A turn is `invoke_agent {agent}`, opened in `TurnRunner`** (C1), around both forms of a turn, tagged from the
  usage scope (session, host, turn, run, task) with the GenAI conventions' names where they have one (`SpanTag`).
  Never a word of the conversation on a span; under it, Microsoft.Extensions.AI opens no `orchestrate_tools`.
- **`AgentTelemetry.Start()` in the shared bootstrap** (D1): `AgentHost` after the `.env` files load, the
  orchestrator and the eval runner themselves; once per process, flushed at exit, three seconds at most.
- **The dashboard is its own compose stack** (E2): `docker/docker-compose.observability.yml`, started once; both
  stacks send to `host.docker.internal:4317`, Rider to `localhost:4317`.
- **Levels by the table below, fixed as a file is touched** (F1), no sweep.
- **The runner gets the collector's address, never its key** (B): the API's allowlist passes the endpoint and the
  protocol, not the headers or the API's service name.
- **The factory names the engine** (G1): the profile's role, else the default mode, so a trace tells the cook's turn
  from the chat's.
- **Our calls to the collector are no spans** (H1): `OwnTrafficFilter`, a processor before the exporter, drops an
  `HttpClient` span to the endpoint; the log sink sends on a plain client the SDK doesn't hide.
- **A crash is Critical, a lost task an Error** (I1): `LastChance` logs an exception nothing caught once, as the
  exception object, then flushes the logs and the spans; a faulted task nobody awaited is marked observed.
- **A host is named by its project** (J1): `api`, `acp-server`, `mcp-server`, `cli`, `evals.runner`; only `puls`,
  whose project is the API's, sets `OTEL_SERVICE_NAME`.
- **Application Insights on the environment's workspace** (K1): `deploy.ps1 telemetry` creates `puls-insights` on
  the Log Analytics workspace `puls-container-env` logs to and turns on the environment's managed agent.
- **What stays out** ([ADR 0055](0055-keep-it-simple.md)): metrics (the ledger is the cost,
  [ADR 0007](0007-measure-every-request.md); F44.1 takes speed and metrics), alert rules (by hand in App Insights
  first), the browser apps (Blazor sends nothing; the API logs what matters), GlitchTip (only if an errors inbox is
  missed after a month).

**What to log at which level:**

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
- Levels are fixed as a file is touched, no sweep (the Boy Scout Rule, as [ADR 0057](0057-regex-is-the-last-option.md)
  does it).

![ADR 0064: where the telemetry goes, on the PC and in Azure](diagrams/0064-observability-through-opentelemetry-decision.svg)

## Consequences

- **What it costs, measured** (a free replay of the cook baseline `dc272b95eaeb` on the local Puls container, three
  runs each, the median; 2026-11-04): exporters off, the run took 22.7 s and its 20 turns 2.2 s together; on, 22.9 s
  and 2.9 s. That is about 1% of a run, within the runs' own spread (22.0 to 40.7 s off, 22.2 to 28.9 s on): a
  replayed turn takes about 0.1 s, and the judge on a local model is most of a run. Which tool call made the slowest
  turn slow: by grep in the run files, ___ ; by a click in the dashboard, ___ (Louis's stopwatch).
- A cancelled turn's span has no status: a user's stop is not a failure.
- `AgentLogging.Flush()` is a second `Configure` with the same arguments: replacing the logger disposes the old one,
  which is what sends a batching sink's last lines.
- `ServiceName` stays on `AgentLogging`, where the run file reads it, and our span source on `AgentTelemetry`.
- The Azure agent is preview and speaks gRPC only, which both exporters default to; App Insights takes logs and
  traces, not metrics. `deploy.ps1 telemetry -WhatIf` runs the reads and prints the two writes.
- The dashboard keeps everything in memory: a restart forgets it. With it down, every host works as before and no
  error line appears.
- An exception's message travels as it is on a span's exception event and a crash line, as it does in the log file:
  `LogContent`'s hashing doesn't reach it, and it is the one place an exception's text leaves the process.

## Links

TODO's *F52 Observability through OpenTelemetry* · GOAL row 17 · the plan as it was, at
[`fb752e55`](https://github.com/bosbevk/louis-agent/blob/fb752e55/docs/plans/F52-opentelemetry.md) ·
[0007](0007-measure-every-request.md) · [0009](0009-message-text-as-enum-templates.md) ·
[0055](0055-keep-it-simple.md) · [0057](0057-regex-is-the-last-option.md) · [Debug and
logs](../guides/debug-and-logs.md) · [louis-agent.core](../projects/louis-agent.core.md) · the research
(`research/reports/logging-and-error-tracking.md` in the Claude project) · [the
write-up](../learning/17-observability.md) · OpenTelemetry's GenAI semantic conventions · W3C Trace Context

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
