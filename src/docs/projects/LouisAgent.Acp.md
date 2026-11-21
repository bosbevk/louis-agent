# LouisAgent.Acp — design

> **Kind:** host (a library, JSON-RPC over stdio) · **Path:** `src/hosts/LouisAgent.Acp/` · **References:**
> LouisAgent.Core · **Docker service:** `acp-server`, one container per Rider chat
> **Also shows:** `component`

![Rider's chat host, and its parts: the protocol, sessions and files, and Rider's own tools](diagrams/LouisAgent.Acp.svg)

## Purpose

This host puts louis-agent inside JetBrains Rider's AI chat, using the
[Agent Client Protocol](https://agentclientprotocol.com). Rider sends your prompts, with any files you attached, and
the server streams back the model's thinking, its text, and a card for each tool call.

The rest of this doc follows one prompt: the messages on the wire, the session it belongs to, how the prompt is
built from what you attached, how your files are found inside the container, Rider's own tools, the cards, the
commands and the cost line, and what the log keeps.

## How it works

![One prompt from Rider: the server starts a session, streams the engine's updates back, and ends with the prompt's result](diagrams/LouisAgent.Acp-how-it-works.svg)

### 1. Messages, one per line

Rider and the server talk over stdin and stdout, one JSON-RPC message per line. Requests run side by side. A
`session/cancel` is handled straight away, and cancels that session's turn.

### 2. Sessions

A session (`AcpSession`) is a working directory plus a history. Sessions live in memory for as long as the container
does. Rider starts a new container for each new chat (`docker compose run --rm -T acp-server`), so a chat and its
container begin and end together.

### 3. Building the prompt

The model's prompt is built from the blocks Rider sends. A `text` block is used as it is. An embedded `resource`
block is put inline. A `resource_link` is read from disk, but **only when you attached it**. The file that just
happens to be open in the editor isn't sent. Attachments are capped at 100,000 characters.

### 4. Finding your files

Rider sends Windows paths, and the server lives in a Linux container. `ACP_MOUNT_MAPPINGS` maps one to the other,
like `C:\Users\you\RiderProjects=/repositories`. If no mapping applies, the server looks for the file by its last
path parts under the two folders a container mounts: `/workspace`, the main solution, and `/repositories`, your other
repositories. A file that still can't be found is passed to the model as "could not be read — not inside a
mounted folder". It's never guessed.

### 5. Rider's own tools

Rider runs an MCP server with tools of its own. When the host starts, `RiderToolDiscovery` asks it for those tools in
the background, and registers each one on the engine as `rider_{name}`, its description marked `[Rider IDE]`. `RiderOptions` reads
where Rider's server is, `RIDER_MCP_ENDPOINT`, whether to ask it at all, `RIDER_MCP_AUTO_DISCOVER` (on unless it's
`false`), and the path Rider knows the project by, `RIDER_MCP_PROJECT_PATH`.

This lives in the ACP host, not in core, because only Rider's chat needs it. The coding-acp program hands it in
through `HostServices.Discovery`, `AgentHost.Build` starts it, and no other host asks Rider for anything.

*Decided in:* CL57.

### 6. Tool cards

Each tool call shows up in Rider as a card. It starts as a `tool_call`, in progress, and ends with a
`tool_call_update`, completed or failed, with a preview of the output. The card's kind is guessed from how the
tool's name starts, so Rider can show a fitting icon: `Read`, `Get`, `List` and `Show` are read, `Search` and `Find`
are search, `Write`, `Create` and `Update` are edit, `Fetch` is fetch, and a shell or `DotNet` tool is execute.
Anything else is other.

### 7. Commands, and usage

`/tools`, `/approve`, `/reject` and `/usage` are answered by the engine without the model.

Every turn's usage line goes to stderr, as `[ACP] Usage: …`, which lands in Rider's `idea.log`. With
`USAGE_IN_CHAT=true`, it's also added to the chat after the answer, in italics, as its own message chunk. That's off
by default, because in Rider it becomes part of the conversation. A cancelled or failed turn still counts toward the
session, but adds nothing to the chat.

*Decided in:* F3.

### 8. What the log shows of a prompt

