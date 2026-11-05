# ADR 0079: An instance can show a specialist's report instead of retyping it

> **Explanation.** Why Puls's model never writes out the cook's report, and why that's a setting rather than the rule.

## Context

Puls hands a recipe question to its cook, gets the cook's report back, and used to write the whole report out again as
its own answer. We measured that answer: it was about 96% alike the cook's report. So one whole request went on copying,
at the output rate, the most expensive thing we pay for: output was 64% of an answer's cost.

A copy can also only get worse. The cook read the pages and Puls didn't, so a dropped table row or a changed figure is a
loss with nothing to gain from it. A prompt can ask for a faithful copy, but it can't make the copy free.

Not every agent wants this. The coding agent is told to treat a specialist's answer as a claim and to check it, by
reading the diff or the test summary, before it reports the work as done. That agent needs to read the report and keep
working.

## Decision

**`SHOW_SPECIALIST_REPORTS=true` makes the engine show a specialist's report as it is and end the turn there.** A real
answer from `StartSubSession` or `ContinueSubSession` stops the tool loop (`FunctionInvocationContext.Terminate`), so no
further request is made, and the turn runner appends the report as the assistant's answer, without the footer the tool
adds for the model. The setting is off by default. Puls's compose and its Azure app turn it on.

A failed or an empty answer still goes back to the model, which says what to do about it. A minion's answer is never
shown, because it goes to the specialist that asked for it, not to the user.

## Consequences

- An answer costs one request less, and the report reaches the user exactly as the specialist wrote it.
- The model has no last word on that turn: no line above the report, and no second specialist in the same turn after the
  first one has answered. A follow-up works as before, since the report and the footer's handle are both in the history.
- History stays append-only ([ADR 0008](0008-history-is-append-only.md)): the report is added once the turn has finished,
  and the tool's result, footer included, stays as it was.
- The specialists guide still describes relaying, for the agents that relay. An identity that turns this on says so in
  its own routing, as Puls's does.

## Links

- [ADR 0031: orchestration is an engine function](0031-orchestration-is-an-engine-function.md)
- [ADR 0008: history is append-only](0008-history-is-append-only.md)
- `src/engine/louis-agent.core/Delegation/ShownReport.cs`, `src/tests/engine/louis-agent.core.tests/Agent/Turns/ShowTheReportTests.cs`

---
[Docs index](../README.md) · Related: [ADR index](README.md)
