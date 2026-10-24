# Change the orchestrator

> **How-to.** Change `src/louis-agent.orchestrator`, the agent that watches a service's errors and has louis-agent fix
> them: a runbook, a tool, a setting, its tests, and a run. **Elsewhere:** how it works in
> [louis-agent.orchestrator](../projects/louis-agent.orchestrator.md); how it decides and how to write a runbook in
> [Runbooks](../projects/orchestrator-runbooks.md).

The orchestrator is a second agent on core with its own model. It reads new error events from a service's log, triages
each one with the service's runbooks as its system prompt and `ServiceTools` as its only tools, asks louis-agent's API
for a fix when one is allowed, and checks the claim itself. It is a proof of concept: in Phase 6 it becomes the
`voorman` identity on core ([ADR 0031](../adr/0031-orchestration-is-an-engine-function.md)), so keep changes small.

## Change what it decides: a runbook

Most changes are a runbook, not code: a method's expected behaviour, whether it may be fixed, what to check. Follow
[Adding and changing runbooks](../projects/orchestrator-runbooks.md#adding-and-changing-runbooks): a file
`Skills/<service>/<method>.md` from the template, named exactly as the error events name the method. Runbooks are copied
into the build, so rebuild after a change.

## Add a tool

1. Write its test first in `tests/louis-agent.orchestrator.tests`.
2. Add a public method to `Tools/ServiceTools.cs` with a `[Description]` on it and on each parameter. Every public
   method there is a tool the model can call, so a helper stays `private` or `internal`.
3. It returns text; a failure is text with its reason, from `Messages/OrchestratorToolMessage`
   ([ADR 0009](../adr/0009-message-text-as-enum-templates.md)).
4. Anything it runs (git, the service's tests) takes its arguments as a list, never a shell line
   ([ADR 0004](../adr/0004-arguments-as-data.md)). A claim from louis-agent is checked, never trusted.
5. Tell the model when to use it: the runbooks' `service.md` or the method runbooks.

`ServiceTools.cs` is over the 200-line limit and on `FileSizeTests`' list: a new group of tools goes in a class of its
own, passed to the engine beside it (`toolsets: [tools]` in `Program.cs`).

## Add a setting

Settings are read once in `OrchestratorOptions` (`FromEnvironment`), named `ORCHESTRATOR_*`; a new one gets a property
there with its default, a test, and a line in the design doc's *Configuration*.

## Test and run it

```bash
dotnet test tests/louis-agent.orchestrator.tests
dotnet run --project src/louis-agent.orchestrator -- --once     # one pass over the service's new errors
```

The tests cover the feed, the SSE client and `FIX-RESULT`, the decisions, loading the runbooks, the comms log, and the
git checks against a throwaway repository. A run needs a service (`ORCHESTRATOR_SERVICE_ROOT`) and a louis-agent API
(`LOUIS_AGENT_URL`, `LOUIS_AGENT_API_KEY`), and spends real money on both agents' models, so ask first.
`agent-comms.md` then shows what it decided and what it sent.

## Before you ask for a review

- [ ] A behaviour change is a runbook where it can be.
- [ ] A new tool returns text, takes its arguments as data, and has its test.
- [ ] Every text is an orchestrator message enum member.

---
[Guides](README.md) · [Docs index](../README.md)
