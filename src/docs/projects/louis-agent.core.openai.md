# louis-agent.core.openai — design

> **Kind:** Class library (`net10.0`) · **Path:** `src/engine/louis-agent.core.openai/` · **References:** louis-agent.core,
> Microsoft.Extensions.AI.OpenAI (over the OpenAI SDK) · **Used by:** every program whose agent may talk to a server
> that speaks OpenAI's chat API

![The OpenAI-compatible provider, and its parts: your endpoint and the standard usage](diagrams/louis-agent.core.openai.svg)

## Purpose

OpenAI-compatible servers on core (CL57, [ADR 0092](../adr/0092-a-project-holds-what-is-reused-together.md)): the
provider strategy for anything that speaks OpenAI's chat API at an endpoint you name (LM Studio, vLLM, OpenAI
itself). A program hands `OpenAiCompatibleProvider.Instance` to `AgentHost.Build` in its `HostServices`; core itself
ships no SDK.

## How it works

`OpenAiCompatibleProvider` is core's `ILlmProvider` seam made real, a singleton: named `openai-compatible`;
`Create(options)` refuses settings without `LLM_ENDPOINT`, and builds the OpenAI SDK's client with the key given or
a placeholder, since a local server often wants none and the SDK wants something. It reports usage in the standard
fields and marks no cache, so the seam's defaults hold.

![louis-agent.core.openai: How it works](diagrams/louis-agent.core.openai-how-it-works.svg)

## Structure

```
src/engine/louis-agent.core.openai/
├── OpenAiCompatibleProvider.cs   # ILlmProvider made real
└── Messages/OpenAiErrorMessage.cs
src/tests/engine/louis-agent.core.openai.tests/   # the endpoint rule, a client without a key, the seam's defaults
```

## Configuration

`LLM_PROVIDER=openai-compatible`, `LLM_MODEL`, `LLM_ENDPOINT` (required), `LLM_API_KEY` (optional).

## Tests

`src/tests/engine/louis-agent.core.openai.tests`: no endpoint is refused by name, an endpoint and no key builds, and
the registry gives it the standard mapper and no cache.

## Related docs

[louis-agent.core](louis-agent.core.md) · [louis-agent.core.anthropic](louis-agent.core.anthropic.md) ·
[louis-agent.core.ollama](louis-agent.core.ollama.md) · [ADR 0003](../adr/0003-provider-strategies.md)

---
[Docs index](../README.md) · Related: [Projects](README.md) · [Plans](../plans/README.md)
