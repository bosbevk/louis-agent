# FX6 · A turn that fails still reports what it used

> **Status:** done (2026-10-11) · **Area:** louis-agent.api stream, web app, orchestrator · **Found:** F3 steps 5 and 7
> (2026-10-11), while writing them; never seen in a run · **Commit:** `75de390` (built in F3; see its plan)

## Symptom

Expected, not yet observed: when a louis-agent turn fails part-way (a provider error after some tool rounds, e.g. a 529
on round 4), the web app's chat total and the orchestrator's comms log leave that turn's usage out, while the usage
ledger and the API's own session totals (`GET /sessions/{id}`, `/usage`) count it. A demo run with a failed fix reply
would then show a comms log run total below the ledger's total for the run, which is exactly the check F3's *Done
when* makes.

## Cause

The usage of a turn travels on `message_delta` (F3 step 4). A turn that fails ends with an `error` event instead
(`StreamTurnAsync` in `src/louis-agent.api/Program.cs`), which carries no usage, so:

- the web app (`ChatPage.Apply`) never sets `Turn.Usage` for it, and the chat total is summed from the turns;
- the orchestrator (`LouisAgentClient.ReadStreamAsync`) leaves `AgentReply.Usage` null, so the comms log and
  `fixes.jsonl` don't add it.

The requests that ran before the failure were charged and are in the ledger: the recorder writes every request that
returned (F1), and the API adds the turn to the session in its `finally`.

## Fix

The turn's usage goes with the error too, as an extra `usage` field on the `error` event, the same object as on
`message_delta`, left out when no request of the turn returned (the first one failed: nothing to report). Claude's own
`error` event has no usage, but extra fields are ignored by Claude clients, as with `requests` and `cost` on
`message_delta`. Then:

- the web app reads `usage` from `error` as well as from `message_delta`;
- `LouisAgentClient` does the same, so the comms log's per-reply line, the error's totals and `fixes.jsonl` include it.

The alternative, a `message_delta` with `stop_reason: "error"` before the `error` event, would also work, but it adds a
stop reason Claude doesn't have, and clients that treat `message_delta` as success would read a failed turn as finished.

## Tests

- Orchestrator: `ReadStream_FailedReply_KeepsTheErrorAndItsUsage` (`LouisAgentClientTests`) reads the error and its
  usage from an `error` event.
- API: by hand on Ollama. A model that doesn't exist fails before any request returns, and its `error` event has no
  `usage`; a working model's turn still ends with `message_delta` and its usage.
- Not checked by hand: a turn failing *after* a request returned (it needs Ollama stopped mid-turn, and qwen makes one
  request). The API builds that event from the same `usage` object as `message_delta`, and both clients' reading of it
  is the code above.

## Confirmed

The whole solution's tests pass (core 623, orchestrator 32, order-service 14), and the two API cases above. F3's
step 8 demo is the run that would show it, if a fix reply fails.

## Lesson

A total is only as good as its worst path: failure paths must report what they used, or every view built on the
happy path drifts from the ledger, quietly, exactly when something went wrong (skill map #17, observability; #13,
cost).

---
[Fixes](README.md) · Previous: [FX5](FX05-null-conditional-ledger.md)