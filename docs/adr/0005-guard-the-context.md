# 0005. Guard the context: summarise oversized results, never run cut-off calls

**Status:** Accepted · **Date:** 2026-10-06 · **Recorded:** 2026-10-14

## Context

Everything a tool returns goes into the history and is re-sent with every later request. One huge result (a log, a
large file) can push a conversation past the model's window or into long-prompt prices. And when a reply hits the
output limit, a tool call in it has incomplete arguments: running it would act on half an instruction.

## Decision

Tool calls pass through guards (`ToolCallGuards`, split out of the engine in CL2):
- a result over 50,000 characters is summarised by a separate, tool-less model call, falling back to truncation;
- a tool call cut off by the output limit is never run, and the model is asked to write large content in chunks;
- a failure reaches the model with its reason, and is logged;
- a sub-session stops calling tools once its next request would pass its prompt budget (FX14, 2026-10-14).

## Consequences

- One tool can't flood the context; the model learns to read in smaller pieces.
- Summarising costs a model call, recorded in the ledger under its own purpose.
- The records of oversized results and cut-off calls (`logs/*.jsonl`) show which tools need narrower output.

## Links

[Architecture: Guards around tool calls](../architecture/06-runtime-view.md#guards-around-tool-calls) ·
[FX14](../fixes/FX14-sub-session-prompt-budget.md) · [F16 Summariser](../features/F16-summariser.md)
