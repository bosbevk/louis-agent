# Louis Agent: Development Guide

## Quick start

```bash
cp config/.env.example config/.env
cp config/.env.secrets.example config/.env.secrets   # add ANTHROPIC_API_KEY
dotnet build louis-agent-solution.sln
dotnet test tests/louis-agent.core.tests
dotnet run --project src/louis-agent.cli               # interactive CLI
```

Docker (web app + API on http://127.0.0.1:5080):

```bash
docker compose -f docker/docker-compose.yml --env-file config/.env up -d --build api
```

Always pass `--env-file config/.env`; compose needs it for `REPOSITORIES_PATH`. Requires the .NET 10 SDK.

## Project structure

```
louis-agent.core/
├── AgentHost.cs              # Bootstrap for every host: env files, options, skills, LLM client, engine, logging
├── AgentLog.cs               # File logging (LOG_DIRECTORY): stderr copy per run + JSONL records
├── MarkdownSkillLoader.cs    # Parses "## Skill:" procedures from markdown
├── config/
│   ├── LlmOptions.cs         # LLM_* settings, tool-support and thinking resolution
│   └── AgentOptions.cs       # Workspace, skills, keys, Rider MCP, LOG_DIRECTORY
├── providers/
│   ├── LlmClientFactory.cs   # Provider → IChatClient (Anthropic thinking-budget wrapper lives here)
│   ├── CompositeSkillProvider.cs, MarkdownSkillProvider.cs, ISkillProvider.cs
│   └── Personality/Default/DotNet/DevOps/Paymo/CodeReview/TimeLogging SkillsProvider.cs
├── tools/
│   ├── AgentEngine.cs        # Tools, system prompt, streaming turn loop, tool-result guards, /approve commands
│   ├── WorkspaceTools.cs     # Files; ResolvePath/IsSensitive are internal and reused by the API
│   ├── GitTools.cs, DotNetTools.cs, PythonTools.cs, PowerShellTools.cs, BashTools.cs
│   ├── WebTools.cs, WebSearchProviders.cs   # WebSearch (Google / DuckDuckGo) and FetchUrl (SSRF-guarded)
│   ├── PaymoTools.cs, DevOpsTools.cs        # Loaded only when their API keys are set
│   ├── ScriptTool.cs         # Agent-built tools (*.tool.md)
│   └── ProcessRunner.cs      # Process runner: argument lists, no shell, timeouts
└── mcp/
    ├── RiderMcpClient.cs     # Lists tools from Rider's MCP server
    └── RiderMcpToolDiscovery.cs

src/
├── louis-agent.cli/          # Terminal host
├── louis-agent.acp-server/   # Rider (Agent Client Protocol over stdio)
├── louis-agent.api/          # Minimal API: sessions + SSE, /workspace, /git; serves the web app
│   ├── Program.cs, AgentSessions.cs, WorkspaceEndpoints.cs, GitEndpoints.cs
├── louis-agent.web/          # Blazor WebAssembly app (chat, Files, Changes, themes)
│   ├── Pages/ChatPage.razor, Components/*.razor, Services/*.cs
│   └── wwwroot/css/app.css + css/themes/*.css + themes.json
├── louis-agent.mcp-server/   # Publishes the toolset over MCP (no LLM) - stdio by default, MCP_TRANSPORT=http for remote clients
└── louis-agent.orchestrator/ # POC: watches a service's errors, runbook per API method, delegates fixes to louis-agent.api

tests/louis-agent.core.tests/ # NUnit; Config, Loaders, Providers, Tools, mcp
tests/louis-agent.orchestrator.tests/ # NUnit; error feed, SSE client, verification tools
samples/                      # order-service (demo microservice) + run-demo.ps1, see samples/README.md
docker/                       # Dockerfile.{acp-server,api,agent,mcp-server}, docker-compose.yml
config/                       # .env and .env.secrets (both ignored), their examples, Rider config examples
Skills/                       # personality.md + default.md + *-skills.md (system prompt), tools/ (agent-built tools)
docs/                         # This guide and the other docs
```

## Development workflow

### Adding a C# tool

1. Add a public method with `[Description]`s to a tool class (or a new class in `louis-agent.core/tools/`):

   ```csharp
   [Description("What this tool does")]
   public string MyTool([Description("What the parameter is")] string input, int limit = 10) => "result";
   ```

2. For a new class, register it in the `AgentEngine` constructor: `AddPublicMethodsAsTools(new MyTools(WorkspaceRoot));`
3. Mention it in the relevant `Skills/*.md` so the model knows when to use it.
4. Test it in `tests/louis-agent.core.tests/Tools/`.

**Every public method on a tool class becomes a tool.** Helpers must be `internal` or `private` (the test project and
`louis-agent.api` can see internals). Tool names must be unique across all classes.

Return strings the model can act on: say what went wrong and what to do instead. Keep output bounded — results over
50,000 characters get summarised; most toolsets cap their own output at 12,000.

### Adding a skill

Create `Skills/{name}-skills.md`. With `AGENT_FUNCTION=louis` every skill file is loaded; otherwise set
`AGENT_FUNCTION={name}`. For a guide that must always load (like `bash-skills.md`), add it to
`AgentHost.AlwaysLoadedSkillFiles` and `BuiltInSkillFiles`. Procedures use `## Skill: Name` with an `Execution:` fence
(bash, powershell or python); arguments arrive as `SKILL_ARG_*` environment variables.

### Adding a web theme

Copy `src/louis-agent.web/wwwroot/css/themes/paper.css` to `{id}.css`, set the variables listed at the top of
`css/app.css`, and add `{ "id": "{id}", "name": "…" }` to `themes.json`. Ids are lowercase letters, digits and dashes.

### Changing the LLM

Configuration only — see [MODELS.md](MODELS.md). Provider differences belong in `LlmClientFactory`, never in
`AgentEngine` or the tools.

### Running and debugging

- Tool calls are logged to stderr: `[TOOL] -> Name(args)` and `[TOOL] <- Name: result` (or `failed: …`).
- In Docker, logs persist in `logs/`; `docker logs louis_agent_api` shows the API.
- Write debug output to `Console.Error`, never `Console.Out`: stdout carries the ACP and MCP protocols.
- After changing the web app or API, rebuild the `api` image; after changing core, rebuild every image you use. Rider
  chats keep their container until you start a **New Chat**.

### Tests

```bash
dotnet test tests/louis-agent.core.tests
dotnet test tests/louis-agent.core.tests --filter "FullyQualifiedName~AgentEngineTests"
```

Tests use `FakeChatClient` (a scripted `IChatClient` that also streams) and `StubHandler` for HTTP — no network, model or
Docker. A few tests skip themselves when git, pip or the network isn't available. The agent runs tests on Linux inside
its containers, so keep them platform-neutral (forward slashes, no Windows-only paths).

The API and web app have no automated tests yet; check them by hand (curl and a browser) after changes.

## Key design decisions

- **Microsoft.Extensions.AI** — one `IChatClient` across providers; `FunctionInvokingChatClient` runs the tool loop.
- **Streaming everywhere** — hosts render `StreamPromptAsync` updates; the finished turn is appended to the history,
  thinking blocks included (Anthropic requires them when continuing after tool calls).
- **Arguments as data** — skill and tool arguments travel as environment variables or argument lists, never through a
  shell, so model text can't become commands.
- **Guard the context** — oversized results are summarised, cut-off calls are never run, failures carry their reason.
- **Hosts stay thin** — each host maps the same stream to its protocol; behaviour lives in core.

## Anthropic integration tiers (reference)

Four ways to build on Claude, from most managed to least. `louis-agent` deliberately sits at the bottom tier — see
"Why `louis-agent` uses the Messages API tier" below.

| Tier | Who hosts the loop | Who hosts the sandbox/tools | OpenAI equivalent |
|---|---|---|---|
| **Messages API** (`POST /v1/messages`) | You | You | Responses API |
| **Tool Runner** (`client.beta.messages.tool_runner`, Anthropic SDK only) | SDK | You | Agents SDK (partial — Anthropic-only, no handoffs/multi-agent) |
| **Claude Agent SDK** (`claude-agent-sdk`, separate package — Claude Code as a library) | SDK | You | Agents SDK (closer — ships built-in coding tools) |
| **Managed Agents** (beta, Anthropic-hosted) | Anthropic | Anthropic | Agents API |

### Decision tree

```mermaid
flowchart TD
    A[Need to call Claude] --> B{Need to swap model providers<br/>Anthropic, Ollama, etc.?}
    B -->|Yes| C["Messages API + your own loop<br/>(this is louis-agent)"]
    B -->|No, Claude-only is fine| D{Want Anthropic to host<br/>the tool-execution sandbox?}
    D -->|Yes| E[Managed Agents]
    D -->|No, I host it| F{Need Claude Code's built-in<br/>file/bash/grep tools?}
    F -->|Yes| G[Claude Agent SDK]
    F -->|No, just a loop over my own tools| H[Tool Runner]
```

### Real-world use cases

- **Messages API + your own loop** (what this repo uses): multi-provider portability is a hard requirement, or you
  need custom session/approval/skill semantics a vendor SDK doesn't model. `louis-agent`'s `IChatClient`
  abstraction lets `LlmClientFactory` swap Anthropic for Ollama without touching `AgentEngine` or any tool.
- **Tool Runner**: a single-provider (Claude-only) shop building a straightforward custom-tool agent — e.g. an
  internal ticket-triage bot — that wants approval-gate/logging/retry hooks without hand-writing the
  `while stop_reason == "tool_use"` loop.
- **Claude Agent SDK**: a coding/filesystem-centric agent (a Cursor/Devin-style assistant, a PR-review bot) where
  Claude Code's built-in Read/Write/Edit/Bash/Grep tools already cover the surface you need, and being Claude-only
  is acceptable. Reinventing `WorkspaceTools`/`BashTools` wouldn't have been worth it here either, if this repo
  didn't also need DevOps/Paymo tools, markdown-defined skills, and provider portability on top.
- **Managed Agents**: you don't want to run a sandbox at all — a SaaS feature ("AI cleans up your spreadsheet"), a
  nightly scheduled agent (no cron box of your own to maintain), or persisted/versioned agent configs shared
  across teams with no platform/ops capacity to host it themselves.

### Why `louis-agent` uses the Messages API tier

Both higher tiers trade away something this repo depends on:

- **Managed Agents** would mean Anthropic hosts the tool-execution sandbox — incompatible with owning the Docker
  containers, repo mounts, and scoped DevOps/Paymo credentials this repo runs with (see the `IsSensitive` file
  blocking and the tool-approval flow in `AgentEngine`).
- **Tool Runner and Claude Agent SDK** are both Anthropic-specific packages — picking either locks every model call
  to Claude, which conflicts with the `IChatClient` provider abstraction (`LlmClientFactory`) this repo is built
  around.

```mermaid
flowchart LR
    subgraph repo["louis-agent.core"]
        AE["AgentEngine<br/>(tool loop, skills, approval)"] --> IC["IChatClient"]
        IC --> AC["AnthropicClient.AsIChatClient()"]
        IC -.pluggable swap.-> OL["Ollama / other provider"]
    end
    AC --> API["Claude Messages API"]
```

### MCP exposes tools, not a model

`louis-agent.mcp-server` republishes `AgentEngine`'s tools (`WorkspaceTools`, `GitTools`, `DotNetTools`, ...) for
an *external* client's own model to call — it has no `IChatClient`/`AgentEngine` of its own
(`src/louis-agent.mcp-server/Program.cs`: *"The MCP client brings its own model; this server publishes the
agent's tools... so that client can call them"*). That's why `AgentEngine` never calls its own tools through it —
the tools and the loop already live in the same process; routing an in-process call through MCP would only add a
round-trip for nothing. It's also why calling `louis-agent.mcp-server` is a different thing from calling
`louis-agent.api`'s session endpoint: MCP hands over raw tools for the caller's model to drive one at a time;
the API session endpoint runs `louis-agent`'s own full triage → fix → test → PR loop server-side and hands back a
result. Use MCP when another agent should reason with `louis-agent`'s tools itself (any MCP-compliant client works,
regardless of which model is behind it — OpenAI's Agents SDK included); use the API session endpoint when another
agent wants `louis-agent` to autonomously do the whole job and report back.

`MCP_TRANSPORT` picks the transport: `stdio` (default, unchanged) is for a local client that launches this as a
subprocess, like Rider or Claude Desktop. `http` serves Streamable HTTP instead via `MapMcp()`, for a remote client
that isn't local — set `MCP_API_KEY` too, since this exposes the same shell/git/file-write tools over the network
that `stdio` only ever exposed to a local pipe. `MCP_API_KEY` is separate from `louis-agent.api`'s `AGENT_API_KEY`
(falls back to it when unset) so the two services' access can be rotated independently.

## Known limitations

1. **Rider MCP discovery only logs** Rider's tools; they aren't callable yet (needs a full MCP client).
2. **Sessions are in memory** in the API and ACP server; a restart ends them (the web app keeps the transcript).
3. **Ollama tool support is a name heuristic** (`LlmOptions.KnownNoToolsPrefixes`).
4. **Paymo task lookup takes the first match** for a name; ambiguous names can hit the wrong task.
5. **No rate limiting or audit log** beyond the file logs.

## Common errors

- **"ANTHROPIC_API_KEY (or LLM_API_KEY) is required for provider 'anthropic'"** — set it in `config/.env.secrets`.
- **"prompt is too long"** — something put a huge result into the history; check `logs/oversized-tool-results.jsonl`.
- **"adaptive thinking is not supported on this model"** — a pre-4.6 Claude model not listed in
  `LlmClientFactory.ThinkingBudgetModelPrefixes`; add its prefix.
- **Skill not found** — the file isn't loaded for this `AGENT_FUNCTION`, or the name differs (case-sensitive).
- **bash not found on Windows** — install Git for Windows or set `BASH_PATH` to `bash.exe`; WSL's bash is never used.

## Contributing

Branch off `main`, keep changes focused, run `dotnet build` and `dotnet test`, and write commit messages that explain
why. See [ARCHITECTURE.md](ARCHITECTURE.md) for the design, `config/.env.example` for settings, and
[Skills/default.md](../Skills/default.md) for how the agent is instructed to behave.
