# louis-agent.core.anthropic — design

> **Kind:** library (`net10.0`) · **Path:** `src/engine/louis-agent.core.anthropic/` · **References:** louis-agent.core,
> Anthropic (the SDK) · **Used by:** every program whose agent may talk to Claude: the demos' hosts, the eval runners,
> the orchestrator, the usage probe
> **Also shows:** `component`

![Claude's provider, and its parts: the client, the usage mapper and prompt caching](diagrams/louis-agent.core.anthropic.svg)

## Purpose

This project lets the agent talk to Claude. It holds three things: the provider strategy that builds the Claude
client from the settings, the usage mapper that reads Anthropic's cache writes, and Claude's two prompt-cache
markers.

Core ships no model SDK of its own. So a program that wants Claude hands `AnthropicProvider.Instance` to
`AgentHost.Build`, inside its `HostServices`. A host that only runs a local model doesn't ship any of this.

*Decided in:* CL57, [ADR 0092](../adr/0092-a-project-holds-what-is-reused-together.md).

## How it works

Here's how a client gets made.

![A client: refused without a key, the SDK's client built, wrapped for a fixed thinking budget on older models, and for caching by the factory](diagrams/louis-agent.core.anthropic-flow.svg)

`AnthropicProvider` is core's `ILlmProvider` seam made real, and there's only ever one of it. It answers four
questions for core, one at a time.

### 1. Its name

Its name is `anthropic`. That's what `LLM_PROVIDER` picks it by.

### 2. The client

When core asks for a client with `Create(options)`, it first checks there's a key, in `ANTHROPIC_API_KEY` or
`LLM_API_KEY`. Without one it refuses. With one, it builds the SDK's client, using the endpoint and workspace the
settings give.

Claude models before 4.6 need one more thing. They reject adaptive thinking and want a fixed `budget_tokens`
instead. So for those, the client is wrapped with `ThinkingBudget`, which swaps one for the other.

### 3. Reading usage

`CreateUsageMapper()` gives back `AnthropicUsageMapper`. Anthropic reports cache writes outside the standard usage
fields, and splits them by how long they're kept, on the SDK's own usage object. The mapper reads them from there.

### 4. Prompt caching

`CreatePromptCaching(options)` gives back `AnthropicPromptCaching`, with the lifetime `LLM_CACHE_TTL` names for the
prompt's start. It places two markers: one at the end of the system prompt, and the automatic one the API moves
forward through the conversation. The engine's `PromptCachingChatClient` applies them on every request.

*Decided in:* F4.

### 5. Where the factory comes in

`LlmClientFactory`, in core, is the registry that ties this together. It finds the provider by its name, wraps the
client for caching, and asks the provider for its mapper and its markers. If a program handed it no providers at
all, it says every name is unknown.

![Claude's provider: from a program through the client factory to the SDK and its parts](diagrams/louis-agent.core.anthropic-diagrams.svg)

![Making Claude's client: the factory asks the provider for the SDK's client, then wraps the cache markers round it](diagrams/louis-agent.core.anthropic-diagrams-2.svg)

These are the types involved:

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

You pick it with `LLM_PROVIDER=anthropic`. The rest are `LLM_MODEL`, `ANTHROPIC_API_KEY` (or `LLM_API_KEY`),
`LLM_ENDPOINT`, `ANTHROPIC_WORKSPACE_ID`, `LLM_CACHE`, `LLM_CACHE_TTL` and `LLM_THINKING`. Core's `LlmOptions` reads
them all, and picks this provider by default for any model whose name starts with `claude`.

## Tests

The tests are in `src/tests/engine/louis-agent.core.anthropic.tests`, and they need no network. They check that the
provider answers to its setting's name, refuses without a key, and gives its own mapper and its markers with the
lifetime named, or no markers when caching is off. They check the thinking-budget swap and which models get it.
And on a stub handler, they check the markers as they go over the wire and the cache-write lifetimes.

## Limits and plans

Another provider that speaks Anthropic's API, such as Bedrock or Vertex, would be a second class here, on the same
seam.

## Related docs

[louis-agent.core](louis-agent.core.md) · [louis-agent.core.ollama](louis-agent.core.ollama.md) ·
[louis-agent.core.openai](louis-agent.core.openai.md) · [ADR 0003](../adr/0003-provider-strategies.md) ·
[ADR 0092](../adr/0092-a-project-holds-what-is-reused-together.md)

---
[Docs index](../README.md) · Related: [Projects](README.md) · [Plans](../plans/README.md)
