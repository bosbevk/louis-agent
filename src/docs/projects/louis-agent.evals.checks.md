# louis-agent.evals.checks — design

> **Kind:** contracts library (`net10.0`) · **Path:** `src/evals/louis-agent.evals.checks/` · **References:**
> louis-agent.core, louis-agent.session-stream, louis-agent.evals.protocol · **Used by:** louis-agent.evals (the
> harness), the packs' checks projects (where-to-buy, recipes), the Puls demo's evals, louis-agent.api and
> louis-agent.api.evals
> **Also shows:** `component`, `sequence`

![The check contract: it reads a trial, returns a grade, and the checks themselves live elsewhere](diagrams/louis-agent.evals.checks.svg)

## Purpose

A check is one thing an eval looks at, like "did it stay within budget?". This project is the contract every check
follows: what a check reads (a trial and its turns) and what it gives back (a grade).

It lives outside the harness (`src/evals/louis-agent.evals`) so that a pack can reference it and ship checks of its
own. A pack can't reference the harness, because the harness runs evals and names Puls's.

The pattern is *Separated Interface*, from Fowler's PoEAA. The contract sits in a small project its clients can
reference, and the checks themselves live elsewhere: the general ones in the harness, and a pack's own in the pack.

The rest of this doc follows a check from the outside in: what it reads, what it gives back, how an eval file names
it, the catalogue that finds it, the one set of JSON options, and what this project may reference.

*Decided in:* [ADR 0056](../adr/0056-a-check-is-a-catalogue-entry.md).

## How it works

### 1. What a check reads

A `Trial` is one run of a scenario in one mode. Its `Transcript` is the turns it left behind. Each turn is a
`TurnTranscript`, holding the session's events in the order they arrived.

`EngineEvents` reads those events for you: the answer, the error, the usage, the route, the tools the agent and its
specialists called, the sub-session it went back to, and the URLs its tools were given.

`TurnFacts` is what a turn's protocol record says about it. For every eval, `TurnFacts.Of(turn)` reads it from those
events, unless an eval written in C# says its own.

### 2. What a check gives back

`ICheck` scores one trial into a `Grade`. A grade has a score from 0 to 1, an outcome, a reason, and for a model
grader, the `GradeJudge` that judged it.

`Verdict` builds the four kinds of grade. That way a pass always scores 1, and an infrastructure failure is always
Unknown, never counted as the agent failing. The reasons are `CommonGradeMessage` members, formatted by core's
`MessageText`.

*Decided in:* [ADR 0009](../adr/0009-message-text-as-enum-templates.md).

### 3. How a file names a check

An eval file names each check by its type. An `ICheckType` is one kind of check: its type in kebab case (such as
`within-time`), its kind, and the record its settings are read into.

`CheckType<TParameters>` is the base an entry starts from; that's the *Factory Method* pattern. It reads the settings
with `EvalJson.Strict`. If it can't read them, it refuses, naming the check type and, from the JSON reader, the
field. The entry itself only says how to build its check from the record.

An unknown field, a missing one and a null are all refused. So a typo can never quietly become a default.

Besides its settings, an entry gets a `CheckContext`. That holds the eval's folder, which is where a file it names is
read from (a judge's rubric, say), and the checks built before it (a person's review finds its judge there). Most
entries ignore it. `CheckContext.None` is for a check built in C#, or in a test.

*Decided in:* F49.2.

### 4. The catalogue

`CheckCatalog` holds the entries. It's a *Registry*, filled with *Plugin* entries: each owner lists its own, and the
composition gathers them.

It's built once, and it refuses a list a file couldn't name things from. That means a type an MCP tool name can't
carry (letters, digits, `-` and `_`, at most 64, and not empty), or a type listed twice.

`Find` gives you the entry for a type, or nothing. `Get` refuses an unknown type, naming the known ones, just as the
runner's `EvalCatalog` does.

`Listing()` gives each entry's type, its kind, and its settings' JSON schema, made by .NET's `JsonSchemaExporter`
over `EvalJson.Strict`. That's what lets the studio make a form from it: snake_case fields, the required ones listed,
defaults given, and `additionalProperties: false`, because the reader refuses an unknown field anyway.

*Decided in:* F49.5.

### 5. One set of JSON options

`EvalJson` is the eval's one set of JSON options: snake_case, one for the API, and a strict one. It lives here so the
eval file and the check settings inside it read the same way. The root namespace is `LouisAgent.Evals`, the
harness's, so these types share a namespace with the harness.

### 6. What it may reference

It only references what a pack already references, each for one thing. **louis-agent.core** gives it
`MessageText`, which formats a check's reason from its message enum. **louis-agent.session-stream** gives it the
`SessionEvent`s a turn of a trial carries. And **louis-agent.evals.protocol** gives it `CheckKind` and
`GradeOutcome`, the kinds a grade carries on the wire.

