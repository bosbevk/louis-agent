# LouisAgent.Core.Ollama — design

> **Kind:** library (`net10.0`) · **Path:** `src/engine/LouisAgent.Core.Ollama/` · **References:** LouisAgent.Core,
> OllamaSharp · **Used by:** every program whose agent may talk to a local model through Ollama
> **Also shows:** `component`

![Ollama's provider, and its parts: the client and its long timeout](diagrams/LouisAgent.Core.Ollama.svg)

## Purpose

This project lets the agent talk to a model running on your own machine, through Ollama. It's the provider strategy
for Ollama: one class that turns the settings into a client, using the OllamaSharp library.

Core ships no model SDK of its own. So a program that wants a local model hands `OllamaProvider.Instance` to
`AgentHost.Build`, inside its `HostServices`. The rest of this doc follows that client from the settings to the
first reply: how it's made, why it waits so long, and what it adds to usage and caching (nothing).

*Decided in:* CL57, [ADR 0092](../adr/0092-a-project-holds-what-is-reused-together.md).

## How it works

Here's how a client gets made.

![A client: at the endpoint given or Ollama's own port, over an HttpClient with a ten-minute timeout](diagrams/LouisAgent.Core.Ollama-flow.svg)

### 1. Making the client

`OllamaProvider` is core's `ILlmProvider` seam made real, and there's only ever one of it. Its name is `ollama`.

When core asks it for a client with `Create(options)`, it builds OllamaSharp's client over an `HttpClient`. The
address is `LLM_ENDPOINT`, or Ollama's own port on this machine, `http://localhost:11434`, when that's not set.

Only this library knows OllamaSharp. Core asks for a client by the provider's name, and the registry,
`LlmClientFactory`, finds this one and wraps what it builds.

![From a program to OllamaSharp: the program hands the provider in, core's client factory finds it by name, and it builds OllamaSharp's client over the long-timeout HttpClient](diagrams/LouisAgent.Core.Ollama-how-it-works.svg)

### 2. Why the long timeout

The client waits up to ten minutes for a reply. That's on purpose. A small GPU writes about five tokens a second,
so one reply can take minutes, and HttpClient's default of 100 seconds was cancelling replies Ollama was still busy
writing.

### 3. Usage and caching

Ollama reports its token usage in the standard fields, and it has no prompt cache to mark. So this provider adds
nothing there, and the seam's defaults just hold. After the client is made, the provider is out of the picture: the
engine talks to Ollama through OllamaSharp for every reply.

![One reply from a local model: the provider builds the client, then the engine talks to Ollama through OllamaSharp](diagrams/LouisAgent.Core.Ollama-sequence.svg)

These are the types involved:

![The types: OllamaProvider is core's provider seam over OllamaSharp's client](diagrams/LouisAgent.Core.Ollama-interface.svg)

## Structure

```
src/engine/LouisAgent.Core.Ollama/
└── OllamaProvider.cs           # ILlmProvider made real; CreateHttpClient for the timeout and the address
src/tests/engine/LouisAgent.Core.Ollama.Tests/   # the timeout, the address, the seam's defaults
```

## Configuration

You pick it with `LLM_PROVIDER=ollama`, and name the model with `LLM_MODEL`. `LLM_ENDPOINT` says where Ollama is;
when it's not set, that's `http://localhost:11434`.

Core's `LlmOptions` reads these. When `LLM_PROVIDER` isn't set but `LLM_MODEL` is, it picks this provider for any
model whose name doesn't start with `claude`. It also knows which local models can't take tools: on Ollama, a model
whose name starts with `llama2`, `codellama`, `deepseek-r1`, `deepseek-coder` or `qwen2` gets none, and every other
model does. `LLM_SUPPORTS_TOOLS=true` or `false` overrides that guess.

## Tests

The tests are in `src/tests/engine/LouisAgent.Core.Ollama.Tests`. They check that the client waits longer than the
default, that it talks to the endpoint you gave or to localhost, and that the registry gives it the standard usage
mapper and no cache.

## Related docs

[LouisAgent.Core](LouisAgent.Core.md) · [LouisAgent.Core.Anthropic](LouisAgent.Core.Anthropic.md) ·
[LouisAgent.Core.OpenAi](LouisAgent.Core.OpenAi.md) · [ADR 0003](../adr/0003-provider-strategies.md)

---
[Docs index](../README.md) · Related: [Projects](README.md) · [Plans](../plans/README.md)
