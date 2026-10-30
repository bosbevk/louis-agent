# louis-agent.evals — design

> **Kind:** class library (`net10.0`) · **Path:** `evals/louis-agent.evals/` · **References:** louis-agent.core,
> louis-agent.session-stream, louis-agent.evals.protocol, louis-agent.evals.checks · **Used by:** louis-agent.evals.runner, every eval set
> (`louis-agent.evals.puls`, `louis-agent.evals.template`)

## Purpose

The eval harness (F18): the one place that knows how an eval runs, so an eval set only says what it asks and how it
grades. It holds the contract an eval implements (`IEval`, `IEvalSet`), the datasets and their loader, the trials and
their transcripts, the graders every identity can use, the runner loop, the conformance check, the label sheet and
the judge's calibration. It names no identity and no product: Puls's evals live in their own project
([ADR 0029](../adr/0029-simulations-in-csharp.md)).

## How it works

- **An eval is a C# class.** `IEval` names its modes (`EvalMode`: the toolsets and role a session opens with), its
  dataset and its graders; `IEvalSet` groups an identity's evals for the runner's catalogue.
- **A dataset is a folder.** `Datasets/` loads `dataset.json` (name, version, hash, `changes`) and the scenario files
  under `scenarios/{dev,held-out}/`, in Inspect's Sample shape (`id`, `input`, `target`, `metadata`; step 7d). The
  loader is strict: an unknown field, a stale hash or an expected key no grader reads is refused with a message that
  says what to fix.
- **A trial is one conversation.** `Running/` opens a session per mode, repeat and scenario through an
  `ISessionClient` (`HttpSessionClient` against the API, `CannedSessionClient` for the stub), asks the questions and
  follow-ups, and reads the stream into a `Transcript` (`Trials/`). A transport failure is tried once more; an
  infrastructure failure is kept apart from a failing answer (`TurnFailures`); a stream that ends on an error of kind
  `replay_miss_error` marks the turn `TurnFailure.ReplayMiss` (FX26), so a replay with no recording never counts as
  the agent's answer.
- **Checks grade, metrics measure.** `Grading/` holds the general checks (no error, answered within a
  time, cost under a limit, routed to a role, the answer contains a text, a tool was used), the model judge (`Models/`, `EvaluatorCheck` over Microsoft.Extensions.
  AI.Evaluation) and the human review (`Human/`); `ICheck`, `Grade` and `Verdict` are the check contract's, in
  [louis-agent.evals.checks](louis-agent.evals.checks.md). A grade has a score from 0 to 1, an outcome and a reason; a
  conversation's metrics (calls, hosts, turns, tokens, seconds, cost from the ledger) never decide a pass.
- **A general check can be named from a file** ([ADR 0056](../adr/0056-a-check-is-a-catalogue-entry.md)).
  `no-error`, `within-time`, `cost-under`, `contains` and `tool-used` each carry a nested `Entry` and its `Settings`
  record (`WithinTimeCheck.Entry`, built from `{"seconds": 120}`), and `Grading/Common/GeneralChecks.All` is the
  harness's list for the check catalogue. `contains` (`Grading/Common/Answers/`) checks that the first turn's answer
  holds one of a list of texts, case and accents ignored, never a pattern
  ([ADR 0057](../adr/0057-regex-is-the-last-option.md)). `tool-used` (`Grading/Common/Tools/`) checks that a tool
  with this exact name was called in the trial, by the agent or a specialist; a streamed error is `no-error`'s to
  report. `routed-to` has no entry yet: its expected role is a function (Puls's reads a scenario key and the mode's
  toolsets), which F49.2's eval file settles.
- **The protocol is written once.** `Protocol/ProtocolWriter` writes each event as one JSON line through the
  contract's `EvalProtocolJson` ([ADR 0047](../adr/0047-protocol-v2-one-event-contract.md)); `ProtocolTranscripts`
  reads a stored run back for a re-score or a conformance case.
- **The roll-up is pure.** `Reports/RunSummary` turns a run's trials into one overall line and one line per category,
  per grader and per grader kind (pass rate, pass@k, pass^k, `n`, "too few" under five trials).
- **Checks of the checks.** `Conformance/` runs every eval's reference transcripts (each must pass its required
  graders) and known-bad ones (each required grader must fail one); `Labels/` writes the sheet a person labels and
  calibrates a model judge against the labels (90% of each class, ADR 0029).

## Design notes

