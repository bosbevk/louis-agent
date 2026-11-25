# 5. Building block view

> **Explanation.** The parts of louis-agent, from the containers (C4 level 2) down to core's components (level 3).
> **Elsewhere:** each project's files, types and settings in [projects/](../projects/README.md); how the parts work
> together on a message in [runtime view](06-runtime-view.md).

## The layers at a glance

Your application on top, the identity and the packs it picks under it, the core engine between them and the models,
and the evaluation that measures it all.

![Your application, then its identity and packs, then the core engine, then the model providers and the evaluation](diagrams/building-blocks.svg)

## Level 2: containers

![Building blocks, level 2: the containers](diagrams/05-building-block-view-level-2-containers.svg)

Every host is thin. All it does is map the agent's stream to its own protocol, and every bit of behaviour lives in
core. Core is the engine, and it names no product. Who an agent is (its voice, its modes and roles, and the packs it
uses, by name) is an identity. What it can do (its toolsets, their guides and their settings) is a pack in `packs/`.
A framework host knows neither: a demo's program names what it loads and hands the host one `AgentCatalog`.

Every pack stays convertible to an MCP server, so someone could take its tools without our engine.
`PackConvertibilityTests` holds that.

*Decided in:* CL15, [ADR 0010](../adr/0010-folders-by-concern.md), [ADR 0027](../adr/0027-engine-identities-and-packs.md),
[ADR 0028](../adr/0028-packs-stay-convertible-to-mcp.md).

## Hosts

Each project also has its own design doc in [projects/](../projects/README.md).

