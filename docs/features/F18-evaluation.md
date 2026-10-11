# F18 · Evaluation: a small, shared eval harness

> **Status:** planned · **Milestone:** M4 (built with [F16](F16-summariser.md), its first user; before F9) · **Depends
> on:** F1, F2
>
> **In the TODO:** tick **F18 Evaluation** under *Now* ([TODO](../../TODO.md)) · **Replaces:** the first part of the
> backlog's *Evaluation* gap (the CI gate stays in the backlog)

Today the only quality check is the orchestrator demo: ten fixes, each verified, at about $0.66 a run. It says whether
the whole system still works, but not whether one part (a summary, a triage decision) is right, and it's too slow and
costly to run for every prompt change. F18 adds the machinery for small, cheap, repeatable evals. Each feature brings
its own cases and pass bar; F18 runs them, scores them, prices them and compares them with a baseline.

Anthropic's advice, followed here: start evaluating early with small samples of real cases (about 20), judge the
outcome rather than the path, use an LLM judge with one rubric where an answer can be right in more than one way, and
keep looking at results by hand ([multi-agent research
system](https://www.anthropic.com/engineering/multi-agent-research-system)).

## User stories

**F18-S1 — Cases as data.** As a *developer*, I want an eval suite to be a folder of cases, so that adding a case needs
no code.
- *Given* `evals/<suite>/cases.jsonl` (one case per line: id, input, expected, and the scorer to use) *then* the runner
  loads every case and rejects a malformed one with its line number.

**F18-S2 — One runner for every suite.** As a *developer*, I want to run any suite the same way, so that each feature
only writes its target.
- *Given* `dotnet run --project tools/louis-agent.evals -- <suite>` *then* each case is sent to the suite's target (an
  `IEvalTarget`, written by the feature: F16's calls the summariser, F17's runs a triage with louis-agent stubbed) and
  scored.
- *And* `--repeat N` runs each case N times, because model output varies; the report gives the pass rate and its spread.

**F18-S3 — Scorers.** As a *developer*, I want exact checks where the answer is fixed and a judge where it isn't, so
that
scores are cheap where they can be and fair where they must be.
- *Given* scorer `exact` or `contains` *then* the score is 1 or 0 with no model call.
- *Given* scorer `judge` with a rubric *then* one call to `EVAL_JUDGE_MODEL` returns a score from 0 to 1 and pass/fail
  with a reason, recorded as purpose `eval-judge`.

**F18-S4 — Cost and baseline in every report.** As an *operator*, I want each run's cost and its comparison with the
baseline, so that a change is kept only when quality holds and the cost is known.
- *Then* every model call is in the usage ledger with purpose `eval` (or `eval-judge`) and a run id; the report gives
  pass rate, cost per case and total cost.
- *Given* `--baseline` *then* the run is saved as `evals/<suite>/baseline.json`; otherwise the report shows the change
  against it per case (newly failing cases first).
- *And* the runner exits non-zero when the pass rate falls below the suite's bar, ready for a CI gate later.

## Design

- **`tools/louis-agent.evals`**, a console app next to the usage probe, referencing core (and the orchestrator for
  F17's target; its types are `internal`, so the orchestrator grants `InternalsVisibleTo` to the evals tool, as it
  does to its tests). Not in core: evals are a developer tool, not something an agent does.
- **Suites** in `evals/<suite>/`: `cases.jsonl`, `suite.json` (target, pass bar, judge rubric), `baseline.json`.
  Cases are checked in, so a change and its eval result are reviewed together.
- **`IEvalTarget`** (`Task<string> RunAsync(EvalCase, CancellationToken)`) per suite; **`IEvalScorer`** per scorer
  kind. Both are small interfaces, so a suite adds a class, never a branch in the runner.
- **Usage:** each case runs in a `UsageScope` with purpose `eval` and `UsageTags` run id `eval-<suite>-<time>`, so
  `usage-probe -- --ledger` totals it like a demo run.
- **The judge** is another model call with a fixed rubric and a structured result (score, pass, reason); its model is
  set apart from the one under test, so a cheap model isn't marking its own work.
- **Cost:** every run calls a model, so like the benchmark it runs only with Louis's go-ahead; a 20-case suite on
  Haiku 5.5 costs cents.

## Implementation steps

1. **Case format and loader.** *Test:* a valid file loads; a malformed line is reported with its number.
2. **Runner with a fake target and `exact` / `contains` scorers.** *Test:* scores and pass rate from a scripted target;
   `--repeat` aggregates.
3. **Usage scope and cost in the report.** *Test:* records carry purpose `eval` and the run id; the report total equals
   the ledger's.
4. **Baseline save and compare.** *Test:* a newly failing case is listed first; the exit code follows the pass bar.
5. **The `judge` scorer.** *Test (fake client):* the rubric is sent; a malformed judge reply fails the case with a
   reason
   instead of crashing the run.
6. **First real suite:** F16's (built in F16). Then F17's triage suite.
7. **Docs:** a README in `evals/`, the tests doc, the development guide (how to add a suite).

## Done when

- Stories' criteria pass; tests added; suites pass.
- F16's suite runs end to end with a saved baseline; its report's cost equals the ledger's.
- **Then finish up** (see [Finishing a feature](README.md#finishing-a-feature)): tick **F18** in the TODO, set *Status*
  to done here and in the features table, and update skill 12 in the goal map.

---
[Features](README.md) · Previous: [F17 Orchestrator agent](F17-orchestrator-agent.md)
