# Change the CLI

> **How-to.** Change the terminal host, `src/louis-agent.cli`: how a turn is shown, a chat command, its texts, and
> trying it. **Elsewhere:** how it works in [louis-agent.cli](../projects/louis-agent.cli.md); the engine it runs in
> [louis-agent.core](../projects/louis-agent.core.md).

The CLI is the thinnest host: `Program.cs` builds the agent and `ConsoleChat` maps the engine's stream to the terminal.
That makes it the quickest place to try a change to core, and the wrong place for behaviour: anything the agent should
do in every host belongs in core.

## Run it

```bash
dotnet run --project src/louis-agent.cli                      # a chat; exit or Ctrl+D to quit
dotnet run --project src/louis-agent.cli -- "What branch am I on?"   # one prompt, the answer alone on stdout
```

It finds `config/.env` and the repository root from any folder inside the repository. In Rider, its run profile is in
`Properties/launchSettings.json`.

## Change how a turn is shown

1. Open `ConsoleChat.StreamToConsoleAsync`: its `switch` takes each streamed update's contents in turn (thinking, text,
   a tool call, a tool result).
2. Change the case you need, or add one for a new content type. Keep it to writing: no decision about the turn.
3. Any word you print is a member of `Messages/CliUserText` with its template in `[Description]`, never a string in the
   code ([ADR 0009](../adr/0009-message-text-as-enum-templates.md)).
4. Keep stdout for the answer when a prompt is passed: the usage line and other extras go to stderr, so a script can
   read the answer alone.

## Add a chat command

Commands such as `/tools`, `/approve`, `/reject` and `/usage` are core's (`Agent/Commands/UserCommands.cs`), so Rider
and the API get them too. Add one there, in its `switch`, with its reply in core's `UserText`, and its test in
`tests/louis-agent.core.tests/Agent/Commands/UserCommandsTests.cs`, test first. The CLI passes every line to
`AgentEngine.HandleUserCommand` before the model, so it needs no code; add the command to the banner's list
(`CliUserText.BannerCommands`) so people find it.

## Test it

The CLI has no tests of its own: its loop and commands are tested through `AgentEngine` in core's tests. After a change,
run it by hand, once as a chat and once with a prompt, and check a tool call, a stop with Ctrl+C, and the usage line.

```bash
dotnet build louis-agent-solution.sln
dotnet test tests/louis-agent.core.tests
```

## Before you ask for a review

- [ ] `Program.cs` still only builds and starts; behaviour went to core.
- [ ] Every printed word is a `CliUserText` member.
- [ ] A one-shot prompt still writes only the answer to stdout.

---
[Guides](README.md) · [Docs index](../README.md)
