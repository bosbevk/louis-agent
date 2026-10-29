# 0029. Simulations in C#: one harness for checks, regressions and demos

**Status:** Accepted · **Date:** 2026-10-19 (decided in the [platform plan](../plans/platform.md), Phase 3 F18) ·
**Recorded:** 2026-10-19 · Amends [0022](0022-benchmarks-recorded-from-the-protocol.md)

## Context

Puls's checks are Python scripts (1,442 lines in `benchmarks/`), and the session stream is read four times, in four
ways. The demos we sell with have no harness at all. Louis: "[every] demo must be in c# we call it simulations, of a
real world [task]".

## Decision

- **The harness belongs to the engine** and is sold with it: `simulations/louis-agent.simulation/`.
- **A simulation belongs to the identity or pack it proves:** `simulations/louis-agent.simulations.{puls,demo,voorman}/`.
- **We use the field's words:** a task with success criteria, trials, transcripts, outcomes, graders, pass@k and
  pass^k; *modes* are our addition.
- **Cost comes from the ledger** ([0007](0007-measure-every-request.md)); each turn's seconds from the harness. A grader
  never computes a cost.
- **CI gates** the harness, the graders on recorded transcripts and the golden files. An agent regression is caught
  only by a run.
- **A replay cache** serves recorded simulations only, never a live chat.
- **It amends 0022:** the harness is C#, and the protocol's fixture moves to
  `tests/louis-agent.session-stream.tests/Fixtures/`; the stdout protocol and the store stand.

## Consequences

- One stream reader replaces four, and a demo is a simulation a buyer can watch.
- The Python checks are retired once their C# simulations match their last runs.
- Re-running a recorded simulation costs nothing; a live run still waits for Louis's go.

## Amendment (2026-10-24): the word is *eval*

Louis chose *eval* for the harness, its folders and the portal's pages (`evals/louis-agent.evals/`,
`evals/louis-agent.evals.{puls,demo,voorman}/`, `/admin/evals`), on 2026-10-20 for the portal and on 2026-10-24 for the
code. The decision above keeps its substance: one C# harness, each eval beside what it proves, cost from the ledger,
and the stream and the store unchanged. A C# eval still *simulates a real task* in the field's sense. The title and the
file name keep the first word, because the record of the decision keeps its name.

## Links

[Platform plan](../plans/platform.md) (Phases 3 and 4) · [0022](0022-benchmarks-recorded-from-the-protocol.md) ·
[0007](0007-measure-every-request.md)

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
