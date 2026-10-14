# 8. Crosscutting concepts

> **Explanation.** The rules every part follows: security, messages, logging, testing, configuration. **Elsewhere:**
> how to apply them when changing core in the [core guide](../../louis-agent.core/CLAUDE.md); tests in the
> [tests guide](../../tests/CLAUDE.md).

## Security

- **Secrets:** settings (`config/.env`) and secrets (`config/.env.secrets`) are both git-ignored, created from their
  `.example` files.
  `LlmOptions.ToString()` never prints the key. No `.env` file is copied into images (the root `.dockerignore`).
- **Workspace boundary:** paths must be relative and stay inside the workspace; symbolic links are not followed.
  Secret files (`.env*` but not templates such as `.env.example`, `*.pem`, `*.key`, `*.pfx`, `credentials.json`, `secrets.json`, `appsettings.development.json`,
  SSH keys) can't be read or written, are skipped by search, and their git diffs are never shown in the web app.
- **No shell injection:** skills, agent-built tools, git and dotnet run with argument lists or environment variables,
  never by splicing model text into a command line.
- **Web:** `FetchUrl` blocks private, loopback and metadata addresses on every redirect and at connect time; fetched
  content is treated as untrusted. The web app renders markdown without raw HTML and only allows http(s)/mailto links.
- **Network exposure:** the API and web app, and the MCP server in HTTP mode, are published on `127.0.0.1` only.
- **Pushing stays with you:** the web app never pushes (its commits and merges stay local), and the agent pushes only
  when you ask it to.

## Messages

Every exception, log line, tool result and user-facing text is a member of a message enum, its `[Description]` a
template with named holes; the texts the model reads are pinned by a golden file
([0009](../adr/0009-message-text-as-enum-templates.md); the rules in
[Messages and logging](../../louis-agent.core/CLAUDE.md#messages-and-logging)).

## Logging

`ILogger` with Serilog behind it (`Logging/AgentLogging`), every line from a message enum (`LogMessage` in core, each
host's own in its `Messages/`), its holes kept as fields:
- the console on **stderr only** (stdout carries ACP and MCP), `[INFO]`, `[WARN]`, `[ERROR]` or the call's tag
  (`[TOOL]`, `[MCP]`, `[orchestrator]`);
- with `LOG_DIRECTORY` (`/logs` in compose → `logs/` on the host, git-ignored), a run file per process,
  `agent-{host}-{timestamp}-{pid}.log`, the same lines with the time (containers run with `--rm`);
- and, there too, the records `oversized-tool-results.jsonl` and `truncated-tool-calls.jsonl` (`ToolCallRecords`), as
  JSON lines in their own shape (`timestamp` and camelCase fields, `RecordFormatter`), to show which tools need
  narrower output.

The usage ledger is data, not logs: `JsonlUsageSink` writes it in its own format.

## Testing

`tests/louis-agent.core.tests` (NUnit): options and provider factory, skill loading and providers, the engine (tool-call
loop, streaming, thinking, summarising, cut-off guards) with a scripted fake `IChatClient`, every toolset, web search and
fetch with stubbed HTTP, and Rider MCP discovery with mocked HTTP. No network or Docker needed; a few tests skip
themselves when an external tool (git, pip, live network) isn't available.

`tests/louis-agent.api.tests` (NUnit): the API's sessions (F5): toolsets per session, the default, unknown names,
depth, shared engines and separate histories, with a model client that is never called.

`tests/louis-agent.orchestrator.tests` (NUnit): the error feed, the louis-agent API client (event stream and
`FIX-RESULT` parsing), the decision line, runbook loading, the comms log, and the verification tools against a
throwaway git repository.

```bash
dotnet test tests/louis-agent.core.tests
dotnet test tests/louis-agent.api.tests
dotnet test tests/louis-agent.orchestrator.tests
```

The API and web app have no automated tests; the orchestrator demo exercises them end to end against a real model.

## Configuration

Every setting is in [SETUP.md](../SETUP.md) (and `config/.env.example`); the model settings live in LLM profiles, picked by
`LLM_PROFILE`, in [MODELS.md](../MODELS.md).
