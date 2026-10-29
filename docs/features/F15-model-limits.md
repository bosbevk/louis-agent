# F15 · Model limits: each model's context window and cache minimum, and the limits that follow

> **Status:** planned · **Milestone:** M2 (after F4 and F5; F9 and F13 take their limits from it) · **Depends on:** F2
>
> **Spec:** [Optimisation §B (cache minimums), §C, §D](../specs/RESPONSE_OPTIMISATION.md) · [MODELS.md](../MODELS.md)
>
> **In the TODO:** **F15 Model limits and local models** under *Next*, Phase 9 of
> [the platform plan](../plans/platform.md), taking in [F13](F13-local-models.md) and the capability check per model
> ([TODO](../../TODO.md))

Every limit the agent works against is a single number today, the same for every model: F9 compacts at 120,000 tokens,
F13's budget is a per-profile setting. But models differ. The default, Claude Haiku 5.5, has a 1M-token window and
caches a prefix from 512 tokens; Haiku 4.5 only from 4,096; a local model has whatever window it's given. And the window
isn't the only ceiling: on Haiku 5.5 every token of a request costs 5× once its prompt passes 100,000 tokens (the price
table's `long_prompt` step, F2), far below the window. A limit that's right for one model is wrong for another, and
nothing records which model has which.

F15 gives each model its own limits, in one table matched by model id like the prices, and works out the limits the
other features use from them. The idea is standard: model gateways keep prices and context windows per model in one
table (LiteLLM's `model_prices_and_context_window.json`, for example).

**A window is a ceiling, not a target.** Filling it costs more with every token, and models tend to use very long
contexts less well. So F15 gives F9 the point to compact *before* and F13 the budget to fit *within*; it sets no cost
targets (F4's saving and M2's proof are F4's, estimates are F6's).

## User stories

**F15-S1 — Each model's limits, in one place.** As an *operator*, I want each model's context window and cache minimum
recorded beside its prices, so that every feature uses the right limit for the model it runs on.
- *Given* the limits table *when* a host starts on `claude-haiku-5-5` *then* it logs that model's limits:
  `Model claude-haiku-5-5: window 1M tokens, caches from 512 tokens, 5× above 100k prompt tokens (limits as of …)`.
- *Given* a model id the table doesn't list *then* its limits are unknown: one `[WARN]`, and every feature behaves as it
  does today (no budget), never a guessed limit.
- *Given* the table *then* it is matched by longest prefix and dated by `as_of`, like the price table, and copied from
  the provider's documentation (or, where a model isn't documented, from the provider's Models API).

**F15-S2 — A local model's window is the one the agent sets.** As a *user* of a local model, I want the agent to tell
Ollama the window, so that the agent's limit and the server's agree whatever the server's own default is.
- *Given* `LLM_CONTEXT_TOKENS` (the Ollama profile sets 2,048, to fit a small machine) *then* every request to Ollama
  carries it as `num_ctx`, and it is the model's window for every other feature.
- Ollama's own default no longer matters: it was 2,048 on the version that truncated our prompt (the ledger showed
  `input: 2050`); current Ollama picks 4k, 32k or 256k by available memory. Sending `num_ctx` makes it the same
  everywhere.

**F15-S3 — See how full the window is, and warn before it overflows.** As a *user*, I want to see how much of the window
a turn used, so that I notice a chat getting long or expensive before it breaks.
- *Given* a session *then* `/usage` shows its largest prompt against the window and the price step
  (`largest prompt: 58k tokens · 6% of the window · under the 100k price step`).
- *Given* a request whose prompt reaches 90% of the window *then* the host logs a `[WARN]` naming the model and the
  share. On a local model that's the request Ollama is about to truncate, which replaces F13's truncation warning.

**F15-S4 — Each limit worked out per model.** As a *developer*, I want each feature's limit worked out from the model's
window and price step, so that F9 and F13 use the right number for each model.
- *Given* `usage-probe -- --limits` *then* it prints, per model in the tables:
  - **F9:** the compaction point: below the **smaller** of a share of the window (`CONTEXT_COMPACT_SHARE`, e.g. 60%) and
    the model's price step, with a margin. On Haiku 5.5 that's under 100,000 (60% of 1M would be 600,000, every token
    of it at 5×); a model without a price step uses the share alone.
  - **F13:** the request budget: the window minus the output reserve.
  - **F4:** whether the measured prefix (~48,000 tokens in M1) clears the model's cache minimum (on Haiku 5.5, 512:
    easily). No saving figure: that's F4's own measurement against the M1 baseline.

