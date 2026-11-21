# Change the MCP server

> **How-to.** Change `src/hosts/LouisAgent.Mcp`, which hands the agent's tools to any MCP client: publish a tool or a
> feed, add an MCP-only tool, choose the transport, and check it with a client. **Elsewhere:** how it works in
> [LouisAgent.Mcp](../projects/LouisAgent.Mcp.md); why it has no model in
> [MCP exposes tools, not a model](../../../.claude/rules/design-rules.md).

![The MCP server at a glance: an outside client's model calls the engine's tools one at a time, over stdio or HTTP, and reads the packs' feeds as resources](diagrams/change-the-mcp-server.svg)

The MCP server has no model of its own: an outside client's model calls the agent's tools, one call at a time. It
builds the same engine the other hosts do (`AgentHost.BuildToolHost`) with a chat client that refuses to be called,
and mirrors the engine's tools into MCP.

## Publish a tool

Nothing to do here. Every tool the engine has is published, and `EngineToolSync` keeps the list in step: a pack's tool,
one of core's own, or an agent-built tool once it's approved. Clients are told with
`notifications/tools/list_changed`. So add the tool where it belongs, which is nearly always a pack
([Build a pack](build-a-pack.md), and [Change the engine](change-the-engine.md#add-a-tool) for what holds for any
tool). A pack's tools are already held to MCP's shapes by `PackConvertibilityTests`
([ADR 0028](../adr/0028-packs-stay-convertible-to-mcp.md)).

## Publish a feed

Nothing to do here either. Every feed a pack lists (`IAgentPack.Feeds`, [ADR 0070](../adr/0070-packs-publish-data-feeds.md))
is a resource, `feeds://{name}`, set up by `McpPrimitives.Add` for both transports
([ADR 0072](../adr/0072-feeds-are-mcp-resources.md)). The API serves the same feed at `/v1/feeds/{name}`. Add the feed
in its pack ([Build a pack](build-a-pack.md)).

## Add an MCP-only tool

Some tools only MCP clients need. Today those are the three read-only skill tools, the methods `ListSkills`,
`GetSkillInfo` and `GetSkillDocumentation` on `SkillsToolProvider`. To add one:

1. Put it in a class marked `[McpServerToolType]`, as `SkillsToolProvider` is, each tool a method with
   `[McpServerTool]` and `[Description]`s.
2. Register the class with `.WithTools<YourTools>()` in **both** transports, `RunStdioAsync` and `RunHttpAsync`, or it
   exists in one only.
3. Its texts are `Messages/McpText` and its log lines `McpLogMessage`
   ([ADR 0009](../adr/0009-message-text-as-enum-templates.md)).
4. Never expose a step that is the user's to take: approving agent-built tools or skill files stays in a chat
   ([ADR 0033](../adr/0033-what-the-agent-writes-has-a-scope.md)).

`McpHost.cs` holds the transports and auth only. A new class goes in a file and folder of its own (`Tools/`,
`Resources/`), and a new kind of primitive goes in `McpPrimitives.Add`, so both transports get it.

## Choose the transport

| `MCP_TRANSPORT` | For | Notes |
|---|---|---|
| `stdio` (default) | A client that starts the server as its child process | stdout carries the protocol: log only through `ILogger` (stderr) |
| `http` | A client elsewhere, over Streamable HTTP (`MapMcp()`) | Needs `MCP_API_KEY` (else `AGENT_API_KEY`) or it refuses to start; `127.0.0.1:${MCP_PORT:-5090}` in Docker |

Any other value stops start-up with an error that names the two it takes (`McpTransportSetting`,
[ADR 0024](../adr/0024-choices-are-enums.md)).

## Check it with a client

![Checking it with a client: start the server over HTTP and see a call without the key refused, or add it to a client over stdio and list its tools](diagrams/change-the-mcp-server-flow.svg)

```bash
MCP_TRANSPORT=http docker compose -f demos/louis-agent/docker/docker-compose.yml --env-file demos/louis-agent/config/.env up -d --build mcp-server
curl -s -o /dev/null -w "%{http_code}\n" -X POST http://127.0.0.1:5090/                             # 401 without the key
```

For stdio, add the server to a client from `demos/louis-agent/config/rider/rider-mcp-servers.example.json` and ask it
to list the tools. To try stdio by hand, pipe `initialize`, `notifications/initialized`, `resources/list` and
`resources/read` (with `{"uri":"feeds://shop-prices"}`) as JSON-RPC lines into the built server with
`PACKS=shop-prices`.

In code, `src/tests/hosts/LouisAgent.Mcp.Tests` runs a real client against the server over in-memory pipes
(`InMemoryMcp`). Its auth is core's `HostAuth`, tested in `HostAuthTests`, and the tools are tested where they live.

## Before you ask for a review

- [ ] A new tool lives in core or a pack, unless only MCP clients need it.
- [ ] An MCP-only tool class is registered in both transports, outside `McpHost.cs`.
- [ ] Nothing writes to stdout in stdio mode.

---
[Guides](README.md) · [Docs index](../README.md)
