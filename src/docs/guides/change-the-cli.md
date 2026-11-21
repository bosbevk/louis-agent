# Change the CLI

> **How-to.** Change the terminal host, `src/hosts/LouisAgent.Cli`: how a turn is shown, a chat command, its texts,
> and trying it. **Elsewhere:** how it works in [LouisAgent.Cli](../projects/LouisAgent.Cli.md); the engine it runs
> in [Change the engine](change-the-engine.md).

![The CLI: CliHost runs a chat or one prompt, ConsoleChat writes the turn to the terminal, and its texts are CliUserText](diagrams/change-the-cli.svg)

The CLI is the thinnest host. A demo's `Program.cs` names its packs and providers and hands them to
`CliHost.RunAsync`, which builds the agent. `ConsoleChat` then writes the engine's stream to the terminal. That makes
the CLI the quickest place to try a change to core, and the wrong place for behaviour: anything the agent should do in
every host belongs in core.

## Run it

```bash
dotnet run --project demos/louis-agent/hosts/LouisAgent.CodingCli                                # a chat; exit or Ctrl+D to quit
dotnet run --project demos/louis-agent/hosts/LouisAgent.CodingCli -- "What branch am I on?"   # one prompt, the answer alone on stdout
```

It finds `config/.env` and the repository root from any folder inside the repository. In Rider, the coding demo's run
profile is in its `Properties/launchSettings.json`.

## Change how a turn is shown

![Changing how a turn is shown: find the case in StreamToConsoleAsync, change it, put its words in CliUserText, keep stdout for the answer](diagrams/change-the-cli-flow.svg)

1. **Open `ConsoleChat.StreamToConsoleAsync`.** Its `switch` takes each streamed update's contents in turn: thinking,
   text, a tool call, a tool result.
2. **Change the case you need**, or add one for a new kind of content. Keep it to writing, with no decision about the
   turn.
3. **Put every word you print in `Messages/CliUserText`**, with its template in `[Description]`, never as a string in
   the code ([ADR 0009](../adr/0009-message-text-as-enum-templates.md)).
4. **Keep stdout for the answer** when a prompt is passed. The usage line and other extras go to stderr, so a script
   can read the answer alone.

## Add a chat command

Commands such as `/tools`, `/approve`, `/reject` and `/usage` belong to core, in `Agent/Commands/UserCommands.cs`, so
Rider and the API get them too. The CLI passes every line to `AgentEngine.HandleUserCommand` before the model, so it
needs no code of its own.

1. Write the test first, in `src/tests/engine/LouisAgent.Core.Tests/Agent/Commands/UserCommandsTests.cs`.
2. Add the command to `UserCommands`' `switch`, with its reply in core's `UserText`.
3. Add it to the banner's list, `CliUserText.BannerCommands`, so people find it.

## Test it

The CLI has no tests of its own: its loop and its commands are tested through `AgentEngine` in core's tests.

```bash
dotnet build louis-agent-solution.sln
dotnet test src/tests/engine/LouisAgent.Core.Tests
```

Then run it by hand, once as a chat and once with a prompt. Check a tool call, a stop with Ctrl+C, and the usage line.

## Before you ask for a review

- [ ] The demo's `Program.cs` still only names what it composes and starts the host; behaviour went to core.
- [ ] Every printed word is a `CliUserText` member.
- [ ] A one-shot prompt still writes only the answer to stdout.

---
[Guides](README.md) · [Docs index](../README.md)
