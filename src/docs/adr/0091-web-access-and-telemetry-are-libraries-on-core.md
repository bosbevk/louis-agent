# 0091. Web access and telemetry are libraries on core, behind a seam each

**Status:** Accepted · **Date:** 2026-11-07 (Louis, pointing at `Telemetry` and `Web` in core: "those two should also
move"; the names "louis-agent.core.web louis-agent.core.telemetry"; "A", both on top of core) · **Recorded:**
2026-11-07

## Context

Core carried the web (the browser on PuppeteerSharp, the fetcher, the robots policy, search, rendering, the replay
recorders) and the telemetry (the OpenTelemetry tracer and exporter, the collector's log sink), so every host that
took core took a browser binary and a tracer, and ADR 0076 had named the web split as the one left. The three engine
projects below core (`composition`, `choices`, `session-stream`) are shared by more than core and keep their plain
names (Louis: "those are like at a lower level than .core, it can be shared by many").

## Decision

`louis-agent.core.web` and `louis-agent.core.telemetry` are libraries **on top of** core, which keeps one seam and one
Null Object for each: `IWebAccess` with `NoWebAccess` (every part says web access isn't set up, so a tool answers
why) and `WebParts` (web access from parts given); `IHostTelemetry` with `NoTelemetry` (nothing listens; a crash is
still logged once). A host's program composes them through `HostServices(WebAccess.Create, OtlpTelemetry.Instance)`
handed to `AgentHost.Build`, the way it hands in its catalogue of identities and packs; core's engine factory builds
no web itself and core's bootstrap starts no tracer itself. Core keeps its own span source (`TurnSpans`, on
`System.Diagnostics`), the crash handler, and the logging, whose collector sink the telemetry adds through the seam.
PuppeteerSharp, OpenTelemetry, its OTLP exporter and Serilog's OpenTelemetry sink leave core's project file
(`CorePackagesTests` holds it). The name `louis-agent.core.X` says "sits on core"; what sits below core keeps a plain
name.

Weighed and not taken: both beside core, depending on the contracts only (more seams for the same shipping result,
and the names would have to drop `core`); below core (nothing leaves core's package list).

## Consequences

- A host that gives its agent the web and traces adds two project references and one `HostServices`; core alone
  runs, its web tools saying web access isn't set up, its turns measured by nobody.
- A pack reads the web through its `IPackContext` as before; a test builds a context from `WebParts` or
  `WebAccess.Create`.
- A new search provider, renderer or exporter is a class in its library; a new thing the engine needs from the web
  is a member on the seam, with its Null Object answer.
- The replay clock is core's (it reads the store); the recorders are the web's.

## Links

[louis-agent.core.web](../projects/louis-agent.core.web.md) · [louis-agent.core.telemetry](../projects/louis-agent.core.telemetry.md) ·
[ADR 0076](0076-core-ships-no-agent.md) · [ADR 0089](0089-the-account-client-is-a-library-a-site-opts-into.md) ·
[ADR 0064](0064-observability-through-opentelemetry.md)

---
[Docs index](../README.md) · Related: [ADRs](README.md)
