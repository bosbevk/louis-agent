# 0030. Build or adopt: the loop stays ours on Microsoft.Extensions.AI

**Status:** Accepted · **Date:** 2026-10-19 (decided in the [platform plan](../plans/platform.md)) ·
**Recorded:** 2026-10-19

## Context

No doc said why we build our own agent rather than adopt a framework, and some parts we hand-built already exist as
libraries. Louis: "i want to use my own agent [because] i can". The project's aim is learning to be an AI engineer
([GOAL.md](../GOAL.md)); Puls's edge is local data, a South African voice and the businesses that join, not the loop.

Compared (licences checked 2026-10-19 on each project's own page):

| Option | What it gives | Licence | What it would hide |
|---|---|---|---|
| Microsoft Agent Framework 1.0 | Agents, workflows, checkpoints on Microsoft.Extensions.AI; Semantic Kernel's successor | MIT | Orchestration and state |
| Semantic Kernel | Plugins, planners, connectors; succeeded by Agent Framework | MIT | Tool loop and prompts |
| GitHub Copilot SDK | Copilot's own coding agent in an app; needs a Copilot subscription or your own key | MIT | The whole agent |
| Claude Agent SDK | Claude Code as a library: tools, permissions, sessions, hooks; Python and TypeScript | Anthropic's Commercial Terms | The whole agent; Claude only |
| OpenAI Agents SDK | Agents, handoffs, guardrails; Python | MIT | Loop and handoffs |
| Existo (a peer) | A one-person .NET 10 learning project: its own ReAct loop, five provider adapters, packages per part | MIT (0.1.0 alpha) | Nothing to adopt; compared as a peer |

Existo's facts come from its NuGet page (read 2026-10-19), not its code.

## Decision

- **The loop stays our own, on Microsoft.Extensions.AI** (`IChatClient`, `FunctionInvokingChatClient`;
  [0002](0002-messages-api-tier-with-our-own-loop.md)). We own the agent to learn, and adopting parts of Agent Framework
  later is a step, not a rewrite.
- **We adopt instead of hand-building:** `DistributedCachingChatClient` and `UseOpenTelemetry()` (Phase 3),
  Microsoft.Extensions.AI.Evaluation (Phase 3), `ApprovalRequiredAIFunction` and `ToolApprovalRequestContent` (F7, F41),
  `JsonSchemaExporter` (Phase 6), and the MCP SDK's client (Phase 8).
- **From Existo we take:** packages split per optional part, one DI entry point and OpenTelemetry metrics beside the
  traces (F44); a timeout per tool beside its declared effect (Phase 7); a sandboxed runtime for agent-written skills,
  to weigh in F42.
- **From Existo we don't take:** hand-built provider adapters ([0003](0003-provider-strategies.md)); tools added to a
  running session ([0006](0006-toolsets-per-session.md), [0008](0008-history-is-append-only.md)); history rewritten by
  an LLM summary (F9 summarises, then opens a new window).

## Consequences

- We keep maintaining the loop and its guards, which is the learning.
- When to look again: Phase 6 B4 spikes Agent Framework's checkpoints. A one-day spike of one Puls flow on its
  `ChatClientAgent`, compared on the cook simulation, is an option, not a planned step.

## Links

[Platform plan](../plans/platform.md) (*Where this plan challenges you*, Part 3) ·
[0002](0002-messages-api-tier-with-our-own-loop.md) · [0003](0003-provider-strategies.md) ·
[Microsoft Agent Framework](https://github.com/microsoft/agent-framework) ·
[Semantic Kernel](https://github.com/microsoft/semantic-kernel) · [GitHub Copilot SDK](https://github.com/github/copilot-sdk) ·
[Claude Agent SDK](https://code.claude.com/docs/en/agent-sdk/overview) ·
[OpenAI Agents SDK](https://github.com/openai/openai-agents-python) · [Existo](https://www.nuget.org/packages/Existo/)

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
