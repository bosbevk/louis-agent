# Sample output: the short demo with triage on a local model

What one run of `samples/run-demo.ps1 -Short -OrchestratorProfile ollama-triage` produced on 2026-10-12 (run id
`run-20261012-102021`), in Docker: the orchestrator's triage ran on **`qwen3:4b-instruct-2507-q4_K_M` in Ollama** on a
4 GB laptop GPU (RTX 3050), and louis-agent's fixes on `claude-haiku-5-5` with prompt caching. The five production
errors became **4 fixed and 1 escalated**, the same as with Claude doing both
([sample-output-short-cached/](../sample-output-short-cached/README.md), the "before"). It cost **$0.0418, shown as
R0.69**, all of it louis-agent's: triage was free. Times are UTC.

| | Triage on Claude (2026-10-11) | Triage on qwen3 (this run) | Change |
|---|---:|---:|---:|
| Result | 4 fixed, 1 escalated | 4 fixed, 1 escalated | same |
| Orchestrator: requests | 14 | 22 | +57% |
| Orchestrator: input / output tokens | 116,143 / 3,809 | 125,023 / 1,221 | |
| Orchestrator: cost | $0.0135 | **$0** | **−100%** |
| louis-agent: cost | $0.0353 | $0.0418 | +18% (see below) |
| **The run** | **$0.0488** | **$0.0418** | **−14%** |
| Wall-clock time | 3.5 min | 8.4 min | 2.4× |

- **Triage was 28% of the cached run's cost, and it went to zero.** The fixes still need Claude, so this is the most a
  local orchestrator can save on this demo; without prompt caching, triage is a smaller share (5% of the uncached run).
- **louis-agent's +18% is the cache, not the triage.** The first fix wrote the whole 57,639-token prefix (tools and
  system prompt) to the cache cold, about $0.006 at $0.125 per million; in the "before" run the prefix was still warm
  from the full run that had just finished, so its first fix wrote 9,672 tokens. The other three fixes cost within a tenth
  of a cent of theirs. Compare runs started cold, or the run total over several runs.
- **Slower:** the run took 2.4 times as long, fixes included. The model ran at the container's 32k-token context, which doesn't fit 4 GB, so Ollama split it between CPU and GPU (about 5 tokens a
  second); at 8k with every layer on the GPU it writes about 54 a second (measured, not used in this run).

## What qwen3 got right, and wrong

Every triage followed the runbook's workflow: `CallLouisAgentFix` with the runbook's expected behaviour, then
`VerifyFixCommit`, `ReplayRequest` and `RunServiceTests` before deciding, and `refund` was escalated to payments
("Refund code is never auto-fixed as it moves money"), not fixed. Two mistakes a larger model didn't make:

- **Two contradicting calls in one reply** (vat): `AcknowledgeError` (with a reason saying the error is *not* expected)
  and `CallLouisAgentFix`, 11 ms apart. The fix went ahead and the decision was right, but
  [acknowledged.jsonl](orchestrator-state/acknowledged.jsonl) holds an acknowledgement that shouldn't exist.
- **The escalation was sent twice** (refund): the same `EscalateToHuman` call two times, so
  [escalations.jsonl](orchestrator-state/escalations.jsonl) has two identical lines; a real payments team would get it
  twice.

The three tries it took to get here (llama3.1, then qwen3's thinking variant) are in the
[features README](../../docs/features/README.md#local-triage-2026-10-12).

## Run it yourself

Needs an NVIDIA GPU with Docker Desktop's GPU support (or use the CPU `ollama` service: same steps, much slower) and
`ANTHROPIC_API_KEY` in `config/.env.secrets` for the fixes; `LLM_PROFILE` stays `anthropic`.

```powershell
# 1. Ollama on the GPU (pulls llama3.1 on its first start), and the triage model
docker compose -f docker/docker-compose.yml --env-file config/.env --profile ollama-nvidia up -d ollama-nvidia
docker exec louis_ollama_nvidia ollama pull qwen3:4b-instruct-2507-q4_K_M

# 2. The short demo: triage on qwen3 (config/.env.ollama-triage), fixes on Claude
.\samples\run-demo.ps1 -Short -OrchestratorProfile ollama-triage

# 3. Total it: triage shows $0.0000 per request
dotnet run --project tools/louis-agent.usage-probe -- --ledger .demo/logs/usage-2026-10.jsonl
```

This run used a profile named `qwen3` with the same settings; `config/.env.ollama-triage` is it, committed. Use the
**instruct** model: plain `qwen3:4b` thinks
before every answer and times out (below); `ollama show` lists `thinking` for both, so check with a short prompt's
token count instead. The orchestrator needs Ollama's 10-minute client timeout (`LlmClientFactory.OllamaTimeout`):
with .NET's default 100 seconds, a reply from a model split across CPU and GPU can be cancelled mid-way.

## Files

As in [sample-output/](../sample-output/README.md): [agent-comms.md](agent-comms.md) (every message between the agents,
and the run summary in rands at the end), [usage-2026-10.jsonl](usage-2026-10.jsonl) (one line per model request: the
22 triage requests on qwen3 at $0, the 47 fix requests on Claude), [branches.txt](branches.txt) (the repository before
merging, and each branch's files), [louis-agent-api-log.txt](louis-agent-api-log.txt) and
[orchestrator-state/](orchestrator-state/). New here: [orchestrator-log.txt](orchestrator-log.txt), the orchestrator's
own log with every tool call qwen3 made.

Total it with `dotnet run --project tools/louis-agent.usage-probe -- --ledger samples/sample-output-short-local-triage/usage-2026-10.jsonl`.