It never references the harness, a pack, an identity or a package. The harness keeps
`Microsoft.Extensions.AI.Evaluation` to itself.

![The check contract: who lists checks, and how a check is built, reads and grades](diagrams/louis-agent.evals.checks-diagrams.svg)

![From a line to a grade: the catalogue finds the entry, the entry reads its settings strictly and builds the check, and the check grades the trial](diagrams/louis-agent.evals.checks-diagrams-2.svg)

These are the types involved:

![The types: a check type builds a check, a check grades a trial into a grade, and readers find money, numbers, links and images](diagrams/louis-agent.evals.checks-interface.svg)

## Structure

| Path | What's in it |
|---|---|
| `louis-agent.evals.checks.csproj` | The three references, each with what it's for |
| `EvalMode.cs` | One mode a task runs in: its role, its toolsets, and a sub-session's depth and LLM |
| `EvalJson.cs` | The eval's JSON options: `SnakeCase`, `SnakeCaseNoNulls` (what is sent to the API), `Strict` (what a person wrote) |
| `Catalog/` | `ICheckType` and `CheckType<TParameters>`: a check named by its type and built from its settings and its `CheckContext`; `CheckCatalog`, `ListedCheck`, `CheckCatalogException`: the entries found by type and listed |
| `Datasets/Scenarios/` | `EvalScenario`, `EvalInput`, `DatasetSplit`: one scenario, what the user sends, and its split |
| `Trials/` | `Trial`, `Transcript`, `TurnTranscript`, `TurnFacts`, `EngineEvents` |
| `Grading/` | `ICheck`, `Grade` and `GradeJudge`; `Common/Verdict` |
| `Markdown/` | `Lists` (an answer's tables, and its shopping list by the agents' Afrikaans and English headers) and `Links` (inline and reference-style links, images, and `IsHost`, the site or a subdomain): the plain reading an identity, a pack and the harness share (F49.3) |
| `Readers/` | `IReader` and `ReaderCatalog`, the readers found by name and kind (F49.2b). `Kinds/` holds each kind: `IMoneyReader`, `INumberReader`, `IImageReader` and `IPriceReader`, what a general compare reads a value with, and `ILinkReader`, the recipe's link, which the recipes pack's `recipe-from-source` reads with (F35.2). `Listing/` holds `ListedReader` and `ReaderKind`, a reader as the studio lists it |
| `Messages/` | `CommonGradeMessage`: the reasons a grade gives; `CheckCatalogMessage`: what the catalogue refuses |

## Tests

`ArchitectureTests.Checks_ReferencesOnlyTheEngineTheProtocolAndTheStream`, in `louis-agent.core.tests`, holds the
references: exactly the three above, and none into `packs/` or `identities/`.

Most of `EngineEvents` is tested in `louis-agent.evals.tests` (`EngineEventsTests`, `AnswerOfTests`). Those tests
build their trials with the harness tests' shared `TrialBuilder`, and one test project may not reference another.
The one reader a pack's check needs, a string field of a tool's input by name, is tested here too, in
`louis-agent.evals.checks.tests/Trials/EngineEventsTests`. `Verdict` is tested from the outside: by the checks that use it
(`FactsChecksTests` and the other general checks' tests), and by an identity's (`VerdictFromOutsideTests`).

The rest of this project's own tests are in `src/tests/evals/louis-agent.evals.checks.tests`. `Catalog/CheckTypeTests`
builds a check from its settings. It also
checks that a field of the wrong type, a missing field, an unknown field and no settings at all are each refused,
naming the check type and the field. `CheckCatalogTests` finds entries by type, refuses an unknown type (naming the
known ones), a duplicate and an unsafe type, and lists each entry with its settings' schema. `ReaderCatalogTests`
finds readers by name and kind, and `Markdown/` tests the shared reading of links, images and tables.

## Limits and plans

`TrialBuilder` belongs to the harness tests, so most of `EngineEvents`' tests sit in `louis-agent.evals.tests`, not
here.

There are two copies of the MCP name rule: the catalogue's, and a private one in the harness's `EvalConformance`.

`CommonGradeMessage` holds some reasons only the harness gives, such as a check that threw or a scenario that's gone.
A pack never gives those.

The schema's root says `"type": ["object", "null"]`, because .NET's exporter allows a null record. The reader refuses
one anyway (`NoParameters`), so it's harmless, unless the studio ever shows it.

## Related docs

[ADR 0056](../adr/0056-a-check-is-a-catalogue-entry.md) · [ADR 0027](../adr/0027-engine-identities-and-packs.md) ·
[Write-up 20](../learning/20-extensibility.md) · [louis-agent.evals](louis-agent.evals.md) ·
[louis-agent.evals.protocol](louis-agent.evals.protocol.md)

---
[Projects](README.md) · Previous: [louis-agent.evals](louis-agent.evals.md) · Next: [louis-agent.evals.protocol](louis-agent.evals.protocol.md)
