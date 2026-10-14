# Sample output: the full demo with prompt caching (F4)

What one run of `samples/run-demo.ps1` produced with F4's prompt caching on (in Docker, model `claude-haiku-5-5` for
both agents, 2026-10-11, run id `run-20261011-183104`): the 13 production errors became **10 fixed, 2 ignored and
1 escalated**, the same as without caching, with 10 fix branches and nothing pushed. It cost **$0.1334, shown as
R2.21**, against **$0.7157** for the same demo without caching ([sample-output/](../sample-output/README.md), the
"before"). F4's benchmark (step 8); the comparison is in the
[features README](../../docs/BENCHMARKS.md#f4-benchmark-2026-10-11). Times are UTC.

louis-agent cached its own turns (`LLM_CACHE` on, `LLM_CACHE_TTL` 5m, the defaults); the orchestrator's triage was
not cached (`LLM_CACHE=off` in `docker-compose.demo.yml`: its caching is F17's), so its cost is unchanged.

| | Without caching | With caching | Change |
|---|---:|---:|---:|
| louis-agent: cost | $0.6818 | $0.0991 | **−85%** |
| louis-agent: input cost (uncached input, cache reads and writes) | $0.6641 | $0.0820 | **−88%** |
| louis-agent: cost per fix | $0.0682 | $0.0099 | −85% |
| louis-agent: cache-hit rate (reads ÷ prompt) | 0% | 98% | |
| Orchestrator (triage, not cached) | $0.0340 | $0.0343 | +1% |
| **The run** | **$0.7157** | **$0.1334** | **−81%** |

## What the ledger shows

- **The prefix is written once for the whole run.** The first fix wrote the 47,424-token prefix (tools and system
  prompt); each of the nine later fixes, each in its own session about a minute later, opened by *reading* it and
  wrote only its own first message (about 1,100 tokens). That is marker 1, shared across sessions.
- **Each round reads the round before** and writes only what's new (marker 2): a fix's 12 requests read 98% of their
  prompt from the cache.
- **The "Continue the fix…" messages** (4 fixes needed a second message) read the whole conversation so far and wrote
  only the new message, 30 to 312 tokens: the history stays byte-identical between turns.
- Output tokens and requests are unchanged (−1%, −2%): caching changes the price of the prompt, not the work.

## Files

As in [sample-output/](../sample-output/README.md): [agent-comms.md](agent-comms.md) (every message between the agents,
and the run summary in rands at the end), [usage-2026-10.jsonl](usage-2026-10.jsonl) (one line per model request, with
`cache_write`, `cache_write_1h` and `cache_read`), [branches.txt](branches.txt) (the repository before merging, and each
branch's files), [louis-agent-api-log.txt](louis-agent-api-log.txt) and [orchestrator-state/](orchestrator-state/).

Total it with `dotnet run --project tools/louis-agent.usage-probe -- --ledger samples/sample-output-cached/usage-2026-10.jsonl`,
and see which lifetime its gaps favour with `-- --cache-gaps` on the same file.