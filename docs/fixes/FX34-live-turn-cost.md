# FX34 · A live run shows each turn's cost as the turn ends

> **Status:** open · **Area:** API: `EvalRecorder`, `RunRecording`, `LedgerJoin`, the admin run routes; Puls:
> `LiveTurns`, `LiveRunPanel` · **Found:** 2026-11-03 · **Plan:** [FX34](../plans/FX34-live-turn-cost.md)

## Symptom

On the Benchmarks page, a five-repeat puls-cook run on `claude-haiku-5-5` (`cf37574d0f23`, 2026-11-03) showed every
finished turn's role, calls and seconds, and "—" in its *Cost*: *"where is the cost"* (Louis, with the table's
screenshot). The run was spending money, and the page said nothing about how much.

## Cause

The table's cost of a turn is its requests summed, and it reads the requests only from the stored run, once the run
has ended (`LiveRunPanel.CostsAsync` after the event stream's `done`, then `LiveTurns.CostsFrom(StoredRunReply)`). The
recording writes a run's requests only at its end (`RunRecording.CloseAsync` → `JoinLedgerAsync` →
`store.AddRequestsAsync`): until then the store holds none (`GET /runs/cf37574d0f23` gave `"requests":[]` while it
ran). The turn event the runner prints carries no cost, and can't: the agent's model requests are written to the usage
ledger by the API's sessions, not by the runner. So "—" meant two things, "not priced yet" and "no price known".

The ledger had the rows all along: one per request, written as the request finishes, tagged with the run, its
session and its turn, and the recording knows each session's conversation from its question.

## Fix

*(planned)* The API says a recorded run's requests so far (`GET …/runs/{id}/requests`), read from the ledger and
placed on their turns by the same `EvalLedger.Join` the run's end uses; the panel asks after each turn's end and sums
them with the same `LiveTurns.CostsFrom` as at the end, so the live cost and the final one can't disagree.

## Tests

*(each step's, as they are written)*

## Confirmed

*(a free replay on 5070, after the running run has ended)*

## Lesson

*(when it's done)*

---
[Fixes](README.md) · [Plan](../plans/FX34-live-turn-cost.md)
