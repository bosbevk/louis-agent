# louis-agent.evals.checks — design

> **Kind:** class library (`net10.0`) · **Path:** `evals/louis-agent.evals.checks/` · **References:**
> louis-agent.core, louis-agent.session-stream, louis-agent.evals.protocol · **Used by:** louis-agent.evals (the
> harness); from F49.3, the packs

## Purpose

The check contract: what a check reads (a trial and its turns) and what it returns (a grade). It lives outside the
harness (`evals/louis-agent.evals`) so a pack can reference it and ship its own check, which it can't do with the
harness, because the harness runs evals and names Puls's.

The pattern is *Separated Interface* (Fowler, PoEAA): the contract sits in a small project its clients can
reference, and the checks themselves live elsewhere: the general ones in the harness, a pack's own in the pack.
The why is [ADR 0056](../adr/0056-a-check-is-a-catalogue-entry.md).

## How it works

Today it holds what a check reads: a `Trial` is one run of a scenario in one mode, and its `Transcript` is the turns
it left, each a `TurnTranscript` with the session's events as they arrived. `EngineEvents` reads those events (the
answer, the error, the usage, the route, and the tools the agent and its specialists called), and `TurnFacts` is what a turn's protocol record says about it.

And what a check returns: `ICheck` scores one trial into a `Grade` (a score from 0 to 1, an outcome, a reason, and
for a model grader the `GradeJudge` that judged it). `Verdict` builds the four kinds of grade, so a pass always scores
1, and an infrastructure failure is always Unknown, never the agent's fail. The reasons are `CommonGradeMessage`
members (ADR 0009), formatted by core's `MessageText`.

And how a check is named from a file: an `ICheckType` is one kind of check, with its type in kebab case
(`within-time`), its kind, and the record its settings are read into. `CheckType<TParameters>` is the base an entry
starts from (*Factory Method*): it reads the settings with `EvalJson.Strict` and refuses ones it can't read, naming
the check type and, from the JSON reader, the field; the entry only says how to build its check from the record. An
unknown field, a missing one and a null are all refused, so a typo can't become a default.

`CheckCatalog` holds the entries (*Registry*, filled with *Plugin* entries: each owner lists its own, the composition
gathers them). It's built once and refuses a list a file couldn't name from: a type an MCP tool name can't carry
(letters, digits, `-` and `_`, at most 64; an empty one too) or a type listed twice. `Find` gives the entry for a
type or nothing; `Get` refuses an unknown type, naming the known ones, as the runner's `EvalCatalog` does. `Listing()`
gives each entry's type, kind and its settings' JSON schema (.NET's `JsonSchemaExporter` over `EvalJson.Strict`), so
the studio (F49.5) can make a form from it: snake_case fields, the required ones listed, defaults given, and
`additionalProperties: false`, as the reader refuses an unknown field.

`EvalJson` is the eval's one set of JSON options (snake_case; for the API; strict), here so the eval file and a
check's settings inside it read alike. These came from the harness as a pure move: the root namespace is `LouisAgent.Evals`, the harness's, so a moved type
keeps its namespace and nothing that uses it changes.

It references only what a pack already references, each for one thing:

- **louis-agent.core** for `MessageText`, which formats a check's reason from its message enum
  ([ADR 0009](../adr/0009-message-text-as-enum-templates.md));
- **louis-agent.session-stream** for the `SessionEvent`s a turn of a trial carries;
- **louis-agent.evals.protocol** for `CheckKind` and `GradeOutcome`, the kinds a grade carries on the wire.

Never the harness, a pack, an identity or a package: the harness keeps `Microsoft.Extensions.AI.Evaluation`.

## Structure

| Path | What's in it |
|---|---|
| `louis-agent.evals.checks.csproj` | The three references, each with what it's for |
| `EvalMode.cs` | One mode a task runs in: its role, its toolsets, and a sub-session's depth and LLM |
| `EvalJson.cs` | The eval's JSON options: `SnakeCase`, `SnakeCaseNoNulls` (what is sent to the API), `Strict` (what a person wrote) |
| `Catalog/` | `ICheckType` and `CheckType<TParameters>`: a check named by its type and built from its settings; `CheckCatalog`, `ListedCheck`, `CheckCatalogException`: the entries found by type and listed |
| `Datasets/Scenarios/` | `EvalScenario`, `EvalInput`, `DatasetSplit`: one scenario, what the user sends, and its split |
| `Trials/` | `Trial`, `Transcript`, `TurnTranscript`, `TurnFacts`, `EngineEvents` |
| `Grading/` | `ICheck`, `Grade` and `GradeJudge`; `Common/Verdict` |
| `Messages/` | `CommonGradeMessage`: the reasons a grade gives; `CheckCatalogMessage`: what the catalogue refuses |

## Tests

`ArchitectureTests.Checks_ReferencesOnlyTheEngineTheProtocolAndTheStream` (in `louis-agent.core.tests`) holds the
references: exactly the three above, none into `packs/` or `identities/`. `EngineEvents` is still tested in
`louis-agent.evals.tests` (`EngineEventsTests`, `AnswerOfTests`): those tests build their trials with the harness
tests' shared `TrialBuilder`, and a test project may not reference another, so they stay until a second project
needs the builder (F49.3). `Verdict` is tested from outside, by the checks that use it (`CommonChecksTests`) and by
an identity's (`VerdictFromOutsideTests`).

`tests/louis-agent.evals.checks.tests/Catalog/CheckTypeTests` builds a check from its settings and refuses a field of
the wrong type, a missing field, an unknown field and no settings at all, each naming the check type (and the field).
`CheckCatalogTests` finds entries by type, refuses an unknown type (naming the known ones), a duplicate and an unsafe
type, and lists each entry with its settings' schema.

## Limits and plans

What F49.1 left for later, each where its need comes:

- **The runner doesn't build the catalogue yet**, and `routed-to` has no entry: both wait for F49.2's eval file, the
  first thing that names a check from a file and states a route.
- **A check's name is its type** for an entry (`contains`), so an eval holds one of each until F49.2's file names
  its checks; the conformance rule then holds names, not types.
- **`TrialBuilder` is the harness tests' own**, so `EngineEventsTests` and `AnswerOfTests` stay in
  `louis-agent.evals.tests`; they move with a shared builder when a pack's checks need one (F49.3).
- **Two copies of the MCP name rule**: the catalogue's and `EvalConformance`'s (private there); one could reuse the
  other when either changes.
- **`CommonGradeMessage` holds harness-only reasons too** (a check that threw, a scenario gone), which a pack never
  gives; they can move back to the harness if the contract's messages grow.
- **The schema's root says `"type": ["object", "null"]`**: .NET's exporter allows a null record, which the reader
  refuses anyway (`NoParameters`); harmless, unless the studio shows it.

## Related docs

[ADR 0056](../adr/0056-a-check-is-a-catalogue-entry.md) · [ADR 0027](../adr/0027-engine-identities-and-packs.md) ·
[Write-up 20](../learning/20-extensibility.md) · [louis-agent.evals](louis-agent.evals.md) ·
[louis-agent.evals.protocol](louis-agent.evals.protocol.md)

---
[Projects](README.md) · Previous: [louis-agent.evals](louis-agent.evals.md) · Next: [louis-agent.evals.protocol](louis-agent.evals.protocol.md)
