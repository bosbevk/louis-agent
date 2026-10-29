# FX28 · A replay passes for the check's last run, and it costs "nothing"

> **Fix doc.** What was seen on the Benchmarks page on 2026-10-29, why, and what changes. Status: **open** ·
> Branch `fix/FX28-replay-runs-marked`, from `refactor/CL26-eval-card` · Plan: [FX28-replay-runs-marked.md](../plans/FX28-replay-runs-marked.md).

## Symptom

On the Benchmarks page the cook card's *Last run* line read `c0362ff73151 · 2026-10-29 · 1 repeat · R0`, the Run
button's estimate read `≈ R0 for 1 repeat`, and the run's row looked like any other finished run. Louis: "how is
this possible". Nothing on the page said the run was a replay of `dc272b95eaeb`, and there is no way to start a
replay from the page: the API takes `{"repeat": 1, "replay": "{runId}"}`, the page only sends the repeats.

## Cause

Proved by reading the stored run: its settings hold `replay_of: dc272b95eaeb` and `web: replay`, written by
`EvalSettings.WithOptions` when the run started. A replay plays a recording's answers back, web and model, so it asks
no model and its cost is a known R0 (the local judge is priced 0). The Benchmarks page's row (`RunSummary`) carried
only `RescoreOf`; `RunEstimate` took "the newest done run that isn't a re-score with a known cost", so the replay's R0
became the estimate. The Evals page reads `replay_of` since CL26 (`StoredRunRow.ReplayOf`); the Benchmarks page never
did.

## Fix

The row says whether a run was recorded and what it replayed, and the page uses it:

- `StoredRunRow.Recorded`, read from the run's `web` setting by `EvalSettings.Recorded` (the class that writes a
  setting reads it back, as CL26 did for `ReplayOf`).
- `RunSummary` carries `ReplayOf` and `Recorded`; `RunEstimate` leaves a replay out, as it leaves a re-score out; a
  run's row says *replay of …* with a link (`ReplayedFrom`, beside `RescoredFrom`); the card's *Last run* line gets a
  *replay* chip.
- A recorded, finished run's row has a *Replay* button (`ReplayButton`): it asks the page to start the check with the
  run's repeats and `replay: {its id}`, and the page follows the new run as it follows any run.

## Tests

`EvalRunOptionsTests` (a recording is known from its settings), `AdminRowsTests` (the row says it), `EvalReplyJsonTests`
(the field is on the wire), `RunSummaryTests`, `RunEstimateTests` (a replay is not a measure), `RunListItemTests` (the
note and the button), `CheckCardTests` (the chip), `StartRunBodyTests` (the body carries `replay`, and leaves it out
when there is none). Each written first and seen failing.

## Confirmed

Filled in at the end.

## Lesson

Filled in at the end.

---
[Fixes](README.md) · [Docs index](../README.md)
