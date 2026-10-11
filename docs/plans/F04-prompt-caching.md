# Plan: F4 Prompt caching

> **For:** [F4 Prompt caching](../features/F04-prompt-caching.md) · **Approved:** 2026-10-11 (option B for marker 2; louis-agent's chat turns only; caching as a provider strategy)

## Context

M1 made cost visible; M2 makes it smaller. The M1 baseline (Claude Haiku 5.5, full demo) is **$0.6586 a run, $0.0625 a
fix, 10/10 fixes confirmed**, and input is ~97% of it: every request re-sends the same ~48,000-token tools + system
prompt with a 0% cache-hit rate. F4 marks that prefix (and the growing conversation) for Anthropic's prompt cache. It
finishes the measured part of skill 13: the [write-up](../learning/13-cost-and-token-economics.md) says the skill isn't
learned until caching is measured against the baseline with 10/10 fixes held. Stories: F4-S1 … S4; spec: R §B.

F4 doesn't wait for F15: Haiku 5.5's cache minimum is 512 tokens and our prefix is ~48,000.

## Facts checked (Anthropic's prompt-caching page, 2026-10-11)

| Fact | Value | What it means for us |
|---|---|---|
| Render order | tools → system → messages | One marker at the end of the system prompt caches the tools too |
| Breakpoints per request | at most 4 | We use 2; markers must never pile up on stored history |
| Minimum cacheable prefix | 512 tokens on Haiku 5.5 (4,096 on Haiku 4.5) | Easily cleared; below it nothing caches and there is **no error** |
| Price | write 1.25× (5 min) or 2× (1 h); read 0.1× on Haiku 5.5 (0.05× Sonnet/Opus 5.5) | Two requests break even on 5 min; three on 1 h |
| Thinking blocks | can't carry `cache_control`; are cached as part of earlier turns | The automatic marker walks back past them by itself |
| Lookback | a breakpoint looks back at most **20 blocks** for an earlier entry | Fine for us: every request marks its own last block, so the previous entry is a few blocks back |
| Automatic caching | a top-level `cache_control` on the request; the API puts the breakpoint on the last cacheable block and moves it as the conversation grows | What marker 2 uses (design below) |
| Haiku 5.5 thinking | earlier turns' thinking is kept, so a new user message after a tool loop keeps the message cache | The orchestrator's "Continue the fix…" follow-ups should read the whole fix so far |
| Cache key | the exact bytes up to the breakpoint | Tool order, the system prompt and the thinking/effort settings must be byte-identical between requests |
| Reported | `cache_creation_input_tokens`, `cache_read_input_tokens`; `cache_creation` splits writes by TTL | F1's `AnthropicUsageMapper` already reads both totals |

## Design (what changes from F4's doc)

1. **A spike first, as in F1.** The doc assumes the Microsoft.Extensions.AI Anthropic adapter (12.53.0) turns
   `WithCacheControl` content into `cache_control` on the wire. Check it before building on it, by capturing the request
   body through a `StubHandler` (no model call, free): the system marker and a top-level `cache_control` set through
   `RawRepresentationFactory` (the same hook `WithThinkingBudget` already uses).
2. **Marker 2 (the growing conversation) uses automatic caching** (chosen 2026-10-11, option B): an explicit marker on
   the system prompt plus the request's top-level `cache_control`, which the API places on the last cacheable block and
   moves forward as the conversation grows. Anthropic's docs recommend this combination for agent loops. It needs no
   message cloning, so stored history can't be touched, and the API skips thinking blocks itself.
   - *Rejected:* **A**, the doc's explicit marker on a clone of the newest message's last block. Full control, but the
     clone is the risky part (the doc's own Risks table) and it's more code to test.
   - *Fallback:* if the spike (step 1) shows the adapter drops the top-level field, use A and say so in the feature doc.
   - The lifetime is 5 minutes by default: requests in a fix loop start seconds apart and every read resets the timer,
     so the 1-hour lifetime (2× to write) would buy nothing there.
   - Marker 1 stays explicit on the system prompt either way, so the expensive prefix has a guaranteed read point that is
     also shared **across sessions** (every demo fix opens a new session with the same tools and prompt).
3. **Tool order** is sorted, as the doc says. Note: any change to `tools` invalidates the whole cache wherever the tool
   lands, so sorting is about the same order across processes and restarts, not about where new tools go.
4. **Scope: louis-agent's own chat turns only** (decided 2026-10-11). The orchestrator's triage engine and the
   tool-result summariser are not cached in F4: each gets its caching, and its choice of cache lifetime, in its own
   feature ([F17 Orchestrator agent](../features/F17-orchestrator-agent.md), [F16 Summariser](../features/F16-summariser.md)).
   - The orchestrator builds its engine from the same factory and reads the same `config/.env.anthropic`, so the demo's
     orchestrator service sets `LLM_CACHE=off` in `docker-compose.demo.yml`.
   - The summariser calls the engine's own client, so the caching client skips requests made in a `summary`
     `UsageScope`. This matters for cost, not just scope: a summary request sends up to `MaxSummaryInputChars` of tool
     output, which is over 512 tokens, so the automatic marker would write it at 1.25× and nothing would ever read it.
   - Effect on the benchmark: triage was about 5% of the short demo's cost ($0.013 of $0.25), so leaving it uncached
     barely moves the comparison with M1. The report gives louis-agent's cost and the run total separately.
5. **Caching is a provider strategy** (decided 2026-10-11; [Architecture: Provider strategies](../ARCHITECTURE.md#provider-strategies)).
   Instead of an Anthropic-only client added by an `if` in the factory:
   - `IPromptCachingStrategy` (`providers/`): given a request's messages and options, returns what to send.
   - `AnthropicPromptCaching`: the system marker and the top-level `cache_control` (design point 2).
   - `NoPromptCaching`: the default, for Ollama and OpenAI-compatible endpoints (OpenAI caches long prefixes on its
     own and reports the cached tokens, which F1's `StandardUsageMapper` already reads; nothing to send).
   - `PromptCachingChatClient`: one provider-neutral `DelegatingChatClient` that asks its strategy for each request
     (and skips `summary` scopes, design point 4). `LlmClientFactory` picks the strategy once, as it picks the
     `IUsageMapper`.
   *Why:* the provider logic stays in one class per provider, nothing outside the factory names a provider, and a
   provider that later needs markers (or a per-agent policy from F16/F17) adds an implementation, not a branch.
6. **FX4 stays separate.** Its triggers (CreateTool, `/approve`, `/reject`, a skill reload, the API's shared tool list)
   break the cache as well as the thinking signatures, but the demo hits none of them, so F4's measurement is valid
   without it. Recommendation: build FX4 right after F4, before F5 (F5 also gives sessions their own tool lists).

## Steps (each one commit or PR with its test; stop for review after each)

Branch `feature/f4-prompt-caching` off `main`.

0. **The plan in the repo:** this file as `docs/plans/F04-prompt-caching.md`, and the plans table. *Check:* links resolve.
1. **Spike: what the adapter sends.** A test with a capturing `StubHandler`: the system marker and the top-level
   `cache_control` (set through `RawRepresentationFactory`) appear in the request JSON, with the TTL. Kept as a
   regression test. *Concept:* verify the adapter before designing on it (the F1 lesson). *Skill:* #1 models and
   providers, #13.
2. **A byte-stable prefix.** Sort `_tools` by name after registration; agent-built tools inserted in sorted order.
   *Tests:* two engines built from the same options give the same tool names in the same order, and serialise to the
   same tool JSON and system prompt. *Concept:* the cache key is the exact bytes; silent invalidators. *Skill:* #3, #13.
3. **Settings:** `LLM_CACHE` (on/off) and `LLM_CACHE_TTL` (`5m` default, `1h`) in `LlmOptions`, with `.env.example`.
   *Tests:* parsing, defaults, a bad value is rejected with a clear message. *Skill:* #13.
4. **The caching strategy and its client.** `IPromptCachingStrategy`, `AnthropicPromptCaching`, `NoPromptCaching`
   and `PromptCachingChatClient` (design point 5). *Tests:* with Anthropic's strategy (recording fake inner client), the
   system marker and the top-level field carry the same TTL (a 1-hour entry may not follow a 5-minute one); no message
   after the system prompt gets a marker; the caller's message list and messages are unchanged afterwards; a request
   with no system message still works; a request in a `summary` scope gets no marker and no top-level field. With
   `NoPromptCaching`, the request reaches the inner client unchanged. *Concept:* breakpoints, render order, the
   4-marker limit, automatic vs explicit placement; the strategy pattern. *Skill:* #13, #7 context engineering.
5. **Factory wiring.** `LlmClientFactory` picks the strategy from `LLM_PROVIDER` (`AnthropicPromptCaching` only for
   Anthropic with `LLM_CACHE=on`, else `NoPromptCaching`), as it picks the usage mapper. *Tests:* each provider gets
   the right strategy; caching off gives `NoPromptCaching` on Anthropic too. The demo's orchestrator service gets
   `LLM_CACHE=off` (design point 4). *Concept:* provider-neutral core, one place that picks the strategy. *Skill:* #1.
6. **Price writes at their lifetime (F2).** The record carries the TTL the request asked for; `CostCalculator` uses
   `cache_write_1h` for 1-hour writes (today it always uses 5 min). *Tests:* the cost of a 1-hour write; a 5-minute one
   unchanged; old records without a TTL priced as before. *Concept:* write premium and break-even. *Skill:* #13.
7. **Smoke check (needs your go-ahead, about one cent).** One CLI message on Haiku 5.5 that uses a few tool rounds: the
   second request's `cache_read` is at least the prefix (F4-S1), and each later round reads the round before (F4-S2).
   If `cache_read` is 0, diff two consecutive request bodies before anything else.
8. **Benchmark (needs your go-ahead; the M1 run cost $0.66, this one should cost less).** The full demo with caching on.
   Compare with M1, for louis-agent and for the whole run: run cost, input cost, cost per fix, cache-hit rate, writes
   vs reads per round and on the "Continue the fix…" messages, and **10/10 fixes confirmed**. Targets: input cost ≥ 60% lower (F4's *Done when*) and about
   −80% on the prefix per fix (the write-up's target). Record the numbers in the features README.
9. **Docs and finish.** MODELS.md (caching, TTL), SETUP settings, the spec's breakpoint table and implementation map,
   F4 status and the TODO tick, GOAL.md's map; extend the skill-13 write-up with the before/after, which is what makes
   skill 13 learned.

## Verification

- `dotnet test louis-agent-solution.sln` after every step (on your machine; the cloud containers can't run .NET).
- Free: the spike's captured request bodies (step 1).
- Paid, only with your go-ahead: the smoke check (step 7, about a cent) and the benchmark (step 8).

## Concepts

The cache key is the exact prefix bytes, so caching is mostly about keeping the prefix stable; breakpoints and the
render order (tools → system → messages); paying 1.25× once to read at 0.1× after; measuring the hit rate instead of
assuming it. Skill map #13 (cost and token economics), #3 (prompt design: a cache-friendly prefix), #7 (context
engineering).

---
[Plans](README.md) · Previous: [F14](F14-display-currency.md)
