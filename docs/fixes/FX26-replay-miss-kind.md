# FX26 · A replay miss is marked `replay-miss`, not taken for the agent's failure

> **Status:** open · **Area:** core: the model replay client; `louis-agent.session-stream`: the error event; API: the
> failed turn's stream; the eval runner's turn failures · **Found:** 2026-10-29, the free replays of F18's first v2
> baseline (step 5.5) · **Plan:** [FX26](../plans/FX26-replay-miss-kind.md), on the F18 branch (the code it touches
> exists only there)

## Symptom

The cook run's free replay (`6edc15cc1d1e`, of `5d1fd231c9b1`) showed, on a delegating turn:

```
InvalidOperationException: No recorded model answer for this request in trial '5d1fd231c9b1:puls-cook:1/cook-af':
message 3 (tool) changed. The model was not called.
```

The turn's stored row has that `error`, but its `failure` is empty. So the conversation's graders fail it as the
agent's answer, though nothing the agent did went wrong: the replay had no recording to give.

## Cause

The miss is thrown inside the API, by the engine's model replay client (`ModelReplayChatClient`), as a plain
`InvalidOperationException`. The API streams every failed turn the same way, `error.type` `api_error` and the
exception's text (`ClaudeStreamFormat.Fail`, from `TurnStream`'s catch). The runner's failure kinds
(`TurnFailures.Of`) only look at the runner's own exceptions (a timeout, the transport, an API refusal), so a streamed
error never becomes a `TurnFailure`, and `TurnFailure.ReplayMiss`, which the contract has, was never set.

## Fix

Step by step, in the plan's order:

1. **The engine names a replay miss:** `ModelReplayMissException` (core, `Llm/Replay/`), an
   `InvalidOperationException` underneath, is what `ModelReplayChatClient` throws on a miss, its message unchanged.
   Red first: the replay client's two miss tests (streamed and not, 4 cases) expected the named exception and got the
   plain one.
2. **The stream's error kinds:** `StreamErrorKind` in `louis-agent.session-stream` (`api_error`,
   `replay_miss_error`), which now references `louis-agent.choices`; `ErrorReported.Kind` reads `error.type` once with
   `SettingChoice`, through guard clauses, and is none for a type it doesn't know or an event without one. The enum
   joins the repeated-name scan's list (ADR 0043). Red first: the two known kinds read null against a stub; the
   unknown-or-none case passed against the stub too and stays as the guard.

## Tests

(To come.)

## Confirmed

(To come: a free replay of a delegating run after the fix.)

## Lesson

(To come.)
