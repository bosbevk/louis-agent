# louis-agent.core.ollama — design

> **Kind:** library (`net10.0`) · **Path:** `src/engine/louis-agent.core.ollama/` · **References:** louis-agent.core,
> OllamaSharp · **Used by:** every program whose agent may talk to a local model through Ollama
> **Also shows:** `component`

![Ollama's provider, and its parts: the client and its long timeout](diagrams/louis-agent.core.ollama.svg)

## Purpose

This project lets the agent talk to a model running on your own machine, through Ollama. It's the provider strategy
for Ollama: one class that turns the settings into a client, using the OllamaSharp library.

Core ships no model SDK of its own. So a program that wants a local model hands `OllamaProvider.Instance` to
`AgentHost.Build`, inside its `HostServices`.

*Decided in:* CL57, [ADR 0092](../adr/0092-a-project-holds-what-is-reused-together.md).

## How it works

Here's how a client gets made.

![A client: at the endpoint given or Ollama's own port, over an HttpClient with a ten-minute timeout](diagrams/louis-agent.core.ollama-flow.svg)

### 1. Making the client

`OllamaProvider` is core's `ILlmProvider` seam made real, and there's only ever one of it. Its name is `ollama`.

When core asks it for a client with `Create(options)`, it builds OllamaSharp's client over an `HttpClient`. The
address is `LLM_ENDPOINT`, or Ollama's own port on this machine when that's not set.

### 2. Why the long timeout

The client waits up to ten minutes for a reply. That's on purpose. A small GPU writes about five tokens a second,
and HttpClient's default of 100 seconds was cancelling replies Ollama was still busy writing.

![louis-agent.core.ollama: How it works](diagrams/louis-agent.core.ollama-how-it-works.svg)

### 3. Usage and caching

Ollama reports its token usage in the standard fields, and it has no prompt cache to mark. So this provider adds
nothing there, and the seam's defaults just hold.

![One reply from a local model: the provider builds the client, then the engine talks to Ollama through OllamaSharp](diagrams/louis-agent.core.ollama-sequence.svg)

These are the types involved:

![The types: OllamaProvider is core's provider seam over OllamaSharp's client](diagrams/louis-agent.core.ollama-interface.svg)

## Structure

```
src/engine/louis-agent.core.ollama/
└── OllamaProvider.cs           # ILlmProvider made real; CreateHttpClient for the timeout and the address
src/tests/engine/louis-agent.core.ollama.tests/   # the timeout, the address, the seam's defaults
```

## Configuration

You pick it with `LLM_PROVIDER=ollama`, and name the model with `LLM_MODEL`. `LLM_ENDPOINT` says where Ollama is;
when it's not set, that's `http://localhost:11434`. Core's `LlmOptions` already picks this provider for any model
whose name doesn't start with `claude`, and it knows which local models can't take tools.

## Tests

The tests are in `src/tests/engine/louis-agent.core.ollama.tests`. They check that the client waits longer than the
default, that it talks to the endpoint you gave or to localhost, and that the registry gives it the standard usage
mapper and no cache.

## Related docs

[louis-agent.core](louis-agent.core.md) · [louis-agent.core.anthropic](louis-agent.core.anthropic.md) ·
[louis-agent.core.openai](louis-agent.core.openai.md) · [ADR 0003](../adr/0003-provider-strategies.md)

---
[Docs index](../README.md) · Related: [Projects](README.md) · [Plans](../plans/README.md)
