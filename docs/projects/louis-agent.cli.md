# louis-agent.cli — design

> **Kind:** console app · **Path:** `src/louis-agent.cli/` · **References:** louis-agent.core · **Docker service:**
> `local-agent`

## Purpose

The simplest way to use the agent: a terminal chat, or a single prompt from a script. It has no protocol of its own,
which also makes it the quickest host for trying a change to core.

## How it works

`Program.cs` is 13 lines: `AgentHost.Build()`, then either `engine.RunSinglePromptAsync(args[0])` when a prompt is
passed, or `engine.RunAsync()` for the interactive loop. The loop itself lives in `AgentEngine` (core), so it shares
everything with the other hosts:

```text
You: What branch am I on?
[thinking in grey]
  > GetCurrentBranch
    done: main
You're on main.
You: exit
```

- Thinking is printed in grey, the answer as it's written, and one status line per tool call (`> Name args`, then
  `done:` or `failed:`).
- **Ctrl+C** stops the reply in progress; with no reply running it exits. `exit` (or end of input) quits.
- `/tools`, `/approve <Name>` and `/reject <Name>` are answered locally and never sent to the model.
- Outside Docker, `config/.env` and the repository root are found from any folder inside the repository.

## Structure

| File | What's in it |
|---|---|
| `Program.cs` | Bootstrap and the choice between one-shot and interactive |
| `louis-agent.cli.csproj` | References core only |

## Configuration

The core settings ([Setup](../SETUP.md)); nothing CLI-specific.

## Extending it

CLI behaviour (rendering, commands) lives in `AgentEngine.RunAsync` / `RunSinglePromptAsync` and `HandleUserCommand` in
core. Keep `Program.cs` a bootstrap.

## Tests

No CLI-specific tests; the loop and commands are covered through `AgentEngine` in `tests/louis-agent.core.tests`.

## Limits and plans

- History is lost on exit → backlog *CLI: resume a previous session* ([TODO](../../TODO.md)).
- A usage line after each answer and a `/usage` command → [F3](../features/F03-usage-display.md).

## Related docs

[Quick start](../QUICK_START_AGENT.md) · [louis-agent.core](louis-agent.core.md)

---
[Projects](README.md) · Previous: [louis-agent.core](louis-agent.core.md) · Next: [louis-agent.acp-server](louis-agent.acp-server.md)
