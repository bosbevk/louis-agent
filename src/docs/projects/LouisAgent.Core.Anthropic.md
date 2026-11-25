# LouisAgent.Core.Anthropic — design

> **Kind:** library (`net10.0`) · **Path:** `src/engine/LouisAgent.Core.Anthropic/` · **References:** LouisAgent.Core,
> Anthropic (the SDK) · **Used by:** every program whose agent may talk to Claude: the demos' hosts, the eval runners,
> the orchestrator, the usage probe
> **Also shows:** `component`

![Claude's provider, and its parts: the client, the usage mapper and prompt caching](diagrams/LouisAgent.Core.Anthropic.svg)

## Purpose

This project lets the agent talk to Claude. It holds three things: the provider strategy that builds the Claude
client from the settings, the usage mapper that reads Anthropic's cache writes, and Claude's two prompt-cache
markers.

Core ships no model SDK of its own. So a program that wants Claude hands `AnthropicProvider.Instance` to
`AgentHost.Build`, inside its `HostServices`. A host that only runs a local model doesn't ship any of this. The rest
of this doc takes the four questions core asks the provider one at a time, then shows where core's client factory
ties them together.

*Decided in:* CL57, [ADR 0092](../adr/0092-a-project-holds-what-is-reused-together.md).

## How it works

Here's how a client gets made.

![A client: refused without a key, the SDK's client built, wrapped for a fixed thinking budget on older models, and for caching by the factory](diagrams/LouisAgent.Core.Anthropic-flow.svg)

`AnthropicProvider` is core's `ILlmProvider` seam made real, and there's only ever one of it. It answers four
questions for core, one at a time.

### 1. Its name

Its name is `anthropic`. That's what `LLM_PROVIDER` picks it by.

### 2. The client

When core asks for a client with `Create(options)`, it first checks there's a key, in `LLM_API_KEY` or
`ANTHROPIC_API_KEY`. Without one it refuses, with an error that names both. With one, it builds the SDK's client for
the model `LLM_MODEL` names. `LLM_ENDPOINT`, when it's set, replaces the API's address. `ANTHROPIC_WORKSPACE_ID`, when
it's set, goes on every request as the `anthropic-workspace-id` header, because a key that isn't scoped to a workspace
is refused unless the request names one.

Claude models before 4.6 need one more thing. They reject adaptive thinking, which is what the SDK's adapter sends
when the engine asks for reasoning, and want a fixed `budget_tokens` instead. So for those (the Claude 3 models and
the 4.0, 4.1, 4.2 and 4.5 ones), the client is wrapped with `ThinkingBudget`, which swaps one for the other on every
request, one-shot or streamed. The budget follows the effort asked for: 2,048 tokens for low, 8,192 for high, and
4,096 otherwise. Thinking tokens count towards the reply's maximum, so the budget leaves room for the answer and its
tool calls.

### 3. Reading usage

`CreateUsageMapper()` gives back `AnthropicUsageMapper`. Anthropic reports cache writes outside the standard usage
fields, and splits them by how long they're kept, on the SDK's own usage object. The mapper reads them from there.

### 4. Prompt caching

`CreatePromptCaching(options)` gives back `AnthropicPromptCaching`. The API reads a request as the tools, then the
system prompt, then the messages, and caches the exact bytes up to each marker. So it places two markers.

The first goes at the end of the system prompt. It covers the tools and the system prompt, the start every request
and every session shares, and it's kept for the lifetime `LLM_CACHE_TTL` names. The second is the automatic one: the
API puts it on the last block it can cache and moves it forward as the conversation grows, so each round reads the
round before. It's always kept five minutes, because only the next round, seconds later, uses it.

The marker is put on a copy of the last message, never on the history itself. The engine's `PromptCachingChatClient`
applies the markers on every request, and only when `LLM_CACHE` is on: with it off, the factory gives no markers at
all, whoever the provider is.

*Decided in:* F4.

### 5. A resumed chat's thinking

Claude binds each thinking block to everything sent before it, the tools included. When a release changes a tool and
a chat saved before it comes back, its saved thinking no longer matches, and a request carrying it is refused with a
400 that says the block "is bound to a different conversation". Core gives every provider a way to deal with that,
and `CreateStaleThinking(options)` gives Claude's: on the models Anthropic lists as taking it (the Fable, Mythos 5.1
and 5.2 ones, and Opus, Sonnet and Haiku 5.5), `AnthropicDropBlock`; on any other, core's default, which strips the
thinking.

`AnthropicDropBlock` sends the saved turns as they were, and asks on each request of that chat for the API to drop the
blocks that fail instead of refusing: the beta header `thinking-binding-controls-2026-08-01` and
`thinking.block_binding.prefix_mismatch_behavior: "drop_block"`. Those fields aren't typed on the regular Messages
request the adapter builds, so they go through its raw data, the SDK's way of sending a field it doesn't name. Once a
request names its own thinking, the adapter stops adding the turn's effort and how thinking is shown, so the strategy
carries them too. A request with thinking off is left alone, since the API refuses the setting there. The API then
answers, and lists what it dropped as `thinking_dropped` in the reply's `input_transformations`.

### 6. Where the factory comes in

`LlmClientFactory`, in core, is the registry that ties this together. It finds the provider by its name, wraps the
client for caching, and asks the provider for its mapper and its markers. If a program handed it no providers at
all, it says every name is unknown.

![Claude's provider: from a program through the client factory to the SDK and its parts](diagrams/LouisAgent.Core.Anthropic-diagrams.svg)

![Making Claude's client: the factory asks the provider for the SDK's client, then wraps the cache markers round it](diagrams/LouisAgent.Core.Anthropic-diagrams-2.svg)

These are the types involved:

![The types: AnthropicProvider makes core's four seams real, the client, the usage mapper, the cache marks and a resumed chat's thinking](diagrams/LouisAgent.Core.Anthropic-interface.svg)

## Structure

```
src/engine/LouisAgent.Core.Anthropic/
├── AnthropicProvider.cs        # ILlmProvider made real: the client, the mapper, the markers
├── AnthropicUsageMapper.cs     # cache writes, split by lifetime, from the SDK's usage
├── AnthropicPromptCaching.cs   # the two cache markers (F4)
├── Thinking/ThinkingBudget.cs      # the budget swap for Claude models before 4.6
├── Thinking/AnthropicDropBlock.cs  # a resumed chat's thinking: the API asked to drop the blocks that fail
└── Messages/AnthropicErrorMessage.cs
src/tests/engine/LouisAgent.Core.Anthropic.Tests/   # the provider, the budget and drop_block, the markers on the wire, the mapper, the lifetimes
```

## Configuration

You pick it with `LLM_PROVIDER=anthropic`. The rest are `LLM_MODEL`, `ANTHROPIC_API_KEY` (or `LLM_API_KEY`),
`LLM_ENDPOINT`, `ANTHROPIC_WORKSPACE_ID`, `LLM_CACHE`, `LLM_CACHE_TTL` and `LLM_THINKING`. Core's `LlmOptions` reads
them all. When `LLM_PROVIDER` isn't set, it picks this provider for any model whose name starts with `claude`, and when
`LLM_MODEL` isn't set either, the model is `claude-haiku-5-5`.

## Tests

The tests are in `src/tests/engine/LouisAgent.Core.Anthropic.Tests`, and they need no network. They check that the
provider answers to its setting's name, refuses without a key, and gives its own mapper and its markers with the
lifetime named, or no markers when caching is off. They check the thinking-budget swap and which models get it.
And on a stub handler, they check the markers as they go over the wire and the cache-write lifetimes.

## Limits and plans

Only Anthropic's own API is served. Claude through Bedrock or Vertex has no provider here.

## Related docs

[LouisAgent.Core](LouisAgent.Core.md) · [LouisAgent.Core.Ollama](LouisAgent.Core.Ollama.md) ·
[LouisAgent.Core.OpenAi](LouisAgent.Core.OpenAi.md) · [ADR 0003](../adr/0003-provider-strategies.md) ·
[ADR 0092](../adr/0092-a-project-holds-what-is-reused-together.md)

---
[Docs index](../README.md) · Related: [Projects](README.md) · [Plans](../plans/README.md)
