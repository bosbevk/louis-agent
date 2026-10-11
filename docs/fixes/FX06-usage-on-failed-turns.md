# FX6 · A turn that fails still reports what it used

> **Status:** open · **Area:** louis-agent.api stream, web app, orchestrator · **Found:** F3 steps 5 and 7 (2026-10-11),
> while writing them; not yet seen in a run

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

## Fix (proposed)

Send the turn's usage with the error too, as an extra `usage` field on the `error` event, the same object as on
`message_delta`; `null` when the turn made no request. Claude's own `error` event has no usage, but extra fields are
ignored by Claude clients, as with `requests` and `cost` on `message_delta`. Then:

- the web app reads `usage` from `error` as well as from `message_delta`;
- `LouisAgentClient` does the same, so the comms log's per-reply line, the error's totals and `fixes.jsonl` include it.

The alternative, a `message_delta` with `stop_reason: "error"` before the `error` event, would also work, but it adds a
stop reason Claude doesn't have, and clients that treat `message_delta` as success would read a failed turn as finished.

## Tests

- Orchestrator: `ReadStreamAsync` reads `usage` from an `error` event (`LouisAgentClientTests`).
- API and web app: by hand, by making a turn fail after a model request (e.g. stopping Ollama mid-turn) and checking
  the `error` event and the chat total.

## Confirmed

Not yet.

## Lesson

A total is only as good as its worst path: failure paths must report what they used, or every view built on the
happy path drifts from the ledger, quietly, exactly when something went wrong (skill map #17, observability; #13,
cost).

---
[Fixes](README.md) · Previous: [FX5](FX05-null-conditional-ledger.md)