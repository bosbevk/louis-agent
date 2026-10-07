# F4 · Prompt caching (Anthropic)

> **Status:** planned · **Milestone:** M2 · **Depends on:** F1 (to prove it works) ·
> **Spec:** [Optimisation §B](../specs/RESPONSE_OPTIMISATION.md) · **TODO:** *Wire up Anthropic prompt caching on the
> system prompt*

Every request re-sends ~26,000 tokens of tool definitions and system prompt that never change. With cache markers the API
serves them at ~0.1× the input price after the first request. The biggest single saving in the plan.

## User stories

**F4-S1 — Cache the fixed prefix.** As an *operator*, I want the tool definitions and system prompt cached, so that I
don't pay full price for them on every tool round.
- *Given* `LLM_CACHE=on` and an Anthropic model *when* the second request of a session runs *then* its
  `cache_read` tokens (F1) are at least the prefix size.
- *Given* the benchmark *then* its input cost falls by ≥ 60% against the M1 baseline, with 10/10 fixes confirmed.

**F4-S2 — Cache the conversation as it grows.** As an *operator*, I want each request to reuse the conversation so far,
so that long tool loops get cheaper per round.
- *Given* round *n* of a turn *then* round *n + 1* reads everything up to round *n* from cache.

**F4-S3 — Never break other providers or the API.** As a *developer*, I want cache markers added only for Anthropic and
never left on stored history, so that other providers are unaffected and requests never exceed the marker limit.
- *Given* Ollama or an OpenAI-compatible provider *then* no request carries cache markers.
- *Given* any request *then* it carries at most 4 markers, and the session's stored history carries none.

**F4-S4 — Choose the lifetime.** As an *operator*, I want to choose a 5-minute or 1-hour cache lifetime, so that I can
match how people actually pause.
- *Given* `LLM_CACHE_TTL=1h` *then* markers use the 1-hour lifetime and F2 prices writes at the 1-hour rate.

## Design

**Two breakpoints, not three.** The prefix order is *tools → system → messages*, so a marker at the end of the system
prompt already caches the tool definitions with it. A separate tool marker only helps when the system prompt changes
but the tools don't (a skills reload) — rare enough to skip; revisit if F1 shows frequent rebuilds. (Update the spec's
table when this lands.)

| # | Marker on | Caches |
|---|---|---|
| 1 | The system message's last text block | Tools + system prompt |
| 2 | The last cacheable block of the newest message (not a thinking block — those can't carry markers) | The whole conversation so far |

New `AnthropicCachingChatClient` (`louis-agent.core/providers/`), a `DelegatingChatClient` that `LlmClientFactory` adds
only for `LLM_PROVIDER=anthropic` and `LLM_CACHE=on`:

1. Copies the message list for this request (a new list, new `ChatMessage` objects for the two messages it changes).
2. Replaces the target content with a **clone** carrying the marker:
   `new TextContent(text).WithCacheControl(new CacheControlEphemeral { Ttl = … })`; for a `FunctionResultContent`,
   a new instance with the same call id and result. Unsupported block types → walk back to the previous block.
3. Sends the copy. The engine's stored history is never touched, so markers can't accumulate past the limit or leak
   into later requests.

Prefix stability (each of these silently disables caching):

- **Deterministic tool order:** `AgentEngine` sorts `_tools` by name after registration (reflection order isn't
  guaranteed); agent-built tools are inserted in sorted position.
- **No volatile text** in the system prompt or tool descriptions (no dates, ids, counters).
- **Thinking and effort fixed per session** (they already are: `AgentEngine.Thinking` is init-only).

Model notes for the default (Claude Haiku 4.5): minimum cacheable prefix **4,096 tokens** — ours is ~26,000, but F5's
smaller toolsets must stay above it. With thinking on, a *new user message* after a tool loop drops cached thinking and
the message cache from there (marker 1 is unaffected) — measure it in step 6.

## Implementation steps

1. **Deterministic tool order.** *Test:* two engines built from the same options list tools in identical order; order
   is alphabetical.
2. **`AnthropicCachingChatClient` marker placement.** *Test (with a recording fake inner client):* the request has a
   marker on the system text and on the last message's last text/tool-result block; thinking blocks are skipped; at most
   2 markers; the caller's list and messages are unchanged afterwards.
3. **TTL setting.** `LLM_CACHE`, `LLM_CACHE_TTL` in `LlmOptions`. *Test:* option parsing; marker carries the TTL.
4. **Factory wiring.** *Test:* `LlmClientFactory` wraps only the Anthropic client when caching is on; Ollama and
   OpenAI-compatible clients are unchanged.
5. **F2 alignment.** Records carry the cache lifetime so writes are priced at the right rate. *Test:* cost of a 1-hour
   write.
6. **Measure.** Benchmark run with caching on; compare against M1; inspect `cache_read`/`cache_write` per round and per
   new user message (the Haiku thinking note). Write the numbers into the features README.
7. **Docs:** MODELS.md (caching, TTL), SETUP settings, spec status and the breakpoint-table update.

## Done when

- Stories' criteria pass; tests added; suites pass.
- Benchmark: input cost ≥ 60% lower than M1, 10/10 fixes confirmed; numbers recorded.

## Risks

| Risk | Handling |
|---|---|
| Cloning loses a property the adapter needs (e.g. `AdditionalProperties`) | Copy them; test round-trips each content type used in history |
| Cache never hits (prefix differs between requests) | Step 6 shows `cache_read = 0`; diff two consecutive request prefixes from a debug log |

---
[Features](README.md) · Previous: [F3 Show usage](F03-usage-display.md) · Next: [F5 Toolset profiles](F05-toolset-profiles.md)
