# Spec: Cheaper, Faster Responses — Caching, Context and Usage

**Status:** in progress (§A: F1, F2 done) · **Date:** 2026-10-07 · **Related:** [Usage, estimates and budgets](USAGE_AND_BUDGETS.md)

## Implementation

Built as features in [docs/features/](../features/README.md); each is one line under *Now* in the
[TODO](../../TODO.md). When a feature is done, mark it here too.

| Section | Feature | Status |
|---|---|---|
| §A Measure usage | [F1 Usage ledger](../features/F01-usage-ledger.md), [F2 Prices and cost](../features/F02-prices-and-cost.md), [F3 Show usage](../features/F03-usage-display.md) | F1, F2 **done** (2026-10-10; a monthly `usage-YYYY-MM.jsonl`, not `usage.jsonl`; prices in a tracked `config/prices.json`, not `LLM_PRICE_*` settings); F3 planned |
| §B Prompt caching | [F4 Prompt caching](../features/F04-prompt-caching.md) | **done** (2026-10-11; two markers, the second automatic, instead of three; input cost −87% on the demo) |
| §C Toolset profiles | [F5 Toolset profiles](../features/F05-toolset-profiles.md) | **done** (2026-10-14; short benchmark: prefix ~48.5k → ~24.4k tokens, run $0.0488 → $0.0378; the full run waived). Went further than §C: a session names its toolsets when it opens, an engine per list, and sub-sessions on a peer (Puls's delegation, measured in [the delegation check](../../samples/sample-output-puls-check-repeat/README.md)) |
| §D Bounded history | [F9 Bounded history](../features/F09-context-management.md) | planned |
| §E Settings per route | [F10 Route settings](../features/F10-route-settings.md) | planned |
| §F Reliability | [F8 Reliability](../features/F08-reliability.md) | planned |
| §B–D Model limits (cache minimum, window, price step per model) | [F15 Model limits](../features/F15-model-limits.md) | planned |
| §G Later | not planned yet | — |

## 1. Goal

Cut what each chat turn and each agent task costs, and how long it takes, without making answers worse:

1. **Measure** every request's tokens, so cost is visible per turn, per session and per orchestrator fix.
2. **Stop paying full price for the same tokens**: cache the parts of the prompt that never change.
3. **Send less**: only the tools a session needs, and a history that doesn't grow without limit.
4. **Keep it provider-neutral**: everything above `IChatClient` works for Anthropic, Ollama and OpenAI-compatible
   endpoints alike; Anthropic-only features (prompt caching) live in `LlmClientFactory` and are skipped elsewhere.

Out of scope: changing the default model, and anything that trades quality for cost without a measured comparison.

## 2. Where the tokens go today

Every tool round is one request to the model, and every request re-sends everything: the tool definitions, the
system prompt, and the whole conversation so far.

**Measured on the current code** (`AgentEngine` built with the default skills, 2026-10-07):

| Part of every request | Size | ≈ Tokens* |
|---|---:|---:|
| Tool definitions, all 108 tools (Paymo and DevOps keys set, as in the demo) | 68,162 chars | ~17,000 |
| Tool definitions, 73 tools (no Paymo/DevOps) | 47,792 chars | ~12,000 |
| System prompt (personality, default, dotnet, python, powershell, bash, web, self-extension, code-review) | 35,497 chars | ~9,000 |
| **Fixed prefix, demo configuration** | **~104,000 chars** | **~26,000** |
| Conversation so far (files read, tool results, answers) | grows each round | 0 → 100,000+ |

\* Characters ÷ 4 — an estimate. **Measured since** (M1 baseline, 2026-10-10, Claude Haiku 5.5, whose tokenizer counts
more tokens for the same text): the fixed prefix is **~48,000 tokens**, and the largest request 57,710; see the
features' [Benchmark](../features/README.md#the-benchmark).

**Consequences:**

- A demo fix takes 18–29 tool calls (`samples/sample-output/orchestrator-state/fixes.jsonl`), so ~20 requests that
  each re-send the ~26,000-token prefix: **~500,000 input tokens per fix on the prefix alone**, before any history.
  (Measured in M1: 8–16 requests per fix, several tool calls each, and **608,000 input tokens per fix** on average.)
- Nothing is cached: there are no `cache_control` markers anywhere.
- Nothing is shown: since F1 and F2 every request is recorded and priced in the usage ledger, but no host displays it
  yet (F3).
- History only grows. The one guard is that a single tool result over 50,000 characters is summarised
  (`ToolCallGuards.SummariseToolResultAsync`); everything else stays verbatim until the session ends.
- Every session gets all tools, including Paymo and DevOps in a code-fixing container that will never use them.

**What already works and stays:** streaming everywhere; oversized-result summarisation; cut-off tool calls are never
run and the model is asked to continue; a 16,000-token output cap; thinking effort per `LLM_THINKING`.

## 3. Principles

- **Measure before and after every change**, on the same workload: the orchestrator demo (13 errors, 10 fixes) is the
  benchmark, plus one scripted chat session.
- **Free wins before trade-offs**: caching and sending less cost nothing in quality; summarising and clearing history
  can lose detail, so they come later and are tuned against the benchmark.
- **A stable prefix**: tools first, then the system prompt, then messages — byte-identical from request to request.
  Anything that varies goes after the last cache breakpoint.
- **Provider-neutral core**: `AgentEngine` decides *what* to send; `LlmClientFactory` decides *how* a provider wants
  it (cache markers for Anthropic, nothing for Ollama).

## 4. Workstreams

Ordered by value for effort. Each has acceptance criteria measured on the benchmark.

### A. Measure usage (prerequisite for everything else)

The full design — the usage ledger, prices, estimates, budgets and how they are shown — is its own spec:
[USAGE_AND_BUDGETS.md](USAGE_AND_BUDGETS.md). This section is the minimum the optimisation work needs.

**What:** record `ChatResponse.Usage` (`UsageDetails`) for every model request: `InputTokenCount`,
`OutputTokenCount`, `CachedInputTokenCount`, `ReasoningTokenCount`, and `AdditionalCounts["CacheCreationInputTokens"]`
for cache *writes* (see *What the Anthropic adapter reports* below).

- A recording middleware inside the tool loop reads usage from each request (from the last update when streaming), and
  writes one JSON line per request to `{LOG_DIRECTORY}/usage-YYYY-MM.jsonl`: host, session, turn, round, model, the
  four token kinds, reasoning, duration. *(Built in F1.)*
- The HTTP API adds a `usage` object to `message_delta` (Claude's own event shape already has it), so the web app can
  show tokens per reply and per chat; the CLI prints a one-line summary after each answer.
- The orchestrator adds tokens and an estimated cost per fix to `agent-comms.md` and `fixes.jsonl`, and a total to the
  run summary.
- Prices live in configuration, not in code: the tracked price table `config/prices.json` (or `PRICES_FILE` /
  `PRICES_URL`); without it, show tokens only. *(Built in F2; see the usage spec §4.2.)*

**Accept when:** a demo run produces the usage ledger *(met, F1)*, and the comms log's run summary shows tokens per fix
*(F3)*.

**What the Anthropic adapter reports** (measured 2026-10-10, F1 step 1 spike: Anthropic 12.53.0,
Microsoft.Extensions.AI 10.10.0, `claude-haiku-4-5-20251001`, thinking medium, a 10,266-token prompt):

| Request | `InputTokenCount` | `CachedInputTokenCount` | `AdditionalCounts` | `ReasoningTokenCount` |
|---|---|---|---|---|
| No cache marker | 10,266 | 0 | null | null |
| Cache write (first request with the marker) | 10,266 | 0 | `CacheCreationInputTokens` = 10,227 | null |
| Cache read (same prefix again) | 10,266 | 10,227 | null | null |

- **`InputTokenCount` is the total prompt**, cache reads and writes included. (Anthropic's own `input_tokens`
  excludes them; the adapter adds them back.) Uncached input = `InputTokenCount − CachedInputTokenCount −
  CacheCreationInputTokens`; pricing `InputTokenCount` at the input rate and the cache counts as well would charge
  the cached part twice.
- **Cache writes:** `AdditionalCounts["CacheCreationInputTokens"]`, present only on a request that wrote the cache.
- **Reasoning:** always null; thinking tokens are only inside `OutputTokenCount`.
- **Streaming:** one `UsageContent` in the **last** update (the one with the finish reason), with the same values as
  non-streaming; `ToChatResponse()` carries it into `ChatResponse.Usage`. Non-streaming responses contain no
  `UsageContent`; read `ChatResponse.Usage`.
- `TextContent.WithCacheControl(new CacheControlEphemeral())` on the system message reaches the API: the second
  request read 10,227 cached tokens.

**Claude Haiku 5.5** (measured 2026-10-10 with `tools/louis-agent.usage-probe`, `claude-haiku-5-5`, the same prompt and
adapter versions): reported exactly as Haiku 4.5 above, so `AnthropicUsageMapper` holds unchanged.

| Request | `InputTokenCount` | `CachedInputTokenCount` | `AdditionalCounts` |
|---|---|---|---|
| No cache marker | 15,649 | 0 | null |
| Cache write | 15,649 | 0 | `CacheCreationInputTokens` = 15,638 |
| Cache read | 15,649 | 15,638 | null |

- **Tokenizer:** the same prompt is 15,649 tokens on Haiku 5.5 against 10,266 on Haiku 4.5, **+52%** (the migration guide
  says about 30%; this prompt is one repeated sentence, so measure real prompts before trusting either figure).
- **Price:** the 6 requests cost about $0.007, against about $0.054 on Haiku 4.5: about 7.5× cheaper even after the
  extra tokens. Haiku 5.5 is priced by prompt length: over 100,000 tokens (cache included) the whole request costs 5×.
- **Thinking:** `LlmClientFactory`'s adaptive thinking is accepted (no 400). At effort `medium` the model skips
  thinking on a one-line question. On a puzzle (`usage-probe -- --think`) it thinks, and the thinking **text comes back**
  as a summary (662 chars non-streaming; 27 streamed chunks, 292 chars), each block signed: the agent's
  `ReasoningOutput.Full` already gets summarized thinking, so the hosts show it without a change. The guide's empty
  default doesn't apply to us.
- **Thinking is most of the output and can't be told apart:** a two-sentence answer used 805 output tokens, and
  `ReasoningTokenCount` is null, so the ledger records thinking and answer together. Effort is the lever on that cost.
- `ReasoningTokenCount` is still null; streaming usage still arrives in the last update.

**What the Ollama adapter reports** (measured 2026-10-10 with `tools/louis-agent.usage-probe`: OllamaSharp's
`OllamaApiClient`, `qwen2.5:0.5b`, a 686-token prompt sent twice, streaming and not):

- `InputTokenCount` = 686 and `OutputTokenCount` on every request, including the repeat: Ollama reports the whole
  prompt even when it reuses its own prompt cache. `CachedInputTokenCount` and `ReasoningTokenCount` are null;
  `AdditionalCounts` is empty.
- **Streaming:** one `UsageContent` in the last update, as with Anthropic.
- `StandardUsageMapper` is right for it: input 686, output as reported, the cache and reasoning counts null.
- Not yet measured: `openai-compatible`. Rerun the probe for it before relying on its ledger numbers.

### B. Prompt caching (Anthropic)

**What:** mark the stable parts of each request so the API serves them from cache. Reading cached tokens costs **0.1×**
the normal input price on Haiku (**0.05×** on Sonnet 5.5 and Opus 5.5); writing them costs **1.25×** (5-minute
lifetime) or **2×** (1-hour). With the 5-minute lifetime, two requests already break even.

**Breakpoints, as built in F4** (the API allows 4 per request; a marker goes at the *end* of the block it covers; the
order is tools → system → messages, so a marker on the system prompt also covers the tools):

| # | Where | Covers | How (Anthropic SDK 12.53) | Lifetime |
|---|---|---|---|---|
| 1 | The system prompt's last block | Tools + system prompt (47,424 tokens in the demo on Haiku 5.5) | A copy of the block, `new TextContent(text).WithCacheControl(ttl)` | `LLM_CACHE_TTL` (5m default, 1h) |
| 2 | Automatic: the request's top-level `cache_control` | The whole conversation so far; the API puts it on the last block and moves it forward | `MessageCreateParams.CacheControl` through `RawRepresentationFactory` | Always 5 minutes |

*Planned, not built:* a separate marker on the last tool (it only helps when the system prompt changes but the tools
don't, which is rare), and an explicit marker on a clone of the newest message (automatic caching does it without
touching history).

**Where it lives:** `PromptCachingChatClient` (a provider-neutral `DelegatingChatClient`) asks the provider's
`IPromptCachingStrategy` for each request: `AnthropicPromptCaching` marks a **per-request copy**, never the stored
history (`WithCacheControl` changes the block it is called on), and `NoPromptCaching` sends Ollama's and
OpenAI-compatible requests unchanged. `LlmClientFactory` picks the strategy (`LLM_CACHE=on`, the default) and wraps
every provider's client with it, innermost, so every tool round is marked. Summary requests go unmarked.

**Lifetime:** per marker. The conversation's (marker 2) is always 5 minutes: only the next round, seconds later, reads
it. The prefix's (marker 1) is `LLM_CACHE_TTL`, chosen from the measured gaps between requests that share it:
`usage-probe -- --cache-gaps <ledger>` replays a ledger against both lifetimes per host and model. Under 5 minutes or
over an hour, 5m is cheaper; between, 1h. The longer lifetime always comes first, as the API requires.

**Keep the prefix byte-stable** (each item below silently breaks caching):

- Register tools in a deterministic order: sort by name instead of relying on reflection order.
- Build the system prompt identically every time; it changes only when skills are reloaded (acceptable).
- Never put timestamps, ids or per-request text in the system prompt or tool descriptions.
- Keep thinking and effort fixed for a session; changing them invalidates the message cache.
- Approving or creating an agent-built tool changes the tool list: expected, a one-off rebuild.

**Model-specific traps** (the default model is Claude Haiku 5.5; checked against Anthropic's docs 2026-10-10):

- **Minimum cacheable prefix: 512 tokens on Haiku 5.5** (4,096 on Haiku 4.5). Our ~48,000-token prefix is far above
  it; on Haiku 4.5 a much smaller toolset must stay above 4,096, or nothing caches and no error is raised.
- **Thinking blocks stay in the history on Haiku 5.5** (models numbered 4.6 and later keep earlier turns' thinking and
  bill it as input), so a new user message doesn't lose the message cache. Haiku 4.5 strips them, and loses the
  message cache from that point; tool rounds within one turn and breakpoints 1–2 are unaffected on both.
- **Changing the thinking settings between requests invalidates the cache**: keep thinking and effort fixed for a
  session (see the list above).

**Estimated effect on one demo fix** (~20 requests, ~26,000-token prefix; the 2026-10-07 estimate, before M1 measured
11.4 requests and ~48,000 tokens, which gives about −80%; F4 measures the real figure):

| | Prefix tokens billed (as full-price equivalents) |
|---|---:|
| Today | 20 × 26,000 = **520,000** |
| With breakpoints 1–2 | 1 × 26,000 × 1.25 + 19 × 26,000 × 0.1 ≈ **82,000** (−84%) |

Breakpoint 3 adds savings on the growing history on top of that.

**Measured** (F4, 2026-10-11, the full demo): the prefix per fix about **−88%** ($0.054 to about $0.0063), louis-agent's
input cost **−87%**, the run **−80%** against M1 with 10/10 fixes; the prefix was written once per run and read by every
later fix in its own session ([F4 benchmark](../BENCHMARKS.md#f4-benchmark-2026-10-11)).

**Accept when:** from the second request of a session, `CachedInputTokenCount` ≥ the prefix size; the benchmark's
input cost falls by at least 60%; Ollama and OpenAI-compatible runs are unchanged (no markers sent).

**What the adapter sends** (F4 step 1 spike, 2026-10-11: Anthropic 12.53 with its Microsoft.Extensions.AI adapter;
captured request bodies, no model call; kept as `AnthropicCachingWireTests`):

| Question | Answer |
|---|---|
| Does `TextContent.WithCacheControl(ttl)` on the system message reach the wire? | Yes: `"system":[{"text":…,"cache_control":{"type":"ephemeral","ttl":"1h"}}]` |
| Does a top-level `cache_control` (automatic caching) set through `RawRepresentationFactory` reach it? | Yes, beside the system marker, with its own TTL; no marker lands on the messages |
| Does it keep thinking and tools? | Yes, including through `WithThinkingBudget`, which chains to the previous factory |
| Are writes split by lifetime reported? | Not in `UsageDetails` (only the total `CacheCreationInputTokens`, as F1 found); the SDK's own `Message` (`ChatResponse.RawRepresentation`) has `Usage.CacheCreation.Ephemeral5mInputTokens` / `Ephemeral1hInputTokens` |

Checked against Anthropic's prompt-caching page the same day: a read refreshes the lifetime at no cost; 5-minute and
1-hour entries can be mixed if the 1-hour one comes first; automatic caching uses one of the 4 breakpoint slots; the
lookback is 20 blocks; Haiku 5.5 caches from 512 tokens.

### C. Send only the tools a session needs

**What:** a toolset profile per host or session, fixed for its lifetime (varying it per request would break the
cache). `AGENT_TOOLSETS` lists what to load: `files,git,dotnet,python,powershell,bash,web,skills,paymo,devops`
(default: everything that has its keys, as today).

- The orchestrator demo's `demo-api` sets `files,git,dotnet,skills`: about 35 fewer tools than the 108 it loads now
  (Paymo and DevOps alone are ~20,000 characters, ~5,000 tokens per request).
- The system prompt follows the toolset: skill guides for tools that aren't loaded (python, powershell, web …) are
  left out too.
- The build-mode POC uses the same profile.

**Accept when:** the demo's fixed prefix shrinks by at least 25% and its fix success rate (10/10) holds.

### D. Keep history bounded: clear, then summarise

This is the "summarising" part. Two steps, cheapest first; both rewrite earlier history, which invalidates the
message cache from that point, so they run **rarely, at thresholds** — not every round.

1. **Clear stale tool results.** When the conversation passes `CONTEXT_CLEAR_AT` tokens (default 60,000), replace
   tool results older than the last `CONTEXT_KEEP_ROUNDS` rounds (default 4) with a stub:
   `[cleared: ReadWorkspaceFile src/OrderService/Api/OrderTotal.cs — 3,120 chars; read it again if needed]`.
   File reads, directory listings and test output are the bulk of a fix's history and are cheap to fetch again.
   Provider-neutral, in `AgentEngine`. (Anthropic's server-side context editing does the same, but needs the beta
   client, so it is an option later, not the plan.)
2. **Compact old turns.** When the conversation still passes `CONTEXT_COMPACT_AT` (default: the model's compaction
   point from F15, below the smaller of a share of its window and its price step: under 100,000 tokens on Haiku 5.5),
   replace
   everything before the last `CONTEXT_KEEP_TURNS` user turns (default 3) with one summary message, written by a
   separate tool-less call — the same pattern as `ToolCallGuards.SummariseToolResultAsync`. Keep thinking blocks that later tool
   rounds still need. Write the original turns to the session log before replacing them, so nothing is lost for
   debugging. (Anthropic's server-side compaction is beta and Anthropic-only; it supports Haiku 5.5 but not Haiku 4.5,
   so a hand-rolled step keeps the core provider-neutral.)

This is the first slice of the TODO's context-engineering pipeline (token budget → compress → build prompt); ranked
retrieval and long-term memory stay in that plan.

**Accept when:** a 30-turn scripted chat stays under `CONTEXT_COMPACT_AT` and still answers questions about its first
turns correctly; the demo's 10/10 fixes and its per-fix tokens don't get worse.

### E. Response settings per route

Fixed per session, so they don't break the cache:

| Route | Thinking | Tool rounds per message | Why |
|---|---|---|---|
| Interactive chat (web, Rider, CLI) | `LLM_THINKING` (medium) | 10 | Quick feedback; the user continues if needed |
| API task sessions (orchestrator fix / build) | medium | `AGENT_MAX_TOOL_ROUNDS` (e.g. 30) | Fewer "continue" round-trips per fix |
| Orchestrator triage | low | 10 | Its job is classification against a runbook |
| Summaries (oversized results, compaction) | off | 0 | Plain summarisation; use the cheapest configured model (`SUMMARY_MODEL`, [F16](../features/F16-summariser.md)) |

A higher round limit doesn't make a fix cheaper by itself — the history is re-sent either way — but it removes the
follow-up messages and their latency.

**Cleanup:** `AsIChatClient` now takes an `AnthropicThinkingMode` (`Extended` for models before adaptive thinking).
Check whether it replaces the repo's own thinking-budget wrapper in `LlmClientFactory`.

### F. Reliability (prevents paying twice)

- **Retry with backoff** on rate limits and overload (429, 529, 5xx), honouring `retry-after`; a usage or credit limit
  stops the run with a clear message instead of failing every remaining error (the demo hit both).
- **Model fallback** to a configured second provider/model when retries run out (the TODO's fallback item); note that
  caches are per model, so a fallback starts cold.
- **Structured output** for the machine-read lines (`FIX-RESULT`, `DECISION`) where the provider supports it, so a
  malformed line never costs a "continue" round-trip.

### G. Later, where it fits

- **Message Batches** (about half price, asynchronous) for non-interactive bulk work such as drafting runbooks for a
  whole service; never for chat.
- **Anthropic-native context editing and compaction** via the beta client, if C and D's hand-rolled versions prove
  too lossy or too costly.

## 5. Configuration (new settings)

| Setting | Default | Workstream |
|---|---|---|
| `PRICES_FILE`, `PRICES_URL` (built in F2; replaced the planned `LLM_PRICE_*` settings) | `config/prices.json` | A |
| `LLM_CACHE` | `on` for Anthropic | B |
| `LLM_CACHE_TTL` | `5m` | B |
| `AGENT_TOOLSETS` | all with keys | C |
| `CONTEXT_CLEAR_AT`, `CONTEXT_KEEP_ROUNDS` | 60,000 · 4 | D |
| `CONTEXT_COMPACT_AT`, `CONTEXT_KEEP_TURNS` | per model (F15; under 100,000 on Haiku 5.5) · 3 | D |
| `AGENT_MAX_TOOL_ROUNDS` | 10 | E |
| `SUMMARY_MODEL` | the main model | E ([F16](../features/F16-summariser.md)) |

## 6. Rollout and measurement

1. **A** first, then a **baseline**: one clean demo run and one scripted 30-turn chat, recording tokens and cost per fix,
   per turn and in total.
2. **B** behind `LLM_CACHE`; rerun the benchmark; compare against the baseline. Expected: the biggest single drop.
3. **C** (demo profile), rerun, compare.
4. **D** with conservative thresholds, rerun; check the fix rate and the long-chat recall test, then tune thresholds.
5. **E** and **F** as configuration and resilience, measured the same way.

Each step lands only if quality holds: 10/10 demo fixes confirmed by the orchestrator, and the chat recall test passes.

## 7. Risks

| Risk | Mitigation |
|---|---|
| Cache markers accumulate on stored history → more than 4 breakpoints → 400 | Mark a per-request copy only (B); tests assert the caller's messages and blocks are unchanged and no message carries a marker (`PromptCachingTests`) |
| Prefix changes silently between requests → no hits, higher cost (writes at 1.25×) | Usage logging shows `CachedInputTokenCount` = 0; deterministic tool order; no dynamic text in the prefix |
| Clearing or compacting loses something the model needs | Stubs say how to get it back; originals go to the log; thresholds tuned against the benchmark |
| History rewrites break the message cache | Run them rarely, at thresholds; tools and system caches are unaffected |
| A smaller toolset falls below the model's cache minimum (F15; 512 tokens on Haiku 5.5) | Check prefix size in the usage log after changing profiles |
| Provider features leak into the core | Markers only in the Anthropic `CachingChatClient`; core tests run with the fake client |

## 8. Open questions

- ~~Which `AdditionalCounts` key does the Anthropic adapter use for cache writes?~~ Answered:
  `CacheCreationInputTokens`, and `InputTokenCount` includes the cached tokens (see *What the Anthropic adapter
  reports* under A).
- Should the web app show cost in money, or tokens only? (Prices are per account and change.)
- Do Rider chats benefit from the 1-hour cache lifetime? (Depends on measured gaps between messages.)
- Should the orchestrator, which only needs a short system prompt and 7 tools, use a cheaper model than louis-agent?
  (Its prefix is far below 26,000 tokens, but triage quality must be measured first.)

---
[Docs index](../README.md) · Related: [Usage, estimates and budgets](USAGE_AND_BUDGETS.md) · [TODO](../../TODO.md) · [How a turn works](../AGENT_INTERACTION.md) ·
[How the agents and endpoints talk](../AGENT_COMMUNICATION.md)
