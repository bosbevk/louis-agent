# FX9 · A request cancelled without streaming is still written to the ledger

> **Status:** open · **Area:** core, the usage recorder (`UsageRecordingChatClient`) · **Found:** 2026-10-12

## Symptom

In the short demo with triage on `llama3.1` in Ollama ([Local triage](../BENCHMARKS.md#local-triage-2026-10-12)),
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

## Fix (proposed)

Record a cancelled non-streaming request as the streaming path does: catch `OperationCanceledException` around the
call, write a record with `stop: "cancelled"`, no tokens (a cancelled call returns no usage) and its duration, then
rethrow. Provider errors stay unrecorded, as they are when streaming.

Its tokens stay unknown, as a `null` usage, never 0: the ledger records what it was told. Whether Anthropic charges a
request the client gave up on is not settled here; the record makes such requests visible so F11's reconciliation
against the provider's report can find out.

## Tests

- A non-streaming call that throws `OperationCanceledException` writes one record with `stop: "cancelled"` and is still
  thrown to the caller.
- A non-streaming call that throws any other exception writes nothing (unchanged).

## Confirmed

Not yet: rerun a triage that times out (llama3.1 at the 32k context, or a short `OllamaTimeout` in a test build) and
count the ledger's records against Ollama's `POST /api/chat` lines.

## Lesson

A ledger that only records successes undercounts exactly the requests that went wrong, which are the ones worth
seeing. Skill 17 (observability) in the [goal](../GOAL.md).