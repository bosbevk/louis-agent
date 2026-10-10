# F2 · Prices and cost

> **Status:** done (2026-10-10) · **Milestone:** M1 · **Depends on:** F1
>
> **Spec:** [Usage §4.2](../specs/USAGE_AND_BUDGETS.md)
>
> **In the TODO:** tick **F2 Prices and cost** under *Now* ([TODO](../../TODO.md))

F1 records tokens; F2 turns them into money using a price table the operator keeps, and stores the cost on each record
so later price changes don't rewrite history.

## User stories

**F2-S1 — Set prices in one place.** As an *operator*, I want to keep model prices in a config file, so that cost is
calculated without code changes when prices or models change.
- *Given* `config/prices.json` lists `claude-haiku-4-5` *when* a request runs on `claude-haiku-4-5-20251001` *then* it
  is priced by prefix match (the longest matching prefix wins).
- *Given* no prices file *then* records keep tokens and `cost: null`, and a warning is logged once at start-up.

**F2-S2 — Cost on every record.** As an *operator*, I want each usage record to carry its cost and the price table's date,
so that totals stay correct after prices change.
- *Then* `cost.amount` = input × input price + cache writes × write price (by lifetime) + cache reads × read price +
  output × output price, per million tokens; `cost.price_table` = the table's `as_of`.

**F2-S3 — Unknown is not free.** As an *operator*, I want a model without a price shown as "price unknown", so that I
never mistake missing prices for zero cost.
- *Given* an unpriced model *then* `cost` is `null`, and every view (F3, F11) shows "price unknown".
- *Given* a model priced at 0 (a local Ollama model) *then* cost is `0` — that is a deliberate choice, unlike `null`.

**F2-S4 — Works in Docker.** As an *operator*, I want the same prices file used by the containers, so that Docker and
local runs agree.
- *Given* the main compose file *then* the api reads `/workspace/config/prices.json`; *given* the demo compose file
  *then* `demo-api` and the orchestrator read a read-only mount of `config/`.

## Design

New, in `louis-agent.core/usage/`:

| Type | Responsibility |
|---|---|
| `PriceTable` | Loads `PRICES_FILE` (default `config/prices.json`, found the same way as `config/.env`); `Find(modelId)` by longest prefix; exposes `AsOf` and `Currency` |
| `CostCalculator` | `Cost? For(UsageRecord, PriceTable)`; cache writes priced by lifetime (`cache_write_5m` / `cache_write_1h`), the lifetime taken from the record (set by F4; 5 minutes by default) |

- `UsageRecordingChatClient` (F1) calls `CostCalculator` before handing the record to the sink.
- Ship `config/prices.example.json` (tracked) with the format and illustrative values plus a comment line telling the
  operator to copy current prices from the provider's pricing page; `config/prices.json` is the operator's copy,
  git-ignored like `config/.env`.
- Compose: `docker-compose.demo.yml` mounts `../config:/config:ro` on `demo-api` and `orchestrator` with
  `PRICES_FILE=/config/prices.json`; the main compose file already mounts the repository at `/workspace`.
- Rounding: store the amount with 6 decimals; round only for display.
- **Prices that depend on prompt length.** Claude Haiku 5.5, the default model, charges 5× per token when a request's
  prompt is over 100,000 tokens (cache reads and writes included), for the whole request. The table needs a tier per
  model (`over_100k` prices) and `CostCalculator` the request's total prompt (`input + cache_read + cache_write`) to pick
  one. Today's requests are 48,000–60,000 tokens, so they stay in the lower tier.

## Implementation steps

1. **`PriceTable` loading and prefix matching.** *Test:* exact and dated ids match; longest prefix wins; missing file →
   empty table with one warning; malformed file → clear error naming the file.
2. **`CostCalculator`.** *Test:* the worked example in U §4.1 (1,840 input + 26,110 cache-write + 412 output on Haiku
   prices = $0.0365); cache reads at the read price; 1-hour writes at the 1-hour price; unpriced → `null`; priced at 0 → 0.
3. **Attach cost in `UsageRecordingChatClient`.** *Test:* records written by the F1 tests now carry `cost` and
   `price_table`.
4. **Example file, `.gitignore`, compose mounts, settings docs.** *Test (manual):* demo run → ledger has costs.
   **Done:** both demos on Haiku 5.5 wrote a cost on every record (`samples/sample-output*/usage-2026-10.jsonl`), the
   API from its mounted file and the orchestrator from the API's `GET /prices` (`PRICES_URL`, added on request with a
   fallback to the file).
5. **M1 baseline:** run the demo, record tokens and cost per fix and in total in `docs/features/README.md` (Benchmark).
   **Done:** `usage-probe -- --ledger` (totals from core's `UsageReport`) on the full run: $0.6586 for 150 requests,
   $0.0625 a fix on average; the stored costs add up to the same total.

## Done when

- Stories' criteria pass; tests added; suites pass. **Met:** 568 core tests (52 new for F2), orchestrator 26.
- The M1 baseline (tokens and cost per fix) is written down. **Met:** features README, *Benchmark*: $0.6586 for the
  full demo, $0.0625 a fix on average.
- **Then finish up** (see [Finishing a feature](README.md#finishing-a-feature)): tick **F2** in the TODO,
  set *Status* to done here and in the features table, and note it in the spec's implementation map. **Done.**

## What the build changed from this plan

- **The price file is tracked** (`config/prices.json`), not an example plus an ignored copy: prices aren't secret, so a
  clone and every container price the same way.
- **Pricing is a sink decorator** (`PricingUsageSink` in `AgentHost.CreateUsageSink`), not a call inside
  `UsageRecordingChatClient`: every host's records already pass through that sink, so neither the engine nor any host
  changed.
- **A `long_prompt` tier** for Claude Haiku 5.5, which costs 5× for a whole request above 100,000 prompt tokens.
- **`PRICES_URL` and `GET /prices`** (asked for during the build): a host can fetch the table from louis-agent.api's
  public endpoint, falling back to the file when it can't be reached; the demo's orchestrator does.
- **`UsageReport` and `usage-probe -- --ledger`**: reading, pricing and totalling a ledger live in core, for F3 and F11.

---
[Features](README.md) · Previous: [F1 Usage ledger](F01-usage-ledger.md) · Next: [F3 Show usage](F03-usage-display.md)
