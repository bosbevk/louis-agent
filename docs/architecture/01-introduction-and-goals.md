# 1. Introduction and goals

> **Explanation.** What louis-agent is, the qualities that matter most, and who it is for. **Elsewhere:** what the
> project is for as a whole in the [goal](../GOAL.md); using it in the [quick start](../QUICK_START_AGENT.md).

## What it is

Louis Agent is a multi-host LLM agent for working on code. One engine (`louis-agent.core`) holds the tools, skills and
the tool-call loop; several thin hosts put it in front of you: Rider (ACP), a web app and HTTP API, a CLI, and an MCP
server. A second agent, the orchestrator (a proof of concept), is built on the same core and talks to louis-agent
through the HTTP API. How all of them connect, and what goes over the wire, is in
[AGENT_COMMUNICATION.md](../AGENT_COMMUNICATION.md).

**Key principle:** the LLM provider is configuration only (`LLM_PROVIDER`, `LLM_MODEL`). Everything goes through
`Microsoft.Extensions.AI`'s `IChatClient`, so tool calling, streaming and skills work the same on Anthropic, Ollama and
OpenAI-compatible endpoints.

The same core also runs **Puls**, a general assistant in everyday South African Afrikaans that hands each task to a
specialist sub-session.

## Quality goals

The qualities that win when they conflict with others, most important first:

| # | Quality | What it means here | Held by |
|---|---|---|---|
| 1 | **Measured cost** | Every model request is recorded with its tokens and cost, and every answer shows it; a change to cost is proven on the ledger | [0007](../adr/0007-measure-every-request.md), [benchmarks](https://github.com/bosbevk/louis-agent/blob/pre-cl10/docs/BENCHMARKS.md) |
| 2 | **Safety** | Model text never becomes a command; tools stay in the workspace and away from secrets; nothing listens beyond `127.0.0.1` | [0004](../adr/0004-arguments-as-data.md), [0005](../adr/0005-guard-the-context.md) |
| 3 | **Provider portability** | Claude, Ollama or an OpenAI-compatible server, by configuration | [0002](../adr/0002-messages-api-tier-with-our-own-loop.md), [0003](../adr/0003-provider-strategies.md) |
| 4 | **Explainable** | Every design decision can be explained, with what it cost (the project's goal) | [ADRs](../adr/README.md) |

## Stakeholders

| Who | Expects |
|---|---|
| Louis (developer, learning AI engineering) | Each step small, measured and explained; the docs and decisions to stay true |
| People using the agent (Rider, web app, CLI) | Answers that stream, show their tools and their cost, and don't touch what they shouldn't |
| Puls's users (South Africa, a public release aimed at 2027) | Afrikaans, local context, a cost they can see |
| Another agent (the orchestrator, MCP clients) | A stable API and protocol |
| Claude Code, working on this repo | Rules it can find, in docs small enough to load |