| Host | Project | Bootstrap | Transport |
|---|---|---|---|
| CLI | `src/hosts/LouisAgent.Cli` | `AgentHost.Build()` | Terminal; interactive or one-shot (`LouisAgent.Cli [prompt]`) |
| ACP server | `src/hosts/LouisAgent.Acp` | `AgentHost.Build()` | [Agent Client Protocol](https://agentclientprotocol.com) JSON-RPC over stdio, for Rider's AI chat |
| HTTP API + a site | `src/hosts/LouisAgent.Api`, with a site: `LouisAgent.CodeReview`, `LouisAgent.Starter`, `LouisAgent.Site` or `LouisAgent.SiteFramework` | `AgentHost.Build()` | ASP.NET Core minimal API; Server-Sent Events; serves its program's Blazor WebAssembly site |
| MCP server | `src/hosts/LouisAgent.Mcp` | `AgentHost.BuildToolHost()` | Model Context Protocol over stdio (default) or Streamable HTTP (`MCP_TRANSPORT=http`); publishes the tools, the client brings its own model |
| Orchestrator (POC) | `demos/louis-agent/hosts/LouisAgent.Orchestrator` | Own `AgentEngine` with `toolsets:` | Not a host for users: an agent that reads a service's error log and calls LouisAgent.Api for fixes ([samples/README.md](https://github.com/bosbevk/louis-agent/blob/pre-cl10/samples/README.md)) |

### CLI

The CLI streams the answer as it's written. Thinking shows in grey, and each tool call gets one status line. Ctrl+C
stops the current reply. You manage the tools the agent built for itself with `/tools`, `/approve <Name>` and
`/reject <Name>`, and those commands are never sent to the model.

### ACP server (Rider)

The ACP server answers Rider's `initialize`, `session/new`, `session/prompt` and `session/cancel`, and keeps one chat
history per session. While it works it streams `session/update` notifications: `agent_thought_chunk` for thinking,
`agent_message_chunk` for the answer's text, and `tool_call` and `tool_call_update` for each tool, which goes from in
progress to completed or failed with a preview of its output.

Files you attach in Rider arrive as `resource_link` blocks, and the server reads them into the prompt. Windows paths
are mapped into the container with `ACP_MOUNT_MAPPINGS`. A file that's only open in the editor isn't sent. Embedded
`resource` blocks are put inline too, and attachments are capped at 100,000 characters. Each Rider chat starts its
own container (`docker compose run --rm -T acp-server`).

### HTTP API and web app

The API keeps its sessions in memory (`AgentSessions`). A session answers one message at a time, and a second one
while it's busy gets a 409. If the client disconnects, or calls `POST /v1/sessions/{id}/cancel`, the reply stops.
A session that sits idle for 4 hours is dropped.

A session picks its toolsets when it opens, with `POST /v1/sessions` and `{ "toolsets": [...] }`. It gets the engine
of that profile from `AgentEngineFactory`, and keeps it for its whole life. No list means `skills` only, and a name
that's unknown or not available here is a 400 that lists the valid ones. Sessions with the same list share one
engine, and so one prefix the provider can cache. `GET /v1/toolsets` lists the names, descriptions and whether each
is available (never the tool definitions), and `GET /v1/sessions/{id}` returns a session's toolsets. A session's
`depth` runs from 0, the agent a person talks to, to `AGENT_MAX_DEPTH` (4 by default), and a lookup agent may not load
`agents`.

*Decided in:* F5.

Every site shares the one API codebase and the framework's web, `LouisAgent.Web`, and each is served by its own
program. The code-review app (`LouisAgent.CodeReview`, served by `LouisAgent.CodingApi`) has every toolset. The
starter (`LouisAgent.Starter`) is the smallest site.
And the framework's two sites, `LouisAgent.Site` for louis-agent.dev and `LouisAgent.SiteFramework` for the
framework's own pages, are served by `LouisAgent.SiteApi` and `LouisAgent.SiteFrameworkApi`.

When the agent asks another agent with `AskAgent`, you see it happen. The asked agent's own events stream to the chat
as `sub_session` events (from `SubSessionFeed` into the API's turn stream), and the delegation card shows its
thinking, tool calls, answer and cost as they come. The model itself only gets the answer.

An answer streams as Server-Sent Events shaped like Claude's: `message_start`, then `content_block_start`, `delta`
and `stop` for `thinking`, `text`, `tool_use` and `tool_result`, then `message_delta` (the stop reason and the turn's
usage and cost) and `message_stop`. An error uses Claude's `{ "type": "error", "error": { "type", "message" } }`
shape, plus the turn's usage when a request had already come back.

Every route is under `/v1` except `/health`. Routes are nouns, mapped in one place (`ApiMap`), and a golden file pins
them.

*Decided in:* CL20, [ADR 0039](../adr/0039-api-routes-under-v1.md).

The web app can also browse the workspace, read-only (`/v1/workspace/entries`, `/v1/workspace/file`), and work with
git. There are routes for the working tree (`/v1/git/status`, `/v1/git/diff`, `/v1/git/stage`, `unstage`,
`discard`), for commits (`/v1/git/commits`, `/v1/git/commits/{sha}`) and for branches (`/v1/git/branches`,
`/v1/git/branches/{name}`, `/v1/git/merge`). They reuse the file tools' checks on paths and secret files
(`WorkspaceBoundary`, `SensitiveFiles`). The Branches tab queues merges and deletes (`GitHistory`): every click is
kept and they run one at a time, in the order you clicked, and only the branch being worked on shows as busy
("Merging…", "Queued").

Every `/v1` route needs `AGENT_API_KEY`, unless the code marks it open, as it does for the prices, the rates, the
feeds and a site's search of its places. A host may also open a few identities to the public within a daily cap
(`OPEN_IDENTITIES`, `OPEN_SESSIONS_PER_DAY`), as louis-agent.dev opens its guide. The
admin routes take the admin key, and `/health` and the app's own files stay public. Without a key the API refuses to
start, unless `AGENT_AUTH=off` says you meant it. That's one pure rule in core, `Guardrails/Auth/HostAuth`, which the
MCP server shares.

*Decided in:* FX18.

The web app is Blazor WebAssembly, served by the API with `MapStaticAssets` and `MapLouisAgentSite`. When you open a
page's address, the server answers with that page already rendered as HTML inside `index.html`, until the app takes
over. The app keeps chats in the browser's `localStorage`. Its themes are named among its areas, and the frame's switch
offers them.

*Decided in:* F121.

### MCP server

The MCP server publishes every toolset this instance can load: the packs' tools (files, git, .NET, the shells, the
web, and agents when their settings are set), the skills and the agent-built tools, plus three read-only ones,
`list_skills`, `get_skill_info` and `get_skill_documentation`. It stays in step with the engine: when
`AgentEngine.ToolsChanged` fires, it sends `notifications/tools/list_changed`. Approving an agent-built tool is for
the user only, so it isn't offered over MCP. The packs' read-only feeds are published too, as resources at
`feed://{name}`.

By default it talks over stdio, for a local client that starts it as a subprocess, such as Rider or Claude Desktop.
`MCP_TRANSPORT=http` serves Streamable HTTP instead (`MapMcp()`), for clients elsewhere. That puts the shell, git and
file-writing tools on the network, so set `MCP_API_KEY` (sent as `x-api-key` or `Authorization: Bearer`; without it
`AGENT_API_KEY` is used). With neither set it refuses to start, unless `AGENT_AUTH=off` says you meant it, the same
`Guardrails/Auth/HostAuth` rule as the API's.

*Decided in:* FX18.

## Level 3: core's components

![Building blocks, level 3: the engine's parts](diagrams/05-building-block-view-level-3-core-s-components.svg)

Dependencies point one way, down this picture; `ArchitectureTests` fails on one that points up
([0010](../adr/0010-folders-by-concern.md)).

## Core abstractions

### The model's settings: `LlmOptions`

`LlmOptions` (`Config/LlmOptions.cs`) says which model to talk to and how. `LLM_PROVIDER` is `anthropic`, `ollama` or
`openai-compatible`. If you leave it unset it's worked out from the model's name: a `claude…` model is `anthropic`,
any other model is `ollama`, and no model at all is `anthropic`. `LLM_MODEL` defaults to `claude-haiku-5-5`. Beside
it sit `LLM_ENDPOINT`, `LLM_API_KEY` (which falls back to `ANTHROPIC_API_KEY`), and `ANTHROPIC_WORKSPACE_ID` for a key
that isn't scoped to a workspace.

Some Ollama models can't take tools. Unless `LLM_SUPPORTS_TOOLS` says otherwise, models whose names start with
`llama2`, `codellama`, `deepseek-r1`, `deepseek-coder` or `qwen2` run without them. `LLM_THINKING` is `off`, `low`,
`medium` or `high`, and defaults to `medium` for Anthropic and `off` for everything else.

### The agent's settings: `AgentOptions`

`AgentOptions` (`Config/AgentOptions.cs`) holds the rest. `WORKSPACE_ROOT` is the folder the agent works in; without
it, it's the repository root found from the working directory. `SKILLS_DIRECTORY` is where the agent writes its own
skills and tools, and `LOG_DIRECTORY` is where logs go. The web search settings live here too: `WEB_SEARCH_PROVIDER`,
`TAVILY_API_KEY`, `GOOGLE_SEARCH_API_KEY` and `GOOGLE_SEARCH_ENGINE_ID`. For asking other agents there's `AGENT_PEERS`
(`name=url` pairs), `AGENT_PEER_KEYS` (each peer's own key, by the same names) and `AGENT_API_KEY`, which is sent to a
peer that has no key of its own. `AGENT_MAX_DEPTH` says how deep a chain of asks may go.

A pack's own settings are the pack's, not core's: the docs pack's `DOCS_ROOT` is read by the docs pack, and
Rider's MCP settings (`RIDER_MCP_ENDPOINT`, `RIDER_MCP_AUTO_DISCOVER`, `RIDER_MCP_PROJECT_PATH`) by the ACP server.

*Decided in:* F5, CL57.

### Finding the model: `LlmClientFactory`

`LlmClientFactory` (`Llm/LlmClientFactory.cs`) is the registry of the providers a program put together. It finds the
`ILlmProvider` that `LLM_PROVIDER` names, wraps its client for prompt caching, and asks it for its usage mapper and
its cache markers. Each provider is a small library on core.

`LouisAgent.Core.Anthropic` makes its client with `AnthropicClient.AsIChatClient(model, 16000)`. Models before
Claude 4.6, Haiku 4.5 among them, reject adaptive thinking, so for those `ThinkingBudget` turns the reasoning setting
into a fixed thinking budget: 2,048 tokens for low, 4,096 for medium and 8,192 for high. `LouisAgent.Core.Ollama`
uses `OllamaApiClient` with a ten-minute timeout, and `LouisAgent.Core.OpenAi` uses
`OpenAIClient(...).GetChatClient(model).AsIChatClient()`.

*Decided in:* CL57.

### Provider strategies

Sometimes Anthropic, the OpenAI-compatible endpoints and Ollama each need different logic for the same job. That job
then becomes a strategy: one interface, one implementation for each provider that differs, and a default for the rest
(often one that does nothing). The provider `LlmClientFactory` finds is the strategy's home, so the choice is made
once. Callers only ever see the interface. That's why `AgentEngine`, the tools and the hosts never branch on a
provider's name, and why adding a provider means adding implementations, not editing callers. Only reach for one when
providers really differ; a job every provider does the same way needs none.

| Concern | Interface | Implementations | Status |
|---|---|---|---|
| How usage is reported | `IUsageMapper` | `StandardUsageMapper` (default), `AnthropicUsageMapper` (cache writes, and their split by lifetime from the raw response, F4) | Built (F1) |
| Prompt caching | `IPromptCachingStrategy` | `AnthropicPromptCaching` (two markers), `NoPromptCaching` (default: OpenAI-compatible caches prefixes on its own, Ollama), applied by `PromptCachingChatClient` | Built ([F4](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F04-prompt-caching.md)) |
| Thinking settings | `ThinkingBudget` (core.anthropic) | Anthropic's fixed budget for pre-4.6 models, wrapped on the client by `AnthropicProvider` | Built (CL57) |
| Context window | to be designed | Ollama needs `num_ctx` sent; Claude's window is fixed | Planned ([F15](../features/F15-model-limits.md)) |
| Errors, retries and limits | to be designed | Each provider reports rate and account limits differently | Planned ([F8](../features/F08-reliability.md)) |

The same pattern shows up away from the providers too: `IRatesMapper` for where exchange rates come from, and
`IWebSearchProvider` for the search engines (SearXNG, Tavily, Google and DuckDuckGo).

*Decided in:* [ADR 0003](../adr/0003-provider-strategies.md).

### The engine: `AgentEngine`

`AgentEngine` (`Agent/AgentEngine.cs`) doesn't care which provider it talks to. It owns the turn loop, the rules of
the history and the chain of chat clients, and it's the one face the hosts talk to. The rest has its own classes:
`ToolRegistry` keeps the tools in name order, `ToolCallGuards` (in `Agent/Turns/`) guards each call, `SkillTools`
holds `ExecuteSkill`, `CreateSkillFile`, `ReloadSkills` and the skills the system prompt is built from, and
`AgentBuiltTools` holds `CreateTool`, `/approve` and `/reject`. Core's pure safety checks are in `Guardrails/`: the
hosts' key rule (`Auth/`) and the address rule (`Network/SsrfGuard`). The console chat belongs to the CLI
(`ConsoleChat`). Everything the engine sends uses LF line endings, whatever the checkout.

A host starts a chat with `StartConversation()`, which gives it a `Conversation`: the chat's messages, opening with
the system prompt, which the host can read and add to but never change. It hands the engine a message with
`StreamPromptAsync(conversation, input)`. That streams `ChatResponseUpdate`s back (text, `TextReasoningContent`, tool
calls and their results) and adds the finished turn to the conversation.
`ProcessPromptAsync` does the same without streaming. `FunctionInvokingChatClient` runs the loop, up to 10 tool rounds
per request, and gives the model the full error when a tool fails. A reply may be up to 16,000 tokens, and
`LLM_THINKING` sets `ChatOptions.Reasoning`.

The engine gets its tools through `toolsets:`. Each tool object's public methods become tools, plus `ExecuteSkill`.
Those come from a session's toolsets in the catalogue (`AgentEngineFactory`), or from another program's own tools, as
the orchestrator's `ServiceTools` are. `selfExtension:` adds `CreateSkillFile`, `ReloadSkills`, `CreateTool` and
`ListAgentTools`, which is what the `skills` toolset is.

### Toolsets

Every toolset is a `ToolsetDefinition`: a name, a one-line description, the settings it needs, its tool classes and
their factory, and its guide. Core itself offers just two, in `ToolsetCatalog` (`Agent/Toolsets/`): `agents`, for
asking another agent (it needs `AGENT_PEERS`), and `skills`, which the engine provides. Every other toolset (`files`,
`git`, `dotnet`, the shells, `web` and the rest) comes with its pack.

`AgentCatalog` (`Agent/Catalog/`) is built once, by the program that composes the host. It lists core's toolsets
first and then each pack's, and it owns every tool name. An agent-built tool can't take one of those names, and a
name that two toolsets, or a toolset and the engine, share stops the start-up. It also resolves the list a session
asks for.

*Decided in:* CL15, [ADR 0076](../adr/0076-core-ships-no-agent.md).

`AgentEngineFactory` (`Hosting/`) builds one engine per profile, cached by its sorted toolset list, its role and its
level. Past 16 engines, the one used longest ago is dropped. Each engine has only its own tools, and only their
guides go in its system prompt. The factory logs each profile's tool count and an estimate of its fixed prefix. The
CLI, the ACP server and the MCP server use every toolset the instance can load; the API's sessions choose theirs.

*Decided in:* F21, CL8.

When the agent asks another agent with `AskAgent`, a sub-session runs on a peer as the address the parent named:
an identity in its own voice, or one of its roles, with the toolsets its own file declares. There can be at most 3 in
a turn, 5 minutes each, and the asked agent may ask the ones its own identity's `Calls` allow, down to
`AGENT_MAX_DEPTH`; an agent already in the chain of asks is refused. An identity may fix the order of some of those asks with a sequence: asking its first address runs every step in order, in code, as one ask. Only the answer comes back, under a line that says who answered and which tools it called,
and its usage records carry the parent turn (`parent`). If the answer comes back empty, because the asked agent used
up all its tool rounds, it's asked once more in the same session. A sub-session gets no routing: its toolsets come
from its own file.

*Decided in:* F127.3.1, F127.5, FX11.

## Skills

The system prompt is built from plain markdown files. The engine factory's `SkillLoader` puts it together for each
profile, from a `CompositeSkillProvider` of files `MarkdownSkillProvider` reads, in this order.

First come the principles (`shared/principles.md`): never lie, cheat, steal or murder. They're the same for every
agent at every level.

Then comes the role, so who the agent is frames everything after it. At level 0 that's a mode: its voice, and then
its routing, which says which toolset a task needs and how asking another agent works. The coding agent's default is
`louis-agent/voice`; the triage team's manager has `triage/voice`; the orchestrator has
`louis-agent/voice-orchestrator`. At level 1 it's a subagent's voice, such as `triage.sorter`'s, which
says what it does and how it reports. A lookup agent's voice comes after its agent's `lookup.md`. Below level 0 there's
no routing. At level 0 the identity's own skills follow its voice: the ones it always loads in full, then a list of
the ones it reads when it needs them.

After that comes the guide of each of the profile's toolsets, and no other, so the prompt never describes a tool the
session doesn't have. With the `agents` toolset, the addresses the identity may ask are listed after its guide. And
only with the `skills` toolset, every function skill the agent wrote itself is added
(`agents/functions/*.md` in the skills folder).

Core's own files are in `src/engine/LouisAgent.Core/`'s prompt folders (the principles in `shared/`, the `agents`
and self-extension guides in `toolsets/`). Each identity and pack brings its own, registered with `SkillFileSource.Use`
at start-up. All of these are read only from their assemblies, so what the model reads is fixed by the build. Only
what the agent writes (its function skills and its tools) lives on disk, in the skills folder: `SKILLS_DIRECTORY`, or
else a `skills/` above the working directory, found by its README.

A markdown skill is a named procedure with a fenced script, in bash, PowerShell or Python, chosen by the fence's
language. The model runs it with `ExecuteSkill(name, jsonArgs)`. The arguments become `SKILL_ARG_*` environment
variables and are never spliced into the script's text.

*Decided in:* [ADR 0017](../adr/0017-skills-by-level.md), [ADR 0018](../adr/0018-built-in-prompts-embedded.md),
[ADR 0019](../adr/0019-modes-one-file-per-product.md), [ADR 0020](../adr/0020-principles-for-every-agent.md),
[ADR 0114](../adr/0114-an-identitys-skills-load-always-or-on-demand.md), CL9, F129.

## Tools

Every session also gets `GetToday` (`Agent/EngineTools/Clock`), because a model doesn't know what day it is. It gives
the date, the time and the part of the day in the user's time zone when they shared it (`UserPlace`), or else the
host's (`HOST_TIME_ZONE`), or else the machine's own. A sub-session gets the user's attachments by name
(`SessionAttachments`) and their place (`UserPlace`), both set by the API for each turn. The files pack's
`Documents/DocumentText` reads PDF, Word and spreadsheets for `ReadDocument`, `ReadTable` and `FetchUrl`, with
PdfPig, DocumentFormat.OpenXml and ExcelDataReader.

*Decided in:* F22.

The public methods on a tool class become tools through `AIFunctionFactory.Create`. So every public method on a tool
class is shown to the model, and a helper must be `internal` or `private`. Names and parameter names are taken
exactly as written. These are the tool classes, and the pack each lives in:

| Class | Tools |
|---|---|
| `WorkspaceFileTools`, `WorkspaceOrganiseTools`, `WorkspaceBrowseTools`, `WorkspaceSearchTools`, `DocumentTools` | The files pack: read / write / append; create a directory, delete, rename, copy; list, tree, exists, file info; search text, find files; documents and spreadsheets |
| `GitChangeTools`, `GitBranchTools`, `GitRemoteTools`, `GitHistoryTools`, `GitConfigTools` | The git pack: status, diffs, staging, commit, stash; branches, merge/rebase, tags; push/pull/fetch; log, blame, history; config |
| `DotNetTools` | The dotnet pack: `DotNetBuild`, `DotNetTest`, `DotNetRun`, restore, clean, packages, project and SDK info |
| `PythonTools`, `PowerShellTools`, `BashTools` | The shell pack: run code or scripts (bash is Git Bash on Windows, never WSL) |
| `WebTools`, `QuoteTools`, `CurrencyTools` | The web pack: `WebSearch` (via `IWebSearchProvider`), `FetchUrl` and `ReadRenderedPage` (through the SSRF-guarded `PageFetcher`); `VerifyQuote`; `ConvertCurrency` |
| `AgentEngine` | `ExecuteSkill`, `CreateSkillFile`, `ReloadSkills`, `CreateTool`, `ListAgentTools` |

The agent can also build a tool for itself (`ScriptTool`). With `CreateTool` it writes
`demos/louis-agent/skills/tools/pending/{Name}.tool.md`: typed parameters and a script. It works at once, but only as
pending. It's saved for every session only when you type `/approve <Name>`, and `/reject <Name>` throws it away.
Approved tools load at start-up, and their scripts get their arguments as JSON on stdin, never in the code.

## Bootstrap: `AgentHost`

The CLI, the ACP server and the API all start with `AgentHost.Build(catalog)`, using the catalogue their program
handed them.

It starts by loading `config/.env.secrets` and then `config/.env`, searching upward from the working directory, and a
legacy `./.env`, then the LLM profile's `config/.env.{LLM_PROFILE}`. A variable that's already set in the process
wins, and each file only fills in what the ones before it left unset.

With the settings read, it binds `LlmOptions` and `AgentOptions` and sets up logging
(`AgentLogging.Configure(LOG_DIRECTORY)`). It registers the identities' and packs' prompts with `SkillFileSource.Use`,
and checks that every name in `ROLES_ENABLED`, and the default mode, can be found. That way a missing identity stops
the host here, not at its first chat.

Then it creates the `IChatClient`, the usage ledger and an `AgentEngineFactory`, with `LLM_THINKING` and the display
currency. The host's own engine has every toolset this instance can load, with the approved agent-built tools when it
has `skills`. Last, it starts whatever tool discovery the program handed it, which under the ACP server is Rider's.

`AgentHost.BuildToolHost()`, for the MCP server, does the same without a model, because the client brings its own.

*Decided in:* CL15, CL33, CL57.
