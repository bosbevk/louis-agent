# Architecture: Louis Agent

## Overview

Louis Agent is a multi-host LLM agent for working on code. One engine (`louis-agent.core`) holds the tools, skills and
the tool-call loop; several thin hosts put it in front of you: Rider (ACP), a web app and HTTP API, a CLI, and an MCP
server.

**Key principle:** the LLM provider is configuration only (`LLM_PROVIDER`, `LLM_MODEL`). Everything goes through
`Microsoft.Extensions.AI`'s `IChatClient`, so tool calling, streaming and skills work the same on Anthropic, Ollama and
OpenAI-compatible endpoints.

## Hosts

| Host | Project | Bootstrap | Transport |
|---|---|---|---|
| CLI | `src/louis-agent.cli` | `AgentHost.Build()` | Terminal; interactive or one-shot (`louis-agent.cli [prompt]`) |
| ACP server | `src/louis-agent.acp-server` | `AgentHost.Build()` | [Agent Client Protocol](https://agentclientprotocol.com) JSON-RPC over stdio, for Rider's AI chat |
| HTTP API + web app | `src/louis-agent.api`, `src/louis-agent.web` | `AgentHost.Build()` | ASP.NET Core minimal API; Server-Sent Events; serves the Blazor WebAssembly app |
| MCP server | `src/louis-agent.mcp-server` | `AgentHost.BuildToolHost()` | Model Context Protocol over stdio (default) or Streamable HTTP (`MCP_TRANSPORT=http`); publishes the tools, the client brings its own model |

### CLI
- Streams the answer as it is written, thinking in grey, one status line per tool call; Ctrl+C stops the current reply.
- `/tools`, `/approve <Name>` and `/reject <Name>` manage agent-built tools (never sent to the model).

### ACP server (Rider)
- Handles `initialize`, `session/new`, `session/prompt` and `session/cancel`; one chat history per session.
- Streams `session/update` notifications: `agent_thought_chunk` (thinking), `agent_message_chunk` (answer text), and
  `tool_call` / `tool_call_update` (in progress → completed/failed, with an output preview).
- Files the user attaches in Rider arrive as `resource_link` blocks and are read into the prompt (Windows paths are
  mapped into the container with `ACP_MOUNT_MAPPINGS`); the file merely open in the editor is not. Embedded `resource`
  blocks are inlined too. Attachments are capped at 100,000 characters.
- Each Rider chat starts its own container (`docker compose run --rm -T acp-server`).

### HTTP API and web app
- In-memory sessions (`AgentSessions`); one reply at a time per session (409 while busy); a client disconnect or
  `POST /sessions/{id}/cancel` stops the reply; idle sessions are dropped after 4 hours.
- A message's answer streams as SSE shaped like Claude's: `message_start`, `content_block_start/delta/stop` for
  `thinking`, `text`, `tool_use` and `tool_result`, `message_delta` (stop reason), `message_stop`. Errors use Claude's
  `{ "type": "error", "error": { "type", "message" } }` shape.
- Read-only workspace browsing (`/workspace/entries`, `/workspace/file`) and a git interface (`/git/status`, `/git/diff`,
  `/git/stage`, `unstage`, `discard`, `commit`) for the web app. They reuse `WorkspaceTools`' path and secret-file checks.
- `AGENT_API_KEY`, when set, is required for `/sessions`, `/workspace` and `/git`; the app's own files stay public.
- The web app (Blazor WebAssembly) is served by the API with `MapStaticAssets` and an `index.html` fallback. It keeps
  chats in the browser's `localStorage`. Themes are CSS files in `wwwroot/css/themes/` listed in `themes.json`.

### MCP server
- Publishes the full toolset (workspace, git, `DotNet*`, `Python*`, `PowerShell*`, `Bash*`, web, skills, agent-built
  tools, and Paymo/DevOps when their keys are set) plus read-only `list_skills`, `get_skill_info`, `get_skill_documentation`.
- Stays in sync with the engine (`AgentEngine.ToolsChanged` → `notifications/tools/list_changed`).
- Approving agent-built tools is user-only and not exposed over MCP.
- Transport: stdio by default, for a local client that launches it as a subprocess (Rider, Claude Desktop).
  `MCP_TRANSPORT=http` serves Streamable HTTP instead (`MapMcp()`), for remote clients. That exposes the shell, git
  and file-write tools over the network, so set `MCP_API_KEY` (`x-api-key` or `Authorization: Bearer`; falls back to
  `AGENT_API_KEY`). With neither set it only logs a warning and accepts unauthenticated requests.

## Core abstractions

### `LlmOptions` (`config/LlmOptions.cs`)
- `LLM_PROVIDER`: `anthropic` | `ollama` | `openai-compatible`. If unset it is inferred: `claude*` → `anthropic`, other
  models → `ollama`, no model → `anthropic`.
- `LLM_MODEL` (default `claude-haiku-4-5-20251001`), `LLM_ENDPOINT`, `LLM_API_KEY` (falls back to `ANTHROPIC_API_KEY`),
  `ANTHROPIC_WORKSPACE_ID` (for keys not scoped to a workspace).
- `LLM_SUPPORTS_TOOLS`: override; otherwise Ollama models starting with `llama2`, `codellama`, `deepseek-r1`,
  `deepseek-coder` or `qwen2` run without tools.
- `LLM_THINKING`: `off` | `low` | `medium` | `high`; default `medium` for Anthropic, `off` otherwise.

### `AgentOptions` (`config/AgentOptions.cs`)
- `WORKSPACE_ROOT` (default: the repository root found from the working directory), `SKILLS_DIRECTORY`,
  `AGENT_FUNCTION` (default `code-review`; `louis` loads every skill file), `LOG_DIRECTORY`.
- Keys: `PAYMO_API_KEY`, `DEVOPS_API_KEY`, `WEB_SEARCH_PROVIDER`, `GOOGLE_SEARCH_API_KEY`, `GOOGLE_SEARCH_ENGINE_ID`.
- Azure DevOps: `DEVOPS_ORGANIZATION`, `DEVOPS_PROJECT`, optional `DEVOPS_TEAM` (sprints live under the team).
- Rider MCP: `RIDER_MCP_ENDPOINT` (default `http://127.0.0.1:64482`), `RIDER_MCP_AUTO_DISCOVER`, `RIDER_MCP_PROJECT_PATH`.

### `LlmClientFactory` (`providers/LlmClientFactory.cs`)
Maps `LlmOptions` to an `IChatClient`:
- **Anthropic:** `AnthropicClient.AsIChatClient(model, 16000)`. For models before Claude 4.6 (including Haiku 4.5),
  which reject adaptive thinking, a small wrapper turns `ChatOptions.Reasoning` into a fixed thinking budget
  (low 2,048 / medium 4,096 / high 8,192 tokens).
- **Ollama:** `OllamaApiClient`. **OpenAI-compatible:** `OpenAIClient(...).GetChatClient(model).AsIChatClient()`.

### `AgentEngine` (`tools/AgentEngine.cs`)
Provider-agnostic. Owns the tools, the system prompt (skill documentation) and the tool-call loop:
- `StreamPromptAsync(history, input)` streams `ChatResponseUpdate`s (text, `TextReasoningContent`, tool calls and
  results) and records the finished turn in `history`; `ProcessPromptAsync` is the non-streaming form.
- `FunctionInvokingChatClient` runs the loop: up to 10 tool rounds per request, detailed errors returned to the model.
- Output limit 16,000 tokens per reply; `LLM_THINKING` sets `ChatOptions.Reasoning`.

## A chat turn

```
User message
  ↓ host (CLI / ACP / API) → AgentEngine.StreamPromptAsync(history, message)
  ↓ system prompt = skill documentation; ChatOptions: tools, reasoning, 16k output limit
  ↓ FunctionInvokingChatClient loop
  │   model streams thinking + text, then tool calls
  │   each call → LogAndInvokeAsync (log, run, guard the result) → result back to the model
  ↓ answer streams to the host as it is written
  ↓ the whole turn (thinking blocks, calls, results, answer) is appended to the history
```

### Guards around tool calls
- **Oversized results:** a result over 50,000 characters is summarised by a separate, tool-less model call (its input
  capped at 200,000 characters), falling back to truncation, so one tool can't flood the context.
- **Cut-off calls:** if a reply hits the output limit, any tool call in it has incomplete arguments. It is not run; the
  model is told to write large content in chunks. When streaming, a cut-off call is dropped by the provider, so the
  engine asks the model to continue (up to 3 times).
- **Failures:** exceptions reach the model with their message (e.g. a missing parameter) and are logged as failures.

## Skills

`AgentHost.LoadSkills` builds a `CompositeSkillProvider` whose combined documentation is the system prompt:
1. `personality.md` (`PersonalitySkillsProvider`) — who the agent is and how it talks; optional, and kept apart so it
   can be changed without touching how the agent works
2. `default.md` (`DefaultSkillsProvider`) — tools, rules and workflows
3. `dotnet-skills.md`, `python-skills.md`, `powershell-skills.md`, `bash-skills.md`, `web-skills.md`,
   `self-extension-skills.md` — always loaded, because their tools are always registered
4. the `AGENT_FUNCTION` skill file (`code-review`, `devops`, `paymo`, `time-logging`, or any `{name}-skills.md`);
   `louis` loads all of them plus any other `*-skills.md` in the folder

Skill files are found in `SKILLS_DIRECTORY`, the working directory and its parents (also their `Skills/` folders),
`/app` and the app's base directory.

A markdown **skill** is a named procedure with a fenced script (bash, PowerShell or Python by fence language). The model
runs it with `ExecuteSkill(name, jsonArgs)`; arguments are passed as `SKILL_ARG_*` environment variables and are never
spliced into the script text.

## Tools

Public methods on the tool classes become tools via `AIFunctionFactory.Create` (so **every public method on a tool class
is exposed to the model** — helpers must be `internal` or `private`). Names and parameter names are exact.

| Class | Tools |
|---|---|
| `WorkspaceTools` | Read / write / append / list / search / find files, directories, copy, rename, delete |
| `GitTools` | Status, diffs, branches, staging, commit, stash, push/pull/fetch, merge/rebase, tags, config |
| `DotNetTools` | `DotNetBuild`, `DotNetTest`, `DotNetRun`, restore, clean, packages, project and SDK info |
| `PythonTools`, `PowerShellTools`, `BashTools` | Run code or scripts (bash is Git Bash on Windows, never WSL) |
| `WebTools` | `WebSearch` (Google or DuckDuckGo via `IWebSearchProvider`), `FetchUrl` (SSRF-guarded) |
| `PaymoTools` | Time entries, tasks, projects, summaries — when `PAYMO_API_KEY` is set |
| `DevOpsTools` | Azure DevOps work items and sprints (empty sprint = the team's current one) — when `DEVOPS_API_KEY`, `DEVOPS_ORGANIZATION` and `DEVOPS_PROJECT` are set |
| `AgentEngine` | `ExecuteSkill`, `CreateSkillFile`, `ReloadSkills`, `CreateTool`, `ListAgentTools` |

**Agent-built tools** (`ScriptTool`): the model can write `Skills/tools/pending/{Name}.tool.md` (typed parameters + a
script) with `CreateTool`. It only becomes a real tool after the user types `/approve <Name>`.

## Rider MCP discovery (partial)

With `RIDER_MCP_AUTO_DISCOVER=true`, `AgentHost.Build()` starts a background task that asks Rider's MCP server
(`RIDER_MCP_ENDPOINT`) for its tools (`RiderMcpClient`) and passes each to `AgentEngine.RegisterDiscoveredTool` as
`rider_{name}` (`RiderMcpToolDiscovery`). **Today this only logs the discovered tools; they are not added to the
model's tool list**, because calling them needs a full MCP client. Discovery is off in `config/.env`. It never blocks
startup and failures only log.

## Security

- **Secrets:** settings (`config/.env`) and secrets (`config/.env.secrets`) are both git-ignored, created from their
  `.example` files.
  `LlmOptions.ToString()` never prints the key. No `.env` file is copied into images (`docker/.dockerignore`).
- **Workspace boundary:** paths must be relative and stay inside the workspace; symbolic links are not followed.
  Secret files (`.env*`, `*.pem`, `*.key`, `*.pfx`, `credentials.json`, `secrets.json`, `appsettings.development.json`,
  SSH keys) can't be read or written, are skipped by search, and their git diffs are never shown in the web app.
- **No shell injection:** skills, agent-built tools, git and dotnet run with argument lists or environment variables,
  never by splicing model text into a command line.
- **Web:** `FetchUrl` blocks private, loopback and metadata addresses on every redirect and at connect time; fetched
  content is treated as untrusted. The web app renders markdown without raw HTML and only allows http(s)/mailto links.
- **Network exposure:** the API and web app, and the MCP server in HTTP mode, are published on `127.0.0.1` only.

## Logging

`AgentLog` (when `LOG_DIRECTORY` is set; `/logs` in compose → `logs/` on the host):
- each run's stderr copied to `agent-{host}-{timestamp}-{pid}.log` with timestamps (containers run with `--rm`);
- `oversized-tool-results.jsonl` and `truncated-tool-calls.jsonl`, to show which tools need narrower output.

## Bootstrap: `AgentHost`

`AgentHost.Build()` (CLI, ACP, API):
1. Load `config/.env.secrets` then `config/.env` (searching upward from the working directory) and a legacy `./.env`;
   variables already set in the process win.
2. Bind `LlmOptions` and `AgentOptions`; start file logging.
3. Load the composite skills, create the `IChatClient`, create `AgentEngine` (with `LLM_THINKING`), load approved tools.
4. Start Rider MCP discovery in the background if enabled.

`AgentHost.BuildToolHost()` (MCP server) does the same without an LLM.

## Deployment

All images build and run on the .NET 10 SDK image (the `DotNet*` tools shell out to `dotnet`), with `git` and Python
installed and `Skills/` copied in.

| Service (`docker/docker-compose.yml`) | Dockerfile | Notes |
|---|---|---|
| `acp-server` | `Dockerfile.acp-server` | Run per Rider chat with `docker compose run --rm -T acp-server` |
| `api` | `Dockerfile.api` | API + web app on `127.0.0.1:${API_PORT:-5080}`; health check on `/health` |
| `mcp-server` | `Dockerfile.mcp-server` | stdio: run per client with `run --rm -T mcp-server`; with `MCP_TRANSPORT=http`, on `127.0.0.1:${MCP_PORT:-5090}` |
| `local-agent` | `Dockerfile.agent` | The CLI |
| `ollama` | `services/Dockerfile.ollama` | Only with `--profile ollama` |

Every agent service mounts the repo at `/workspace`, `Skills/` at `/skills`, `logs/` at `/logs` and
`REPOSITORIES_PATH` at `/repositories`, reads `config/.env` and `config/.env.secrets`, and sets `core.autocrlf=true`
for git (the repo is a Windows checkout). Always pass the env file:

```bash
docker compose -f docker/docker-compose.yml --env-file config/.env up -d --build api
```

## Testing

`tests/louis-agent.core.tests` (NUnit): options and provider factory, skill loading and providers, the engine (tool-call
loop, streaming, thinking, summarising, cut-off guards) with a scripted fake `IChatClient`, every toolset, web search and
fetch with stubbed HTTP, and Rider MCP discovery with mocked HTTP. No network or Docker needed; a few tests skip
themselves when an external tool (git, pip, live network) isn't available.

```bash
dotnet test tests/louis-agent.core.tests
```

## Configuration reference

See [SETUP.md](SETUP.md) and `config/.env.example`. Typical model settings:

```bash
# Anthropic (default)
LLM_PROVIDER=anthropic
LLM_MODEL=claude-haiku-4-5-20251001     # ANTHROPIC_API_KEY in config/.env.secrets

# Ollama (docker compose --profile ollama)
LLM_PROVIDER=ollama
LLM_MODEL=llama3.1
LLM_ENDPOINT=http://ollama:11434

# OpenAI-compatible (LM Studio, vLLM, ...)
LLM_PROVIDER=openai-compatible
LLM_MODEL=your-model
LLM_ENDPOINT=http://localhost:8000/v1
```
