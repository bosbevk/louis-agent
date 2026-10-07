# F6 · Cost estimates before a run

> **Status:** planned · **Milestone:** M3 · **Depends on:** F1, F2
>
> **Spec:** [Usage §5](../specs/USAGE_AND_BUDGETS.md)
>
> **In the TODO:** tick **F6 Estimates** under *Now* ([TODO](../../TODO.md))

Cost is only known afterwards today. F6 predicts it — always as a range with its basis — for an orchestrator run, a
single fix, and (later) each method of a build-mode service, and checks itself against what actually happened.

## User stories

**F6-S1 — Estimate a run before it starts.** As an *operator*, I want the demo script to tell me what a run is likely to
cost, so that I can decide before spending.
- *Given* new errors in the log *when* `run-demo.ps1` reaches the orchestrator step *then* it prints e.g.
  `13 errors · ~10 likely fixes · estimated $0.40–0.90 (median of the last 20 fixes)` before any model call.
- *Given* a budget (F7) whose remainder is below the estimate's high end *then* it asks whether to continue.

**F6-S2 — A range with its basis.** As an *operator*, I want every estimate to say how it was made, so that I know how
far to trust it.
- *Given* at least 5 similar past tasks (same kind, model and toolset) in the ledger *then* the range is their 25th–75th
  percentile and the basis says "history (n tasks)".
- *Given* fewer *then* the model-based estimate is used (S3) and the basis says "model (no history yet)".

**F6-S3 — A sensible estimate with no history.** As an *operator* on a fresh install, I want a model-based estimate, so
that the first run isn't a blind guess.
- *Then* estimate = rounds × (prefix + average history) priced with caching applied (first round at write price, later
  rounds at read price) + expected output; defaults (≈ 20 rounds per fix) come from the measured demo profile, and the
  prefix from the last recorded request (or the startup estimate, F5).

**F6-S4 — Estimate vs actual.** As an *operator*, I want each fix's estimate shown next to its actual cost, so that the
estimates visibly improve.
- *Then* `fixes.jsonl` stores `estimate`; the comms log shows *estimated / actual* per fix and the run's overall error.

**F6-S5 — Estimates for the orchestrator itself.** As the *orchestrator*, I need an estimate for the next fix, so that
budgets (F7) can stop a fix before it starts rather than halfway.
- *Then* `ServiceTools` gets the fix estimate before `CallLouisAgentFix` sends anything.

## Design

New, in `louis-agent.core/usage/`:

| Type | Responsibility |
|---|---|
| `UsageHistory` | Reads the ledger (current and previous month) and groups completed tasks by kind (`fix`, `build`, `triage`, chat `turn`), model and toolset, with their total tokens and cost |
| `Estimator` | `Estimate(kind, count, context)` → `{ low, median, high, currency, basis, n }`; history when n ≥ 5, otherwise the model-based formula |
| `EstimateDefaults` | Rounds per task kind and average history growth, overridable in settings, seeded from the demo's measured numbers |

Uses:

- **Orchestrator `--estimate`** mode: reads the error log and, without any model call, counts the errors that look
  auto-fixable — a heuristic over each runbook's sections: an exception type listed under *Expected* is not fixable,
  *Auto-fixable: nothing* is not fixable, anything else is. The real triage (by the model) still decides; this only
  sizes the estimate. Prints the run estimate and exits; `run-demo.ps1` calls it in a container before the real run.
- **`ServiceTools`** asks for the per-fix estimate (S5) and records it.
- **`POST /estimates`** (spec §7.1) is added in F11, reusing `Estimator`.

## Implementation steps

1. **`UsageHistory`.** *Test:* a fixture ledger yields per-task totals grouped correctly; incomplete tasks are skipped.
2. **`Estimator` (history and model-based).** *Test:* percentiles on a fixture; fallback when n < 5; caching applied in
   the model formula; unpriced model → tokens-only estimate.
3. **Orchestrator: per-fix estimate recorded** in `fixes.jsonl` and the comms log; run summary shows estimate vs actual.
   *Test:* `CommsLog` renders both; error % computed.
4. **Orchestrator `--estimate` mode** (heuristic count). *Test:* with the demo's error log and runbooks it counts
   10 likely fixes without any model call (a fake chat client asserts zero requests).
5. **`run-demo.ps1`:** print the estimate; ask when it exceeds a budget's remainder (after F7). *Test (manual).*
6. **Docs:** samples README (what the script shows), spec status.

## Done when

- Stories' criteria pass; tests added; suites pass.
- After two benchmark runs, the second run's estimate comes from history and the run's actual cost falls within its range.
- **Then finish up** (see [Finishing a feature](README.md#finishing-a-feature)): tick **F6** in the TODO,
  set *Status* to done here and in the features table, and note it in the spec's implementation map.

---
[Features](README.md) · Previous: [F5 Toolset profiles](F05-toolset-profiles.md) · Next: [F7 Budgets](F07-budgets.md)
