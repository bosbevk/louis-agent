# Change the usage probe

> **How-to.** Use and change `tools/louis-agent.usage-probe`, the developer tool that shows what a provider reports as
> token usage and what the ledger makes of it: check a new provider or model, fix a usage mapper, total a ledger, add a
> command. **Elsewhere:** what it prints in [louis-agent.usage-probe](../projects/louis-agent.usage-probe.md); why every
> request is measured in [ADR 0007](../adr/0007-measure-every-request.md).

Every cost the agent shows comes from the usage ledger, and the ledger is only as right as the mapper that reads each
provider's usage. The probe sends a few small real requests and prints the provider's raw counts beside the `ledger:`
line the mapper makes of them, so a misread shows up before it reaches a bill. Its other commands read a ledger and make
no model call.

## Check a new provider or model

1. Set the provider in `config/.env`, or pick a profile for one run, and run the probe. The requests are small, but
   they are real: they cost a little.

   ```bash
   LLM_PROFILE=ollama dotnet run --project tools/louis-agent.usage-probe
   LLM_PROFILE=ollama LLM_MODEL=qwen2.5:0.5b dotnet run --project tools/louis-agent.usage-probe
   dotnet run --project tools/louis-agent.usage-probe -- --think        # is thinking text returned?
   ```

2. Compare each `ledger:` line with the raw counts above it: input, output, cache reads and writes. They must say the
   same thing in the ledger's terms.
3. If the model has no row in `config/prices.json`, its cost is recorded as unknown (`null`, never 0). Add its five
   prices from the provider's own price page, and set the table's `as_of` to the day you copied them.

## Fix a usage mapper

When the standard mapping misreads a provider (cache writes under a key of its own, input that leaves out cached
tokens):

1. Write the failing case first in core's usage mapper tests (`UsageMapperTests`), with the raw counts the probe printed.
2. Add a mapper in its provider's folder under `louis-agent.core/Llm/Providers/` (Anthropic's is
   `AnthropicProvider/AnthropicUsageMapper.cs`), and choose it in
   `LlmClientFactory.CreateUsageMapper`: the one place a provider is picked, never a check on its name elsewhere
   ([ADR 0003](../adr/0003-provider-strategies.md)).
3. Run the probe again; the `ledger:` lines now match.
4. Write what you measured in the [optimisation spec](../specs/RESPONSE_OPTIMISATION.md), beside *What the Anthropic
   adapter reports*.

## Read a ledger

```bash
dotnet run --project tools/louis-agent.usage-probe -- --ledger logs/usage-2026-10.jsonl       # price and total it
dotnet run --project tools/louis-agent.usage-probe -- --cache-gaps logs/usage-2026-10.jsonl   # which LLM_CACHE_TTL fits
```

Both use the same records and totals as every host (`UsageReport.Total`), so their numbers agree with what a chat showed.

## Add a command

A command is a flag read at the top of `Program.cs`, which runs it and returns before any model request. Its words go
in `Messages/ProbeUserText` ([ADR 0009](../adr/0009-message-text-as-enum-templates.md)), and anything it computes
(a sum, a replay) in a class with a test in core's tests or its own, never in `Program.cs`. `Program.cs` is over the
200-line limit and on `FileSizeTests`' list, so a new command is the moment to move one of the existing ones out too.

## Before you ask for a review

- [ ] A mapper change has its failing test, with the probe's raw counts.
- [ ] A new price is from the provider's page, and `as_of` says when.
- [ ] A new command's logic is outside `Program.cs`, with a test.

---
[Guides](README.md) · [Docs index](../README.md)
