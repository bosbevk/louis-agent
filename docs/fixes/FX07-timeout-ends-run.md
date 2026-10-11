# FX7 · A timed-out request no longer ends the orchestrator's run

> **Status:** done (2026-10-11) · **Area:** orchestrator, triage loop · **Found:** F3's split demo test (triage on
> Ollama, fixes on Haiku 5.5) (2026-10-11) · **Commit:** `f2d38d2` (built in F3; see its plan)

## Symptom

`run-demo.ps1 -Short -OrchestratorProfile ollama`: the orchestrator triaged the first of five errors, then the run
ended. Errors 2–5 were never triaged, and the first was left as **unknown** with nothing in the comms log but its
heading:

```
[orchestrator 00:33:27] --- 1/5 4c1503b5bd72: order-total 1003 -> System.Collections.Generic.KeyNotFoundException …
[orchestrator 00:35:08] Agent communications written to /demo/agent-comms.md
Finished 2026-10-11 00:35:08 +00:00. 1 unknown.
```

## Cause

The triage request to llama3.1 ran past the HTTP client's 100-second timeout (00:33:27 → 00:35:08). .NET reports an
`HttpClient` timeout as `TaskCanceledException`, which is an `OperationCanceledException`, and the triage loop in
`src/louis-agent.orchestrator/Program.cs` caught every `OperationCanceledException` with `break`, the path meant for
the user stopping the orchestrator (Ctrl+C). So a single slow request ended the whole run as if it had been stopped.

Not specific to Ollama: a slow Claude request, or any other cancelled call that isn't the user's stop, did the same.

## Fix

Only the run's own stop token ends the run: `catch (Exception ex) when (Stopping.ByUser(ex, stop.Token))`, where
`Stopping.ByUser` is true for an `OperationCanceledException` while `stop` is cancelled. Any other cancellation falls
through to the failure path: the error is recorded as **failed** with its reason (and its usage, F3), left unprocessed
so the next poll retries it, and the run goes on to the next error.

The alternative, checking for `TimeoutException` as the inner exception, would catch this case only; the stop token is
the one thing that says the user stopped the run.

## Tests

`Stopping_OnlyTheUsersStopEndsTheRun` (`OrchestratorTests`): a timeout's `TaskCanceledException` doesn't stop the run;
an `OperationCanceledException` once the stop token is cancelled does; another exception doesn't.

## Confirmed

The same split demo again (`run-20261011-004025`, $0: louis-agent was never called). Three triage requests timed out,
each error was recorded as **failed**, and the run went through all five:

```
[orchestrator 00:42:05] Triage of 5f19795d48d0 failed: The request was canceled due to the configured HttpClient.Timeout of 100 seconds elapsing.
[orchestrator 00:42:06] --- 2/5 1360214c3a0a: shipping-cost 1006 -> System.DivideByZeroException: Attempted to divide by zero.
…
Finished 2026-10-11 00:50:07 +00:00. 1 acknowledged, 3 failed, 1 unknown.
```

Error 4's triage completed one request before timing out on the next, and its summary still shows that request's
usage (`Usage: triage | 1 request · 2.1k tokens · $0`).

## Lesson

Cancellation is not one thing: "the user stopped" and "something timed out" share an exception type in .NET, so tell
them apart by the token that was cancelled, never by the type alone (skill map #14, reliability: timeouts in long
tasks; #18, testing and shipping).

---
[Fixes](README.md) · Previous: [FX6](FX06-usage-on-failed-turns.md)