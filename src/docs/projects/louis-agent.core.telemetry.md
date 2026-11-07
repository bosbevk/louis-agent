# louis-agent.core.telemetry — design

> **Explanation.** Telemetry on core: the OpenTelemetry tracer that listens to core's turn spans and exports them
> over OTLP, the log sink that sends the log lines the same way, the own-traffic filter and the crash handler, which
> a host opts into through core's `IHostTelemetry` seam. **Elsewhere:** core's seam and Null Object in
> [louis-agent.core](louis-agent.core.md); why it is a library of its own in
> [ADR 0091](../adr/0091-web-access-and-telemetry-are-libraries-on-core.md); the tracing itself in
> [ADR 0064](../adr/0064-observability-through-opentelemetry.md). Being built as CL33
> ([the plan](../plans/CL33-core-web-and-telemetry.md)).

## Purpose

Traces and logs out of the process, apart from the engine: a host hands `AgentHost.Build` a `HostServices` with
`OtlpTelemetry.Instance`; core alone runs on `NoTelemetry` and still measures every turn on its own
`ActivitySource`, which nothing listens to.

---
[Docs index](../README.md) · Related: [Projects](README.md) · [Plans](../plans/README.md)
