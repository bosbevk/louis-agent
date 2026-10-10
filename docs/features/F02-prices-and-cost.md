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
  *then* `demo-api` and the orchestrator read a read-only mount of `config/prices.json` alone (not the folder with its
  secrets), and the orchestrator fetches the API's `GET /prices` first.

## Design

As built, in `louis-agent.core/usage/` (the original plan, and how it changed, is under *What the build changed*):

| Type | Responsibility |
|---|---|
| `UsageCost` | On `UsageRecord` as `Cost`: `{ currency, amount, price_table }`, `price_table` being the table's `as_of`; null means "price unknown" |
| `PriceTable` | Loads the table from a file or a URL (one parser, the same checks); `Find(modelId)` by longest prefix; `AsOf`, `Currency`; a model may carry a `long_prompt` tier (`above_tokens` plus the five prices) |
| `CostCalculator` | `UsageCost? For(UsageRecord, PriceTable)`: picks the tier from the prompt (`input + cache_read + cache_write`), sums each kind × its price per million, rounds to 6 decimals (away from zero) |
| `PricingUsageSink` | Wraps the JSONL ledger in `AgentHost.CreateUsageSink`: prices each record, then writes it; a record that has a cost keeps it |
| `UsageReport` | Reads a ledger back, prices records written before F2, totals them by any key (used by `usage-probe -- --ledger`) |

- **Where the prices come from** (`AgentHost.LoadPrices`, once per host in `Build`): `PRICES_URL` if set and reachable
  (e.g. the API's public `GET /prices`), else `PRICES_FILE`, else `config/prices.json` found like `config/.env`. No table
  → one warning, costs `null`; a malformed table or a non-http `PRICES_URL` stops the host.
- **The table** `config/prices.json` is tracked, with comments saying where the prices came from. Compose sets
  `PRICES_FILE` for the main services; the demo mounts only that file, read-only, into `demo-api` and the orchestrator,
  and the orchestrator reads `PRICES_URL=http://demo-api:8080/prices` with the file as its fallback.
- **Prices that depend on prompt length.** Claude Haiku 5.5, the default model, charges 5× per token when a request's
  prompt is over 100,000 tokens (cache reads and writes included), for the whole request: its `long_prompt` tier. Today's
  requests are 48,000–60,000 tokens, so they stay in the lower tier.
- **Cache writes are always priced at the 5-minute rate** (`cache_write_5m`); `cache_write_1h` is in the table but unused
  until F4 records which lifetime a request's cache entries have. Today nothing is cached, so no write is priced at all.
- Reasoning isn't added to the cost: Claude counts thinking inside `output`.

## Implementation steps

1. **`PriceTable` loading and prefix matching.** *Test:* exact and dated ids match; longest prefix wins; missing file →
   empty table with one warning; malformed file → clear error naming the file.
2. **`CostCalculator`.** *Test:* the worked example in U §4.1 (1,840 input + 26,110 cache-write + 412 output on Haiku
   prices = $0.0365); cache reads at the read price; the long-prompt tier above its threshold; unpriced → `null`;
   priced at 0 → 0. (1-hour cache writes at the 1-hour price wait for F4, which records a write's lifetime; until then
   every write is priced at the 5-minute rate.)
3. **Attach cost to every record** (as built: `PricingUsageSink`, wrapping the ledger in `AgentHost.CreateUsageSink`,
   rather than inside `UsageRecordingChatClient`). *Test:* a record reaches the ledger with `cost` and `price_table`, end
   to end through `AgentHost.Build`.
4. **The tracked `config/prices.json`, compose settings, settings docs** (as built: tracked, not an example file plus a
   `.gitignore`d copy). *Test (manual):* demo run → ledger has costs.
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
- **A `long_prompt` tier** for Claude Haiku 5.5, which costs 5× for a whole request above 100,000 prompt tokens, instead
  of the planned `over_100k` prices.
- **Cache writes at the 5-minute price only.** The plan priced writes by the lifetime on the record, but nothing records
  a lifetime until F4, so `CostCalculator` uses `cache_write_5m` for every write; F4 adds the lifetime.
- **`PRICES_URL` and `GET /prices`** (asked for during the build): a host can fetch the table from louis-agent.api's
  public endpoint, falling back to the file when it can't be reached; the demo's orchestrator does.
- **`UsageReport` and `usage-probe -- --ledger`**: reading, pricing and totalling a ledger live in core, for F3 and F11.

---
[Features](README.md) · Previous: [F1 Usage ledger](F01-usage-ledger.md) · Next: [F3 Show usage](F03-usage-display.md)
