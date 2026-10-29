# FX26 · A replay miss is marked `replay-miss`, not taken for the agent's failure

> **Status:** done · **Area:** core: the model replay client; `louis-agent.session-stream`: the error event; API: the
> failed turn's stream; the eval runner's turn failures · **Found:** 2026-10-29, the free replays of F18's first v2
> baseline (step 5.5) · **Plan:** [FX26](../plans/FX26-replay-miss-kind.md), on `fix/FX26-replay-miss-kind`,
> branched from the F18 branch (the code it touches exists only there)

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
3. **The API streams the kind:** `StreamErrorKinds.Of` (`Sessions/Streaming/`) maps `ModelReplayMissException` to
   `ReplayMiss` and anything else to `Api`; `TurnStream`'s catch takes the kind with the error, and
   `ITurnStreamFormat.Fail` (with `ClaudeStreamFormat`'s) writes `error.type` from the kind's wire name. The API now
   references `louis-agent.session-stream` itself (it reached it only through its web app). Red first: a turn whose
   model request throws the miss streamed `api_error`; the other case (any other failure still `api_error`) passed
   and stays as the guard. The stream golden is unchanged.
4. **The runner reads the kind:** `ScenarioAttempt` sets a turn's failure to `ReplayMiss` when the API's stream ends
   on an error of that kind and no failure of the runner's own is set; any other streamed error stays the agent's
   answer, for the graders. Red first: a stream ending in `replay_miss_error` gave a turn with no failure; the
   `api_error` case passed and stays as the guard.

## Tests

Each written first and seen failing for the expected reason; the other case of each pair passed from the start and
stays as the guard:

- `ModelReplayChatClientTests` (core): a miss, streamed or not, throws `ModelReplayMissException`; red on the plain
  `InvalidOperationException`.
- `ErrorKindTests` (session-stream): `replay_miss_error` reads `ReplayMiss`, `api_error` reads `Api`; red on a stub
  that read null. An unknown type, or an event without one, reads none.
- `TurnFailureKindTests` (api): a turn whose model request throws the miss streams `replay_miss_error`; red on
  `api_error`. Any other failure still streams `api_error`.
- `EvalRunnerFailureTests.AStreamedReplayMissGetsReplayMiss` (evals): a stream ending in `replay_miss_error` writes a
  turn with `failure: replay-miss`; red on no failure. An `api_error` leaves the failure empty.

## Confirmed

Puls rebuilt from the fix branch, then the same free replay of the cook baseline (`176ad599149e`, of
`5d1fd231c9b1`, $0). It still misses at the same 8 turns (the six delegating first turns, and the second turn of
`cook-af` and `cook-en`), and each is now `failure: replay-miss`; the other 12 turns have no failure. The cook mode's
overall slice went from 2 passed, 6 failed (`6edc15cc1d1e`, before) to 2 passed, 0 failed, 6 infrastructure;
`puls-tools`, which never delegates, is unchanged.

Why the replay misses at all is FX27, the next fix: the sub-session's answer carries its usage
in the footer the parent reads, and a replay's usage is zero.

## Lesson

An error that crosses a process boundary as text loses its kind. The replay client knew exactly what had gone wrong,
but the API flattened every failure to `api_error` and the runner only classified its own exceptions, so the label
the contract already had (`TurnFailure.ReplayMiss`) was never reachable. Name a failure where it happens, carry the
name on the wire as a typed value, and read it once at the other end. And a label is not a fix: the turn is now
counted fairly, but it still has no answer until FX27 removes the cause.
