# louis-agent.evals — design

> **Kind:** library (`net10.0`) · **Path:** `src/evals/louis-agent.evals/` · **References:** louis-agent.core,
> louis-agent.session-stream, louis-agent.evals.protocol, louis-agent.evals.checks · **Used by:** louis-agent.evals.runner,
> louis-agent.identity.puls.judges
> **Also shows:** `component`

![The eval harness, and its parts: an eval as a class, trials, checks and metrics, the protocol and the judge's calibration](diagrams/louis-agent.evals.svg)

## Purpose

This is the eval harness: the one place that knows how an eval runs. Because of it, an eval set only has to say what
it asks and how it grades.

It holds the contract an eval implements (`IEval`), the datasets and their loader, the trials and their transcripts,
the graders every identity can use, the loop that runs it all, the conformance check, the label sheet, and the
judge's calibration. It names no identity and no product. Puls's evals live in a project of their own.

The rest of this doc follows one run: what an eval is, its dataset, a trial, the checks that grade it, the protocol
it's written in, the roll-up, and how the checks themselves are checked. It ends with the rules every grade keeps,
then the files, the settings and the tests, to look things up.

*Decided in:* F18, [ADR 0029](../adr/0029-simulations-in-csharp.md).

## How it works

Here's one run, from start to finish.

![One run: the dataset loaded strictly, each trial a conversation, a failure of the plumbing kept apart, the rest graded](diagrams/louis-agent.evals-flow.svg)

### 1. An eval is an IEval

`IEval` names an eval's modes, its dataset, its graders, and the identity it proves. A mode (`EvalMode`) is the
toolsets and the role a session opens with. `EVALS=` picks evals by the identity they prove.

Most evals today are a folder of files, read by `Files/FileEval`, as the template's example in `src/evals/template/`
is. The only one still written in C# is the runner's stub.

*Decided in:* F49.2.

### 2. A dataset is a folder

`Datasets/` loads `dataset.json` (its name, version, hash and `changes`), and the scenario files under
`scenarios/{dev,held-out}/`. A scenario has Inspect's Sample shape: `id`, `input`, `target` and `metadata`.

The loader is strict. An unknown field, a stale hash, or an expected key no grader reads is refused, with a message
that says what to fix.

*Decided in:* F18 step 7d.

### 3. A trial is one conversation

`Running/` opens one session per mode, repeat and scenario, through an `ISessionClient`. That's `HttpSessionClient`
against the API, or `CannedSessionClient` for the stub. It asks the questions and the follow-ups, and reads the stream
into a `Transcript` (`Trials/`).

Things can fail that aren't the agent's fault, so those are kept apart. A failure of the connection is tried once
more. An infrastructure failure is kept apart from a failing answer (`TurnFailures`). And a stream that ends on an
error of kind `replay_miss_error` marks the turn `TurnFailure.ReplayMiss`, so a replay with no recording never counts
as the agent's answer.

*Decided in:* FX26.

### 4. Checks grade, metrics measure

`Grading/` holds the general checks (`Common/`), the model judge (`Models/`, `EvaluatorCheck`, over
Microsoft.Extensions.AI.Evaluation), and the human review (`Human/`). `ICheck`, `Grade` and `Verdict` are the check
contract's, in [louis-agent.evals.checks](louis-agent.evals.checks.md).

A grade has a score from 0 to 1, an outcome and a reason. A conversation also has metrics: its calls, hosts, turns,
tokens, seconds, and its cost from the ledger. Metrics are there to measure; they never decide a pass.

### 5. A general check for every family

Every family of check a standard eval tool ships has a general check here. Each is a small class with a nested
`Entry` and its `Settings` record, named from a file. For example, `WithinTimeCheck.Entry` is built from
`{"seconds": 120}`. `Grading/Common/GeneralChecks.All` lists all 25 the catalogue gets from the harness, by family,
the judge and the human review last. [Add an eval](../guides/add-an-eval.md#4-choose-the-checks) has the table.

There's a folder per family. `Facts/` has no error, time, cost, turns and tokens. `Answers/` has the text rules and
`is-json`. `Tools/` has `tool-used` and its kin, `routed-to`, and under `Results/` `tool-rows-marked`, which lists the rows a
tool marked and only ever passes, since it measures. `Compare/` extracts a value and then compares it, or checks that
the answer links to one of the hosts it names (`links-to-host`).
`Similarity/` has `near`, the Levenshtein distance. And `Language/` has `answers-in-language`, which counts words per
language, with no model.

They read the engine's events through `EngineEvents`, and they use plain string methods, never a regular
expression. A text rule ignores case and accents. A tool rule counts the calls of the agent and of its specialists.

*Decided in:* F49.2b, [ADR 0056](../adr/0056-a-check-is-a-catalogue-entry.md),
[ADR 0057](../adr/0057-regex-is-the-last-option.md).

### 6. Extract, then compare

Some checks compare a value in the answer with the scenario's. Each is a general compare plus a reader, which the
file names beside the key. In `Compare/`, `amount-under` uses an `IMoneyReader`, `number-named` an `INumberReader`,
and `image-shown` an `IImageReader`. The plain readers are in `Compare/Readers/`: `currency-amount`, `digits` and
`markdown-images`, listed in `GeneralReaders.All`.

An entry gets its reader from `CheckContext.Readers`. That's a `ReaderCatalog` the runner fills with these readers
and every identity's own. So an identity ships readers, not checks. A compare passes a scenario that sets no key.

Embedding similarity (`similar`) isn't built. No eval asks for it yet, and it needs a provider with embeddings; it's
on the TODO.

### 7. The route, and the judge

`routed-to` reads which role should have answered from a scenario key (`expected_key`). A mode with none of the
`delegating_toolsets` is expected to answer itself (`RoutedToCheck.ByScenarioKey`).

`judge` (`Grading/Models/Rubrics/JudgeCheck`) asks a model to grade the answer. It sends a rubric file from the eval's
folder as the system prompt. Then it sends only the scenario keys the rubric lists (`ask`), the conversation so far,
what the tools named in `sources` gave back, and the answer. It reads `{"pass", "reason"}` from the reply. A null, or
no verdict at all, is Unknown, and the judge is never a required grader.

The tools' results come from `Rubrics/Sources/JudgeSources`. Each result is labelled, in call order, a specialist's
too, and cut at 12,000 characters with the cut said out loud. A tool that was never called is told in one line. With
no `sources` named, nothing is added. A file that adds `sources` bumps its judge's `version`, because the rubric's
hash doesn't cover them.

`human` reviews a judge the eval lists before it, asking the rubric's title as its question.

### 8. The protocol is written in one place

`Protocol/ProtocolWriter` writes each event as one JSON line, through the contract's `EvalProtocolJson`.
`ProtocolTranscripts` reads a stored run back, for a re-score or a conformance case.

*Decided in:* [ADR 0047](../adr/0047-protocol-v2-one-event-contract.md).

### 9. The roll-up

`Reports/RunSummary` turns a run's trials into one overall line, plus one line per category, per grader and per kind
of grader. Each line has the pass rate, pass@k, pass^k and `n`, and says "too few" under five trials. It's pure: it
only works the numbers out, and touches nothing else.

### 10. Checking the checks

`Conformance/` runs every eval's reference transcripts, each of which must pass its required graders. It also runs
known-bad ones, where each required grader must fail at least one.

`Labels/` writes the sheet a person labels, and calibrates a model judge against those labels. The bar is 90% of each
class.

*Decided in:* [ADR 0029](../adr/0029-simulations-in-csharp.md).

### 11. The rules every grade keeps

A person's review only ever checks a judge. Each `Human` grader is named `{x}.human`, beside a model grader `x` in
the same eval, and conformance refuses any other name. A human review is never required, so the roll-up has no
"pending" count.

A required grade nobody could give is Unknown, never a pass (`PassRates`).

Names are enums, one set per project. An eval set keeps its own toolset enum (`PulsToolset`, `StubToolset`), and
`RepeatedNameTests` keeps the bare strings out.

*Decided in:* F18 step 5.7, F49.3, [ADR 0043](../adr/0043-repeated-names-are-enums.md).

![The eval harness: what the runner uses, and where a run's events go](diagrams/louis-agent.evals-diagrams.svg)

![One run: for each mode, repeat and question the runner asks a session and writes the events, then grades each conversation and sums up](diagrams/louis-agent.evals-diagrams-2.svg)

These are the types involved:

![The types: an eval is an IEval, most read from a folder, and the runner asks the agent through a session client and writes each event on a line](diagrams/louis-agent.evals-interface.svg)

## Structure

| Folder | What's in it |
|---|---|
| `Api/` | The session clients and the session request the runner sends (with `replay`) |
| `Conformance/` | `EvalConformance`, its cases and the known-bad transcripts |
| `Datasets/` | The loader, `EvalFiles` (where an eval's data is at run time), `Manifest/`, `Samples/` (a scenario's types are in [louis-agent.evals.checks](louis-agent.evals.checks.md)) |
| `Files/` | An eval as a folder of files (F49.2): `EvalFile` and its records, the shape of `eval.json`; `EvalFileReader`, which reads one strictly and refuses what it can't trust; `FileEval`, the eval built from the folder (its checks from the catalogue in the file's order, `Dataset/` beside it, the manual's path from the source the caller gives) |
| `Grading/` | `NamedCheck`, a check under the name an eval file gives it; the general checks (`Common/`, a folder per family: `Facts/`, `Answers/`, `Tools/`, `Compare/` with its `Readers/`, `Similarity/`, `Language/`), the model checks (`Models/`) and the human review (`Human/`), the pass rates (`Outcomes/`) (`ICheck`, `Grade`, `Verdict` are in [louis-agent.evals.checks](louis-agent.evals.checks.md)) |
| `Labels/` | The label sheet, the label loader and the calibration with its `@` report line |
| `Messages/` | The harness's texts (errors, logs, conformance, calibration); the grade reasons, `CommonGradeMessage`, are in [louis-agent.evals.checks](louis-agent.evals.checks.md) |
| `Protocol/` | The writer and the stored-run reader |
| `Reports/` | `RunSummary`, `SummaryLine`, `TrialResult` |
| `Running/` | `EvalRunner`, a trial's attempt and grading, re-scoring, the tallies |
| `Trials/` | The conversation's metrics (`Trial`, `Transcript`, `TurnFacts` and the engine's events are in [louis-agent.evals.checks](louis-agent.evals.checks.md)) |

## Configuration

| Setting | What it does |
|---|---|
| `AGENT_API`, `AGENT_API_KEY` | Where the runner's sessions open, and the key the API asks for |
| `EVAL_JUDGE_PROVIDER`, `EVAL_JUDGE_MODEL`, `EVAL_JUDGE_ENDPOINT`, `EVAL_JUDGE_API_KEY` | The model judge (the Puls stack uses a local Ollama) |

The runner's own settings, `EVALS` and `BENCHMARK_RUN`, are in [louis-agent.evals.runner](louis-agent.evals.runner.md).

## Extending it

A new identity's evals are folders of files beside it; the steps are in [Add an eval](../guides/add-an-eval.md).

A check every identity could use goes in `Grading/Common/`, in its family's folder, with a nested `Entry` and a line
in `GeneralChecks.All`. A value an identity writes its own way is a reader in the identity, named beside a general
compare. Only a rule no general check can say stays with the identity's own checks.

A new language for `answers-in-language` is one member of `AnswerLanguage`, and one list in `LanguageWords`.

## Tests

The tests are in `src/tests/evals/louis-agent.evals.tests`. They cover the loader (both shapes refused or read, the
hash, the expected keys), and the general checks and their entries: `GeneralChecksTests`, plus one test class per
check under `Grading/Common/`, by family. They also cover the roll-up, the conformance cases, the label sheet and the
calibration, and the protocol writer, against its golden file. The runner's tests run the stub end to end
([tests](tests.md)).

## Limits and plans

A gate that only looks at recorded transcripts can't see the agent; only a real run can. The learning write-up for
skill 12 goes into that.

The judge's grades are advisory. In its recorded calibration it agreed with a blind labeller on 31 of 36 meals, under
the 90% bar ([write-up 12](../learning/12-evaluation.md)).

There's no parity run against the old Python harness: its runs were deleted before they could be exported. So the
first C# run is the baseline, and the trends start there. That was `puls-cook` 23 of 24 when Puls handed the meal to
its cook, 25% when it answered itself with web tools, and `puls-routing` 72 of 72, for $0.37 together (runs
`5d1fd231c9b1` and `0d3b95e093a6`). After FX27 the cook's baseline is `dc272b95eaeb`: `puls-cook` 8 of 8 and
`puls-tools` 2 of 8, for $0.075. Its free replay, `c0362ff73151`, hits at all 20 turns. A replay costs $0, because
every request is a cache hit. The full plan of the first harness is at the tag
[`pre-cl24`](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F18-evaluation.md).

The evals are to be redesigned: one `.eval.json` of conversations with turns, its own host and screens, and today's
evals deleted rather than ported ([F126's plan](../plans/F126-evals-redesigned.md)).

## Related docs

[ADR 0029](../adr/0029-simulations-in-csharp.md) · [ADR 0047](../adr/0047-protocol-v2-one-event-contract.md) ·
[Add an eval](../guides/add-an-eval.md) · [Run the evals](../guides/run-the-evals.md) ·
[Write-up 12](../learning/12-evaluation.md) · [Write-up 20](../learning/20-extensibility.md)

---
[Projects](README.md) · Previous: [louis-agent.usage-probe](louis-agent.usage-probe.md) · Next: [louis-agent.evals.checks](louis-agent.evals.checks.md)