What F18 settled while building the harness, and why (the plan, the feature doc with every step's notes and the
unattended run's decisions are at the tag [`pre-cl24`](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F18-evaluation.md)):

- **No parity run against the Python.** Its runs were deleted before they could be exported, so the first C# run is
  the baseline and the trends start there; step 3's tests and reference transcripts that pass every grader hold the
  graders instead.
- **The first v2 baseline** (2026-10-29): `puls-cook` 23 of 24 when Puls hands the meal to its cook (25% when it
  answers itself with web tools), `puls-routing` 72 of 72, $0.37 together; baseline runs `5d1fd231c9b1` (cook) and
  `0d3b95e093a6` (routing). A replay costs $0, every request a cache hit. After FX27 the cook baseline is
  `dc272b95eaeb` (`puls-cook` 8 of 8, `puls-tools` 2 of 8, $0.075): its free replay `c0362ff73151` hits at all 20
  turns, the delegating ones included, where the first baseline's replay missed at 8.
- **The judge isn't trusted yet.** The local judge agreed with a blind labeller on 31 of 36 meals, under the 90% bar
  ([ADR 0029](../adr/0029-simulations-in-csharp.md)), so its grades stay advisory until Louis's own 36 labels
  calibrate it (TODO, *Now*).
- **A person's review only checks a judge.** Each `Human` grader is named `{x}.human` beside a model grader `x` of the
  same eval; conformance refuses any other, and a human review is never required, so the roll-up has no pending count.
- **A required grade nobody could give is Unknown, never a pass** (`PassRates`).
- **Names are enums per project** ([ADR 0043](../adr/0043-repeated-names-are-enums.md)): an eval set keeps its own
  toolset enum (`PulsToolset`, `StubToolset`), and `RepeatedNameTests` keeps the strings out.
- **The cook gives estimates and reads no shop page** (step 5.7): Checkers refuses our browser and the other shops'
  robots.txt disallow their search pages ([ADR 0038](../adr/0038-the-browser-says-what-it-is.md)), so
  `estimates-at-shop` checks that no price is given as fact; the old `PricedAtRetailer` waits, tested, for a
  retailer's yes.

## Structure

| Folder | What's in it |
|---|---|
| `Api/` | The session clients and the session request the runner sends (with `replay`) |
| `Conformance/` | `EvalConformance`, its cases and the known-bad transcripts |
| `Datasets/` | The loader, `EvalFiles` (where an eval's data is at run time), `Manifest/`, `Samples/` (a scenario's types are in [louis-agent.evals.checks](louis-agent.evals.checks.md)) |
| `Grading/` | The general checks (`Common/`, with `Answers/` and `Tools/`), the model and human checks, the outcomes (`ICheck`, `Grade`, `Verdict` are in [louis-agent.evals.checks](louis-agent.evals.checks.md)) |
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

The runner's own settings (`EVALS`, `BENCHMARK_RUN`) are in [louis-agent.evals.runner](louis-agent.evals.runner.md).

## Extending it

A new identity's evals are a new project with one `IEvalSet`, its evals, datasets and graders; the steps are
[Add an eval set](../guides/add-an-eval-set.md). A check every identity could use goes in `Grading/Common/`,
with a nested `Entry` and a line in `GeneralChecks.All` when a file should be able to name it; one that knows an
identity's rules stays in its set.

## Tests

`tests/louis-agent.evals.tests`: the loader (both shapes refused or read, the hash, the expected keys), the general
checks and their entries (`CommonChecksTests`, `GeneralChecksTests`, `ContainsCheckTests`, `ToolUsedCheckTests`),
the roll-up, the conformance cases, the label sheet and the calibration, the protocol writer against its
golden. The runner's tests run the stub end to end ([tests](tests.md)).

## Limits and plans

- A gate on recorded transcripts can't see the agent; only a run can (the learning write-up, skill 12).
- A run's first line carries no dataset yet.

## Related docs

[ADR 0029](../adr/0029-simulations-in-csharp.md) · [ADR 0047](../adr/0047-protocol-v2-one-event-contract.md) ·
[Add an eval set](../guides/add-an-eval-set.md) · [Run the evals](../guides/run-the-evals.md) ·
[Write-up 12](../learning/12-evaluation.md) · [Write-up 20](../learning/20-extensibility.md)

---
[Projects](README.md) · Previous: [louis-agent.usage-probe](louis-agent.usage-probe.md) · Next: [louis-agent.evals.checks](louis-agent.evals.checks.md)
