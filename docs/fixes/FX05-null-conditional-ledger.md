# FX5 · Requests outside a usage scope are written to the ledger again

> **Status:** done (2026-10-11) · **Area:** core, `UsageRecordingChatClient` · **Commit:** `3269fea` (F3 step 1; never
> committed broken)

## Symptom

While building F3 step 1, ten existing F1 and F2 tests failed at once, e.g. `GetResponse_NoScope_RecordsRound1AsTurn`
and `AgentHostBuild_LedgerLinesCarryTheirCost`, all with `InvalidOperationException: Sequence contains no elements`: the
sink had no records. Every one of them makes a request with no `UsageScope` open.

## Cause

Step 1 made the recorder add each stored (priced) record to the turn's scope, written as one line:

```csharp
scope?.Add(sink.Record(record));
```

In C#, `a?.M(x)` doesn't evaluate `x` when `a` is null: the whole call, arguments included, is skipped. With no scope
open, `sink.Record` never ran, so the request was never written to the ledger. Any request made outside a scope would
have been lost silently: the usage probe, a host that forgot its scope, or a future caller of the engine.

## Fix

Two statements, so the write doesn't depend on the scope, with a comment saying why:

```csharp
UsageRecord stored = sink.Record(record);
scope?.Add(stored);
```

## Tests

No new test: F1's tests already record without a scope (`GetResponse_NoScope_RecordsRound1AsTurn` and others), which is
how the mistake was caught before it was committed.

## Confirmed

The whole solution's tests pass (core 579, orchestrator 26, order-service 14).

## Lesson

`?.` short-circuits the rest of the expression, arguments included, so a side effect inside the argument (writing,
counting, logging) disappears when the target is null. Keep side effects out of a null-conditional call. And
recording must never depend on optional context: the ledger is the source of truth, and the scope is only a view of it.
The tests that caught it were written for F1, which is why every step keeps its tests (skill map #17, observability;
#18, testing and shipping).

---
[Fixes](README.md) · Previous: [FX4](FX04-append-only-history.md)
