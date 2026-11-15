# louis-agent.core — design

> **Kind:** library (`net10.0`) · **Path:** `src/engine/louis-agent.core/` · **References:** Microsoft.Extensions.AI,
> Anthropic SDK, OpenAI and Ollama clients, dotenv.net, YamlDotNet (a prompt's header, CL27) · **Used by:** every host, the orchestrator, the tests
> **Also shows:** `composite-structure`, `component`

![The engine, and its parts: the loop, the guards, usage, skills and prompts, providers and bootstrap](diagrams/louis-agent.core.svg)

## Purpose

Core is the engine. It's everything that makes louis-agent an agent, no matter how you talk to it: the tools, the
skills, the loop that runs the model's tool calls, the model providers, the settings, the logging and the start-up.

The hosts are the doors in. The terminal (CLI), Rider, the HTTP API and the MCP server each speak their own
protocol, and all they do is translate it to and from `AgentEngine`. So when you want the agent to behave
differently, the change belongs here, not in a host.

This doc first walks you through one turn, from a message coming in to the answer going out. Then it tells the
parts that make a turn work, one at a time. At the end there's a table of every folder and type, for when you need
to find something.

## How it works

Here's one turn from start to finish, before we look at each part on its own.

![One turn: the message is appended, the model asked, each tool call run behind the guards round after round, and the whole turn appended at the end](diagrams/louis-agent.core-flow.svg)

![louis-agent.core: How it works](diagrams/louis-agent.core-how-it-works.svg)

### 1. Starting up

Every host starts the engine the same way, by calling `AgentHost.Build()`. First it reads the settings files. It
looks for `config/.env.secrets` and then `config/.env`, searching upward from the folder you started in. If a
variable is already set in the environment, that one wins, so you can always override a file from the outside.

With the settings read, it fills in `LlmOptions` and `AgentOptions`, starts logging to file and loads the skills.
Then it creates the chat client (the thing that talks to the model) and the `AgentEngine` itself. After that it
loads the tools the agent built for itself that you've already approved. Last, it starts whatever tool discovery
the program handed it. Today that's Rider's, which comes from the ACP server.

The MCP server doesn't need a model, because the client on the other side brings its own. So it calls
`BuildToolHost()` instead, which does the same work without the model.

*Decided in:* CL57.

### 2. A turn

A turn starts when a host hands the engine your message, through `StreamPromptAsync` (streamed) or
`ProcessPromptAsync` (all at once). The engine adds your message to the conversation's history. Then it sends the
model three things: the history, the tools it may call, and the reasoning settings.

The model usually answers by asking for a tool. `FunctionInvokingChatClient`, from Microsoft.Extensions.AI, runs
that tool, gives the model the result, and asks again. It keeps going like that for up to 10 rounds. While it
works, everything streams back to the host as it happens: the model's thinking, its text, each tool call and each
result.

When the turn is done, the whole turn is added to the history in one piece.

### 3. The guards around every tool call

A tool call can go wrong in a few ways, and each one has a guard.

A tool can hand back far too much. If a result is over 50,000 characters, a separate model call, with no tools,
summarises it first, so the model's context doesn't fill up with one page.

The model can also get cut off by its output limit halfway through writing a call. A half-written call is never
run. When the turn is streaming, the model is asked to carry on instead, up to 3 times.

And if a tool throws an exception, the engine doesn't crash. The exception's message goes back to the model as the
tool's answer, so the model can see what went wrong and try something else.

### 4. Measuring what every request uses

Every request to the model is measured. `UsageRecordingChatClient` sits inside the tool loop, so each tool round
and each summary is its own request with its own line in the usage ledger (the file where every request is
written down).

A line holds the token counts, the model, how long it took, why the model stopped, and the `UsageScope` the host
opened for the turn. The scope says which host, session and turn it was, why the request was made, and any task,
run or service tags. The token counts come from the provider's `IUsageMapper`, since every provider reports them a
bit differently.

A line holds counts and ids only. The prompt's text is never written. And if a request is cancelled before it
finishes, it's still written down, with its tokens as unknown rather than 0, because 0 would be a lie.

*Decided in:* F1, FX9.

### 5. Putting a price on it

`PricingUsageSink` wraps the ledger, so each record gets its price at the moment it's written. The price table is
loaded once, by `AgentHost`. It comes from `PRICES_URL` if that answers (the API serves its own at
`GET /v1/prices`), or else from `PRICES_FILE`, or else from the tracked `config/prices.json`.

`PriceTable.Find` finds a model's price by the longest matching start of its name. `CostCalculator` then works out
the cost. It picks the price tier from the whole prompt: the input, the cache reads and the cache writes together.
Some models have a `long_prompt` tier. Haiku 5.5 is one: above 100,000 tokens, the whole request costs more, not
just the part past the line. Cache writes are priced by how long they're kept, and the result is rounded to 6
decimals. Thinking is already counted inside `output`, so it isn't added on top.

A record's `cost` names the table's `as_of` date. That way a later price change never rewrites an old record. And
when there's no price for a model, the cost is `null`, never 0.

*Decided in:* F2, [ADR 0007](../adr/0007-measure-every-request.md).

### 6. Showing you what it cost

The turn's `UsageScope` collects its priced records as they come in. A summary request counts toward its own
turn. When the agent asks another agent, that whole conversation counts toward the parent's turn as one
`sub_session` record per reply.

Every host shows the same totals, from `UsageReport.Total`, each in its own place. The API sends them on
`message_delta`, and on the `error` event when a turn fails. The web apps show them per answer and per chat. The
CLI shows them after each answer. Rider shows them when `USAGE_IN_CHAT` is on. And `/usage` gives you a session's
total.

A turn's line keeps the four kinds of token apart (in, cache write, cached, out), because each one has its own
price. It also tells you how many requests it couldn't put a price on.

*Decided in:* F3, FX6.

### 7. Showing it in your currency

The ledger always stays in the currency the bill comes in, which is US dollars. A host only converts when it
shows you a total. It uses `DISPLAY_CURRENCY` if you set it, or else the currency of `HOST_CULTURE`.

`HOST_CULTURE` is written once on your PC by `usage-probe --host-culture`. That's because a container can't see
which region you're in, and running the whole container in your culture would change the number format of every
tool.

The exchange rates come from `RATES_URL`, which is Frankfurter by default. An `IRatesMapper` turns that reply into
our own `rates.json` shape. If it can't be reached, the tracked `config/rates.json` is used. If a rate is
missing, you see the figure as charged, never a guess.

Amounts always use `.` for the decimal point (`R35.00`). And a currency symbol that would be confused with `$` is
written as its code instead.

*Decided in:* F14.

### 8. Prompt caching

The model's provider can keep the start of a prompt it has seen recently, so the next request that starts the same
way is cheaper and faster. That's prompt caching. `PromptCachingChatClient` sits innermost, right around every
provider's client. It asks the provider's `IPromptCachingStrategy` how to mark the request: `AnthropicPromptCaching`
for Claude, `NoPromptCaching` everywhere else.

There are two markers. The first goes on a copy of the system prompt's last block. It caches the tools and the
system prompt, for as long as `LLM_CACHE_TTL` says: 5 minutes by default, or an hour. The second is Anthropic's
automatic marker for the conversation, which is always 5 minutes. The history we store is never marked itself.

A cache only works if the start of the prompt is the same, byte for byte, every time. Three things keep it that
way. The tools are sorted by name (and two tools with the same name stop the start-up). The system prompt holds
nothing that changes per request. And thinking is fixed for each engine.

You can turn it all off with `LLM_CACHE=off`. And `usage-probe --cache-gaps` reads the gaps between requests in a
ledger, to help you pick the lifetime.

*Decided in:* F4.

### 9. Toolsets per session, and asking another agent

Nothing loads at start-up. Instead, a session names its toolsets when it opens, and keeps them for its whole life.
`AgentEngineFactory` builds one engine per sorted list of toolsets and keeps up to 16 of them, all sharing the chat
client, the ledger and the settings. The system prompt only carries the guides of that session's toolsets
(`ToolsetCatalog`). A session that names no toolsets gets `skills`.

An agent can also ask another agent, by its address, with `AskAgent` from the `agents` toolset. There are fences
around that. The address has to be one the asking identity lists in its `Calls`, on a peer named in
`AGENT_PEERS`. It goes one level deep only, at most 3 asks a turn, 5 minutes each. And the asked agent stops using
tools once its prompt nears 80,000 tokens.

The chat keeps one conversation per address. So when you ask the same agent again, it carries on where it left
off. The asked agent's own file says which toolsets it loads.

*Decided in:* F5, [ADR 0006](../adr/0006-toolsets-per-session.md), F127.3.1,
[ADR 0117](../adr/0117-one-address-for-every-agent-call.md), FX14, F26, F33,
[ADR 0025](../adr/0025-a-role-declares-its-toolsets.md).

### 10. Minions

A minion is a small helper on a cheap model. A specialist can hand a minion one task and one of its own tools. The
cheap model is the one `MINION_LLMS` names, through a session that names its LLM profile.

This is built but not in use yet. No session is given `minions` until the cook is measured with them and without
them (the TODO's *F30's minions tested*).

A minion's engine is small on purpose: the principles, its product's base, its role, and that one tool. If the
specialist asks for a tool it doesn't hold itself, the request is refused before any HTTP call is made.

An LLM profile takes nothing from the instance except its API key. So a local model never picks up Claude's
`LLM_THINKING` by accident. The profile goes to the peer as data, and the peer refuses one it doesn't offer
(`GET /v1/llm-profiles` lists the ones it does).

`VerifyQuote(url, quote, product)` checks a quoted price against the page itself, in code, through the same
guarded reader as `FetchUrl`. A quote that's only a price has to name its product too.

*Decided in:* F30, FX16.

### 11. The web

The agent searches with `WebSearch`, behind `IWebSearchProvider`: SearXNG running in Docker, or a licensed API.
Some pages are drawn by JavaScript, so a plain fetch would come back empty. Those are read through one headless
Chrome behind `IPageRenderer`, driven by PuppeteerSharp over CDP (Chrome's remote-control protocol). That browser
lives in `louis-agent.core.web`.

The browser is locked down. Every request it makes is held to the address rule (`SsrfGuard`), it can't open
WebSockets, and it runs on a network apart from the database. It also says honestly what it is
(`BROWSER_USER_AGENT`).

`WEB_REPLAY` records the pages a session reads and can play them back. That way an eval reads exactly the same web
twice.

Building it taught a few things worth knowing. The browser has to check every request a page makes, not just the
first address, because it follows redirects and loads images and scripts by itself. Request interception doesn't
see WebSockets, so `WebSocket` is made to refuse before the page's own scripts run, in every frame. A failed name
lookup counts as a refusal. The request handler catches everything, because an exception escaping its
`async void` would end the process and every chat with it. The turn's cancellation reaches the browser, which then
closes the page. And there's one guarded browser (`IWebBrowser`, also in `louis-agent.core.web`) that serves the
renderer and every shop, with a visit that can carry its own cookies and one script of ours.

SearXNG is AGPL-3.0. We run it unchanged, beside us, over HTTP. The endpoint we sell uses a licensed search API
instead, because the search engines SearXNG asks don't license their results for that. On the day it was
measured, only Bing answered from Louis's address.

*Decided in:* F32 to F34, CL33, [ADR 0038](../adr/0038-the-browser-says-what-it-is.md),
[ADR 0023](../adr/0023-copyleft-service-beside-us.md).

### 12. The engine from the inside

The full step by step of a turn is in [How a turn works](../architecture/06-runtime-view.md). Here's what happens
when a host hands the engine a message:

![One turn: the engine runs the tool loop, each model request measured and each tool call guarded, and the turn joins the history at the end](diagrams/louis-agent.core-diagrams.svg)

And these are the types a host meets on its way in:

![The types a host meets: AgentHost builds the context from a catalogue of identities and packs, an engine per toolset list, and the model from a provider](diagrams/louis-agent.core-interface.svg)

Inside, the engine builds six parts once, in its constructor. In the picture below, the left side is what a host
asks of it, each passed on to the part that does the work. The right side is what it's handed from outside. A
plain line joins two parts where one is built with the other.

![AgentEngine from the inside: what is asked of it on the left, its six parts and which is built with which, and what it is handed on the right](diagrams/louis-agent.core-agent-engine.svg)

## Structure

This part is for looking things up: every folder and file of core, and what's in it.

| Path | What's in it |
|---|---|
| `Agent/AgentEngine.cs` | The engine as a facade (S, CL15): wires the skills, the tools, the guards and the pipeline once; a turn, a command and the self-extension tools go to the classes below |
| `Agent/Turns/` | `TurnRunner` (one turn: the history seeded and kept current, the reply whole or streamed, a cut-off reply continued; it opens the turn's span (F52), made current again after every `yield` of the streamed form), `ChatPipeline` (the middleware in its order, and the summaries' client; every model request traced (OpenTelemetry, no content)), `ToolCallGuards` (cut-off calls never run, oversized results summarised), `ToolCallText` (a call's arguments on one line for the CLI and Rider) |
| `Agent/EngineTools/OwnTools.cs` | The tools an engine registers itself: `ExecuteSkill` and the clock in every session, the toolsets' objects, and the four self-extension tools as the `skills` toolset |
| `Agent/Commands/UserCommands.cs` | `/tools`, `/approve`, `/reject`, `/usage`: answered without the model, from the agent-built tools, the skill files and the session's usage |
| `Agent/ToolRegistry.cs` | The tools in name order (one name each), public methods as tools, discovered tools |
| `Agent/Turns/ToolCallGuards.cs` | Around each call: logging, cut-off calls never run, oversized results summarised (a model call), tools kept on the final request; a sub-session's prompt budget (FX14: past `SUB_SESSION_PROMPT_BUDGET` the next request answers without tools) |
| `Agent/Toolsets/ToolsetCatalog.cs`, `ToolsetDefinition.cs`, `ToolsetContext.cs` | Every toolset a session can load (F5): name, description, the settings it needs, its tool class and guide; resolves a session's list (`ToolsetException` for an unknown or unavailable one) |
| `Agent/Catalog/AgentCatalog.cs` | The catalogue the composition root builds from the contracts an identity or a pack implements (`IAgentIdentity`, `IAgentPack`, `IDataFeed`; CL15, ADR 0027; they live in `louis-agent.composition`'s `Packs/`): core's toolsets then each pack's, every tool name, the packs' read-only feeds (ADR 0070), their data uploads (`Imports/`: `IDataImport` and its records, ADR 0071), the start-up checks (a name nothing provides, a name twice, a `Requires` cycle) |
| `Agent/EngineTools/SkillTools.cs` | `ExecuteSkill`, `CreateSkillFile` (to `agent/functions/pending/`, FX20), `ReloadSkills`, the user's `/approve` and `/reject` of a skill file, and the skill set the system prompt is built from |
| `Agent/EngineTools/AgentBuiltTools.cs` | `CreateTool`, `ListAgentTools`, `LoadApprovedTools`, `/approve`, `/reject`, running their scripts |
| `Guardrails/` | `WorkspaceBoundary` (relative, inside the root, no symbolic links), `SensitiveFiles`, `SsrfGuard` (public addresses only, every hop and at connect time) |
| `Hosting/AgentHost.cs`, `AgentHostContext.cs` | Bootstrap for every host (S, CL15: the flow only): options, client, prices, rates, ledger, engine; `UsageLedger` (where the records go), `ToolsOnlyChatClient` (the MCP server's stand-in) |
| `Hosting/EnvFiles/HostEnvironment.cs` | The .env files a host loads (secrets, .env, the LLM profile), and the profile-name rule |
| `Hosting/Prompts/SkillLoader.cs` | The system prompt assembled from skill files: the principles, the role, the guides of the session's toolsets, the function skills, each built-in one as its body from `PromptCatalog` (no header, CL27); `ToolsetsOf`, the toolsets a role's header always loads (F33, ADR 0025), which the API's sessions add |
| `Hosting/SessionLlms/SessionLlmLoader.cs` | The models a session may name (F30): their profiles read, the minions' choices checked |
| `Hosting/AgentEngineFactory.cs` | One engine per toolsets + role + level (F5, F21, CL8), shared by the sessions that ask for the same; checks the request and builds the engine, and keeps the cache in `Engines/` |
| `Hosting/Engines/EngineCache.cs`, `ProfileKey.cs` | The factory's cache (a class: built once on first use, outside the lock; the least recently used of 16 is dropped) and the key it holds engines by (a record: toolsets, role, level, LLM profile, tool; its `Text` is what `Profiles` shows) |
| `Hosting/Discovery/IToolDiscovery.cs`, `NoToolDiscovery.cs` | The seam for tools found outside the engine (CL57): a host hands its discovery in through `HostServices.Discovery` and `AgentHost` starts it once the engine is built; none looks nowhere. Rider's is the ACP server's ([louis-agent.acp](louis-agent.acp.md)) |
| `Logging/AgentLogging.cs` | Serilog behind `ILogger`, configured once per process: the console on stderr (`[INFO]`, or a tag such as `[TOOL]`), and with `LOG_DIRECTORY` a run file per process and the JSON-lines records; with `OTEL_EXPORTER_OTLP_ENDPOINT` every line also to that OpenTelemetry collector, named by `ServiceName` unless `OTEL_SERVICE_NAME` says otherwise (F52); its `Configure(logDirectory, content)` also sets what the lines show of values (F18 step 4f) |
| `Telemetry/IHostTelemetry.cs`, `NoTelemetry.cs` | The seam for what a host does with telemetry (CL33): add the log sinks, start the tracer once the .env files loaded, flush; `NoTelemetry` does nothing but install the crash handler. The tracer and the collector's sink are [louis-agent.core.telemetry](louis-agent.core.telemetry.md)'s, handed in through `Hosting/HostServices` |
| `Telemetry/TurnSpans.cs`, `SpanTag.cs` | A turn's span (F52), `invoke_agent {agent}` (the GenAI conventions' name), opened by `TurnRunner` around both forms of a turn; tagged with the agent (`AgentEngine.Name`: the profile's role, or the default mode, set by the factory) and, from the usage scope, the session, host, turn, run and task (`SpanTag`, wire names the conventions' or `louis_agent.*`; a missing id left off). Under it Microsoft.Extensions.AI opens no `orchestrate_tools`: the model requests and tool calls are its children. `Fail` sets the error status with no description and records the exception (type, message, stack) as an event. A sub-session's turn nests under the parent's tool call across the HTTP hop with nothing of ours: `HttpClient` writes the W3C `traceparent` and ASP.NET Core reads it (held by `PeerTraceTests` in core and the API) |
| `Hosting/HostServices.cs` | What a host's program hands `AgentHost.Build` besides its catalogue (CL33): how to build the web access (`WebAccess.Create`) and the telemetry (`OtlpTelemetry.Instance`); what is left out is the Null Object |
| `Telemetry/LastChance.cs`, `Messages/TelemetryLogMessage.cs` | The process's last word (F52), installed by the host's telemetry (`IHostTelemetry.Start`, collector or not, CL33): an exception nothing caught (`AppDomain.UnhandledException`) is one Critical line with the exception object (type, message, stack), then the logs and the spans flush; a faulted task nobody awaited (`TaskScheduler.UnobservedTaskException`) is marked observed and logged as an Error, no flush (Louis's pick I1). Log an exception once, where it is handled |
| `louis-agent.composition`'s `Logging/Content/LogContent.cs`, `LogContentMode.cs` | F18 step 4f: what a log line shows of a value it was given. `LOG_CONTENT=hashed` (the default): `#` + 12 hex digits of an HMAC-SHA256 under a random per-process key (made at start-up, never written, kept across `Configure`) + the length, e.g. `#3f9c0a1b2d4e (10)`, so equal values match inside a process and a list of towns can't reverse them; `full`: the values. `Of(value, cut)`, `OfArguments(arguments)` (names in order, each value hashed), `ModeOf(setting)` (parsed once; unknown throws). Used by the tool lines, the two records and the shops pack |
| `Logging/ToolCallRecords.cs`, `RecordFormatter.cs`, and `louis-agent.composition`'s `Logging/MessageLogging.cs` | `logger.Log(level, Member, new { Hole = value })` for a message enum member (`MessageLogging`); the two records (`oversized-tool-results.jsonl`, `truncated-tool-calls.jsonl`), written as `timestamp` and camelCase fields |
| `Config/LlmOptions.cs`, `CacheLifetime.cs` | `LLM_*`: provider (inferred from the model when unset), model, endpoint, key, tool support, thinking, `LLM_CACHE` and `LLM_CACHE_TTL` |
| `Config/AgentOptions.cs` | Workspace, skills directory, keys for Paymo / DevOps / web search, the self-hosted web (`SEARXNG_URL`, `BROWSER_URL`, `PAGE_RENDERER`), Rider MCP, `LOG_DIRECTORY`, `LOG_CONTENT`, prices and rates, `AGENT_PEERS`, `SUB_SESSION_PROMPT_BUDGET` |
| `Config/` (and `louis-agent.choices`) | `SettingChoice`, the one parser for a value that names a member of an enum (F18 step 7.5.2 moved it out of core into `louis-agent.choices`, a project that references nothing, so a pack, an eval set, a host or the browser can all use it; core references it): the member's name or its wire name (`[WireName]`, built on .NET's `JsonStringEnumMemberName`) with the case ignored, an alias, never a number; `Wire(member)` writes the text back, `Names<T>()` lists the wire names (ADR 0024, ADR 0043) |
| `Config/EnvFile.cs` | Finds the `config/` folder; sets one line in `config/.env` (`usage-probe --host-culture`), keeping the rest of the file |
| `Messages/` | The shared message enums (`ErrorMessage`, which keeps only the message system's own texts and `ApiKeyRequired`, the one text two hosts share; `ToolMessage`; `UserText`; one file each, the texts two areas use) and `MessageText` (`Format`, `Template`, `Holes`). An area's own enums sit in its `Messages/` folder beside its code: `Agent/Messages/` (`AgentErrorMessage`, `AgentToolMessage`), `Tools/Web/Messages/` (`WebLogMessage`, `WebToolMessage`), and so on, each named `{Area}ToolMessage`, `{Area}LogMessage` or `{Area}ErrorMessage` (the split by area, F18) |
| `Llm/LlmClientFactory.cs`, `ILlmClientFactory.cs`, `Llm/Providers/ILlmProvider.cs`, `LlmLimits.cs` | Options → `IChatClient` through the provider `LLM_PROVIDER` names among the ones the host gave (CL57, ADR 0003): `ILlmProvider` is a provider's name, client, usage mapper and prompt caching; the factory is the registry, wraps the client for caching and reads both cache settings for every provider; `LlmLimits.MaxOutputTokens` (16,000) is the one number the turn and a provider share. The providers are [core.anthropic](louis-agent.core.anthropic.md), [core.ollama](louis-agent.core.ollama.md) and [core.openai](louis-agent.core.openai.md); core ships no SDK |
| `Llm/Caching/IPromptCachingStrategy.cs`, `NoPromptCaching.cs`, `PromptCachingChatClient.cs` | F4: how a provider marks a request for its prompt cache, the no-op default, and the client that applies it (skips summary requests) |
| `Llm/UsageMapping/IUsageMapper.cs`, `StandardUsageMapper.cs` | Provider usage → input / cache write / cache read / output / reasoning, by the Microsoft.Extensions.AI contract |
| `Llm/UsageMapping/UsageRecordingChatClient.cs`, `PromptHash.cs` | Middleware that records one `UsageRecord` per model request, streaming or not, with the prompt's hash (`PromptHash.Of`: SHA-256 of the leading system messages, the instructions and the tools as sent, F18 step 4c); a cancelled request (a stopped stream, or a timed-out non-streaming call, FX9) is recorded as `cancelled`, a provider error isn't |
| `Llm/Replay/` | F18 step 4e.2: the model's answers recorded and replayed. `ModelReplayChatClient` (MEAI's `CachingChatClient` with streaming coalesced, over the replay store: web `Record` calls the model and saves the answer and never reads; web `Replay` with `Model` answers only from the recording and throws on a miss; anything else passes through; the mode is read from the flow on each request; a hit is tagged `replay_hit` on the response and has no `Usage`), `ModelAsk` (a request as a recording keys it: model, system hash, each tool's name and hash, settings hash, each message's role and hash, never a message's `CreatedAt` or `MessageId`; the recording format) and `ReplayMiss` (a miss names the first part that changed, against the recording that matches longest). Innermost in the pipeline from step 4e.3 |
| `louis-agent.core.anthropic` ([its doc](louis-agent.core.anthropic.md)) | `AnthropicUsageMapper` (cache writes, and the 1-hour part from the raw response or the stream's `message_start`) and `AnthropicPromptCaching` (marker 1 on a copy of the system prompt with `LLM_CACHE_TTL`, marker 2 the top-level `cache_control`, 5 minutes) |
| `Skills/MarkdownSkillLoader.cs` | Parses `## Skill:` procedures (description, parameters, a bash / PowerShell / Python fence) from Markdown |
| `Skills/Providers/MarkdownSkillProvider.cs`, `CompositeSkillProvider.cs` | Every skill file we control is plain markdown read by `MarkdownSkillProvider`; `CompositeSkillProvider` joins them into the system prompt (CL13: the seam's implementations in `Providers/`) |
| `Skills/Languages/ScriptLanguage.cs` | The script languages a fence can name (bash, PowerShell, Python; F18 step 7.5.3), parsed once in `MarkdownSkillLoader.NormalizeLanguage`; a leaf folder that uses nothing of core, so the top of `Skills/` and every other folder may use it |
| `Skills/Roles/RoleCatalog.cs`, `RoleException.cs` | A session's role (F21; "personality" before CL13 and CL14): its file in its level's folder (CL8), valid names, the ones available |
| `Skills/RoleLevel.cs` | The level a session runs at (agent, specialist, minion; CL8, ADR 0017), at the top of `Skills/` since CL27, since a prompt's header names it too and the skills' folders use only the top |
| `Skills/Headers/` | Every built-in prompt's front matter (CL27, ADR 0063): `PromptHeader` (name, description, level, toolsets), `PromptFile` (path, header, body), `PromptHeaders.Split` (the one strict reader, YamlDotNet's node model: an unknown key, a name not the file's, a bad level, a missing description or an unclosed fence refuses the file with `PromptHeaderException`, naming it; a file without the fence is read whole), `PromptCatalog` (every embedded prompt read once, lazily, after the host registered its prompts; the loader takes bodies from it, `SkillLoader.ToolsetsOf` a role's toolsets, F33, and the API's admin list). The refusals are `Messages/PromptHeaderMessage` |
| `Agent/Toolsets/CoreToolset.cs` | Core's twelve toolsets as an enum (ADR 0043), shared by core, the hosts and their tests; the lower-case name is the wire name, written with `.Wire()` (F18 step 7.5.3). The catalogue stays open: a pack adds names core does not list |
| `Skills/SkillFileSource.cs`, `SkillFiles.cs` | The built-in prompts in `prompts/`, embedded with a manifest and read only from the assembly (`ManifestEmbeddedFileProvider`); their paths, the files `AgentHost` loads by name; which names the agent may not take |
| *Tools the model asks for* | Packs now: [the general packs](../../../packs/docs/projects/general-packs.md) ([ADR 0076](../adr/0076-core-ships-no-agent.md)) |
| `Web/` | The seam for what the engine needs from the web (CL33): `IWebAccess` (the search provider, the browser if any, the renderer, a fetcher per call), `NoWebAccess` (every part says web access isn't set up, so a tool answers why) and `WebParts` (web access from parts given). The browser, search, rendering, robots and the recorders are [louis-agent.core.web](louis-agent.core.web.md)'s; the web tools are the web pack's |
| `Delegation/` | `Agents/AskAgentTools.AskAgent` (F127.3.1, ADR 0117): another agent asked by its address (`Addresses/AgentAddresses` resolves it against the identities' files, the asker's `Calls` allow it; an address on another server, `partner:shop`, is checked against that server's `GET /v1/identities` first, read with its own key and kept ten minutes by `Addresses/Peers/PeerIdentities`, every request carrying the server's key from `AGENT_PEER_KEYS` through `SubSessions/PeerHttp`, F127.3.2), one kept conversation per address per chat (`AgentLine`: a follow-up carries on, a busy one says wait, a gone one starts over in the same call), its answer under a line built in code from its own tool calls (`DidLine`: who answered and what each call gave back, its first line, never what it cost, F127.4.2; FX27: a replay keys on what the model reads), an empty answer asked for once more (FX11); `AgentWorkTools.ReadAgentWork`: what an asked agent did, step by step, without its thinking, the latest 12,000 characters, read from its server's `GET /v1/sessions/{id}/work` so turns a visitor had in its window count (`SubSessions/SessionWorkReader`, F127.4.2), what streamed through the asker when the server can't say; at most 3 asks a turn, 5 minutes each; each reply's usage rolled up as one `sub_session` record into the parent turn and the ledger; `ShownReport`: an answer shown whole for an asker that relays answers (ADR 0079). `MinionAsk` (F30, F127.5): what `AskAgent` does for a minion's address (`puls.find-recipe`, on this instance only, listed in the identity's `Calls`, resolved by `AgentAddresses.ResolveMinion` to `minions/puls-minion-find-recipe.md`): one task and one of the asker's own tools, one level down, on the model `MINION_LLMS` names, at most 16 a user turn, never kept, on the same child path; a tool sent to anyone else is refused, and a mode's prompt lists only the agents among its `Calls`. `AskAgent` also refuses an agent already in the turn's chain of asks (`Sessions/TurnChain`, the agents named by `AgentAddresses.ChainKeyOf`) and an ask past `AGENT_MAX_DEPTH` (`Sessions/TurnDepth`); `AgentTools`: the agent's own tool names, set per turn, for what a minion may be handed; `CallSlots`: the calls a turn may make, with the clock injected. The path both share is `SubSessions/` (S, CL15): `PeerSession` (a sub-session's life on a peer: opened, kept or deleted), `SubSessionMessenger` (one message sent and read, asked again once when empty), `SubSessionStream` (the SSE stream read: the seam Phase 3 step 1c replaces), `SubSessionUsage` (what it reported, and the line), `UsageRollUp` (one ledger row in the parent's turn), `SubSessionKind` (an asked agent's or a minion's labels), `PeerHttp` (the key on every request, the timeout, the peer named or the only one) |
| `Sessions/` | What a host sets for a session's turn: `UserPlace` (where the user is, F22), `SessionAttachments` (files forwarded by name), `SubSessionRegistry` and `OpenSubSession` (the children a chat keeps, F26, each with its `AgentWork`: the events it streamed, for the did line and `ReadAgentWork`), `SubSessionFeed` and `SubSessionEvent` (a child's stream, passed to the host live); `Replay/` (F18 step 4a): `SessionReplay` (what an eval session does with the web, its trial in `Of`, held per flow like `UserPlace`, and sent on to a sub-session) and `WebReplayMode` (`Off`, `Record`, `Replay`; `WEB_REPLAY` parsed once), `ReplayTrial` (the trial this flow records into or replays from) and `Store/` (F18 step 4e.1, moved from `Tools/Web/Replay/`): `ReplayStore`, `ReplayAsk`, `ReplayKind`, `ReplayAnswer<T>`, `ReplayIndexLine` and `TrialFileName`, the saved input of every seam |
| `Agent/EngineTools/Clock/` | `ClockTools.GetToday` in every session (F22), in the user's time zone |
| `Agent/EngineTools/Scripts/` | Agent-built tools from `*.tool.md` (`ScriptTool`, its definition and parameters): typed parameters, arguments validated and passed as JSON on stdin |
| `ProcessRunner` (in the composition, `Tools/`), with the script runners | The one way to start processes: argument lists, no shell, timeouts, bounded output; a child that exits before reading its input is not a failure (FX23) |
| `Usage/Records/UsageScope.cs`, `UsageTags.cs` | The turn's context in an `AsyncLocal`: host, session, turn, purpose, tags (task, run, service); `Activate()` for hosts that stream from an async iterator; `Add` / `Records` / `Total()` collect the turn's stored (priced) records, passing them to the outer scope (F3) |
| `Usage/Records/UsageRecord.cs`, `UsageTokens.cs`, `UsageCost.cs`, `UsagePurpose.cs` | The record, its token counts and cost, and why it was made (`turn`, `continue`, `summary`, `compaction`, `triage`, `sub_session`, and `grading` for a model grader's judge, F18 step 6); a `sub_session` roll-up carries `Requests`, the model requests it stands for (F5); every model request carries `prompt_hash` (null on a roll-up and on a row from before 4c) |
| `Usage/Sinks/IUsageSink.cs`, `JsonlUsageSink.cs` | Where records go: the monthly JSONL file (shared append, UTC month) |
| `Usage/Reports/UsageReport.cs`, `UsageTotal.cs` | Reads a ledger back, prices records written before F2, totals by any key (`WorkOf`: a fix, a triage, a session); requests weighted by a roll-up's `Requests`, so a turn that delegated says how many were really made |
| `Usage/Reports/UsageText.cs`, `Usage/Records/UsageSession.cs` | The usage text every host shows (F3): the line after an answer and the `/usage` summary (k/M tokens, two significant digits below $1, "price unknown" never $0); a host session's finished turns |
| `Usage/Reports/CacheGaps.cs`, `CacheGapGroup.cs`, `LifetimeCost.cs` | The prefix's cache lifetime from a ledger: gaps per host and model, both lifetimes replayed and priced (F4) |
| `Pricing/Prices/PriceTable.cs`, `ModelPrice.cs`, `TokenPrices.cs`, `LongPromptPrices.cs`, `CostCalculator.cs` | `config/prices.json` (longest-prefix match, a `long_prompt` tier) and a record's cost; no price → `null`, never 0 |
| `Pricing/Prices/PricingUsageSink.cs`, `PriceTableLoader.cs` | Wraps the ledger in `UsageLedger.Open`: prices each record before it's written; the table from PRICES_URL or the file |
| `Pricing/Rates/ExchangeRates.cs`, `Mappers/IRatesMapper.cs` and its mappers | Exchange rates in our `rates.json` format, the only one read; a mapper per provider turns its reply into it (`FrankfurterRatesMapper`, `RatesJsonMapper`), chosen by `RATES_FORMAT` (F14) |
| `Pricing/Rates/DisplayCurrency.cs` | The currency costs are shown in, with its symbol, rate and date: `DISPLAY_CURRENCY` → `HOST_CULTURE`'s region → the process's culture → as charged (F14) |

Folders split the code by concern and the namespace is the folder path (`LouisAgent.Core.Tools.Web`); dependencies
point one way (Hosting → Agent → Llm, Tools, Skills, Usage, Pricing, Protocols → Guardrails → Config, Messages,
Logging). `ArchitectureTests` holds both rules ([src/engine/louis-agent.core/CLAUDE.md, Project layout](../../engine/louis-agent.core/CLAUDE.md#project-layout)).

## Key types

| Type | Role |
|---|---|
| `AgentHost` | Static bootstrap, the flow: `Build`, `BuildToolHost`; the pieces are `HostEnvironment.Load`, `SkillLoader.Load`, `RoleFiles.Check`, `PriceTableLoader.Load`, `ExchangeRatesLoader.Load`, `UsageLedger.Open` |
| `UsageScope` | Open one per turn in a host (`Begin(host:, session:, turn:, tags:)`); every request inside it is attributed to it |
| `AgentEngine` | `StreamPromptAsync`, `ProcessPromptAsync`, `NewHistory`, `HandleUserCommand` (`/tools`, `/approve`, `/reject`, `/usage` from the session records the host passes), `RecordsUsage`, `Tools` and `ToolsChanged`. Optional `toolsets:` replaces the coding tools with your own objects (used by the orchestrator) |
| `ISkillProvider` | `Documentation` (system prompt text) + `Skills` (runnable procedures) |
| `ILlmClientFactory` | Builds the `IChatClient`; tests inject a fake |
| `WorkspaceBoundary` / `SensitiveFiles` | Internal (`Guardrails/`), used by the file tools and the API's file and git endpoints, so the same rules apply |

**Every public method on a tool class becomes a tool**, named after the method, described by its `[Description]`
attributes (121 tools with every toolset, Paymo and DevOps included). Helpers must be `internal` or `private`.

## Configuration

`LLM_PROVIDER`, `LLM_MODEL`, `LLM_ENDPOINT`, `LLM_API_KEY` / `ANTHROPIC_API_KEY`, `ANTHROPIC_WORKSPACE_ID`,
`LLM_SUPPORTS_TOOLS`, `LLM_THINKING`, `LLM_PROFILE`, `WORKSPACE_ROOT`, `SKILLS_DIRECTORY`,
`LLM_CACHE`, `LLM_CACHE_TTL`, `LOG_DIRECTORY`, `USAGE_LEDGER`, `USAGE_IN_CHAT`, `AGENT_PEERS`, `MINION_LLMS`, `PRICES_FILE`, `PRICES_URL`, `RATES_URL`, `RATES_FORMAT`, `RATES_FILE`,
`HOST_CULTURE`, `DISPLAY_CURRENCY`,
`PAYMO_API_KEY`, `DEVOPS_API_KEY`, `DEVOPS_ORGANIZATION`, `DEVOPS_PROJECT`, `DEVOPS_TEAM`, `WEB_SEARCH_PROVIDER`,
`GOOGLE_SEARCH_API_KEY`, `GOOGLE_SEARCH_ENGINE_ID`, `SEARXNG_URL`, `BROWSER_URL`, `PAGE_RENDERER`, `WEB_REPLAY` (the default for eval
sessions: off, record or replay) and `REPLAY_DIRECTORY` (where the saved pages are kept: private, never served; unset keeps none),
`RIDER_MCP_*`, `BASH_PATH`. All read once at start-up in
`LlmOptions` / `AgentOptions` — tools never read the environment themselves. Details: [Setup](../SETUP.md),
[Models](../MODELS.md).

## Extending it

The steps are in [Change the engine](../guides/change-the-engine.md). In short: a tool is a public method with
`[Description]`s on a toolset's class in `Tools/{Toolset}/`, named in the toolset's guide; a toolset is a
`ToolsetDefinition` in `Agent/Toolsets/ToolsetCatalog.cs` and its guide in `prompts/toolsets/`; a provider difference
is a strategy chosen in `LlmClientFactory`, with the provider's own classes in `Llm/Providers/{Name}Provider/`
([Architecture: Provider strategies](../architecture/05-building-block-view.md#provider-strategies)); a new capability
for one agent is a pack, not core. Another agent on core is
[Building another agent on core](../../engine/louis-agent.core/CLAUDE.md#building-another-agent-on-core).

## Security

The file tools keep every path inside the workspace. A path can't climb out with `..`, and a symbolic link is
never followed. Secret files can't be read, written, searched or diffed at all: that's `.env*` (except templates
such as `.env.example`), keys, certificates, `credentials.json` and the like.

When a tool starts a process, it hands over a list of arguments or environment variables. The model's text is
never spliced into a command line, so the model can't sneak in a second command. And `FetchUrl` refuses private,
loopback and cloud-metadata addresses, so the agent can't be talked into reading something inside our own network.

## Tests

The tests are in `src/tests/engine/louis-agent.core.tests`, and [tests](tests.md) says how they're laid out. They
need no network, no model and no Docker.

## Limits and plans

A message gets at most 10 tool rounds; choosing that per route is [F10](../features/F10-route-settings.md). The
history grows without limit for now; trimming it is [F9](../features/F09-context-management.md). Estimates and
budgets are [F7](../features/F07-budgets.md), and knowing each model's window and cache minimum is
[F15](../features/F15-model-limits.md).

The browser's address check can be beaten by DNS rebinding. The check looks a name up, and then Chrome looks it up
again, so the second answer can differ from the one we checked. A Web Worker's socket isn't blocked either. One
egress proxy that looks a name up only once would close both holes; that's F42.

Totals convert at the one rate a host loaded when it started. That's fine for a single session, but a report over
months (F7) will have to convert each record at the rate of its own date.

On the round after the limit, the engine sends `tool_choice: none` (FX1). When we measured it, that didn't break
the conversation's cache, even though Anthropic's docs say a change of `tool_choice` should. If the API starts
enforcing it, each such request rewrites the conversation's cache, which is about $0.006 more per round-limited
message on Haiku 5.5.

A few smaller limits are on the TODO's [known limitations](../../../TODO.md#known-limitations): Rider MCP discovery
only logs, Ollama's tool support is guessed from the model's name, and a Paymo task lookup takes the first match.

## Related docs

[Architecture](../architecture/README.md) · [How a turn works](../architecture/06-runtime-view.md) · [Development guide](../CLAUDE.md) ·
[Setup](../SETUP.md) · [Models](../MODELS.md)

---
[Projects](README.md) · Next: [louis-agent.identity.puls](../../../demos/puls/docs/projects/louis-agent.identity.puls.md)
