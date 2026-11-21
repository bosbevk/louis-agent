# LouisAgent.Core.OpenAi — design

> **Kind:** library (`net10.0`) · **Path:** `src/engine/LouisAgent.Core.OpenAi/` · **References:** LouisAgent.Core,
> Microsoft.Extensions.AI.OpenAI (over the OpenAI SDK) · **Used by:** every program whose agent may talk to a server
> that speaks OpenAI's chat API
> **Also shows:** `component`

![The OpenAI-compatible provider, and its parts: your endpoint and the standard usage](diagrams/LouisAgent.Core.OpenAi.svg)

## Purpose

This project lets the agent talk to any server that speaks OpenAI's chat API, at an address you name. That could be
LM Studio, vLLM, or OpenAI itself. It's the provider strategy for all of them, built on the OpenAI SDK.

Core ships no model SDK of its own. So a program that wants one of these hands
`OpenAiCompatibleProvider.Instance` to `AgentHost.Build`, inside its `HostServices`. The rest of this doc follows the
client from the settings to the first reply: how it's made, what it does without a key, and what it adds to usage
and caching (nothing).

*Decided in:* CL57, [ADR 0092](../adr/0092-a-project-holds-what-is-reused-together.md).

## How it works

Here's how a client gets made.

![A client: refused without an endpoint, built with the key given or a placeholder](diagrams/LouisAgent.Core.OpenAi-flow.svg)

### 1. Making the client

`OpenAiCompatibleProvider` is core's `ILlmProvider` seam made real, and there's only ever one of it. Its name is
`openai-compatible`.

When core asks it for a client with `Create(options)`, it first checks for `LLM_ENDPOINT`. Without an address
there's nothing to talk to, so it refuses, with an error that names `LLM_ENDPOINT`. With one, it builds the OpenAI
SDK's client for the model `LLM_MODEL` names.

Only this library knows the OpenAI SDK. Core asks for a client by the provider's name, and the registry,
`LlmClientFactory`, finds this one and wraps what it builds.

![From a program to the OpenAI SDK: the program hands the provider in, core's client factory finds it by name, and it builds the SDK's chat client](diagrams/LouisAgent.Core.OpenAi-how-it-works.svg)

### 2. The key, or a placeholder

If you gave a key, the client uses it. If you didn't, it uses the placeholder `none`. That's because a local server
often wants no key at all, but the SDK insists on having something.

### 3. Usage and caching

These servers report token usage in the standard fields, and there's no prompt cache to mark. So this provider
adds nothing there, and the seam's defaults just hold. After the client is made, the provider is out of the picture:
the engine talks to your endpoint through the OpenAI SDK for every reply.

![One reply from your endpoint: no endpoint and the provider refuses, with one the engine talks to it through the OpenAI SDK](diagrams/LouisAgent.Core.OpenAi-sequence.svg)

These are the types involved:

![The types: OpenAiCompatibleProvider is core's provider seam over the OpenAI SDK](diagrams/LouisAgent.Core.OpenAi-interface.svg)

## Structure

```
src/engine/LouisAgent.Core.OpenAi/
├── OpenAiCompatibleProvider.cs   # ILlmProvider made real
└── Messages/OpenAiErrorMessage.cs
src/tests/engine/LouisAgent.Core.OpenAi.Tests/   # the endpoint rule, a client without a key, the seam's defaults
```

## Configuration

You pick it with `LLM_PROVIDER=openai-compatible` and name the model with `LLM_MODEL`. `LLM_ENDPOINT` is
required, and `LLM_API_KEY` is optional. Core never picks this provider by itself, so `LLM_PROVIDER` has to say it.
Every model here is taken to accept tools, unless `LLM_SUPPORTS_TOOLS=false` says otherwise.

## Tests

The tests are in `src/tests/engine/LouisAgent.Core.OpenAi.Tests`. They check that a missing endpoint is refused,
naming the setting, that an endpoint with no key still builds, and that the registry gives it the standard usage
mapper and no cache.

## Related docs

[LouisAgent.Core](LouisAgent.Core.md) · [LouisAgent.Core.Anthropic](LouisAgent.Core.Anthropic.md) ·
[LouisAgent.Core.Ollama](LouisAgent.Core.Ollama.md) · [ADR 0003](../adr/0003-provider-strategies.md)

---
[Docs index](../README.md) · Related: [Projects](README.md) · [Plans](../plans/README.md)
