# F15 · Model limits: each model's context window, output limit and cache minimum

> **Status:** planned · **Milestone:** M2 (first: F4, F9 and F13 take their targets from it) · **Depends on:** F2
>
> **Spec:** [Optimisation §B (cache minimums), §C, §D](../specs/RESPONSE_OPTIMISATION.md) · [MODELS.md](../MODELS.md)
>
> **In the TODO:** tick **F15 Model limits** under *Now* ([TODO](../../TODO.md))

Every limit the agent works against is a single number today, the same for every model: F9 compacts at 120,000 tokens,
F13's budget is a per-profile setting (2,048 for Ollama), F4's target is "input cost down 60%". But models differ: a
local llama3.1 on Ollama's default sees 2,048 tokens, the Claude models far more; Haiku 5.5 caches a prefix from 512
tokens, Haiku 4.5 only from 4,096; Haiku reads its cache at 0.1× the input price, Sonnet 5.5 and Opus 5.5 at 0.05×. A
limit that is right for one model is wrong for another, and nothing records which model has which.

F15 gives each model its own limits, in one table matched by model id like the prices (F2), and works out each
feature's target from them. The idea is standard: model gateways keep prices and context windows per model in one
table (LiteLLM's `model_prices_and_context_window.json`, for example).

**A window is a ceiling, not a target.** Filling it costs more with every token and models tend to use very long
contexts less well, so the window is what F9 compacts *before* and what F13 must fit *within*. F4's target is a saving,
worked out from each model's cache prices; its limit is the model's cache minimum.

## User stories

**F15-S1 — Each model's limits, in one place.** As an *operator*, I want each model's context window, output limit and
cache minimum recorded beside its prices, so that every feature uses the right limit for the model it runs on.
- *Given* the limits table *when* a host starts on `claude-haiku-5-5` *then* it logs that model's limits, e.g.
  `Model claude-haiku-5-5: window N tokens, output up to M, caches from 512 tokens (limits as of …)`.
- *Given* a model id the table doesn't list *then* its limits are unknown: one `[WARN]`, and every feature behaves as it
  does today (no budget), never a guessed limit.
- *Given* the table *then* it is matched by longest prefix and dated by `as_of`, like the price table, and copied from
  the provider's documentation (Anthropic's models overview; Ollama's `num_ctx`).

**F15-S2 — A local model's window is what it's given.** As a *user* of a local model, I want its window to be the one
Ollama actually uses, so that the agent's limit and the server's agree.
- *Given* the Ollama profile *then* the window is `num_ctx` (2,048 by Ollama's default, or `LLM_CONTEXT_TOKENS` when
  set), not the model's theoretical maximum: Ollama cuts anything beyond `num_ctx` without an error.

**F15-S3 — See how full the window is.** As a *user*, I want to see how much of the window a turn used, so that I notice
a chat getting long before it breaks or gets expensive.
- *Given* a turn *then* `/usage` shows the largest prompt of the session against the window
  (`largest prompt: 58k of N tokens (x%)`).
- *Given* a request whose prompt reaches 90% of the window *then* the host logs a `[WARN]` naming the model and the
  share (for local models this is F13's truncation warning, generalised).

**F15-S4 — Each feature's target, per model.** As a *developer*, I want each model's targets worked out from its limits
and prices, so that F4, F9 and F13 measure against the right number for each model.
- *Given* `usage-probe -- --targets` *then* it prints, per model in the tables:
  - **F9:** compact at a share of the window (`CONTEXT_COMPACT_SHARE`, e.g. 60%) instead of a fixed 120,000;
  - **F13:** the request budget = the window minus the output reserve;
  - **F4:** the expected saving on the repeated prefix from that model's cache read and write prices, using M1's
    measured shape (a ~48,000-token prefix, 11.4 requests per fix), and whether that prefix clears the cache minimum;
    "no caching" for local models.
- *Given* a model whose prefix is below its cache minimum *then* the target says so (caching would do nothing, with no
  error from the API).

## Design

- **`ModelLimits`** (core, `usage/` beside `PriceTable`): `ContextWindow`, `MaxOutput`, `CacheMinimum` (all optional:
  unknown is null, never 0), with the table's `as_of`. Looked up by longest model-id prefix, like prices.
- **Where the table lives** (decide when planning F15):

  | Option | For | Against |
  |---|---|---|
  | **A. A `limits` block per model in `config/prices.json`** | One table per model id, as gateways keep it; `PRICES_URL` / `GET /prices` already carry it to other hosts | The file is no longer only prices; the name says less than it holds |
  | **B. A separate `config/models.json`** (`MODELS_FILE` / `MODELS_URL`, `GET /models`) | Each file does one thing; limits can change without touching prices | A second table matched by the same ids, kept in step by hand; another URL and endpoint |

  Either way: our own format, the only one read, a tracked fallback file, and served on the API, as for prices
  ([F2](F02-prices-and-cost.md)) and exchange rates ([F14](F14-display-currency.md)).
- **Local models:** the window comes from `num_ctx` (the profile's `LLM_CONTEXT_TOKENS`, Ollama's 2,048 by default),
  not from the table: that's the window Ollama really applies.
- **Targets** (`ModelTargets`, core): pure functions of a model's limits, prices and the measured shape, so they're
  unit-tested and the same in the probe and in the features that use them.
- **The consumers change in their own features:** F4 checks the cache minimum and reports its saving against the
  model's own target; F9's threshold becomes a share of the window; F13's budget comes from the window. F15 only
  provides the limits and the targets.

## Implementation steps

1. **Gather the limits** for every model in `config/prices.json` from the providers' documentation (dated), and
   Ollama's `num_ctx` default. *Output:* the values with their sources, and the table/file decision.
2. **`ModelLimits` and its table** (the decided option) with loading, prefix matching and the fallback file. *Test:*
   dated ids match; unknown model → null limits with a warning; a negative or zero limit fails start-up naming the
   source.
3. **Hosts load the limits and log them** (`AgentHostContext.Limits`); local models take `num_ctx`. *Test:* the Ollama
   profile's window is 2,048; Haiku's is the table's.
4. **`/usage` shows the largest prompt against the window; the 90% warning.** *Test:* exact strings; the warning at 90%
   and not at 89%.
5. **`ModelTargets` and `usage-probe -- --targets`.** *Test:* the F4 saving per model from its cache prices (Haiku's
   0.1× read against Sonnet's 0.05×), the cache-minimum check, F9's share and F13's budget.
6. **Docs and the consumers' docs:** F4, F9 and F13 take their targets from F15 (their docs, not their code: each
   changes its code when it's built).

## Done when

- Every model in the price table has its limits (or is marked unknown), every host logs its model's limits, `/usage`
  shows how full the window got, and `usage-probe -- --targets` prints each feature's target per model.
- **Then finish up** (see [Finishing a feature](README.md#finishing-a-feature)).

## Not in this feature

Changing F4, F9 or F13's behaviour (each uses the targets when it's built); choosing a model by its limits (routing,
F10 and later).

---
[Features](README.md) · Previous: [F14 Display currency](F14-display-currency.md)