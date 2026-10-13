# Sample output: the short demo with toolset profiles (F5)

What one run of `samples/run-demo.ps1 -Short` produced with F5 on top of F4's caching (in Docker, model
`claude-haiku-5-5` for both agents, 2026-10-13, run id `run-20261013-161416`): five production errors became
**4 fixed and 1 escalated**, the same as before. Each fix session loaded only the coding profile (`dotnet`, `files`,
`git`, `skills`: 64 tools instead of 108). It cost **$0.0378, shown as R0.62**, against **$0.0488** for the same demo
with F4 alone ([sample-output-short-cached/](../sample-output-short-cached/README.md), the "before"). Times are UTC.

| | F4 (caching) | F5 (+ toolset profiles) | Change |
|---|---:|---:|---:|
| A fix session's first request (its prefix) | ~48,500 tokens | ~24,400 tokens | **−50%** |
| louis-agent: cost | $0.0353 | $0.0243 | **−31%** |
| louis-agent: cost per fix | $0.0088 | $0.0061 | −31% |
| louis-agent: cache reads | 2,353,128 | 1,074,071 | −54% |
| louis-agent: cache-hit rate | 98% | 95% | |
| Orchestrator (triage, not cached) | $0.0135 | $0.0135 | 0% |
| **The run** | **$0.0488** | **$0.0378** | **−23%** |

- **The prefix halved:** the coding profile drops the shell, web, Paymo and DevOps tools and their guides. F5's target
  was ≥ 25% smaller.
- **The cache still hits** from the second request of every fix; fixes 2–4 opened by reading the prefix fix 1 wrote.
- **Cache writes look higher (60,419 against 40,501), but that favours F4:** in F4's short run the full run just before
  had left its prefix in the cache, so fix 1 read it (47,424) and the run never paid to write it; this run started
  cold and wrote its 24,475-token prefix once. Each fix's own writes are about the same in both.
- **What's left is the triage:** the orchestrator's $0.0135, a third of the run now, isn't cached (F17).
- **The start-up line's estimate is low:** demo-api logged "about 15,395 tokens of fixed prefix" for the coding profile;
  the first request was 24,4xx, as the estimate counts characters / 4 and leaves out the task message.

## Files

| File | What it is |
|---|---|
| `agent-comms.md` | Every message between the agents, per error, and the run summary |
| `usage-2026-10.jsonl` | The run's 53 usage records (both agents), each with its cost; `parent` is null (no sub-sessions) |
| `orchestrator-state/` | `decisions.jsonl`, `fixes.jsonl`, `escalations.jsonl`, `processed.txt` |
| `branches.txt` | The demo repository's branches after the run, before any merge |
