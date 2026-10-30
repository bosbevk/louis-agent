# 20 · Extensibility: a check as a catalogue entry

> **Skill:** #20 in the [goal](../GOAL.md)'s map, with #12 (evaluation) for what a check is · **Stage:** Phase 3b,
> F49.1 *The checks catalogue*, the first child of F49 *Evals as content* (2026-10-30) · **Written by** Claude, for
> Louis to read (2026-10-30) · **Next:** an eval as a file that names its checks (F49.2); a pack shipping its own
> checks (F49.3); the studio's form made from a check's settings (F49.5)

What this answers: how a check went from "a constructor call in an eval's C#" to "a name a file can use", why the
contract left the harness, and what was left out on purpose.

## Before and after

| | Before F49.1 | After F49.1 |
|---|---|---|
| Where the contract lives | Inside the harness (`evals/louis-agent.evals`): `IGrader`, `Grade`, `Trial` | Its own project, `evals/louis-agent.evals.checks`, which references only what a pack references |
| Who can write a check | The harness and an eval set | Also a pack (F49.3), without reaching the harness that runs Puls's evals |
| How an eval gets a check | `new WithinTimeGrader(TimeSpan.FromSeconds(120))` in C# | That, or `within-time` with `{"seconds": 120}` from a file, through the catalogue |
| The words | "Grader" everywhere | `ICheck` in C#; the wire still says `grader`, so stored runs read the same (ADR 0049) |
| The general checks | four, all C# only | six: `no-error`, `within-time`, `cost-under`, `contains`, `tool-used` named from a file; `routed-to` C# only |

The cook's and routing's evals didn't change: they still build their checks in C#, every check kept its name and
version, and their reference and known-bad transcripts grade the same in the test suite before and after.

## Three patterns, each for one job

**Separated Interface** (Fowler, PoEAA) for where the contract lives. A pack may not reference the harness (ADR 0027),
so a pack couldn't ship a check while `ICheck` lived there. The contract moved to a small project its clients can
reference. It isn't a "references nothing" project: a check's reason is a message enum formatted by core's
`MessageText` (ADR 0009), a trial carries the session stream's events, and a grade carries the protocol's kinds. So it
references exactly those three, which a pack references anyway; `ArchitectureTests` holds the list.

**Factory Method** (Gang of Four) for how a file's settings become a check. `CheckType<TParameters>` does the part every
entry shares: read the JSON into a typed record, strictly (an unknown field, a missing one and a null are refused,
naming the type and the field, so a typo never becomes a default). The entry only says how to build its check from the
record. The record is also the schema: .NET's `JsonSchemaExporter` turns it into the JSON schema the studio will make
its form from, so the form and the reader can't disagree.

**Registry** (Fowler) for how a name finds its entry. Each owner lists its entries in one place
(`GeneralChecks.All` for the harness; a pack's list in F49.3) and the composition gathers the lists, as it gathers
`PACKS` and `IDENTITIES`. Weighed against it: an attribute scanned by reflection (magic a reader can't follow) and an
enum with a switch in the harness (the harness would have to reference every pack).

## What was left out, and why

ADR 0055 (*keep it simple*) did most of the work in this feature:

- **No `regex` check.** The plan had one because "the field uses it" (promptfoo has both). No eval of ours needs a
  pattern, and a pattern in a file is a language to learn, a slow-match risk (ReDoS) and hard to read back.
  `contains` takes a list of texts instead (one is enough, case and accents ignored), which covers "one of these words".
  The question became a rule for the code too: [ADR 0057](../adr/0057-regex-is-the-last-option.md).
- **No `routed-to` entry.** Its expected role is a function that reads a scenario key and the mode's toolsets. A file
  can't hold a function, so it waits for F49.2's eval file to settle how a route is stated.
- **The runner doesn't build the catalogue yet.** Nothing reads one until F49.2's file reader; `GeneralChecksTests`
  proves the catalogue on every test run in the meantime.
- **No `judge` or `human` entry.** They need a rubric as a file, which is F49.2's.

## What was learned

- **A rename stops at the wire.** `IGrader` became `ICheck` in C# (through Rider, one commit with nothing else in it),
  but the JSON keys stayed `graders`/`grader`: a stored run is data someone else may read, and ADR 0049 forbids an
  alias. The protocol tests and the golden files proved the wire didn't move.
- **A pure move is provable.** Steps 2 and 3 moved thirteen files with `git mv` and a SHA-256 check of each, and kept
  their namespaces (a namespace is logical, an assembly physical), so none of the 118 files using them changed.
- **A folder name is a namespace, and it can hide a name.** The plan's `Grading/Common/Text/` made a namespace `Text`,
  and the tests' `TrialBuilder.Text(…)` stopped compiling. The folder became `Answers/`, which also says better what
  the check reads.
- **Who did the work matters less than whether it was done.** `tool-used` counts a specialist's call too: Puls hands
  the meal to its cook, and an eval that asks "was the shop searched?" would otherwise fail because the work was
  delegated. The reason says who called it, so nothing is hidden.
- **One check, one question.** `tool-used` ignores a streamed error; `no-error` reports it. An eval that wants both
  lists both, and each grade says one thing.

## Sources

Fowler, *Patterns of Enterprise Application Architecture* (Separated Interface, Registry, Plugin) · Gamma et al.,
*Design Patterns* (Factory Method) · Microsoft, *Framework Design Guidelines* (namespaces) and the System.Text.Json
schema exporter · promptfoo, *assertions* (`icontains-any`) · Inspect AI, *scorers* (`includes()`) · Anthropic,
*Develop tests* (tool-call checks) · [ADR 0056](../adr/0056-a-check-is-a-catalogue-entry.md) ·
[ADR 0055](../adr/0055-keep-it-simple.md) · [ADR 0057](../adr/0057-regex-is-the-last-option.md)

---
[Docs index](../README.md) · Related: [Learning](README.md) · [louis-agent.evals.checks](../projects/louis-agent.evals.checks.md)
