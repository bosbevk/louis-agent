# F4 smoke check: prompt caching on a real request

One CLI message on Claude Haiku 5.5 with caching on (`LLM_CACHE` default on, `LLM_CACHE_TTL` default 5m), run on
2026-10-11 for F4 step 7. It cost **$0.0070**. Kept as a reference: what a cached turn looks like in the ledger, and
the numbers to compare with if caching ever stops working (the API raises no error when a prefix misses; the ledger
just shows `cache_read: 0`).

## How it was run

```bash
LLM_PROFILE=anthropic LOG_DIRECTORY="$(pwd -W)/samples/smoke-f4-caching" \
  dotnet run --project src/louis-agent.cli -- "Read louis-agent.core/usage/CostCalculator.cs and \
  louis-agent.core/usage/UsageRecord.cs (one tool call each), then answer in two sentences: how is a cache write \
  priced, and what is cache_write_1h? Don't change any files."
```

`LLM_PROFILE` in the shell overrides `config/.env`, and `LOG_DIRECTORY` keeps this run's ledger and log apart.
The model read both files in one round (parallel tool calls), so the turn took 2 requests.

## Files

| File | What it is |
|---|---|
| [usage-2026-10.jsonl](usage-2026-10.jsonl) | The usage ledger: one line per request, with tokens and cost |
| [cli-output.txt](cli-output.txt) | What the CLI printed (stdout): the tool calls, thinking summary and answer |
| [agent-cli-20261011-202607-196.log](agent-cli-20261011-202607-196.log) | The run's log (stderr copy) |

## What the ledger shows

| Round | Uncached input | Cache write | of which 1 h | Cache read | Output | Cost |
|---|---:|---:|---:|---:|---:|---:|
| 1 | 4 | 47,581 | 0 | 0 | 210 | $0.006053 |
| 2 | 2 | 2,608 | 0 | 47,581 | 384 | $0.000994 |

- **F4-S1, the prefix is cached:** round 2 read 47,581 tokens, more than the ~48k prefix of tools and system prompt
  (the short demo's ledger estimates it at 48,424 with `usage-probe -- --cache-gaps`).
- **F4-S2, each round reads the round before:** round 2 read exactly what round 1 wrote, and wrote only what was
  new: the tool calls and their results (2,608). That is marker 2, the automatic one, moving forward.
- **The live stream carries the split by lifetime:** `cache_write_1h` is `0`, not `null`, so `message_start` has
  `cache_creation` on the real API too, as step 6 measured on a captured stream. At the default 5 minutes, every write
  is a 5-minute write.
- **The first round costs more, the second much less:** round 1 wrote 47.6k at 1.25×; round 2 read them at 0.1×.
  Without caching the same two requests would have cost about $0.0101 (97,776 input tokens at $0.10 per million, plus
  output), so this 2-round turn already cost **30% less**. A 1-round turn would cost 25% more than uncached on its
  input: the write pays off from the first read, and a fix takes about 11 rounds.

Total it with `dotnet run --project tools/louis-agent.usage-probe -- --ledger samples/smoke-f4-caching/usage-2026-10.jsonl`.