What a developer types is personal data once the host runs for anyone else. So the log doesn't show it. The raw
parameters and the prompt's first 300 characters are logged as a keyed hash and a length, like
`Prompt: 412 chars: #3f9c0a1b2d4e (412)`. On your own machine, `LOG_CONTENT=full` shows the text. The protocol on
stdout doesn't change either way. Paths to a developer's own files do stay in the log; those are out of scope.

*Decided in:* F18 step 4f.

Here are the pieces:

![Rider's chat: from AcpHost through the prompt handler to Rider, and back](diagrams/LouisAgent.Acp-diagrams.svg)

These are the types involved:

![The types: the server holds a session per chat, a handler builds each prompt and relays the engine's turn, and Rider's tools are found over MCP](diagrams/LouisAgent.Acp-interface.svg)

## Structure

| File | What's in it |
|---|---|
| `AcpHost.cs` | The one call a program makes: takes the catalogue, then `AgentHost.Build` and `new AcpServer(engine).RunAsync()` |
| `AcpServer.cs` | The message loop and the dispatch (S, CL15: the flow only); its reader and writer are constructor arguments (stdin and stdout in Rider, a script and a buffer in the transcript test) |
| `Protocol/` | `JsonRpcWriter` (one JSON-RPC message per line on stdout, under a lock), `AcpCapabilities` (the `initialize` reply) |
| `Prompting/PromptBuilder.cs` | The model's prompt from the content blocks: text, embedded resources, attached files read and capped, the file open in the IDE left out |
| `Paths/` | `ContainerPaths` (a Windows path from the IDE as the file in the container: `ACP_MOUNT_MAPPINGS`, the mount roots, the drive letter), `GitRoots` (the repository the active file is in) |
| `Turns/` | `PromptHandler` (one prompt's turn: the git root, a command or the model, the usage scope, the stop reason), `TurnRelay` (thinking, text and tool cards as `session/update`), `ToolCards` (a card's kind and output preview), `AcpSession` |
| `Rider/` | `RiderToolDiscovery` (Rider's tools found and registered), `RiderMcpClient` (the MCP call to Rider), `RiderOptions` (the `RIDER_MCP_*` settings), `DiscoveredTool` |
| `Messages/` | The log lines, the words the model reads about an attachment, and the words the person sees (ADR 0009) |

## Tests

The tests are in `src/tests/hosts/LouisAgent.Acp.Tests`. `AcpTranscriptTests` plays a recorded conversation and
pins everything the server writes to `AcpTranscript.json`, along with the prompt the model received. The pure parts
have tests of their own: `ToolCards`, `PromptBuilder`, `ContainerPaths`, `GitRoots` and `JsonRpcWriter`
([tests](tests.md#louis-agentacptests)).

## Configuration

Rider starts a fresh `acp-server` container for each chat.

![Where the ACP server runs: Rider starts a container a chat and speaks JSON-RPC to it, and the server asks Rider's MCP server for its tools](diagrams/LouisAgent.Acp-deployment.svg)

It uses core's settings, plus `ACP_MOUNT_MAPPINGS`, `USAGE_IN_CHAT` and the three `RIDER_MCP_*` settings. Rider's
agent-server entry is `demos/louis-agent/config/rider/rider-acp.example.json`.

**stdout carries the protocol.** Anything written to `Console.Out` corrupts it, so all logging goes to stderr. That
holds when the server can't start too: the reason goes to the log on stderr, and the process exits with code 1.

## Extending it

The steps are in [Change the ACP server](../guides/change-the-acp-server.md). In short: a new method is a `case` in
`AcpServer.HandleRequestAsync`, with its handler in its concern's folder. A new kind of update is a case in
`TurnRelay`. The server only ever translates.

## Limits and plans

Sessions end when the container does, and a chat can't be picked back up. The
[session architecture](../specs/SESSION_ARCHITECTURE.md) spec describes how keeping them would work.

## Related docs

[How the agents and endpoints talk § Rider](../AGENT_COMMUNICATION.md#rider-and-the-acp-server) ·
[Quick start § Rider](../../../demos/louis-agent/docs/QUICK_START_AGENT.md) · [Setup](../SETUP.md)

---
[Projects](README.md) · Previous: [LouisAgent.Cli](LouisAgent.Cli.md) · Next: [LouisAgent.Api](LouisAgent.Api.md)