## Design

- **`ModelLimits`** (core, `usage/` beside `PriceTable`): `ContextWindow` and `CacheMinimum` (optional: unknown is null,
  never 0), with the table's `as_of`; the price step comes from the model's price entry (`long_prompt.above_tokens`).
  Looked up by longest model-id prefix, like prices. No output limit until something reads it.
- **Where the table lives** (decide when planning F15):

  | Option | For | Against |
  |---|---|---|
  | **A. A `limits` block per model in `config/prices.json`** | One table per model id, as gateways keep it; the price step is already there; `PRICES_URL` / `GET /prices` already carry it to other hosts | The file is no longer only prices; the name says less than it holds |
  | **B. A separate `config/models.json`** (`MODELS_FILE` / `MODELS_URL`, `GET /models`) | Each file does one thing; limits can change without touching prices | A second table matched by the same ids, kept in step by hand; another URL and endpoint |

  Either way: our own format, the only one read, a tracked fallback file, and served on the API, as for prices
  ([F2](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F02-prices-and-cost.md)) and exchange rates ([F14](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F14-display-currency.md)).
- **Local models** (taken over from F13): `LLM_CONTEXT_TOKENS` in `LlmOptions` (unset = the table's window, or none);
  `LlmClientFactory` sends it to Ollama as `num_ctx` on every request. Provider differences stay in the factory.
- **Limits** (`ModelTargets`, core): pure functions of a model's limits, its price step and the measured prefix, so
  they're unit-tested and the same in the probe and in the features that use them.
- **The consumers change in their own features:** F9's threshold becomes the per-model compaction point; F13's budget
  comes from the window. F15 provides the limits, sends `num_ctx` and warns at 90%.

## Implementation steps

1. **Gather the limits** for every model in `config/prices.json` from Anthropic's documentation (dated); for a model it
   doesn't document (Haiku 4.5, if it stays in the table), from the Models API. *Output:* the values with their
   sources, and the table/file decision.
2. **`ModelLimits` and its table** (the decided option) with loading, prefix matching and the fallback file. *Test:*
   dated ids match; unknown model → null limits with a warning; a zero or negative limit fails start-up naming the
   source.
3. **`LLM_CONTEXT_TOKENS` and `num_ctx`** (from F13). *Test:* the Ollama client's requests carry `num_ctx`; Anthropic's
   don't; the setting is the model's window.
4. **Hosts load the limits and log them** (`AgentHostContext.Limits`). *Test:* the Ollama profile's window is
   `LLM_CONTEXT_TOKENS`; Haiku 5.5's is the table's.
5. **`/usage` against the window and the price step; the 90% warning** (replaces F13's truncation warning). *Test:*
   exact strings; the warning at 90% and not at 89%; a local model's 2,040 of 2,048 warns.
6. **`ModelTargets` and `usage-probe -- --limits`.** *Test:* Haiku 5.5's compaction point is under its 100k price step,
   not 60% of 1M; a model with no price step uses the share; F13's budget; the cache-minimum check.
7. **Docs:** F9 and F13 take their limits from F15 (their docs; each changes its code when it's built).

## Done when

- Every model in the price table has its limits (or is marked unknown), every host logs its model's limits, requests to
  Ollama carry `num_ctx`, `/usage` shows how full the window got, and `usage-probe -- --limits` prints each feature's
  limit per model.
- **Then finish up** (see [Finishing a feature](README.md#finishing-a-feature)).

## To settle when planning F15

- **Look the limits up by each request's model**, not once per host: a fallback model (F8) or a cheaper summary model
  (F10) must get its own window and price step, not the main model's.
- **Define F13's output reserve.** The budget is the window minus what the reply may use, and that can't be the
  engine's 16,000-token output limit on a 2,048-token window; it has to be set per model (or as a share of the window).

## Not in this feature

Changing F9's or F13's behaviour (each uses its limit when it's built); cost targets and estimates (F4, F6); an output
limit per model (until something uses it); choosing a model by its limits (routing, F10 and later).

---
[Features](README.md) · Previous: [F14 Display currency](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F14-display-currency.md) · Next: [F16 Summariser](F16-summariser.md)
