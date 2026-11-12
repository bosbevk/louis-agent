# louis-agent.core.anthropic — design

> **Kind:** Class library (`net10.0`) · **Path:** `src/engine/louis-agent.core.anthropic/` · **References:** louis-agent.core,
> Anthropic (the SDK) · **Used by:** every program whose agent may talk to Claude: the demos' hosts, the eval runners,
> the orchestrator, the usage probe

![Claude's provider, and its parts: the client, the usage mapper and prompt caching](diagrams/louis-agent.core.anthropic.svg)

## Purpose

Claude on core (CL57, [ADR 0092](../adr/0092-a-project-holds-what-is-reused-together.md)): the provider strategy
that builds the Claude client from the settings, the usage mapper that reads Anthropic's cache writes, and its two
prompt-cache markers. A program hands `AnthropicProvider.Instance` to `AgentHost.Build` in its `HostServices`; a
host on a local model ships none of this, and core itself ships no SDK.

## How it works

`AnthropicProvider` is core's `ILlmProvider` seam made real, a singleton:

- **`Name`** is `anthropic`, what `LLM_PROVIDER` picks it by.
- **`Create(options)`** refuses settings without a key (`ANTHROPIC_API_KEY` or `LLM_API_KEY`), builds the SDK's
  client with the endpoint and workspace the settings give, and wraps it with `ThinkingBudget` for Claude models
  before 4.6, which reject adaptive thinking and take a fixed `budget_tokens` instead.
- **`CreateUsageMapper()`** is `AnthropicUsageMapper`: Anthropic reports cache writes outside the standard fields, and
  splits them by lifetime on the SDK's own usage object.
- **`CreatePromptCaching(options)`** is `AnthropicPromptCaching` with the prefix lifetime `LLM_CACHE_TTL` names: the
  marker at the end of the system prompt and the automatic one the API moves forward, which the engine's
  `PromptCachingChatClient` applies on every request (F4).

`LlmClientFactory` in core is the registry: it finds the provider by name, wraps the client for caching, and asks the
provider for its mapper and its markers; with no providers it says every name is unknown.

## Diagrams

![Claude's provider: from a program through the client factory to the SDK and its parts](diagrams/louis-agent.core.anthropic-diagrams.svg)

![Making Claude's client: the factory asks the provider for the SDK's client, then wraps the cache markers round it](diagrams/louis-agent.core.anthropic-diagrams-2.svg)

![The types: AnthropicProvider makes core's three seams real, the client, the usage mapper and the cache marks](diagrams/louis-agent.core.anthropic-interface.svg)

## Structure

```
src/engine/louis-agent.core.anthropic/
├── AnthropicProvider.cs        # ILlmProvider made real: the client, the mapper, the markers
├── AnthropicUsageMapper.cs     # cache writes, split by lifetime, from the SDK's usage
├── AnthropicPromptCaching.cs   # the two cache markers (F4)
├── ThinkingBudget.cs           # the budget swap for Claude models before 4.6
└── Messages/AnthropicErrorMessage.cs
src/tests/engine/louis-agent.core.anthropic.tests/   # the provider, the budget, the markers on the wire, the mapper, the lifetimes
```

## Configuration

`LLM_PROVIDER=anthropic`, `LLM_MODEL`, `ANTHROPIC_API_KEY` (or `LLM_API_KEY`), `LLM_ENDPOINT`, `ANTHROPIC_WORKSPACE_ID`,
`LLM_CACHE`, `LLM_CACHE_TTL`, `LLM_THINKING`; read by core's `LlmOptions`, whose defaults name this provider for a
`claude*` model.

## Tests

`src/tests/engine/louis-agent.core.anthropic.tests`: the provider is named for the setting, refuses without a key,
gives its own mapper and its markers with the lifetime named, and none when caching is off; the budget swap and who
gets it; the markers on the wire and the cache-write lifetimes, on a stub handler. No network.

## What the build decided (CL57, 2026-11-07, unattended)

- The thinking-budget swap became its own class, `ThinkingBudget`, so the provider stays under the size limit and
  the swap can be tested without a client.
- The SDK's `Usage` type is aliased inside the library: its namespace `Anthropic` and core's `Usage` folder would
  otherwise shadow each other.

## Limits and plans

A second Anthropic-shaped provider (Bedrock, Vertex) is a second class here on the same seam.

## Related docs

[louis-agent.core](louis-agent.core.md) · [louis-agent.core.ollama](louis-agent.core.ollama.md) ·
[louis-agent.core.openai](louis-agent.core.openai.md) · [ADR 0003](../adr/0003-provider-strategies.md) ·
[ADR 0092](../adr/0092-a-project-holds-what-is-reused-together.md)

---
[Docs index](../README.md) · Related: [Projects](README.md) · [Plans](../plans/README.md)
