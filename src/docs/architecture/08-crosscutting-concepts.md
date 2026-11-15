# 8. Crosscutting concepts

> **Explanation.** The rules every part follows: security, messages, logging, testing, configuration. **Elsewhere:**
> how to apply them when changing core in the [core guide](../../engine/louis-agent.core/CLAUDE.md); tests in the
> [tests guide](../guides/write-tests.md).

## Security

Settings live in `config/.env` and secrets in `config/.env.secrets`. Both are ignored by git, and you create each from
its `.example` file. `LlmOptions.ToString()` never prints the key, and no `.env` file is copied into an image (the root
`.dockerignore` sees to that).

A path the agent uses must be relative and stay inside the workspace, and symbolic links aren't followed. Secret files
can't be read or written: `.env*` (but not templates such as `.env.example`), `*.pem`, `*.key`, `*.pfx`,
`credentials.json`, `secrets.json`, `appsettings.development.json` and SSH keys. Search skips them, and the web app
never shows their git diffs.

Model text never becomes part of a command line. Skills, agent-built tools, git and dotnet run with lists of arguments
or with environment variables. A list isn't enough on its own against options, though, so git gets the model's values
after `--end-of-options` (and paths after `--`), refuses one that starts with `-`, and sets only `user.name` and
`user.email`.

*Decided in:* FX19, [ADR 0004](../adr/0004-arguments-as-data.md).

`FetchUrl` blocks private, loopback and metadata addresses, on every redirect and again when it connects, and what it
fetches is treated as untrusted. The web app renders markdown without raw HTML, and only allows http(s) and mailto
links.

The API and the web app, and the MCP server in HTTP mode, are published on `127.0.0.1` only. And pushing stays with
you: the web app never pushes (its commits and merges stay local), and the agent pushes only when you ask it to.

## Messages

Every exception, log line, tool result and user-facing text is a member of a message enum, its `[Description]` a
template with named holes; the texts the model reads are pinned by a golden file
([0009](../adr/0009-message-text-as-enum-templates.md); the rules in
[Messages and logging](../../engine/louis-agent.core/CLAUDE.md#messages-and-logging)).

## Logging

Logging is `ILogger` with Serilog behind it (`Logging/AgentLogging`). Every line comes from a message enum (an area's
`{Area}LogMessage` in core, or a host's own in its `Messages/`), and its holes are kept as fields.

The console gets the lines on stderr only, because stdout carries ACP and MCP. Each is tagged `[INFO]`, `[WARN]`,
`[ERROR]` or with the call's own tag (`[TOOL]`, `[MCP]`, `[orchestrator]`). With `LOG_DIRECTORY` set (`/logs` in
compose, which is `logs/` on the host and ignored by git), each process also writes a run file,
`agent-{host}-{timestamp}-{pid}.log`, with the same lines and their times. That matters because containers run with
`--rm`, so their own logs vanish when they exit.

The same folder gets two records as JSON lines in their own shape (`timestamp` and camelCase fields,
`RecordFormatter`): `oversized-tool-results.jsonl` and `truncated-tool-calls.jsonl` (`ToolCallRecords`). They show
which tools need narrower output.

The usage ledger is data, not logs: `JsonlUsageSink` writes it in its own format.

## Testing

Every project has its tests in a project of its own, written first, with NUnit. Unit tests need no network, Docker or
outside program: the engine is tested with a scripted fake `IChatClient`, and web search and fetching with stubbed
HTTP. A test that does leave its process is an integration test, in a sibling `*.integration-tests` project, and skips
itself on your machine when the program it needs isn't there. Golden files pin what the model reads and what the API
offers. Which project tests what is in [the tests' design doc](../projects/tests.md), and how to write one is in
[Write and run tests](../guides/write-tests.md).

```bash
dotnet test louis-agent-solution.sln
```

*Decided in:* [ADR 0014](../adr/0014-test-first.md), [ADR 0075](../adr/0075-test-what-you-touch.md).

## Configuration

Every setting is in [SETUP.md](../SETUP.md) (and `config/.env.example`); the model settings live in LLM profiles, picked by
`LLM_PROFILE`, in [MODELS.md](../MODELS.md).
