# LouisAgent.Mcp — design

> **Kind:** host (a library, serving stdio or ASP.NET Core, Streamable HTTP) · **Path:** `src/hosts/LouisAgent.Mcp/` ·
> **References:** LouisAgent.Core (and through it the composition), ASP.NET Core, the ModelContextProtocol SDK and
> its ASP.NET Core part · **Docker service:** `mcp-server`
> **Also shows:** `component`

![The MCP server, and its parts: the tools in sync, the feeds as resources, and its two transports](diagrams/LouisAgent.Mcp.svg)

## Purpose

This host hands louis-agent's tools, and the packs' data feeds, to any
[Model Context Protocol](https://modelcontextprotocol.io) client: Claude Desktop, Rider, or an agent built on another
SDK.

The thing to know is that it has **no model of its own**. The client's model decides which tool to call, one call at
a time. If you want louis-agent to do a whole task by itself, use the API's sessions instead
([which interface to use](../AGENT_COMMUNICATION.md#which-interface-to-use)).

The rest of this doc follows the server from start-up: how it starts without a model, the two ways a client reaches
it, the tools it keeps in step, the feeds it serves as resources, the key over HTTP, and what it never offers.

## How it works

![The MCP server: from a client's call to the engine's tools, and the notice back when they change](diagrams/LouisAgent.Mcp-how-it-works.svg)

### 1. Starting up without a model

It starts with `AgentHost.BuildToolHost()`. That gives it the same engine and skills as every other host. The only difference is the chat client: it's a stand-in that refuses to be called, since the model is
the client's.

### 2. Stdio or HTTP

`MCP_TRANSPORT` picks how clients reach it. `stdio` is the default: the client starts the server as a child process
and talks to it over its input and output. `http` is Streamable HTTP, through `MapMcp()`, for clients on another
machine. In the container that's port 8080, which shows up as `127.0.0.1:${MCP_PORT:-5090}` on your PC.

The setting is read once, as the `McpTransport` enum (`McpTransportSetting`, in `Transport/`). Any other value stops
the start-up.

*Decided in:* CL57.

### 3. The tools, kept in step

`EngineToolSync` copies `AgentEngine.Tools` into the MCP tool collection, and keeps the two in step. So when a
skill is approved or the skills are reloaded, `UseSkill`'s list of skills changes, and clients get told with
`notifications/tools/list_changed`.

`SkillsToolProvider` adds one read-only tool of its own, `GetSkillDocumentation`, which hands a client the
guidance the agent itself reads. The skills the agent keeps reach a client through the engine's `UseSkill`, published
with the other tools: its description lists the approved skills, and a call reads one by name or runs it. MCP has a
standard of its own for skills, SEP-2640, which serves a skill's files as `skill://` resources with `skills/list` and
`skills/get`; the SDK this host is on (2.2.0) doesn't have it.

### 4. The feeds as resources

`FeedResources` turns each feed in the catalogue (`AgentCatalog.Feeds`) into one resource: `feeds://{name}`, with the
feed's description, as `application/json`, read whole when a client asks. They're built once at start-up, and the
SDK answers `resources/list` and `resources/read` itself. `McpPrimitives.Add` sets up the tools and the resources
for both transports.

*Decided in:* F56, [ADR 0072](../adr/0072-feeds-are-mcp-resources.md), [ADR 0070](../adr/0070-packs-publish-data-feeds.md).

### 5. The key, over HTTP

Over HTTP a client needs a key: `MCP_API_KEY`, or `AGENT_API_KEY` when that's not set. It's sent as `x-api-key` or
`Authorization: Bearer`, and compared in constant time by core's `HostAuth`, the same rule the API uses.

Without a key, the server refuses to start. The one exception is `AGENT_AUTH=off`, which says a machine may run it
open on purpose; then keep it on `127.0.0.1`. Over stdio there's no key, because the client started the server
itself as its own child process.

*Decided in:* FX18.

### 6. Approval stays with you

Approving the agent's own tools is never offered over MCP. That stays with the user.

Here's a client using it:

![A client using the tools: it lists and calls the engine's tools through the SDK's server, hears when they change, and reads a feed](diagrams/LouisAgent.Mcp-diagrams.svg)

These are the types involved:

![The types: the host adds the engine's tools kept in step, the feeds as resources and the skills as tools](diagrams/LouisAgent.Mcp-interface.svg)

## Structure

| File | What's in it |
|---|---|
| `McpHost.cs` | The one call a program makes: transport choice, `RunStdioAsync`, `RunHttpAsync` (with the key middleware) |
| `McpPrimitives.cs` | What both transports publish, in one call: the engine's tools and the feeds |
| `Tools/` | `EngineToolSync` (engine tools → MCP tools, kept in sync), `SkillsToolProvider` (the agent's guidance, `GetSkillDocumentation`) |
| `Resources/` | `FeedResources` (feeds → resources), `ResourceScheme` (`feeds`) |
| `Transport/` | `McpTransport` (stdio or HTTP), `McpTransportSetting` (`MCP_TRANSPORT`, read once) |
| `Messages/` | The errors, the log lines and the words a client reads (ADR 0009) |

## Configuration

It runs in its program, in the `mcp-server` container, over stdio or on the PC's own address.

![Where the MCP server runs: a client reaches its container over stdio or on 127.0.0.1:5090, and it reaches the collector and its folders](diagrams/LouisAgent.Mcp-deployment.svg)

It uses core's settings, without any `LLM_*`, plus `MCP_TRANSPORT`, `MCP_PORT`, `MCP_API_KEY` and `AGENT_AUTH`. The
entry for Rider as a client is `demos/louis-agent/config/rider/rider-mcp-servers.example.json`.

**In stdio mode, stdout carries the protocol**, so logging goes to stderr only.

## Extending it

The steps are in [Change the MCP server](../guides/change-the-mcp-server.md). In short: the engine's tools show up
by themselves. A tool only for MCP is an `[McpServerToolType]` class, registered in both transports.

## Tests

The tests are in `src/tests/hosts/LouisAgent.Mcp.Tests`. They run the SDK's real client against the server's setup,
over two in-memory pipes, and check the feeds as resources with the tools beside them. The key rule is core's
`HostAuth`, tested in `HostAuthTests`, and the tools it publishes are tested where they live. To try it by hand,
[Change the MCP server](../guides/change-the-mcp-server.md#check-it-with-a-client) shows how: over HTTP, no key
gives 401 and the right key 200.

## Limits and plans

`AGENT_AUTH=off` runs the HTTP transport with no key at all, so it's for local use only. Without it, a missing key
stops the server before it listens, so it can't be left open by accident.

It publishes every toolset the instance can load. A client can't choose only some, the way an API session names its
toolsets.

## Related docs

[How the agents and endpoints talk § MCP](../AGENT_COMMUNICATION.md#mcp-clients-and-the-mcp-server) ·
[Architecture § MCP server](../architecture/05-building-block-view.md#mcp-server) · [Setup](../SETUP.md)

---
[Projects](README.md) · Previous: [LouisAgent.Web](LouisAgent.Web.md) · Next: [LouisAgent.Orchestrator](../../../demos/louis-agent/docs/projects/LouisAgent.Orchestrator.md)
