# louis-agent.evals.checks — design

> **Kind:** class library (`net10.0`) · **Path:** `evals/louis-agent.evals.checks/` · **References:**
> louis-agent.core, louis-agent.session-stream, louis-agent.evals.protocol · **Used by:** nothing yet (from F49.1
> step 2, the harness; from F49.3, the packs)

## Purpose

The check contract: what a check reads (a trial and its turns) and what it returns (a grade). It lives outside the
harness (`evals/louis-agent.evals`) so a pack can reference it and ship its own check, which it can't do with the
harness, because the harness runs evals and names Puls's.

The pattern is *Separated Interface* (Fowler, PoEAA): the contract sits in a small project its clients can
reference, and the checks themselves live elsewhere: the general ones in the harness, a pack's own in the pack.
The why is [ADR 0056](../adr/0056-a-check-is-a-catalogue-entry.md).

## How it works

Today the project is empty: F49.1 step 1 only makes it and fences its references. Steps 2 and 3 move the contract in
from the harness (`Trial` and its turn types, then `IGrader`, `Grade` and `Verdict`), a pure move: the root namespace
is `LouisAgent.Evals`, the harness's, so a moved type keeps its namespace and nothing that uses it changes.

It references only what a pack already references, each for one thing:

- **louis-agent.core** for `MessageText`, which formats a check's reason from its message enum
  ([ADR 0009](../adr/0009-message-text-as-enum-templates.md));
- **louis-agent.session-stream** for the `SessionEvent`s a turn of a trial carries;
- **louis-agent.evals.protocol** for `GraderKind` and `GradeOutcome`, the kinds a grade carries on the wire.

Never the harness, a pack, an identity or a package: the harness keeps `Microsoft.Extensions.AI.Evaluation`.

## Structure

| Path | What's in it |
|---|---|
| `louis-agent.evals.checks.csproj` | The three references, each with what it's for; no code yet |

## Tests

`ArchitectureTests.Checks_ReferencesOnlyTheEngineTheProtocolAndTheStream` (in `louis-agent.core.tests`) holds the
references: exactly the three above, none into `packs/` or `identities/`. `tests/louis-agent.evals.checks.tests` is
empty until step 2 moves the contract's tests in.

## Related docs

[ADR 0056](../adr/0056-a-check-is-a-catalogue-entry.md) · [ADR 0027](../adr/0027-engine-identities-and-packs.md) ·
[F49.1 The checks catalogue](../plans/F49.1-checks-catalogue.md) · [louis-agent.evals](louis-agent.evals.md) ·
[louis-agent.evals.protocol](louis-agent.evals.protocol.md)

---
[Projects](README.md) · Previous: [louis-agent.evals](louis-agent.evals.md) · Next: [louis-agent.evals.protocol](louis-agent.evals.protocol.md)
