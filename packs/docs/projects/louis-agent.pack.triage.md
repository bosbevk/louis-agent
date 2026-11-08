# louis-agent.pack.triage

> **Explanation.** What the triage pack does and why it's built this way. **Elsewhere:** the workflow it serves in
> [the F106.3 plan](../../../src/docs/plans/F106.3-triage-and-fix-a-service.md); building a pack in
> [Build a pack](../../../src/docs/guides/build-a-pack.md); the service it reads, the louis-agent demo's
> [samples](../../../demos/louis-agent/samples/README.md); the orchestrator that read the feed before, in
> [its doc](../../../demos/louis-agent/docs/projects/louis-agent.orchestrator.md).

An agent team that triages a service's errors needs what every department's workflow needs and no agent should hold
itself: where the errors come from, where a run's steps are kept, a typed way to say what it found or decided, a
person to raise a case to, and a way to read a run back. This pack holds those, as tools and records, so the
identities that use it stay markdown and the workflow stays a page of code (F106.3.1).

## What it holds

| Part | Does |
|---|---|
| `Feed/IErrorFeed`, `JsonLinesErrorFeed`, `ServiceError` | The errors not handled yet, one by its id, and the mark that one was handled, kept in a file beside the log so it survives a restart; a JSON-lines log today, a tracker's webhook later (F100). Moved from the orchestrator, which reads it from here |

The runs, the decision tools, the person's endpoint, the run read back and the departments arrive with the steps of
[the child's plan](../../../src/docs/plans/F106.3.1-the-service-and-the-pack.md).

## The setting

`TRIAGE_RUNS`: the folder a run's steps are written under, one file per run. Unset, the toolset reports it missing
and stays off.

## Tests

`packs/tests/louis-agent.pack.triage.tests`: the feed over a small log (an error is handed out until it is marked
handled, the mark survives a restart, a bad line is skipped, an id that isn't a plain token is ignored).

---
[Docs index](../../../src/docs/README.md) · Related: [Projects](../../../src/docs/projects/README.md)
