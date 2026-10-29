# 0007. Measure every request, priced from a dated table

**Status:** Accepted · **Date:** 2026-10-10 · **Recorded:** 2026-10-14

## Context

`ChatResponse.Usage` was ignored everywhere, so nobody knew what a turn, a fix or a run used or cost. Without that, no
optimisation (caching, smaller toolsets, delegation) can be shown to work. Prices change over time, and some models
(local ones) cost nothing while others have no known price.

## Decision

Every model request of every host is recorded in a usage ledger, one JSON line per request in
`{LOG_DIRECTORY}/usage-YYYY-MM.jsonl`, with its host, session, turn, purpose and, for the orchestrator, its fix and run.
It holds no prompt text or tool output. Each record is priced as it is written, from a price table dated by `as_of`, so
a later price change never rewrites history. Unknown counts and unknown prices are `null`, never `0`; a model priced at
`0` (a local one) is a deliberate `0`. Every host shows a turn's usage from the same records and totals.

## Consequences

- Every change to cost is measured against the ledger (the [benchmarks](https://github.com/bosbevk/louis-agent/blob/pre-cl10/docs/BENCHMARKS.md)).
- A new model needs its prices in `config/prices.json`, or its cost is recorded as unknown.
- The views and the ledger agree, because they total the same records (`UsageReport.Total`).

## Links

[F1 Usage ledger](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F01-usage-ledger.md) · [F2 Prices and cost](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F02-prices-and-cost.md) ·
[F3 Show usage](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F03-usage-display.md)
