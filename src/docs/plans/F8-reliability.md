nxt# F8 Reliability: a busy moment retried, an account limit said plainly

> **Plan.** How a chat survives the model provider being briefly overloaded, and how an account's spend limit stops
> work at once with a plain reason and the time it lifts, instead of failing turn after turn. **Elsewhere:** the
> feature doc it replaces, [F08-reliability.md](../features/F08-reliability.md).

![F8 at a glance: what the SDK already retries, the gap inside a stream, a retrying client of ours, an account limit as its own failure, and who acts on it](diagrams/F8-reliability-overview.svg)

## Context

F8 is first in the TODO's chain (F8, F7, F41, F42, F140, F171.2), and Louis said "F8 nxt" on 2026-11-27. Its feature
doc dates from 2026-10-19 and predates most of today's engine, so the research came first, at Louis's word ("research
what it should be doing, and report back"), and three choices were his.

What the research found, on 2026-11-27. Anthropic's C# SDK retries a failed request twice by default, on 408, 409,
429 and every 5xx (529 among them), waiting as `retry-after` says or backing off, and following `x-should-retry`
([C# SDK](https://platform.claude.com/docs/en/api/sdks/csharp)); but "the SDK doesn't retry an error in a stream"
([errors](https://platform.claude.com/docs/en/api/errors)). Every turn of ours is streamed, so a free probe on our SDK
version (12.53.0) over a stub settled what that means: a 429 or 529 that comes before the stream starts is retried
(two calls, then the answer), a 400 is not, and an `overloaded_error` that arrives as an event inside the stream,
before any words, fails at once (one call, `AnthropicSseException`). That last one is the gap: Anthropic writes that
a 529 "can occur when the API experiences high traffic across all users".

An account limit comes three ways, none of which a retry helps: a spend limit a person set answers 400
`invalid_request_error`, its message starting "You have reached your specified API usage limits" or "…workspace API
usage limits" and saying when access resumes (we met it ourselves that morning: "You will regain access on
2026-12-01 at 00:00 UTC"); the tier's monthly cap answers 429 with `error_code` `enforced_spend_limit_reached`, no
`retry-after` and the same resume sentence; and a billing problem answers 402 `billing_error`
([rate limits](https://platform.claude.com/docs/en/api/rate-limits)). Today any of them reaches a chat as a plain
`api_error` and an eval run as the provider's raw text.

Microsoft.Extensions.AI documents custom behaviour as a `DelegatingChatClient`, and has a `FailoverChatClient` that
asks again only "before any streaming output was exposed", but it is experimental (`MEAI001`) and in 11.0.0, and we
are on 10.10.0 ([IChatClient](https://learn.microsoft.com/dotnet/ai/ichatclient)).

Louis's picks, each over two others: **retry before any words**, with a small client of ours, over MEAI 11's failover
and over leaving it to the SDK; **the fallback model later, as its own feature** (F180), over building it in F8 and
over dropping it; and **the chat and the evals** act on an account limit, over also stopping the orchestrator POC and
over a resume command it never had. The feature doc's S4 (results as tool calls) moved to F39 long ago and stays there.

## Design

### 1. What a person gets

A chat whose model is briefly overloaded before it says anything is asked again, after a short wait, and answers as
if nothing happened. Once words have gone out it is never asked again, so nothing is said twice. When the account's
limit is reached, the chat says so in plain words, with the time access comes back, and an eval run stops at the
first such answer as could-not-finish, naming the limit, instead of failing every conversation after it.

### 2. A failure, as the provider says it

What a failure means is the provider's to say (ADR 0003). `ILlmProvider.CreateFailures` gives an `IModelFailures`
that names a failure's kind: worth asking again, an account limit (with its reason and, when given, when it lifts), or
neither. The default, `NoModelFailures`, calls nothing worth asking again, so OpenAI and Ollama behave as today.
`AnthropicFailures` reads the SDK's exceptions: an overloaded or server-error event inside a stream is worth asking
again; the three account limits are account limits, the resume time read from Anthropic's own sentence.

### 3. Asking again, before any words

`RetryingChatClient`, a Decorator on `DelegatingChatClient`, sits where the factory wraps the provider's client. It
asks again when a request or a stream fails before it has given any content and the provider calls the failure worth
it, up to `LLM_MAX_RETRIES` (default 2, the SDK's own) with backoff, logging each try. An account limit is never
asked again: it becomes an `AccountLimitException` carrying the reason and the resume time, so every caller can tell
it from any other failure. `LLM_MAX_RETRIES` is also handed to the SDK, so one number governs both.

### 4. Who acts on it

The API streams a new error kind, `account_limit_error`, with the plain reason, and the chat shows it as the turn's
error. An eval run stops at the first one, as it does at a replay miss, and the judge's own model hitting the limit
stops it the same way.

![F8's interface design](diagrams/F8-reliability-interface-design.svg)

![F8's architecture](diagrams/F8-reliability-architecture.svg)

![F8's main cases in order: an overload before any words asked again, an account limit stopping a turn](diagrams/F8-reliability-communication.svg)

![What one request holds while it is retried](diagrams/F8-reliability-objects.svg)

### Namespace map

| Type | Folder | Namespace | Step |
|---|---|---|---|
| `ModelFailureKind`, `ModelFailure`, `IModelFailures`, `NoModelFailures`, `ILlmProvider.CreateFailures` | `src/engine/LouisAgent.Core/Llm/Failures/` | `LouisAgent.Core.Llm.Failures` | 1 |
| `AccountLimitException` | `src/engine/LouisAgent.Core/Llm/Failures/` | `LouisAgent.Core.Llm.Failures` | 1 |
| `AnthropicFailures` | `src/engine/LouisAgent.Core.Anthropic/Failures/` | `LouisAgent.Core.Anthropic.Failures` | 2 |
| `RetryingChatClient`, `LlmOptions.MaxRetries` (`LLM_MAX_RETRIES`) | `Llm/Failures/`; `Config/` | as the folder | 3, 4 |
| `StreamErrorKind.AccountLimit` (`account_limit_error`), `StreamErrorKinds` | `LouisAgent.SessionStream`; the API's `Sessions/Streaming/` | as today | 5 |
| `AccountLimitReachedException` and the runner's stop | `src/evals/LouisAgent.Evals/Runs/` | `LouisAgent.Evals.Runs` | 6 |
| Tests: `AnthropicFailuresTests`, `RetryingChatClientTests`, the stream's and the runner's | mirroring the source | as the source | 1 to 6 |

### What is reused

The provider seam (`ILlmProvider`, ADR 0003) and the factory's one place of wrapping; MEAI's `DelegatingChatClient`;
the SDK's own retries and exception types; the stream's error kinds and the API's `StreamErrorKinds`; the eval runner's
stop on a replay miss. No package, and the MEAI version stays.

## Steps

Each step is one commit with its test, written first and seen failing, and a stop for Louis's review.

1. **A failure's kind, and the seam.** `ModelFailure`, `IModelFailures`, `NoModelFailures` and `AccountLimitException`
   in core; `ILlmProvider.CreateFailures`.
2. **Anthropic's failures.** The overloaded and server-error stream events as worth asking again; the 400, 429 and 402
   account limits as account limits, with the resume time read from the message (tested on the real wording).
3. **`RetryingChatClient`.** Asks again only before any content, only what the provider calls worth it, with backoff;
   an account limit as `AccountLimitException`. Tested over a stub like the probe's.
4. **`LLM_MAX_RETRIES`.** Read once, default 2, handed to the SDK and to the retrying client; in SETUP.
5. **The chat.** `account_limit_error` on the stream, with the reason in words; the chat shows it.
6. **The evals.** A run stops at the first account limit, the agent's or the judge's, as could-not-finish, naming it.
7. **The docs and the close.** Core's, the Anthropic provider's, the API's and the evals' design docs, MODELS and
   SETUP, AGENT_COMMUNICATION's error kinds, the reliability write-up, the work log, the TODO (F180, the fallback
   model), the feature doc folded and deleted, and this plan.

## Verification

The unit tests of each step, every eval replay, and the full suite before the pull request. By hand, free: the API on
`:5082` with a stub provider is not possible, so the stream's error kind is held by its tests; a real account limit,
if one comes again, is checked on the chat page. No paid run: an overload can't be caused on purpose, and the wire
tests use the SDK's own adapter over a stub.

**Done when** an overload inside a stream before any words is asked again and answered, held by a test on the SDK's
own client; an account limit reaches a chat as `account_limit_error` with its resume time, and stops an eval run at
once; OpenAI and Ollama behave as before; and the fallback model is in the TODO as F180.

## Concepts

Steps 1 to 3 are *14 Reliability* in [GOAL.md](../GOAL.md): retry what is transient, never what is final, and never
after the answer has started. Steps 5 and 6 are *17 Observability* and *13 Cost and token economics*: a limit said
plainly, with when it lifts, instead of a wall of identical failures. Step 2 is ADR 0003's pattern again.

## Approved during the build

---
[Docs index](../README.md) · [Plans](README.md) · Feature doc: [F8](../features/F08-reliability.md)
