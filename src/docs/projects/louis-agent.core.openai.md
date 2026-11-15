# louis-agent.core.openai — design

> **Kind:** library (`net10.0`) · **Path:** `src/engine/louis-agent.core.openai/` · **References:** louis-agent.core,
> Microsoft.Extensions.AI.OpenAI (over the OpenAI SDK) · **Used by:** every program whose agent may talk to a server
> that speaks OpenAI's chat API
> **Also shows:** `component`

![The OpenAI-compatible provider, and its parts: your endpoint and the standard usage](diagrams/louis-agent.core.openai.svg)

## Purpose

This project lets the agent talk to any server that speaks OpenAI's chat API, at an address you name. That could be
LM Studio, vLLM, or OpenAI itself. It's the provider strategy for all of them, built on the OpenAI SDK.

Core ships no model SDK of its own. So a program that wants one of these hands
`OpenAiCompatibleProvider.Instance` to `AgentHost.Build`, inside its `HostServices`.

*Decided in:* CL57, [ADR 0092](../adr/0092-a-project-holds-what-is-reused-together.md).

## How it works

Here's how a client gets made.

![A client: refused without an endpoint, built with the key given or a placeholder](diagrams/louis-agent.core.openai-flow.svg)

### 1. Making the client

`OpenAiCompatibleProvider` is core's `ILlmProvider` seam made real, and there's only ever one of it. Its name is
`openai-compatible`.

When core asks it for a client with `Create(options)`, it first checks for `LLM_ENDPOINT`. Without an address
there's nothing to talk to, so it refuses. With one, it builds the OpenAI SDK's client.

### 2. The key, or a placeholder

If you gave a key, the client uses it. If you didn't, it uses a placeholder. That's because a local server often
wants no key at all, but the SDK insists on having something.

![louis-agent.core.openai: How it works](diagrams/louis-agent.core.openai-how-it-works.svg)

### 3. Usage and caching

These servers report token usage in the standard fields, and there's no prompt cache to mark. So this provider
adds nothing there, and the seam's defaults just hold.

![One reply from your endpoint: no endpoint and the provider refuses, with one the engine talks to it through the OpenAI SDK](diagrams/louis-agent.core.openai-sequence.svg)

These are the types involved:

![The types: OpenAiCompatibleProvider is core's provider seam over the OpenAI SDK](diagrams/louis-agent.core.openai-interface.svg)

## Structure

```
src/engine/louis-agent.core.openai/
├── OpenAiCompatibleProvider.cs   # ILlmProvider made real
└── Messages/OpenAiErrorMessage.cs
src/tests/engine/louis-agent.core.openai.tests/   # the endpoint rule, a client without a key, the seam's defaults
```

## Configuration

You pick it with `LLM_PROVIDER=openai-compatible` and name the model with `LLM_MODEL`. `LLM_ENDPOINT` is
required, and `LLM_API_KEY` is optional.

## Tests

The tests are in `src/tests/engine/louis-agent.core.openai.tests`. They check that a missing endpoint is refused,
naming the setting, that an endpoint with no key still builds, and that the registry gives it the standard usage
mapper and no cache.

## Related docs

[louis-agent.core](louis-agent.core.md) · [louis-agent.core.anthropic](louis-agent.core.anthropic.md) ·
[louis-agent.core.ollama](louis-agent.core.ollama.md) · [ADR 0003](../adr/0003-provider-strategies.md)

---
[Docs index](../README.md) · Related: [Projects](README.md) · [Plans](../plans/README.md)
