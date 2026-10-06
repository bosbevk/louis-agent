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
│   └── Default/DotNet/DevOps/Paymo/CodeReview/TimeLogging SkillsProvider.cs
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
└── louis-agent.mcp-server/   # Publishes the toolset over MCP (no LLM)

tests/louis-agent.core.tests/ # NUnit; Config, Loaders, Providers, Tools, mcp
docker/                       # Dockerfile.{acp-server,api,agent,mcp-server}, docker-compose.yml
config/                       # .env and .env.secrets (both ignored), their examples, Rider config examples
Skills/                       # default.md + *-skills.md (system prompt), tools/ (agent-built tools)
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
