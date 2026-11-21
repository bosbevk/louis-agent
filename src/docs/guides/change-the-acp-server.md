# Change the ACP server

> **How-to.** Change the Rider host, `src/hosts/LouisAgent.Acp`: run it in Rider, add a protocol method or a
> `session/update` kind, its texts and its tests. **Elsewhere:** how it works in
> [LouisAgent.Acp](../projects/LouisAgent.Acp.md); the protocol at
> [agentclientprotocol.com](https://agentclientprotocol.com).

![The ACP server: Rider speaks JSON-RPC over stdin and stdout to AcpServer, which hands a turn to AgentEngine and relays its stream back as session/update](diagrams/change-the-acp-server.svg)

The ACP server puts the agent in JetBrains Rider's AI chat. It speaks the Agent Client Protocol, which is JSON-RPC
over stdin and stdout, and it only translates: the prompt into a turn for `AgentEngine`, and the turn's stream into
`session/update` notifications. Behaviour belongs in core.

**stdout is the protocol.** One stray `Console.Write` corrupts it for Rider. Log through `ILogger`, which writes to
stderr ([Messages and logging](../../engine/LouisAgent.Core/CLAUDE.md#messages-and-logging)).

## Run it in Rider

1. **Add the agent server** in Rider's AI Assistant settings, from
   `demos/louis-agent/config/rider/rider-acp.example.json` with your paths
   ([Quick start](../../../demos/louis-agent/docs/QUICK_START_AGENT.md)). It starts
   `docker compose run --rm -T acp-server`, one container per chat.
2. **After a change, rebuild the image and start a New Chat.** A chat keeps its container, and so its old build,
   until then:

   ```bash
   docker compose -f demos/louis-agent/docker/docker-compose.yml --env-file demos/louis-agent/config/.env build acp-server
   ```

3. **To debug without Docker**, use the coding demo's run profile, in
   `demos/louis-agent/hosts/LouisAgent.CodingAcp/Properties/launchSettings.json`.

## Add a protocol method

![Adding a protocol method: the test first in the transcript, a case in HandleRequestAsync, the capability if the client must know, the reply through JsonRpcWriter](diagrams/change-the-acp-server-flow.svg)

1. **Write the test first.** `AcpTranscriptTests` plays a recorded Rider conversation through `AcpServer` and pins
   everything it writes to `AcpTranscript.json`. Add the new request to the script there, or give the handler a test
   of its own if it is pure.
2. **Add the method's `case`** to `AcpServer.HandleRequestAsync`'s `switch`. It calls a handler in the folder of its
   concern: `Turns/` for a turn, `Protocol/` for the protocol itself. `AcpServer` keeps only the flow.
3. **Say so in `Protocol/AcpCapabilities.cs`** if the client must know the server can do it. That's the one place the
   capabilities are stated.
4. **Reply through `JsonRpcWriter`**, never by writing to stdout yourself.

## Add a `session/update` kind

`Turns/TurnRelay.cs` turns the engine's stream into updates: `agent_thought_chunk` for thinking, `tool_call` and
`tool_call_update` for tools (with their cards from `ToolCards`), and `agent_message_chunk` for text. Add a case to
its `switch` for the new content, named the way ACP spells the kind. Then regenerate the transcript (below) and read
its diff.

## Texts

Log lines are `Messages/AcpLogMessage`, what the user sees is `AcpUserText`, and what the model is told is
`AcpModelText` ([ADR 0009](../adr/0009-message-text-as-enum-templates.md)). A changed model text changes the
transcript's golden file.

## Test it

```bash
dotnet test src/tests/hosts/LouisAgent.Acp.Tests
LOUIS_UPDATE_GOLDEN=1 dotnet test src/tests/hosts/LouisAgent.Acp.Tests   # after a deliberate change, then read the diff
```

The tests mirror the folders: `Paths/`, `Prompting/`, `Protocol/`, `Rider/`, `Turns/`, and the transcript at the root.
Then check it in Rider: a prompt with a file attached, a tool call, and a stop.

## Before you ask for a review

- [ ] Nothing new writes to stdout except `JsonRpcWriter`.
- [ ] The server translates; any new behaviour went to core.
- [ ] The transcript golden's diff is exactly the change you meant.

---
[Guides](README.md) · [Docs index](../README.md)
