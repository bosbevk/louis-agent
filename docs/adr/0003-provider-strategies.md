# 0003. Provider differences are strategies, chosen once in `LlmClientFactory`

**Status:** Accepted · **Date:** 2026-10-11 · **Recorded:** 2026-10-14

## Context

Behind one `IChatClient` ([0002](0002-messages-api-tier-with-our-own-loop.md)) the providers still differ: Anthropic
reports cache writes and needs cache markers, Ollama needs its context window sent, providers report usage and limits
differently. F1 met the first of these (`IUsageMapper`, 2026-10-10); F4's prompt caching was the second. Branching on
the provider's name wherever a difference shows up would spread provider knowledge through the engine and the tools.

## Decision

A concern where providers need different logic is a **strategy**: one interface, one implementation per provider that
differs plus a default (often a no-op), chosen once by `LlmClientFactory` from `LLM_PROVIDER`. Callers depend only on
the interface; nothing outside `Llm/` branches on a provider's name. A concern every provider handles the same way gets
no strategy.

## Consequences

- A new provider means new implementations, not edits to `AgentEngine`, the tools or the hosts.
- Provider SDKs stay in `Llm/{Name}Provider/`, which the architecture test enforces (ADR 0010).
- Built: `IUsageMapper` (F1), `IPromptCachingStrategy` (F4). Candidates: thinking settings (F10), the context window
  (F15), errors and limits (F8). The same pattern serves `IRatesMapper` and `IWebSearchProvider`.

## Links

[Architecture: Provider strategies](../ARCHITECTURE.md#provider-strategies) ·
[F4 Prompt caching](../features/F04-prompt-caching.md) ·
[Changing the LLM](../../louis-agent.core/CLAUDE.md#changing-the-llm)
