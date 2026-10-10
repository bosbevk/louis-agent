# louis-agent.usage-probe — design

> **Kind:** console app (developer tool) · **Path:** `tools/louis-agent.usage-probe/` · **References:** louis-agent.core
> · **Docker service:** none

## Purpose

Shows what a provider's adapter actually reports as token usage, and what the usage ledger would record from it. Run it
before trusting the ledger for a new provider, or after upgrading an adapter package: the ledger's numbers are only as
right as the mapping in `IUsageMapper`, and the adapters don't all report the same way. It started as the F1 step 1
spike.

## How it works

It builds the client and the usage mapper the agents use (`LlmClientFactory.Create` and `CreateUsageMapper`) and sends a
few one-word requests with a long system prompt, both non-streaming and streaming. For each one it prints:

- every `UsageDetails` property and `AdditionalCounts` key, as the adapter reported it;
- for streams, which update carried the `UsageContent`;
- the `ledger:` line: what the provider's `IUsageMapper` turns that into.

| Provider | Requests |
|---|---|
| Anthropic | Plain, then with a `cache_control` marker on the system prompt twice (a cache write, then a read); each pair streaming and not. 6 requests |
| Others | Plain, then the same prompt again, to see whether the provider reports any caching itself. 4 requests |

```bash
dotnet run --project tools/louis-agent.usage-probe                          # provider from config/.env
LLM_PROFILE=ollama dotnet run --project tools/louis-agent.usage-probe     # another LLM profile for one run
LLM_PROFILE=ollama LLM_MODEL=qwen2.5:0.5b dotnet run --project tools/louis-agent.usage-probe   # and another model
dotnet run --project tools/louis-agent.usage-probe -- --think           # 2 reasoning requests: is thinking text returned?
dotnet run --project tools/louis-agent.usage-probe -- --lines 100           # prompt size (default 600 Anthropic, 40 others)
dotnet run --project tools/louis-agent.usage-probe -- --ledger .demo/logs/usage-2026-10.jsonl   # price and total a ledger
```

**`--ledger <file>` makes no model call.** It reads a usage ledger, prices the records that have no cost yet (ledgers
written before F2) with the current price table (`PRICES_URL` / `PRICES_FILE` / `config/prices.json`), and prints totals
per piece of work (a fix's task, a triage, a chat session), per purpose, per run and in all. It's how the M1 baseline was
measured; the reading and totalling live in core (`UsageReport`), for F3 and F11 to reuse.

**Every request goes to the real provider.** With Anthropic and the default prompt that is about 62,000 input tokens,
roughly $0.05 on Haiku 4.5. Anthropic only caches a prompt above a minimum size (4,096 tokens on Haiku 4.5), which is
why its prompt is long.

## Structure

| File | What's in it |
|---|---|
| `Program.cs` | The whole probe |
| `louis-agent.usage-probe.csproj` | References core only |

## Configuration

The core LLM settings (`LLM_PROVIDER`, `LLM_MODEL`, `LLM_ENDPOINT`, `LLM_THINKING`, keys); variables set in the shell
override `config/.env`.

## Extending it

For a new provider, run it and compare the `ledger:` lines with the raw counts. If the provider reports something the
standard mapping misreads (cache writes under its own key, input that excludes cached tokens), add a mapper next to
`AnthropicUsageMapper` in `louis-agent.core/usage/IUsageMapper.cs`, pick it in `LlmClientFactory.CreateUsageMapper`, and
record the findings in the optimisation spec next to *What the Anthropic adapter reports*.

## Tests

None: it exists to call real providers. The mappers it exercises are tested in `UsageMapperTests`, and `--ledger`'s
reading, pricing and totals in `UsageReportTests`.

## Limits and plans

- It reads the output by eye; nothing asserts on it.
- The `openai-compatible` provider hasn't been measured yet.

## Related docs

[Optimisation spec §A](../specs/RESPONSE_OPTIMISATION.md) · [F1 Usage ledger](../features/F01-usage-ledger.md) ·
[louis-agent.core](louis-agent.core.md)

---
[Projects](README.md) · Previous: [louis-agent.orchestrator](louis-agent.orchestrator.md) · Next: [tests](tests.md)
