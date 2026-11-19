# 8. Crosscutting concepts

> **Explanation.** The rules every part follows: security, messages, logging, testing, configuration. **Elsewhere:**
> how to apply them when changing core in the [core guide](../../engine/louis-agent.core/CLAUDE.md); tests in the
> [tests guide](../guides/write-tests.md).

![The crosscutting concepts: security, messages, logging and tracing, testing and configuration](diagrams/08-crosscutting-concepts.svg)

## Security

Settings live in `config/.env` and secrets in `config/.env.secrets`. Both are ignored by git, and you create each from
its `.example` file. `LlmOptions.ToString()` never prints the key, and no `.env` file but the templates is copied into
an image (the root `.dockerignore` sees to that).

A path the agent uses must be relative and stay inside the workspace, and symbolic links aren't followed. Secret files
can't be read or written: `.env*` (but not templates such as `.env.example`), `*.pem`, `*.key`, `*.pfx`,
`credentials.json`, `secrets.json`, `appsettings.development.json` and SSH keys. Search skips them, and the web app
never shows their git diffs.

Model text never becomes part of a command line. Skills, agent-built tools, git and dotnet run with lists of arguments
or with environment variables. A list isn't enough on its own against options, though, so git gets the model's values
after `--end-of-options` (and paths after `--`), refuses one that starts with `-`, and sets only `user.name` and
`user.email`, since other keys can make git run commands.

*Decided in:* FX19, [ADR 0004](../adr/0004-arguments-as-data.md).

Fetching a page has its own fences. Core's SSRF guard (server-side request forgery, using the agent to reach what it
shouldn't) lets a fetch reach only public addresses. It refuses loopback, private, link-local (which holds the cloud's
metadata address), multicast and reserved ranges, before every request and every redirect, and again when it connects.
What a fetch brings back is treated as untrusted. Every fetch also obeys the site's `robots.txt` and its no-AI
signals, and the browser says what it is instead of passing itself off as a person's.

*Decided in:* [ADR 0038](../adr/0038-the-browser-says-what-it-is.md), [ADR 0068](../adr/0068-every-fetch-obeys-robots-txt.md).

A host that serves the agent's tools over the network, the API or the MCP server over HTTP, fails closed. Without a
key it refuses to start, unless `AGENT_AUTH=off` says it may be open on purpose, and a key that is set is always
checked. On a developer's machine these hosts are published on `127.0.0.1` only.

The web app renders markdown without raw HTML, and only allows http(s) and mailto links. And pushing stays with you:
the web app never pushes (its commits and merges stay local), and the agent pushes only when you ask it to.

*Decided in:* FX18.

## Messages

No message text is written inline in C#. Every exception, log line, tool result and text a user reads is a member of
a message enum, and its `[Description]` is the template, with named holes the code fills from a record. That way the texts the model reads can be reviewed in one place.
They are pinned by a golden file, so a change to one can't slip through unnoticed: it shows in the file's diff. The rules are in
[Messages and logging](../../engine/louis-agent.core/CLAUDE.md#messages-and-logging).

*Decided in:* [ADR 0009](../adr/0009-message-text-as-enum-templates.md).

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

The usage ledger is data, not logs: `JsonlUsageSink` writes it in its own format, and it is the only source of what a
request cost.

A host can also send its spans and log lines to an OpenTelemetry collector, through `louis-agent.core.telemetry`, when
`OTEL_EXPORTER_OTLP_ENDPOINT` names one. That covers every model request and tool call, every request the host serves
or sends, and core's own turn spans. With no endpoint nothing listens, so a host without a collector pays nothing.
On a developer's machine the collector is the Aspire dashboard (the [deployment view](07-deployment-view.md#the-dashboard)).

*Decided in:* [ADR 0007](../adr/0007-measure-every-request.md), [ADR 0064](../adr/0064-observability-through-opentelemetry.md).

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

Every setting is in [SETUP.md](../SETUP.md), and a demo's `config/.env.example` lists them with their defaults. The
model's settings live in LLM profiles, picked by `LLM_PROFILE`, as [MODELS.md](../MODELS.md) explains. A setting that is
a choice is an enum, parsed once where it enters, with `SettingChoice.TryParse`.

*Decided in:* [ADR 0024](../adr/0024-choices-are-enums.md).
