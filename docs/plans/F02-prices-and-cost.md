# Plan: F2 Prices and cost

> **For:** [F2 Prices and cost](../features/F02-prices-and-cost.md) · **Approved:** 2026-10-10 · **Recreated:** 2026-10-11,
> from the plan approved in the session

## Context

F1 records every model request's tokens, but not what it cost; the demo had been priced by hand (≈ $0.27 short run,
≈ $0.66 full run). F2 turns tokens into money from a price table and stores the cost on each record, so later price
changes don't rewrite history. It unblocks F3 (show cost), F6 (estimates), F7 (budgets), and gives M1 its baseline.
Stories: F2-S1 … S4 in the feature doc; spec: usage §4.2.

**Decided before starting:** `config/prices.json` is tracked (prices aren't secret, it works right after a clone, and
Docker can mount one file); `PRICES_FILE` can point elsewhere.

## What changed since F2 was written

- Haiku 5.5 is the default, and its price depends on prompt length: over 100,000 prompt tokens (input + cache reads +
  writes) the whole request costs 5×, so the table needs a long-prompt tier.
- The record's `input` is uncached input (F1 subtracts cache reads and writes), so cost is a plain sum per kind.
- Every host gets its sink from `AgentHost.CreateUsageSink`, including the orchestrator.
- Saved ledgers exist (written before F2, `cost: null`), so the baseline can be priced without a paid run.

## Design

| Type | Responsibility |
|---|---|
| `UsageCost(Currency, Amount, PriceTable)` | On `UsageRecord` as `Cost`; JSON `"cost": {"currency","amount","price_table"}`, `null` when unknown |
| `ModelPrice` / `PriceTable` | Loads the JSON; `Find(modelId)` by longest prefix; `AsOf`, `Currency`; an optional `long_prompt` tier |
| `CostCalculator` | `For(UsageRecord, PriceTable)`: picks the tier, sums each kind × its price per million, rounds to 6 decimals; cache writes at the 5-minute price (F4 adds the lifetime) |
| `PricingUsageSink` | Decorator: prices each record, then the inner sink, so `UsageRecorded` (F3) sees the cost |

**A sink decorator, not the recorder** (the doc's first idea): every host's records already pass through
`AgentHost.CreateUsageSink`, so neither the engine nor any host changes. **Rules:** unknown model → `cost: null`, never
0; a model priced at 0 → `0`; input or output not reported → `null`; missing cache counts are 0. **Loading:**
`PRICES_FILE`, default `config/prices.json` found like `config/.env`; missing → one warning, costs `null`; malformed →
start-up fails naming the file. **Docker:** the main compose sets `PRICES_FILE` (repository at `/workspace`); the demo
mounts `config/prices.json` alone, read-only.

## Steps (each one commit with its test; stop for review after each)

Branch `feature/f2-prices` off `main`.

1. **`UsageCost` on the record + `PriceTable` loading and prefix matching.** *Tests:* dated ids match; longest prefix
   wins; `long_prompt` parsed; missing file → empty; malformed → error naming the file; old ledger lines still read back.
2. **`CostCalculator`.** *Tests:* the spec's worked example ($0.0365); cache reads at the read price; the tier switch at
   the threshold; unknown → null; priced at 0 → 0; null counts → null; rounding.
3. **`PricingUsageSink` + `PRICES_FILE` + wiring in `CreateUsageSink`.** *Tests:* priced record in the file;
   `UsageRecorded` sees the cost; end to end through `AgentHost.Build`; no file → warning and `cost: null`.
4. **Tracked `config/prices.json`, compose settings, docs.** *Manual, free:* an Ollama CLI turn records `cost.amount: 0`.
5. **M1 baseline from the saved ledger:** `usage-probe -- --ledger` totals a ledger per task, purpose and run; the
   baseline goes in the features README's *Benchmark*. *Test:* totals on a small fixture.
6. **Finish F2.**

## Approved during the build (2026-10-10)

- **`PRICES_URL` and `GET /prices`:** a host can fetch the price table over HTTP, e.g. from louis-agent.api's
  `GET /prices`; the file option stays. Decided: if the URL can't be reached at start-up, warn and **fall back to the
  file**; `GET /prices` is **public**, like `/health`. Built as steps 4b (core) and 4c (the API endpoint, the demo's
  orchestrator using it, docs).
- **Rerunning both demos** with F2 in place, to exercise the Docker wiring and save priced ledgers as the samples.

## Verification

- `dotnet test` on the solution after every step.
- Free: the Ollama CLI turn; the `GET /prices` and fallback checks against a local API.
- `usage-probe -- --ledger` on the saved full run ≈ $0.66, matching the hand-priced figure. No paid run needed (the
  demo reruns above were approved separately).

## Concepts

Pricing per token kind; why a record stores its cost with the price table's date; "unknown" vs "free"; the decorator
pattern on the sink. Skill map #13 (cost and token economics) and #17 (observability).

---
[Plans](README.md) · Previous: [F1](F01-usage-ledger.md) · Next: [F3](F03-usage-display.md)