# louis-agent.core.web — design

> **Explanation.** Web access on core: the implementations of the composition's web contracts (the guarded browser
> on headless Chrome, the page fetcher, the robots.txt policy, search, rendering, the recorders for replay), which
> core's engine takes through its `IWebAccess` seam and a host opts into. **Elsewhere:** core's seam and Null Object
> in [louis-agent.core](louis-agent.core.md); why it is a library of its own in
> [ADR 0091](../adr/0091-web-access-and-telemetry-are-libraries-on-core.md). Being built as CL33
> ([the plan](../plans/CL33-core-web-and-telemetry.md)).

## Purpose

What the engine needs from the web, apart from the engine: a host that wants its agent to read pages hands
`AgentHost.Build` a `HostServices` with `WebAccess.Create`; a host that doesn't ships no browser binary, and its web
tools say web access isn't set up.

---
[Docs index](../README.md) · Related: [Projects](README.md) · [Plans](../plans/README.md)
