# 20 · Extensibility: a check as a catalogue entry, an eval as a folder

> **Skill:** #20 in the [goal](../GOAL.md)'s map, with #12 (evaluation) for what a check is · **Stages:** Phase 3b,
> F49.1 *The checks catalogue* and F49.2 *Evals as files*, the first two children of F49 *Evals as content*
> (2026-10-30), and F49.2b *The generic checks* (2026-10-31) · **Written by** Claude, for Louis to read (2026-10-30,
> extended the same day for F49.2 and on 2026-10-31 for F49.2b) · **Next:** a
> pack shipping its own checks (F49.3); the studio's form made from a check's settings (F49.5)

What this answers: how a check went from "a constructor call in an eval's C#" to "a name a file can use", why the
contract left the harness, and what was left out on purpose (F49.1); then how an eval went from a C# class in its own
project to a folder of files beside its identity, and what that move taught (F49.2, from
[An eval as a folder of files](#an-eval-as-a-folder-of-files-f492) on).

## Before and after

| | Before F49.1 | After F49.1 |
|---|---|---|
| Where the contract lives | Inside the harness (`evals/louis-agent.evals`): `IGrader`, `Grade`, `Trial` | Its own project, `evals/louis-agent.evals.checks`, which references only what a pack references |
| Who can write a check | The harness and an eval set | Also a pack (F49.3), without reaching the harness that runs Puls's evals |
| How an eval gets a check | `new WithinTimeGrader(TimeSpan.FromSeconds(120))` in C# | That, or `within-time` with `{"seconds": 120}` from a file, through the catalogue |
| The words | "Grader" everywhere | `ICheck` in C#; the wire still says `grader`, so stored runs read the same (ADR 0049) |
| The general checks | four, all C# only | six, five of them named from a file: `no-error`, `within-time`, `cost-under`, `contains`, `tool-used`; `routed-to` C# only |

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

## An eval as a folder of files (F49.2)

| | Before F49.2 | After F49.2 |
|---|---|---|
| What an eval is | A C# class (`CookEval`, `RoutingEval`) in `evals/louis-agent.evals.puls` | A folder with an `eval.json`, its manual, dataset, rubric and transcripts, under the identity's `Evals/` |
| How the runner knows it | An `IEvalSet` per identity, chosen by an enum, `EVALS=puls` | Every `eval.json` copied beside the runner; `EVALS=` chooses by the identity the file names, and a typo is refused |
| Where its checks come from | `new …()` in the eval's C# | By type from the catalogue: the general checks, each identity's own through the composition (`EvalChecks`), and Puls's judge from its own project |
| A new eval that uses existing checks | A C# class, a set entry and a registration | A copy of `evals/template/Example/`, edited; zero C# |
| A turn's facts | Each eval's own (`PulsTurnFacts`) | The harness reads them from the engine's events, for every eval |

The stored runs, the trend and the portal didn't move: every check kept its name and version, and the cook's eval
kept its version (10), since moving it changed no scenario, mode or check.

**Three more patterns.** *Registry* again, this time in the composition: `EvalChecks.All` is where every identity's
check list meets, because the composition is the one project that knows every identity, so the runner never names
one. The *Stable Dependencies Principle* (Robert Martin) for where the cook's judge lives: it is built on the
harness's judge, and the identity, which every host loads, must never carry the harness, so the judge got a small
project of its own beside the identity that only the runner references. *Prototype* (Gang of Four) for the template:
a working eval you copy and change, proven to load and conform with no C#, instead of a scaffold with empty members
that throw.

**What F49.2 taught:**

- **Prove a move with the thing it must not change.** The cook moved from C# to files in step 8b, and a free replay
  of the recorded baseline (`--replay`, every model answer from the recording) graded 140 of 140 the same: outcome,
  score, version, reason and the judge's prompt hash. A test suite proves the code; a replay proves the stored meaning.
- **Same name, same meaning, or a new name.** The cook's `cook-no-error` looked like the general `no-error`, but the
  cook's leaves a broken turn Unknown before counting errors and the general one fails on an error first, with other
  reasons. Swapping one for the other would have changed what stored grades mean under the same name, so the cook's
  stayed its own entry.
- **A rubric is content, and its words are its identity.** The judge's prompt left its embedded resource for a file
  in the eval's folder word for word, and the review found its hash was of the bytes: `161e5cebcc15` on a Windows
  checkout (CRLF), another on Linux (CI went red). `RubricHash` now makes the line endings LF before hashing, so one
  rubric is one hash (`438a8deb91f4`) everywhere; the 36 labels the judge was calibrated against still describe it,
  and the runs recorded before carry the old hash as another measure.
- **A dependency rule finds the design for you.** "The identity never references the harness" is what put the judge
  in a project of its own; a check the identity's code could hold (the six fixed rules) went beside it, one that
  needed the harness couldn't. Writing the rule as a test (`ArchitectureTests`) keeps it.
- **Keep a move reviewable.** Step 9b moved 60 files with `git mv` and a byte check, and only their namespaces
  changed, so git showed 60 renames; their missing summaries and long comments were tidied in a step of their own
  (9c), checked by comparing each file's code with comments and whitespace stripped. Mixed in one commit, the moves
  would have read as rewrites.
- **A plan is cut where the work turns out to be.** Nine steps became fourteen: the cook needed its checks named from
  a file before it could become one (7, 8a), the sets had to go after both evals were files (8c), the judge needed a
  home before the rest could move (9a), and the template and the docs were each a review of their own (10a to 10c).

**Left out on purpose:** the cook's shop hosts are no longer a turn's facts (the harness reads the URLs a tool was
given, and a passing cook run calls no shop tool), and a check of an identity's own is still C#: a rule is code, and
F49.3 moves the shops' checks to the shops pack.

## Readers: the smallest thing a domain ships (F49.2b)

F49.2b made the extension point smaller. An identity used to ship whole checks; now a check that compares a value in
the answer with the scenario's is a general compare plus a **reader**, a one-method Strategy the file names beside the
key (`"reader": "rand-total"`). Readers sit in their own registry (`ReaderCatalog`), built like the check catalogue:
the harness lists its plain ones, Puls lists its three (`PulsReaders.All`), the composition gathers them. Puls's
readers are Adapters over the scoring code its old checks used, unchanged, so the knowledge didn't move, only the
seam did. It's the same shape as a pack shipping tools: the engine owns the loop, the domain owns the small piece only
it knows. How we proved no grade moved is in [write-up 12](12-evaluation.md#generic-checks-and-readers-instead-of-pulss-own-f492b).

## Sources

Fowler, *Patterns of Enterprise Application Architecture* (Separated Interface, Registry, Plugin) · Gamma et al.,
*Design Patterns* (Factory Method, Prototype) · Robert C. Martin, *Agile Software Development* (the Stable
Dependencies Principle) · Microsoft, *Framework Design Guidelines* (namespaces) and the System.Text.Json schema
exporter · promptfoo, *assertions* (`icontains-any`) · Inspect AI, *scorers* (`includes()`) and *datasets* (`Sample`)
· Anthropic, *Develop tests* (tool-call checks) · [ADR 0056](../adr/0056-a-check-is-a-catalogue-entry.md) ·
[ADR 0058](../adr/0058-an-eval-is-a-folder-of-files.md) · [ADR 0055](../adr/0055-keep-it-simple.md) ·
[ADR 0057](../adr/0057-regex-is-the-last-option.md)

---
[Docs index](../README.md) · Related: [Learning](README.md) · [louis-agent.evals.checks](../projects/louis-agent.evals.checks.md)
