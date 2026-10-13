# Architecture: Louis Agent

## Overview

Louis Agent is a multi-host LLM agent for working on code. One engine (`louis-agent.core`) holds the tools, skills and
the tool-call loop; several thin hosts put it in front of you: Rider (ACP), a web app and HTTP API, a CLI, and an MCP
server. A second agent, the orchestrator (a proof of concept), is built on the same core and talks to louis-agent
through the HTTP API. How all of them connect, and what goes over the wire, is in
[AGENT_COMMUNICATION.md](AGENT_COMMUNICATION.md).

**Key principle:** the LLM provider is configuration only (`LLM_PROVIDER`, `LLM_MODEL`). Everything goes through
`Microsoft.Extensions.AI`'s `IChatClient`, so tool calling, streaming and skills work the same on Anthropic, Ollama and
OpenAI-compatible endpoints.

## Hosts

Each project also has its own design doc in [projects/](projects/README.md).


| Host | Project | Bootstrap | Transport |
|---|---|---|---|
| CLI | `src/louis-agent.cli` | `AgentHost.Build()` | Terminal; interactive or one-shot (`louis-agent.cli [prompt]`) |
| ACP server | `src/louis-agent.acp-server` | `AgentHost.Build()` | [Agent Client Protocol](https://agentclientprotocol.com) JSON-RPC over stdio, for Rider's AI chat |
| HTTP API + web app | `src/louis-agent.api`, `src/louis-agent.web` | `AgentHost.Build()` | ASP.NET Core minimal API; Server-Sent Events; serves the Blazor WebAssembly app |
| MCP server | `src/louis-agent.mcp-server` | `AgentHost.BuildToolHost()` | Model Context Protocol over stdio (default) or Streamable HTTP (`MCP_TRANSPORT=http`); publishes the tools, the client brings its own model |
| Orchestrator (POC) | `src/louis-agent.orchestrator` | Own `AgentEngine` with `toolsets:` | Not a host for users: an agent that reads a service's error log and calls louis-agent.api for fixes ([samples/README.md](../samples/README.md)) |

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
- **A session chooses its toolsets when it opens** (F5): `POST /sessions` with `{ "toolsets": [...] }` gets the engine of
  that profile from `AgentEngineFactory`, fixed for the session's life; no list means `skills` only; an unknown or
  unavailable name is a 400 listing the valid ones. Sessions with the same list share one engine and one cacheable
  prefix. `GET /toolsets` lists names, descriptions and availability (never tool definitions); `GET /sessions/{id}`
  returns a session's toolsets. A session opened by another agent (`depth: 1`) may not load `agents`.
- Two web apps share one API codebase and `louis-agent.web.shared`: the code-review app (`louis-agent.web`, every
  toolset; served by default) and **Puls** (`louis-agent.puls`, the general assistant: chats load only `agents` and
  delegate every task; served when the API is built with `-p:WebApp=puls`).
- **Live delegation:** while a `StartSubSession` call runs, the specialist's own events stream to the chat as
  `sub_session` events (`SubSessionFeed` → the API's turn stream), and the delegation card shows its thinking, tool
  calls, answer and cost as they happen; the model gets only the answer.
- A message's answer streams as SSE shaped like Claude's: `message_start`, `content_block_start/delta/stop` for
  `thinking`, `text`, `tool_use` and `tool_result`, `message_delta` (stop reason and the turn's usage and cost),
  `message_stop`. Errors use Claude's `{ "type": "error", "error": { "type", "message" } }` shape (plus the turn's
  usage, when a request had returned).
- Read-only workspace browsing (`/workspace/entries`, `/workspace/file`) and a git interface (`/git/status`, `/git/diff`,
  `/git/stage`, `unstage`, `discard`, `commit`, plus history: `/git/branches`, `/git/log`, `/git/commit`, `/git/branch`,
  `/git/merge`, `/git/branch/delete`) for the web app. They reuse `WorkspaceTools`' path and secret-file checks.
- `AGENT_API_KEY`, when set, is required for `/sessions`, `/toolsets`, `/workspace` and `/git`; the app's own files
  stay public.
- The web app (Blazor WebAssembly) is served by the API with `MapStaticAssets` and an `index.html` fallback. It keeps
  chats in the browser's `localStorage`. Themes are CSS files in `wwwroot/css/themes/` listed in `themes.json`.
- The Branches tab queues merges and deletes (`GitHistory`): every click is kept and they run one at a time, in click
  order; only the branch being worked on is marked busy ("Merging…", "Queued").

### MCP server
- Publishes every toolset this instance can load (workspace, git, `DotNet*`, `Python*`, `PowerShell*`, `Bash*`, web,
  skills and agent-built tools, and Paymo, DevOps and agents when their settings are set) plus read-only `list_skills`,
  `get_skill_info`, `get_skill_documentation`.
- Stays in sync with the engine (`AgentEngine.ToolsChanged` → `notifications/tools/list_changed`).
- Approving agent-built tools is user-only and not exposed over MCP.
- Transport: stdio by default, for a local client that launches it as a subprocess (Rider, Claude Desktop).
  `MCP_TRANSPORT=http` serves Streamable HTTP instead (`MapMcp()`), for remote clients. That exposes the shell, git
  and file-write tools over the network, so set `MCP_API_KEY` (`x-api-key` or `Authorization: Bearer`; falls back to
  `AGENT_API_KEY`). With neither set it only logs a warning and accepts unauthenticated requests.

## Core abstractions

### `LlmOptions` (`Config/LlmOptions.cs`)
- `LLM_PROVIDER`: `anthropic` | `ollama` | `openai-compatible`. If unset it is inferred: `claude*` → `anthropic`, other
  models → `ollama`, no model → `anthropic`.
- `LLM_MODEL` (default `claude-haiku-5-5`), `LLM_ENDPOINT`, `LLM_API_KEY` (falls back to `ANTHROPIC_API_KEY`),
  `ANTHROPIC_WORKSPACE_ID` (for keys not scoped to a workspace).
- `LLM_SUPPORTS_TOOLS`: override; otherwise Ollama models starting with `llama2`, `codellama`, `deepseek-r1`,
  `deepseek-coder` or `qwen2` run without tools.
- `LLM_THINKING`: `off` | `low` | `medium` | `high`; default `medium` for Anthropic, `off` otherwise.

### `AgentOptions` (`Config/AgentOptions.cs`)
- `WORKSPACE_ROOT` (default: the repository root found from the working directory), `SKILLS_DIRECTORY`,
  `AGENT_FUNCTION` (default `code-review`; `louis` loads every skill file), `LOG_DIRECTORY`.
- Keys: `PAYMO_API_KEY`, `DEVOPS_API_KEY`, `WEB_SEARCH_PROVIDER`, `GOOGLE_SEARCH_API_KEY`, `GOOGLE_SEARCH_ENGINE_ID`.
- Sub-sessions (F5): `AGENT_PEERS` (`name=url` pairs) and `AGENT_API_KEY`, sent to the peers.
- Azure DevOps: `DEVOPS_ORGANIZATION`, `DEVOPS_PROJECT`, optional `DEVOPS_TEAM` (sprints live under the team).
- Rider MCP: `RIDER_MCP_ENDPOINT` (default `http://127.0.0.1:64482`), `RIDER_MCP_AUTO_DISCOVER`, `RIDER_MCP_PROJECT_PATH`.

### `LlmClientFactory` (`Llm/LlmClientFactory.cs`)
Maps `LlmOptions` to an `IChatClient`:
- **Anthropic:** `AnthropicClient.AsIChatClient(model, 16000)`. For models before Claude 4.6 (including Haiku 4.5),
  which reject adaptive thinking, a small wrapper turns `ChatOptions.Reasoning` into a fixed thinking budget
  (low 2,048 / medium 4,096 / high 8,192 tokens).
- **Ollama:** `OllamaApiClient`. **OpenAI-compatible:** `OpenAIClient(...).GetChatClient(model).AsIChatClient()`.

### Provider strategies

When Anthropic, OpenAI-compatible endpoints and Ollama each need different logic for the same concern, that concern is
a **strategy**: one interface, one implementation per provider that differs (plus a default for the rest, often a
no-op), chosen once by `LlmClientFactory` from `LLM_PROVIDER`. Callers depend only on the interface, so `AgentEngine`,
the tools and the hosts never branch on a provider's name, and adding a provider means adding implementations, not
editing callers. Use one only when providers really differ; a concern every provider handles the same way needs none.

| Concern | Interface | Implementations | Status |
|---|---|---|---|
| How usage is reported | `IUsageMapper` | `StandardUsageMapper` (default), `AnthropicUsageMapper` (cache writes, and their split by lifetime from the raw response, F4) | Built (F1) |
| Prompt caching | `IPromptCachingStrategy` | `AnthropicPromptCaching` (two markers), `NoPromptCaching` (default: OpenAI-compatible caches prefixes on its own, Ollama), applied by `PromptCachingChatClient` | Built ([F4](features/F04-prompt-caching.md)) |
| Thinking settings | to be extracted | Anthropic's fixed budget for pre-4.6 models is an inline wrapper in `LlmClientFactory` today | Candidate ([F10](features/F10-route-settings.md) S4) |
| Context window | to be designed | Ollama needs `num_ctx` sent; Claude's window is fixed | Planned ([F15](features/F15-model-limits.md)) |
| Errors, retries and limits | to be designed | Each provider reports rate and account limits differently | Planned ([F8](features/F08-reliability.md)) |

The same pattern is used outside the providers: `IRatesMapper` (exchange-rate sources) and `IWebSearchProvider`
(Google, DuckDuckGo).

### `AgentEngine` (`Agent/AgentEngine.cs`)
Provider-agnostic. Owns the turn loop, the history rules and the client pipeline, and is the façade hosts use; the
rest is in its own classes in `Agent/`: `ToolRegistry` (the tools, in name order), `ToolCallGuards` (the guards around
each call, below), `SkillTools` (`ExecuteSkill`, `CreateSkillFile`, `ReloadSkills` and the skill set the system
prompt is built from) and `AgentBuiltTools` (`CreateTool`, `/approve`, `/reject`). Pure safety checks are in
`Guardrails/` (workspace boundary, sensitive files, SSRF), and the console chat is the CLI's (`ConsoleChat`).
Everything it sends uses LF line endings, whatever the checkout:
- `StreamPromptAsync(history, input)` streams `ChatResponseUpdate`s (text, `TextReasoningContent`, tool calls and
  results) and records the finished turn in `history`; `ProcessPromptAsync` is the non-streaming form.
- `FunctionInvokingChatClient` runs the loop: up to 10 tool rounds per request, detailed errors returned to the model.
- Output limit 16,000 tokens per reply; `LLM_THINKING` sets `ChatOptions.Reasoning`.
- `toolsets:` gives the engine its tool objects (their public methods, plus `ExecuteSkill`): a session's catalogue
  toolsets (`AgentEngineFactory`), or another agent's own tools (the orchestrator's `ServiceTools`); `selfExtension:`
  adds `CreateSkillFile`, `ReloadSkills`, `CreateTool` and `ListAgentTools` (the `skills` toolset).

### Toolsets (`Agent/ToolsetCatalog.cs`, `Hosting/AgentEngineFactory.cs`)
Every toolset is a catalogue entry: a name, a one-line description, the settings it needs, its tool class and factory,
and its guide (docs/CLAUDE.md, *Adding a toolset*). `files`, `git`, `dotnet`, `python`, `powershell`, `bash`, `web`,
`paymo`, `devops`, `agents` (sub-sessions on a peer, `AGENT_PEERS`) and `skills` (engine-provided). `AgentEngineFactory`
builds one engine per normalised list (cached, at most 16), with only those tools and only their guides in the system
prompt, and logs each profile's tool count and fixed-prefix estimate. The CLI, ACP server and MCP server use every
toolset the instance can load; the API's sessions choose theirs. A sub-session (`AgentPeerTools.StartSubSession`) runs
on a peer with its own toolsets, at most 3 per turn and 5 minutes each, and only its answer and a usage line come back;
its usage records carry the parent turn (`parent`).

## A chat turn

```
User message
  ↓ host (CLI / ACP / API) → AgentEngine.StreamPromptAsync(history, message)
  ↓ system prompt = skill documentation; ChatOptions: tools, reasoning, 16k output limit
  ↓ FunctionInvokingChatClient loop
  │   model streams thinking + text, then tool calls
  │   each call → ToolCallGuards.InvokeAsync (log, run, guard the result) → result back to the model
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

`AgentHost.LoadSkills(agent, toolsets)` builds a `CompositeSkillProvider` whose combined documentation is the system
prompt; every file is plain markdown read by `MarkdownSkillProvider`:
1. `personality.md` — who the agent is and how it talks; kept apart so it can change without touching how the agent works
2. `default.md` — the router: which toolset a task needs, and how sessions and sub-sessions work
3. the guide of each of the profile's toolsets (`files-skills.md`, `git-skills.md`, …) and no other, so the prompt never
   describes tools the session doesn't have
4. the `AGENT_FUNCTION` skill file (`code-review`, `time-logging`, or any `{name}-skills.md`); `louis` loads them all plus
   any other `*-skills.md` in the folder

The built-in files are embedded in `louis-agent.core` (`SkillFileSource`); a file of the same name on disk wins
(`SKILLS_DIRECTORY`, the working directory and its parents, also their `Skills/` folders, `/app`, the app's base
directory), so a guide can be changed without a rebuild. Files the agent writes are on disk only.

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
  `LlmOptions.ToString()` never prints the key. No `.env` file is copied into images (the root `.dockerignore`).
- **Workspace boundary:** paths must be relative and stay inside the workspace; symbolic links are not followed.
  Secret files (`.env*`, `*.pem`, `*.key`, `*.pfx`, `credentials.json`, `secrets.json`, `appsettings.development.json`,
  SSH keys) can't be read or written, are skipped by search, and their git diffs are never shown in the web app.
- **No shell injection:** skills, agent-built tools, git and dotnet run with argument lists or environment variables,
  never by splicing model text into a command line.
- **Web:** `FetchUrl` blocks private, loopback and metadata addresses on every redirect and at connect time; fetched
  content is treated as untrusted. The web app renders markdown without raw HTML and only allows http(s)/mailto links.
- **Network exposure:** the API and web app, and the MCP server in HTTP mode, are published on `127.0.0.1` only.

## Logging

`ILogger` with Serilog behind it (`Logging/AgentLogging`), every line from a message enum (`LogMessage` in core, each
host's own in its `Messages/`), its holes kept as fields:
- the console on **stderr only** (stdout carries ACP and MCP), `[INFO]`, `[WARN]`, `[ERROR]` or the call's tag
  (`[TOOL]`, `[MCP]`, `[orchestrator]`);
- with `LOG_DIRECTORY` (`/logs` in compose → `logs/` on the host), a run file per process,
  `agent-{host}-{timestamp}-{pid}.log`, the same lines with the time (containers run with `--rm`);
- and, there too, the records `oversized-tool-results.jsonl` and `truncated-tool-calls.jsonl` (`ToolCallRecords`), as
  JSON lines in their own shape (`timestamp` and camelCase fields, `RecordFormatter`), to show which tools need
  narrower output.

The usage ledger is data, not logs: `JsonlUsageSink` writes it in its own format.

## Bootstrap: `AgentHost`

`AgentHost.Build()` (CLI, ACP, API):
1. Load `config/.env.secrets` then `config/.env` (searching upward from the working directory) and a legacy `./.env`;
   variables already set in the process win.
2. Bind `LlmOptions` and `AgentOptions`; configure logging (`AgentLogging.Configure(LOG_DIRECTORY)`).
3. Create the `IChatClient`, the usage ledger and an `AgentEngineFactory` (with `LLM_THINKING` and the display
   currency); the host's own engine has every toolset this instance can load (approved agent-built tools with `skills`).
4. Start Rider MCP discovery in the background if enabled.

`AgentHost.BuildToolHost()` (MCP server) does the same without an LLM.

## Deployment

All images build and run on the .NET 10 SDK image (the `DotNet*` tools shell out to `dotnet`), with `git` and Python
installed and `Skills/` copied in.

| Service (`docker/docker-compose.yml`) | Dockerfile | Notes |
|---|---|---|
| `acp-server` | `Dockerfile.acp-server` | Run per Rider chat with `docker compose run --rm -T acp-server` |
| `api` | `Dockerfile.api` | API + web app on `127.0.0.1:${API_PORT:-5080}`; health check on `/health` |
| Puls: `puls`, `puls-coder` (`docker-compose.puls.yml`) | `Dockerfile.puls` | The general assistant on `127.0.0.1:${PULS_PORT:-5070}` and the specialist it delegates to on `${PULS_CODER_PORT:-5071}`; logs in `logs/puls` |
| `mcp-server` | `Dockerfile.mcp-server` | stdio: run per client with `run --rm -T mcp-server`; with `MCP_TRANSPORT=http`, on `127.0.0.1:${MCP_PORT:-5090}` |
| `local-agent` | `Dockerfile.agent` | The CLI |
| `ollama` | `services/Dockerfile.ollama` | Only with `--profile ollama` |
| `ollama-nvidia` | `services/Dockerfile.ollama-nvidia` | Only with `--profile ollama-nvidia`: on an NVIDIA GPU, 32k-token context |
| `ollama-amd` | `services/Dockerfile.ollama-amd` | Only with `--profile ollama-amd`: on an AMD GPU (ROCm, Linux hosts), 32k-token context |

Every agent service above mounts the repo at `/workspace`, `Skills/` at `/skills`, `logs/` at `/logs` and
`REPOSITORIES_PATH` at `/repositories`, reads `config/.env` and `config/.env.secrets`, and sets `core.autocrlf=true`
for git (the repo is a Windows checkout). Always pass the env file:

```bash
docker compose -f docker/docker-compose.yml --env-file config/.env up -d --build api
```

The orchestrator demo has its own compose file, `docker/docker-compose.demo.yml` (project `louis-agent-demo`):

| Service | Dockerfile | Notes |
|---|---|---|
| `demo-setup` | `Dockerfile.orchestrator` | One-off: fresh demo repository in `.demo/order-service` and its production traffic |
| `demo-api` | `Dockerfile.api` | louis-agent.api and the web app on `127.0.0.1:${DEMO_PORT:-5081}`, workspace `/demo/order-service` |
| `orchestrator` | `Dockerfile.orchestrator` | Triages new errors once and calls `demo-api` for fixes; SDK image, as it replays requests and runs tests |

All three mount the host's `.demo/` at `/demo` (the demo repository is LF, so no `core.autocrlf`) and share a `nuget`
volume. `samples/run-demo.ps1` drives them; see [samples/README.md](../samples/README.md).


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

## Configuration reference

See [SETUP.md](SETUP.md) and `config/.env.example`. The model settings live in LLM profiles, picked by `LLM_PROFILE`
in `config/.env` ([MODELS.md](MODELS.md)):

```bash
# config/.env.anthropic (LLM_PROFILE=anthropic, the default)
LLM_PROVIDER=anthropic
LLM_MODEL=claude-haiku-5-5              # ANTHROPIC_API_KEY in config/.env.secrets

# config/.env.ollama (LLM_PROFILE=ollama; docker compose --profile ollama)
LLM_PROVIDER=ollama
LLM_MODEL=llama3.1
LLM_ENDPOINT=http://host.docker.internal:11434

# no profile yet; set these in config/.env

# OpenAI-compatible (LM Studio, vLLM, ...)
LLM_PROVIDER=openai-compatible
LLM_MODEL=your-model
LLM_ENDPOINT=http://localhost:8000/v1
```

---
[Docs index](README.md) · Previous: [How the agents and endpoints talk](AGENT_COMMUNICATION.md) · Next: [Development guide](CLAUDE.md)
