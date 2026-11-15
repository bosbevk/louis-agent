# louis-agent.core.telemetry — design

> **Kind:** library (`net10.0`) · **Path:** `src/engine/louis-agent.core.telemetry/` · **References:**
> louis-agent.core, OpenTelemetry, OpenTelemetry.Exporter.OpenTelemetryProtocol, Serilog.Sinks.OpenTelemetry ·
> **Used by:** every host's program, the eval runner, the orchestrator
> **Also shows:** `component`

![Telemetry, and its parts: the tracer, the log sink, and the filter that keeps our own traffic out](diagrams/louis-agent.core.telemetry.svg)

## Purpose

This project lets a host send what the agent does to an OpenTelemetry collector, so you can follow every turn in a
dashboard. It holds three things. There's the tracer, which listens to core's turn spans (a span is one timed piece
of work, like a turn or a tool call) and exports them over OTLP, OpenTelemetry's wire format. There's the log sink,
which sends the log lines the same way. And there's a filter that keeps our own calls to the collector out of the
traces.

A host opts in by handing `AgentHost.Build` a `HostServices` with `OtlpTelemetry.Instance`. Core on its own runs on
`NoTelemetry`. It still measures every turn on its own `ActivitySource`, but nothing listens. And it still logs a
crash once.

*Decided in:* CL33, [ADR 0091](../adr/0091-web-access-and-telemetry-are-libraries-on-core.md).

## How it works

Here's a host's telemetry, from start to exit.

![Telemetry: the crash handler installed, the tracer built when a collector is set, its own traffic left out, and what is held sent at the end](diagrams/louis-agent.core.telemetry-flow.svg)

`OtlpTelemetry` is core's `IHostTelemetry` seam made real. There's one of it for the whole process, and core calls
it at four moments.

### 1. Adding the log sink

`AgentLogging.Configure` calls `AddSinks(logging)` with the host's configuration. If `OTEL_EXPORTER_OTLP_ENDPOINT`
is set, every log line goes to the collector too. The record files are left out, just as they are on the console.

The sink reads the endpoint, the protocol, the headers and `OTEL_SERVICE_NAME` by itself. Where those are set,
they win over ours.

### 2. Starting the tracer

`AgentHost.Build` calls `Start()` once the .env files have loaded. That timing matters: it means `OTEL_*` settings
in those files count too, even under Rider.

First it installs core's crash handler, `LastChance`, and hands it this telemetry so a crash can flush. Then it
builds the tracer over our sources: `TurnSpans.Source`, Microsoft.Extensions.AI's model requests and tool calls,
ASP.NET Core's requests, and every `HttpClient` call. It adds the OTLP exporter only when there's an endpoint. So a
host with no collector pays nothing.

If something starts it a second time, it gets the same handle back. Disposing that handle stops the tracer, and so
does the process exiting.

*Decided in:* F52.

### 3. Flushing at the end

`Flush()` sends whatever the tracer is still holding. It waits three seconds at most, so a dead collector can never
keep a host from exiting. It's also what a crash calls for its last word.

### 4. Keeping our own traffic out

Sending log lines to the collector is itself an `HttpClient` call, and every `HttpClient` call becomes a span. Left
alone, every batch of log lines would turn into a span of its own. `OwnTrafficFilter` stops that. It sits before
the exporter, and drops any `HttpClient` span whose address is the collector's.

### 5. What stays in core

Some of this lives in core, not here: `TurnSpans` (a span per turn, following the GenAI conventions), `SpanTag`,
`LastChance`, and the seam itself with its Null Object, the stand-in that does nothing.

![Telemetry: the log sink and the tracer, out to the collector](diagrams/louis-agent.core.telemetry-diagrams.svg)

![Switching telemetry on: the host adds the collector's sink and starts the tracer, and a turn's spans go out over OTLP](diagrams/louis-agent.core.telemetry-diagrams-2.svg)

These are the types involved:

![The types: OtlpTelemetry is core's telemetry seam, with a filter that keeps its own calls out of the spans](diagrams/louis-agent.core.telemetry-interface.svg)

## Structure

```
src/engine/louis-agent.core.telemetry/
├── OtlpTelemetry.cs      # IHostTelemetry made real: the sink, the tracer, the flush, the exit
└── OwnTrafficFilter.cs   # the processor that drops our calls to the collector
src/tests/engine/louis-agent.core.telemetry.tests/   # OtlpTelemetryTests, LastChanceTests, AgentLoggingOtlpTests, on the shared OtlpReceiver
```

## Configuration

`OTEL_EXPORTER_OTLP_ENDPOINT` names the collector; when it's not set, nothing listens. The others are
`OTEL_EXPORTER_OTLP_PROTOCOL`, `OTEL_EXPORTER_OTLP_HEADERS`, `OTEL_SERVICE_NAME` and `OTEL_RESOURCE_ATTRIBUTES`.
They're the OpenTelemetry spec's own names, read by the exporter and the sink themselves. The service name is the
host's unless you set it. [ADR 0064](../adr/0064-observability-through-opentelemetry.md) has the why.

## Tests

The tests are in `src/tests/engine/louis-agent.core.telemetry.tests`. They check that a model request reaches the
collector as a span, that the environment's service name wins, that a second start gives the same handle, that no
endpoint means nothing listens, that our source is listened to, and that a call to the collector is left out. A
crash is one fatal line, and it reaches the collector on its own. The log lines reach it too, with the records left
out.

The stand-in collector they send to is `louis-agent.testing/Otlp/OtlpReceiver`. Core's own `HostTelemetryTests`
prove the seam without this library.

## Limits and plans

Metrics, such as speed or tokens per second, are F44.1's to add. A second exporter would be a second class on the
same seam.

## Related docs

[louis-agent.core](louis-agent.core.md) · [louis-agent.core.web](louis-agent.core.web.md) ·
[ADR 0091](../adr/0091-web-access-and-telemetry-are-libraries-on-core.md) · [ADR 0064](../adr/0064-observability-through-opentelemetry.md)

---
[Docs index](../README.md) · Related: [Projects](README.md) · [Plans](../plans/README.md)
