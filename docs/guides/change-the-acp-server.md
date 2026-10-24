# Change the ACP server

> **How-to.** Change the Rider host, `src/louis-agent.acp-server`: run it in Rider, add a protocol method or a
> `session/update` kind, its texts and its tests. **Elsewhere:** how it works in
> [louis-agent.acp-server](../projects/louis-agent.acp-server.md); the protocol at
> [agentclientprotocol.com](https://agentclientprotocol.com).

The ACP server puts the agent in JetBrains Rider's AI chat. It speaks the Agent Client Protocol, JSON-RPC over stdin and
stdout, and only translates: the prompt into a turn for `AgentEngine`, the turn's stream into `session/update`
notifications. Behaviour belongs in core.

## Run it in Rider

1. Add the agent server from `config/rider/rider-acp.example.json` in Rider's AI Assistant settings, with your paths
   ([Quick start](../QUICK_START_AGENT.md)). It starts `docker compose run --rm -T acp-server`, one container per chat.
2. After a change, rebuild the image (`docker compose -f docker/docker-compose.yml --env-file config/.env build
   acp-server`) and start a **New Chat**: a chat keeps its container, and so its old build, until then.
3. To debug without Docker, use the project's run profile in `Properties/launchSettings.json`.

**stdout is the protocol.** One stray `Console.Write` corrupts it for Rider. Log through `ILogger`, which writes to
stderr ([Messages and logging](../../louis-agent.core/CLAUDE.md#messages-and-logging)).

## Add a protocol method

1. Write the test first. `AcpTranscriptTests` plays a recorded Rider conversation through `AcpServer` and pins all it
   writes to `AcpTranscript.json`; add the new request to the script there, or give the handler its own test if it is
   pure.
2. Add the method's `case` to `AcpServer.HandleRequestAsync`'s `switch`, calling a handler in the folder of its concern
   (`Turns/` for a turn, `Protocol/` for the protocol itself). `AcpServer` keeps only the flow.
3. If the client must know the server can do it, say so in `Protocol/AcpCapabilities.cs`, the one place the
   capabilities are stated.
4. Reply through `JsonRpcWriter`, never by writing to stdout yourself.

## Add a `session/update` kind

`Turns/TurnRelay.cs` turns the engine's stream into updates: `agent_thought_chunk` for thinking, `tool_call` and
`tool_call_update` for tools (their cards from `ToolCards`), `agent_message_chunk` for text. Add a case to its `switch`
for the new content, with the kind's name as ACP spells it, then regenerate the transcript (below) and read its diff.

## Texts

Log lines are `Messages/AcpLogMessage`, what the user sees `AcpUserText`, what the model is told `AcpModelText`
([ADR 0009](../adr/0009-message-text-as-enum-templates.md)). A changed model text changes the transcript golden.

## Test it

```bash
dotnet test tests/louis-agent.acp-server.tests
LOUIS_UPDATE_GOLDEN=1 dotnet test tests/louis-agent.acp-server.tests   # after a deliberate change: then read the diff
```

The tests mirror the folders: `Paths/`, `Prompting/`, `Protocol/`, `Turns/`, and the transcript at the root. Then check
it in Rider: a prompt with a file attached, a tool call, a stop.

## Before you ask for a review

- [ ] Nothing new writes to stdout except `JsonRpcWriter`.
- [ ] The server translates; any new behaviour went to core.
- [ ] The transcript golden's diff is exactly the change you meant.

---
[Guides](README.md) · [Docs index](../README.md)
