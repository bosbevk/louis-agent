# Sample output: the short demo with prompt caching (F4)

What one run of `samples/run-demo.ps1 -Short` produced with F4's prompt caching on (in Docker, model `claude-haiku-5-5`
for both agents, 2026-10-11, run id `run-20261011-184153`): five production errors became **4 fixed and 1 escalated**,
the same as without caching. It cost **$0.0488, shown as R0.81**, against **$0.2884** for the same demo without caching
([sample-output-short/](../sample-output-short/README.md), the "before"). The full run is in
[sample-output-cached/](../sample-output-cached/README.md); the comparison is in the
[features README](../../docs/features/README.md#f4-benchmark-2026-10-11). Times are UTC.

| | Without caching | With caching | Change |
|---|---:|---:|---:|
| louis-agent: cost | $0.2749 | $0.0353 | **−87%** |
| louis-agent: input cost | $0.2683 | $0.0286 | **−89%** |
| louis-agent: cost per fix | $0.0687 | $0.0088 | −87% |
| louis-agent: cache-hit rate | 0% | 98% | |
| Orchestrator (triage, not cached) | $0.0135 | $0.0135 | 0% |
| **The run** | **$0.2884** | **$0.0488** | **−83%** |

The files are laid out as in [sample-output-cached/](../sample-output-cached/README.md). One fix (`vat 1009`) needed a
second message; its first request read the whole first turn (59,633 tokens) and wrote only the new message (61).