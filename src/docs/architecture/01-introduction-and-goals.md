# 1. Introduction and goals

> **Explanation.** What louis-agent is, the qualities that matter most, and who it is for. **Elsewhere:** what the
> project is for as a whole in the [goal](../GOAL.md); using it in the [quick start](../../../demos/louis-agent/docs/QUICK_START_AGENT.md).

![Introduction and goals: a framework in C#, the four agents on it, its four quality goals and who it is for](diagrams/01-introduction-and-goals.svg)

## What it is

louis-agent is an AI agent framework in C#, and the agents built on it. The framework is the engine
(`louis-agent.core`: the tool-call loop, streaming, the usage ledger and the guards), the hosts that serve it, and
packs of tools. Core itself ships no agent and no tool. A few lines of a program's own say who the agent is (an
identity: its voice, its modes, its roles) and what it can do (its packs), and start a host.

The hosts are thin, and each puts the same engine in front of you a different way: Rider's AI chat (ACP), an HTTP API
with a web app, a CLI, and an MCP server. How all of them connect, and what goes over the wire, is in
[AGENT_COMMUNICATION.md](../AGENT_COMMUNICATION.md).

Four kinds of agent run on it today. The coding agent works on a repository. Puls is a general assistant in everyday
South African Afrikaans, which hands each task to a specialist sub-session. The triage team sorts a service's errors.
And the guide answers visitors on [louis-agent.dev](https://louis-agent.dev), the framework's own website, beside the
agents of three made-up businesses in its showroom. A second agent, the orchestrator (a proof of concept), is built on
the same core and asks the coding agent for fixes through the HTTP API.

**The key principle: the model's provider is configuration.** Everything goes through `Microsoft.Extensions.AI`'s
`IChatClient`, so tool calling, streaming and skills work the same on Anthropic, Ollama and OpenAI-compatible
endpoints. Each provider is a small library a program hands in, and `LLM_PROVIDER` and `LLM_MODEL` pick among them.

## Quality goals

When these qualities conflict with others, they win, the most important first. Each one is held by the decision
records in its last column.

| # | Quality | What it means here | Held by |
|---|---|---|---|
| 1 | **Measured cost** | Every model request is recorded with its tokens and cost, and every answer shows it; a change to cost is proven on the ledger | [0007](../adr/0007-measure-every-request.md), [benchmarks](https://github.com/bosbevk/louis-agent/blob/pre-cl10/docs/BENCHMARKS.md) |
| 2 | **Safety** | Model text never becomes a command; tools stay in the workspace and away from secrets; on a PC nothing listens beyond `127.0.0.1`, and a host on the network refuses to start without its key unless `AGENT_AUTH=off` | [0004](../adr/0004-arguments-as-data.md), [0005](../adr/0005-guard-the-context.md) |
| 3 | **Provider portability** | Claude, Ollama or an OpenAI-compatible server, by configuration | [0002](../adr/0002-messages-api-tier-with-our-own-loop.md), [0003](../adr/0003-provider-strategies.md) |
| 4 | **Explainable** | Every design decision can be explained, with what it cost (the project's goal) | [ADRs](../adr/README.md) |

## Stakeholders

These are the people and agents who depend on it, and what each one expects.

| Who | Expects |
|---|---|
| Louis (developer, learning AI engineering) | Each step small, measured and explained; the docs and decisions to stay true |
| People using the coding agent (Rider, web app, CLI) | Answers that stream, show their tools and their cost, and don't touch what they shouldn't |
| Puls's users (South Africa, on mypuls.co.za) | Afrikaans, local context, a cost they can see |
| Visitors to louis-agent.dev | A guide that answers without a sign-in, within a daily cap |
| Another agent (the orchestrator, MCP clients) | A stable API and protocol |
| Claude Code, working on this repo | Rules it can find, in docs small enough to load |

---
[Architecture](README.md) · Next: [Constraints](02-constraints.md)
