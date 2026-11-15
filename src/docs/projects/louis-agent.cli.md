# louis-agent.cli — design

> **Kind:** host (a library) · **Path:** `src/hosts/louis-agent.cli/` · **References:** louis-agent.core · **Docker service:**
> `local-agent`
> **Also shows:** `component`

![The terminal host, and what it does: a chat, one prompt for a script, and its local commands](diagrams/louis-agent.cli.svg)

## Purpose

This is the simplest way to use the agent: a chat in your terminal, or a single prompt from a script. It has no
protocol of its own, which also makes it the quickest host for trying out a change to core.

## How it works

### 1. Starting

Your program makes one call, `CliHost.RunAsync`. It builds the engine with `AgentHost.Build`, then hands it to a
`ConsoleChat`. If you passed a prompt, the chat answers that one prompt and stops (`RunSinglePromptAsync`).
Otherwise it opens the interactive loop (`RunAsync`).

`ConsoleChat` only turns the engine's stream into text on the terminal. The turn itself is core's
`AgentEngine.StreamPromptAsync`, the same one every other host uses. A chat looks like this:

```text
You: What branch am I on?
[thinking in grey]
  > GetCurrentBranch
    done: main
You're on main.
You: exit
```

### 2. What you see

The model's thinking is printed in grey, and the answer appears as it's written. Each tool call gets one status
line: `> Name args`, then `done:` or `failed:`.

### 3. Stopping and quitting

**Ctrl+C** stops the reply that's running. If no reply is running, it exits. Typing `exit`, or reaching the end of
the input, quits too.

### 4. Commands that never reach the model

`/tools`, `/approve <Name>`, `/reject <Name>` and `/usage` are answered right here, and never sent to the model.
`/usage` gives you the session's tokens and cost, split by purpose.

### 5. What each answer cost

After each answer, including stopped and failed ones, a grey line goes to **stderr**, like this:
`turn: 3 requests · 28k tokens (26k cached) · $0.006 · session $0.04`.

With a single prompt (`-- "…"`), that line leaves out the session total. And because it's on stderr, stdout stays
the answer alone, which is what a script wants. You only get the line when the ledger is on, meaning `LOG_DIRECTORY`
is set.

*Decided in:* F3.

### 6. Finding its files

Outside Docker, it finds `config/.env` and the repository root from any folder inside the repository.

![The terminal host: from your program through ConsoleChat to the engine and the output](diagrams/louis-agent.cli-diagrams.svg)

![One line typed: a command is answered with no model, a message streams from the engine with its usage at the end](diagrams/louis-agent.cli-diagrams-2.svg)

These are the types involved:

![The types: CliHost builds the context and runs a ConsoleChat over the engine](diagrams/louis-agent.cli-interface.svg)

## Structure

| File | What's in it |
|---|---|
| `CliHost.cs` | The one call a program makes: takes the catalogue, then picks one-shot or interactive |
| `louis-agent.cli.csproj` | References core only |

The program that starts it is your own. The coding agent's is `demos/louis-agent/hosts/louis-agent.coding-cli`, which names no
packs ([ADR 0074](../adr/0074-the-folders-say-what-ships.md)).

## Configuration

It runs in its program, in the `local-agent` container, with a terminal attached.

![Where the CLI runs: you type in a terminal, the program in its container answers, reaching a model, the collector and its folders](diagrams/louis-agent.cli-deployment.svg)

It uses core's settings ([Setup](../SETUP.md)), and has none of its own.

## Extending it

The steps are in [Change the CLI](../guides/change-the-cli.md). In short: how things look on the terminal lives in
`ConsoleChat`. The commands are core's `UserCommands`, shared with Rider and the API. And the program stays a
bootstrap that does nothing but start it.

## Tests

There are no tests of its own. The loop and the commands are covered through `AgentEngine`, in
`src/tests/engine/louis-agent.core.tests`.

## Limits and plans

The history is lost when you exit. Picking a session back up is on the [TODO](../../../TODO.md)'s backlog, as *CLI:
resume a previous session*. Showing costs in another currency is
[F14](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F14-display-currency.md).

## Related docs

[Quick start](../../../demos/louis-agent/docs/QUICK_START_AGENT.md) · [louis-agent.core](louis-agent.core.md)

---
[Projects](README.md) · Previous: [louis-agent.host-packs](../../../demos/puls/docs/projects/louis-agent.host-packs.md) · Next: [louis-agent.acp](louis-agent.acp.md)
