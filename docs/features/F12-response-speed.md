# F12 · Response speed: time to first token and tokens per second, for every provider

> **Status:** planned · **Milestone:** M5 · **Depends on:** F1 (F3 and F11 to show it)
>
> **Spec:** [Usage §4.1](../specs/USAGE_AND_BUDGETS.md) (the record) · [Optimisation §A](../specs/RESPONSE_OPTIMISATION.md) (what the adapters report)
>
> **In the TODO:** tick **F12 Response speed** under *Now* ([TODO](../../TODO.md))

F1 records how long each request took (`duration_ms`), but not how that time splits: waiting before the first token,
then writing the answer. That split is what makes a model feel fast or slow, and it is the only way to compare a local
model (free, but how slow?) with Claude, or to see whether Ollama's prompt cache helps, since Ollama reports no cache
counts (`cache_read` is null, meaning "not reported", not "not cached").

## User stories

**F12-S1 — Speed on every record.** As an *operator*, I want each usage record to say how long the model took to start
answering and how fast it wrote, so that I can compare models and providers.
- *Given* a streamed request on any provider *then* its record has `ttft_ms` (request start to the first update that
  carries text, thinking or a tool call) next to `duration_ms`.
- *Given* a non-streaming request *then* `ttft_ms` is `null`: no first token can be observed.
- *Given* a record with output tokens and a `ttft_ms` *then* its output speed is
  `output / (duration_ms − ttft_ms) × 1000` tokens per second, computed where it is shown, never stored.

**F12-S2 — The provider's own timings, where it gives them.** As a *developer*, I want provider-reported timings kept
when the adapter exposes them, so that a local model's load time and prompt processing are visible.
- *Given* Ollama reports model load, prompt evaluation and generation durations *then* the record keeps them under
  `timing.provider` (`load_ms`, `prompt_ms`, `generate_ms`).
- *Given* a provider that reports none (Anthropic today) *then* `timing.provider` is `null`.

**F12-S3 — See it.** As a *user*, I want the speed shown with the usage, so that I notice when a model is slow.
- *Given* an answer in the web app, CLI or Rider *then* the usage line from F3 adds `1.2 s to first token · 48 tok/s`.
- *Given* the Usage tab (F11) *then* it shows median time to first token and tokens per second per model and provider.

## Design

- **Measured, not reported, for all providers.** `UsageRecordingChatClient` already times every request; in the
  streaming path it also notes the time of the first update with content. Nothing provider-specific, so it works for
  Anthropic, Ollama and OpenAI-compatible servers alike.
- **Provider timings through the mapper.** `IUsageMapper` grows a second job: read timings from the response
  (`AdditionalProperties` or `RawRepresentation`). `StandardUsageMapper` returns null; an `OllamaUsageMapper` reads
  Ollama's `load_duration`, `prompt_eval_duration` and `eval_duration` (nanoseconds). Whether OllamaSharp exposes them,
  and where, is step 1.
- **The record:** `UsageRecord` gets `Timing(long? TtftMs, ProviderTiming? Provider)`; `duration_ms` stays where it is.
- **Benchmark:** add time to first token and tokens per second to the benchmark's demo-run numbers, per model.

## Implementation steps

1. **Spike — what each adapter reports.** Extend `tools/louis-agent.usage-probe` to print time to first token and each
   response's `AdditionalProperties` and `RawRepresentation` type, for Anthropic and Ollama. Record the findings in the
   optimisation spec next to *What the Ollama adapter reports*. *No production code.*
2. **`ttft_ms` in the recorder.** *Test:* a fake stream with a delay before its first text update records `ttft_ms` ≥
   the delay; a non-streaming request records `null`; a stream with no content (cancelled before the first token)
   records `null`.
3. **Provider timings.** `IUsageMapper` timing hook, `OllamaUsageMapper`, picked in `LlmClientFactory.CreateUsageMapper`.
   *Test:* a response carrying Ollama's durations maps to milliseconds; the standard mapper gives `null`.
4. **Show it** in F3's usage line and F11's Usage tab (or with them, if they're built after this). *Test:* the tokens
   per second calculation, including `duration_ms == ttft_ms` (no division by zero).
5. **Docs:** the record in the usage spec, the probe doc, MODELS.md (how to compare models).

## Done when

- Every streamed record has `ttft_ms`, and Ollama records carry its own timings.
- The usage line shows time to first token and tokens per second.
- One comparison of Claude Haiku 4.5 and `llama3.1` on the same prompt is written down in MODELS.md.
- **Then finish up** (see [Finishing a feature](README.md#finishing-a-feature)).

## Not in this feature

Tracing spans per turn and tool call (OpenTelemetry), latency dashboards and alerts: skill-map #17's later stage.

---
[Features](README.md) · Previous: [F11 Usage tab and reconciliation](F11-usage-tab-and-reconciliation.md) · Next: [F13 Local models](F13-local-models.md)
