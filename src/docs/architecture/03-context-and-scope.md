# 3. Context and scope

> **Explanation.** Who and what louis-agent talks to, at [C4](https://c4model.com) level 1 (the system and its
> neighbours). **Elsewhere:** every interface in detail (endpoints, the event stream, ACP, MCP, agent to agent) in
> [AGENT_COMMUNICATION.md](../AGENT_COMMUNICATION.md); what's inside the system in
> [building blocks](05-building-block-view.md).

![Context and scope: louis-agent, the people who use it, the systems that drive it and what it calls](diagrams/03-context-and-scope.svg)

This is louis-agent seen from outside, as one box. People use it, a few systems drive it, and it calls out to the
model, the web and the rest. The picture shows who sits on which side, and the table says what crosses the line
between them.

![Context and scope: louis-agent, its people and the systems around it](diagrams/03-context-and-scope-3-context-and-scope.svg)

| Neighbour | What crosses the boundary |
|---|---|
| **Developer** (web app, CLI, Rider) | Messages and attachments in; streamed thinking, text, tool calls and usage out |
| **Puls user** | Chat in Afrikaans, a time zone, a location only if shared; answers with the subagents' work shown |
| **Site visitor** | A chat with the guide or a showroom's business, with no sign-in, within a daily cap |
| **Model provider** | Every request: system prompt, tools, history; replies with usage. The only place a prompt leaves the machine |
| **Repositories** | The workspace the tools read and write, inside the workspace boundary, never secret files |
| **Web** | Web search (SearXNG, Google, Tavily or DuckDuckGo), the web pack's `FetchUrl` (SSRF-guarded), exchange rates (Frankfurter), a shop's branches (OpenStreetMap) |
| **Mail service** | A lead for the owner, sent through Resend when its key is set |
| **Paymo, Azure DevOps** | Time entries and work items, only when their API keys are set |
| **Orchestrator** | Fix requests as API sessions; branches and a `FIX-RESULT` back |
| **MCP clients** | The tools, one call at a time; the client brings its own model |

**Out of scope:** durable sessions, which live in memory and end with a restart, and a real error tracker. See
[risks and technical debt](11-risks-and-technical-debt.md).

---
[Architecture](README.md) · Previous: [Constraints](02-constraints.md) · Next: [Solution strategy](04-solution-strategy.md)
