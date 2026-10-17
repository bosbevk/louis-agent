# FX9 · A request cancelled without streaming is still written to the ledger

> **Status:** done · **Area:** core, the usage recorder (`UsageRecordingChatClient`) · **Found:** 2026-10-12 ·
> **Fixed:** 2026-10-14

## Symptom

In the short demo with triage on `llama3.1` in Ollama ([Local triage](https://github.com/bosbevk/louis-agent/blob/pre-cl10/docs/BENCHMARKS.md#local-triage-2026-10-12)),
the orchestrator made five model requests and the ledger has four. The missing one is the first triage, cancelled by
the HttpClient's 100-second timeout (`[ERROR] Triage of bca5719ad2e0 failed: The request was canceled due to the
configured HttpClient.Timeout of 100 seconds elapsing.`), after Ollama had read 4,762 tokens of prompt and written 288.
Nothing in the ledger, the run's totals or the comms log shows that request happened.

## Cause

From the code, not yet from a test: `UsageRecordingChatClient` records the two kinds of request differently.

- **Streaming** (`GetStreamingResponseAsync`, louis-agent's turns): the record is written in a `finally`, so a
  cancelled stream is recorded with `stop: "cancelled"` and whatever usage arrived; only a provider error is left out.
- **Not streaming** (`GetResponseAsync`, the orchestrator's triage through `ProcessPromptAsync`): the record is written
  after `await base.GetResponseAsync(…)` returns. A timeout throws `TaskCanceledException` first, so nothing is written.

The orchestrator's triage is the one caller that doesn't stream, so its cancelled requests are the ones lost.

## Fix

`GetResponseAsync` now records a cancelled request as the streaming path does: the call to the inner client is in a
`try`, and a `catch (OperationCanceledException)` (which `TaskCanceledException`, the HttpClient timeout's exception,
derives from) stores a record with `stop: "cancelled"`, a `null` usage and the call's duration, then rethrows. Any
other exception passes through unrecorded, as a provider error does when streaming. One place, the same `Store` and
`CreateRecord` as a finished request, and the existing `Cancelled` constant, so the two paths can't drift apart.

Its tokens stay unknown, as a `null` usage, never 0: the ledger records what it was told. Whether Anthropic charges a
request the client gave up on is not settled here; the record makes such requests visible so F11's reconciliation
against the provider's report can find out.

## Tests

In `tests/louis-agent.core.tests/Llm/UsageRecordingChatClientTests.cs`, written first and seen failing with
`Sequence contains no elements` (the sink was empty after the cancelled call):

- `GetResponse_Cancelled_RecordsCancelledWithUnknownTokensAndRethrows`: a non-streaming call that throws
  `TaskCanceledException` inside a scope writes one record with `stop: "cancelled"`, all-null tokens, the scope's host
  and session and the default model, and the exception still reaches the caller.
- `GetResponse_ProviderError_NotRecorded`: a non-streaming call that throws `HttpRequestException` writes nothing
  (unchanged; pinned so the catch stays narrow).

## Confirmed

By the tests above; the real run is still owed. The next local triage run that times out (llama3.1 at the 32k
context, [Local triage](https://github.com/bosbevk/louis-agent/blob/pre-cl10/docs/BENCHMARKS.md#local-triage-2026-10-12)) should show the ledger's record count equal to
Ollama's `POST /api/chat` lines, with the timed-out request as a `cancelled` row with no tokens.

## Lesson

A ledger that only records successes undercounts exactly the requests that went wrong, which are the ones worth
seeing. Skill 17 (observability) in the [goal](../GOAL.md).