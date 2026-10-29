# louis-agent.mcp-server — design

> **Kind:** console app (stdio) or ASP.NET Core (Streamable HTTP) · **Path:** `src/louis-agent.mcp-server/` ·
> **References:** louis-agent.core, the ModelContextProtocol SDK · **Docker service:** `mcp-server`

## Purpose

Hands louis-agent's tools to any [Model Context Protocol](https://modelcontextprotocol.io) client — Claude Desktop, Rider,
or an agent built on another SDK. It has **no model of its own**: the client's model decides which tool to call, one
call at a time. (To have louis-agent do a whole task itself, use the API's sessions instead —
[which interface to use](../AGENT_COMMUNICATION.md#which-interface-to-use).)

## How it works

```mermaid
flowchart LR
    Client["MCP client<br/>(its own model)"] -- "tools/list, tools/call" --> Server["MCP server<br/>(stdio or Streamable HTTP)"]
    Server --> Sync["EngineToolSync<br/>engine tools → MCP tools"]
    Sync --> Engine["AgentEngine<br/>(BuildToolHost: no model)"]
    Server --> Skills["SkillsToolProvider<br/>list_skills · get_skill_info · get_skill_documentation"]
    Engine -- "ToolsChanged" --> Sync -- "notifications/tools/list_changed" --> Client
```

- **Start-up:** `AgentHost.BuildToolHost()` — the same engine, skills and agent-built tools as the other hosts, with a
  stand-in chat client that refuses to be called.
- **Transport** by `MCP_TRANSPORT`: `stdio` (default; the client starts the server as a subprocess) or `http` (Streamable
  HTTP via `MapMcp()`, for remote clients, port 8080 in the container → `127.0.0.1:${MCP_PORT:-5090}`).
- **Tools:** `EngineToolSync` mirrors `AgentEngine.Tools` into the MCP tool collection and keeps it in sync: when the
  agent builds or approves a tool, clients get `notifications/tools/list_changed`. `SkillsToolProvider` adds three
  read-only skill tools; skills themselves run through the engine's `ExecuteSkill`.
- **Auth (HTTP only):** `MCP_API_KEY` (falls back to `AGENT_API_KEY`) as `x-api-key` or `Authorization: Bearer`,
  compared in constant time by core's `HostAuth`, the API's rule too. Without a key it refuses to start (FX18), unless
  `AGENT_AUTH=off` says a machine may run it open on purpose — then keep it on `127.0.0.1`. Over stdio no key: the
  client's child process.
- **Approval stays with the user:** approving agent-built tools is not exposed over MCP.

## Structure

| File | What's in it |
|---|---|
| `Program.cs` | Transport choice, `RunStdioAsync`, `RunHttpAsync` (with the key middleware), `EngineToolSync`, `SkillsToolProvider` |

## Configuration

Core settings (no `LLM_*` needed), plus `MCP_TRANSPORT`, `MCP_PORT`, `MCP_API_KEY`, `AGENT_AUTH`. Client entry for Rider:
`config/rider/rider-mcp-servers.example.json`.

**In stdio mode stdout carries the protocol**: logging goes to stderr only.

## Extending it

The steps are in [Change the MCP server](../guides/change-the-mcp-server.md). In short: engine tools appear by
themselves; an MCP-only tool is an `[McpServerToolType]` class registered in both transports.

## Tests

None for the server itself; its auth rule is core's `HostAuth` (`HostAuthTests`), and the tools it publishes are tested
in `tests/louis-agent.core.tests`. Checked with an MCP client and, for HTTP, with curl (no key → 401, right key → 200).

## Limits and plans

- `AGENT_AUTH=off` runs the HTTP transport unauthenticated, for local use only (FX18 closed the accidental case).
- It publishes every toolset the instance can load; choosing some, as API sessions do
  ([F5](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F05-toolset-profiles.md)), would need a setting or a per-client profile.

## Related docs

[How the agents and endpoints talk § MCP](../AGENT_COMMUNICATION.md#mcp-clients-and-the-mcp-server) ·
[Architecture § MCP server](../architecture/05-building-block-view.md#mcp-server) · [Setup](../SETUP.md)

---
[Projects](README.md) · Previous: [louis-agent.web.shared](louis-agent.web.shared.md) · Next: [louis-agent.orchestrator](louis-agent.orchestrator.md)
