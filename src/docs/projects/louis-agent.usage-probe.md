# louis-agent.usage-probe — design

> **Kind:** console tool (a console app) · **Path:** `src/tools/louis-agent.usage-probe/` · **References:** louis-agent.core
> · **Docker service:** none
> **Also shows:** `component`, `sequence`

![The usage probe, and what it shows: the raw report, the ledger's line, and a ledger priced](diagrams/louis-agent.usage-probe.svg)

## Purpose

The usage ledger's numbers are only as right as the mapping in `IUsageMapper`, and the providers' adapters don't all
report token usage the same way. This tool shows you what an adapter really reports, and what the ledger would
record from it.

Run it before you trust the ledger for a new provider, or after you upgrade an adapter package. It began as the
spike for the usage ledger's first step.

*Decided in:* F1 step 1.

## How it works

Here's one probe, from start to finish.

![A probe: the client and mapper built, six requests for Anthropic or four for others, each both ways, and what each reported printed](diagrams/louis-agent.usage-probe-flow.svg)

### 1. The default probe

It builds the same client and usage mapper the agents use, with `LlmClientFactory.Create` and `CreateUsageMapper`.
Then it sends a few one-word requests with a long system prompt, both streaming and not.

For each request it prints three things. First, every `UsageDetails` property and `AdditionalCounts` key, exactly as
the adapter reported it. Second, for a stream, which update carried the `UsageContent`. And third, the `ledger:`
line: what the provider's `IUsageMapper` turns all that into.

Which requests it sends depends on the provider:

| Provider | Requests |
|---|---|
| Anthropic | Plain, then with a `cache_control` marker on the system prompt twice (a cache write, then a read); each pair streaming and not. 6 requests |
| Others | Plain, then the same prompt again, to see whether the provider reports any caching itself. 4 requests |

These are the ways to run it:

```bash
dotnet run --project src/tools/louis-agent.usage-probe                          # provider from config/.env
LLM_PROFILE=ollama dotnet run --project src/tools/louis-agent.usage-probe     # another LLM profile for one run
LLM_PROFILE=ollama LLM_MODEL=qwen2.5:0.5b dotnet run --project src/tools/louis-agent.usage-probe   # and another model
dotnet run --project src/tools/louis-agent.usage-probe -- --think           # 2 reasoning requests: is thinking text returned?
dotnet run --project src/tools/louis-agent.usage-probe -- --lines 100           # prompt size (default 600 Anthropic, 40 others)
dotnet run --project src/tools/louis-agent.usage-probe -- --ledger .demo/logs/usage-2026-10.jsonl   # price and total a ledger
dotnet run --project src/tools/louis-agent.usage-probe -- --host-culture   # write the PC's culture to config/.env (F14)
dotnet run --project src/tools/louis-agent.usage-probe -- --cache-gaps .demo/logs/usage-2026-10.jsonl   # which LLM_CACHE_TTL (F4)
```

### 2. It costs real money

**Every request goes to the real provider.** With Anthropic and the default prompt, that's about 62,000 input
tokens, roughly $0.05 on Haiku 4.5. The prompt is that long because Anthropic only caches a prompt above a minimum
size, which is 4,096 tokens on Haiku 4.5.

### 3. Picking a cache lifetime, with `--cache-gaps`

**`--cache-gaps <file>` makes no model call.** It reads a ledger and groups it by host and model, because each of
those has its own cached prefix, shared by every session on that host.

For each group, it shows the gaps between requests: under 5 minutes, 5 to 60, and over an hour. Then it replays both
cache lifetimes over those gaps. A request within the lifetime of the last one reads the cache and restarts the
clock; any other request writes it. It prices each lifetime with the table, using the group's smallest prompt as the
size of the prefix, and recommends an `LLM_CACHE_TTL`. The logic is `CacheGaps`, in core, tested in `CacheGapsTests`.

*Decided in:* F4.

### 4. Pricing a ledger, with `--ledger`

**`--ledger <file>` makes no model call either.** It reads a usage ledger, and prices the records that have no cost
yet (ledgers written before F2) with the current price table: `PRICES_URL`, or `PRICES_FILE`, or
`config/prices.json`. Then it prints totals per piece of work (a fix's task, a triage, a chat session), per purpose,
per run, and in all.

That's how the M1 baseline was measured. The reading and totalling live in core, in `UsageReport`, so F3 and F11 can
use them too.

### 5. Setting your currency, with `--host-culture`

**`--host-culture [name]` makes no model call.** It reads this PC's culture (before the probe switches itself to the
invariant culture), checks it names a region, and prints the currency and today's rate. Then it writes
`HOST_CULTURE=en-ZA` into `config/.env`, through core's `EnvFile`. Only that line changes; comments and CRLF line
endings are kept.

Every container reads `config/.env`, so after a restart, costs show in rands everywhere. You can name a culture to
use instead of the PC's.

*Decided in:* F14.

![The usage probe: each command and the parts it uses](diagrams/louis-agent.usage-probe-diagrams.svg)

![The default job: the probe asks the provider a one-word question each way and prints what the adapter and the ledger report](diagrams/louis-agent.usage-probe-diagrams-2.svg)

These are the types involved:

![The types: no types of its own, one program over core's model factory, its usage mapper and the ledger's reports](diagrams/louis-agent.usage-probe-interface.svg)

## Structure

| File | What's in it |
|---|---|
| `Program.cs` | The whole probe |
| `louis-agent.usage-probe.csproj` | References core only |

## Configuration

It uses core's LLM settings: `LLM_PROVIDER`, `LLM_MODEL`, `LLM_ENDPOINT`, `LLM_THINKING` and the keys. A variable you
set in the shell wins over `config/.env`.

## Extending it

The steps are in [Change the usage probe](../guides/change-the-usage-probe.md). In short: you check a new provider by
running the probe. If it misreads something, the fix is a mapper in that provider's folder, chosen in
`LlmClientFactory.CreateUsageMapper`.

## Tests

It has no tests of its own, because it exists to call real providers. The mappers it exercises are tested beside
their providers (`AnthropicUsageMapperTests`). The reading, pricing and totals behind `--ledger` are tested in
`UsageReportTests`.

## Limits and plans

You read its output by eye; nothing checks it for you. And the `openai-compatible` provider hasn't been measured
with it yet.

## Related docs

[Optimisation spec §A](../specs/RESPONSE_OPTIMISATION.md) · [F1 Usage ledger](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F01-usage-ledger.md) ·
[louis-agent.core](louis-agent.core.md)

---
[Projects](README.md) · Previous: [louis-agent.orchestrator](../../../demos/louis-agent/docs/projects/louis-agent.orchestrator.md) · Next: [louis-agent.evals](louis-agent.evals.md)
