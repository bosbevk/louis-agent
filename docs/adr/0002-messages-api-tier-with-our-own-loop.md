# 0002. The Messages API tier, with our own loop behind `IChatClient`

**Status:** Accepted · **Date:** 2026-10-06 · **Recorded:** 2026-10-14

## Context

There are four ways to build on Claude, from most managed to least:

| Tier | Who runs the loop | Who hosts the tools | Ties us to Claude |
|---|---|---|---|
| Managed Agents | Anthropic | Anthropic | Yes |
| Claude Agent SDK (Claude Code as a library) | The SDK | Us | Yes |
| Tool Runner (`client.beta.messages.tool_runner`) | The SDK | Us | Yes |
| Messages API (`POST /v1/messages`) | Us | Us | No |

louis-agent runs its tools in our own Docker containers, against mounted repositories and scoped DevOps and Paymo
credentials, with its own approval flow and secret-file blocking. It must also run on Ollama and OpenAI-compatible
servers, not only Claude.

## Decision

We call the Messages API tier through Microsoft.Extensions.AI's `IChatClient`, with our own loop in `AgentEngine`
(`FunctionInvokingChatClient` runs the tool rounds). `LlmClientFactory` turns `LLM_PROVIDER` into the client; nothing
else knows which provider it is.

## Consequences

- The provider is configuration: Anthropic, Ollama or an OpenAI-compatible server, with no change to the engine or the
  tools.
- We own what the higher tiers would give us: the loop, sessions, approvals, context guards, caching markers. Each is
  ours to build, test and measure.
- Provider-specific features (prompt caching, thinking, usage details) need a place outside the engine
  ([0003](0003-provider-strategies.md)).

## Links

[Architecture: Anthropic integration tiers](../ARCHITECTURE.md#anthropic-integration-tiers) · [Models](../MODELS.md)
