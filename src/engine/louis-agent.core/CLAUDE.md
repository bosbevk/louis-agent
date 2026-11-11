# louis-agent.core: development guide

> **How-to.** What to know before changing `louis-agent.core`: tools and toolsets, the layout and message rules,
> another agent on core, a new model provider. **Elsewhere:** how to work, steps, commits and tests in the
> [development guide](../../docs/CLAUDE.md). Claude Code loads this file when it works on files in this folder.

## Adding a tool

**A tool the model asks for is a pack's** ([ADR 0076](../../docs/adr/0076-core-ships-no-agent.md)): core has none. The steps are
[Build a pack](../../docs/guides/build-a-pack.md). What holds for any tool class:

- **Every public method on a tool class becomes a tool.** Helpers must be `internal` or `private`. Tool names must be
  unique across all classes: a duplicate stops the engine at start-up.
- Return strings the model can act on: say what went wrong and what to do instead. Keep output bounded: results over
  50,000 characters get summarised; most toolsets cap their own output at 12,000.
- **The texts a tool returns are members of its own `{Area}ToolMessage`** (*Messages and logging*), errors and results alike.
  A new tool or text changes the golden files on purpose: regenerate them and review the diff
  ([golden files](../../tests/CLAUDE.md#golden-files)).

**Every toolset follows one pattern: guide, then tools, then guide.** A mode's routing sends a task to a toolset, the
toolset's guide names its tools and when to use them, and each tool's `[Description]` stays short. A guide is plain markdown,
embedded, and loads only when a session has the toolset (`SkillLoader.Load(agent, toolsets)`). **No provider class per
toolset.**

**Core's own toolsets** are `agents` and `minions` (`Delegation/`, in `Agent/Toolsets/ToolsetCatalog.cs`) and `skills`
(`Agent/EngineTools/`, registered by `AgentEngine` because it needs the registry). Only something that needs the engine's own
insides belongs here; anything else is a pack.

**Skill files we control are plain markdown, always.** That covers every file in `prompts/` and the function skills the agent
writes into a demo's `skills/agent/functions/`. `SkillFilesTests` fails if a provider class for one of them appears. Every
file in `prompts/` opens with its front matter (`name`, `description`, `metadata.level` and `toolsets`; ADR 0063), read once
by `PromptCatalog` and stripped before the model sees the file; `EveryPromptHasAHeaderTests` holds it. An identity's skills
sit beside its voice in `agent/{identity}/skills/`, each with `metadata.load` (`always` or `on-demand`, ADR 0114):
`SkillLoader` puts the always ones after the voice and lists the others, which the engine's `ReadSkill` reads.

**The exception: our own implementation.** A provider class of its own is only for skills we don't control, when we must read
someone else's skill format. A copy of `MarkdownSkillProvider` under another name is not one.

## Project layout

**Folders split the code by concern, and the namespace is the folder path.** Each concern of an agent has one home, so
a change to one (a new provider, a new tool, a new guard) touches one folder:

| Folder (core) | Concern |
|---|---|
| `Agent/` | The engine (`AgentEngine`, the facade that wires one agent) and its tool registry at the top; one folder per thing it does, one layer: `Turns/` (one turn: `TurnRunner`, the `ChatPipeline` middleware order, the tool-call guards that call the model, a call's text for displays), `Commands/` (what the user types that the model never sees: `/tools`, `/approve`, `/reject`, `/usage`), `EngineTools/` (what the engine registers itself: the skill tools, the agent-built tools with their `Scripts/`, and the `Clock/`), `Toolsets/` (a toolset's definition, context and core's catalogue), `Catalog/` (the contracts an identity or a pack implements, and the catalogue built from them: the seam a later contracts project would be cut from) |
| `Llm/` | The model: the client factory at the top (`ILlmClientFactory`, `LlmClientFactory`, the registry of the providers a host gave) and `Providers/` (the `ILlmProvider` seam and `LlmLimits`; each SDK is a library on core, CL57); `Caching/` (how a provider marks a request for its prompt cache: the strategy, the no-op default, the middleware); `UsageMapping/` (a provider's usage into our record: the mapper and the recording middleware); one folder per provider under `Providers/`, `{Name}Provider/` (`Providers/AnthropicProvider/`: a folder named `Anthropic` would hide the SDK's own `Anthropic` namespace) |
| `Delegation/` | Handing work to a specialist or a minion: their tools, the sub-sessions and the usage they roll up. The engine's own (ADR 0031) |
| `Web/` | The seam for what the engine needs from the web (CL33): `IWebAccess` (the search provider, the browser if any, the renderer, a fetcher per call), `NoWebAccess` (every part says it isn't set up) and `WebParts` (web access from parts given). The browser, search, rendering, robots and the recorders are `louis-agent.core.web`'s, which a host hands in through `HostServices` |
| `Sessions/` | What a host sets for one session's turn and the tools read: the user's place (F22) and the attachments at the top; `SubSessions/` (the sub-sessions a chat keeps, F26, and the live feed of a child's events). Not what the model can do, and not the loop |
| `Skills/` | Loading the markdown skills and a session's role into the prompt: the contract at the top (`ISkillProvider`, the embedded files, `SkillFolder`: where a file comes from), its implementations in `Providers/`, in `Languages/` the script languages a fence can name (`ScriptLanguage`, a leaf every other folder may use), in `Headers/` every prompt's front matter (`PromptHeaders`, `PromptCatalog`, CL27), and in `Roles/` what a session is (`RoleCatalog`, CL13; `RoleFiles`: which roles this instance offers); `RoleLevel` sits at the top |
| `Guardrails/` | Pure safety checks, no model: `Auth/` (the hosts' key check, failing closed, FX18), `Network/` (the SSRF guard and the address it blocks), `Workspace/` (the workspace boundary, sensitive files). The guards that call a model are in `Agent/` |
| `Usage/` | The ledger: `Records/` (a record, its tokens and cost, its purpose, the turn's scope and tags, the session), `Sinks/` (where records go: the monthly JSONL file), `Reports/` (reading a ledger back: totals, the usage text every host shows, the lifetime cost, the cache gaps) |
| `Telemetry/` | A turn's span on our own source (`TurnSpans`, F52), the last-chance handler for a crash, and the seam for what a host does with telemetry (CL33): `IHostTelemetry` (the log sinks, start, flush) and `NoTelemetry`. The OpenTelemetry tracer and the collector's sink are `louis-agent.core.telemetry`'s, handed in through `HostServices` |
| `Pricing/` | `Prices/` (the price table and its loader, a model's prices, the cost calculator, the sink that prices each record), `Rates/` (exchange rates and their loader, the display currency; `Mappers/`: a provider's reply into our format) |
| `Hosting/` | Bootstrap shared by every host: `AgentHost` (the flow), its context, the engine factory, `HostServices` (what a program hands in: web, telemetry, discovery, CL33/CL57), the ledger and the tools-only client at the top; `Discovery/` (the seam for tools found outside the engine, with its Null Object; Rider's is the ACP server's); `Engines/` (the factory's cache and the key it holds engines by), `EnvFiles/` (the .env files a host loads), `prompts/` (the system prompt assembled from skill files), `Rider/` (Rider's MCP tools at start-up), `SessionLlms/` (the LLM profiles a session may name, `SESSION_LLMS`) |
| `Config/`, `Messages/`, `Logging/` | Settings; message enums; logging (`AgentLogging`: Serilog behind `ILogger`) |

The rules:

1. **Namespace = folder path**, PascalCase (`LouisAgent.Core.Llm.Providers`), one top-level type per file, the file
   named after the type. No `Helpers/`, `Utils/`, `Common/` or `Interfaces/` folders: an interface lives beside its
   implementations, a helper beside its one user.
2. **Dependencies point one way:** hosts → `Hosting` → `Agent` → `Llm`, `Delegation`, `Web`, `Skills`, `Usage`, `Pricing`,
   `Guardrails` → `Config`, `Messages`, `Logging`. Delegation and web access never reference the agent loop or a model;
   `Agent` never references a provider's SDK (provider differences stay in `Llm/{Name}Provider/`, as in *Changing the
   LLM*).
3. **Prompts are content, not code:** system prompt text and procedures stay in `prompts/`, in the folder of the level
   that reads them (`agent/`, `specialists/`, `minions/`; `toolsets/` for what every level shares), embedded and read
   only from the assembly; C# only loads them. `SkillsLayoutTests` fails on a misplaced file.
4. **Hosts stay thin:** a host project maps the agent's stream to its protocol and holds no agent behaviour. Its
   `Program.cs` is start-up only; its routes are in `Endpoints/`, one file per group, and the rest sits in a folder per
   concern (the API: `Sessions/`, `Admin/`, `Evals/`, `Config/`, `Messages/`), never a `Services/` folder, which
   names a kind of thing, not a concern; another agent on core (the orchestrator) keeps its own `Tools/` and `Skills/` the same way.
5. **Tests mirror the source:** `src/tests/engine/louis-agent.core.tests/Llm/Caching/…` tests `Llm/Caching/…`.
6. **An architecture test enforces 1 and 2:** namespaces match folders, and no type depends against the arrow.

**Held by a test:** `src/tests/engine/louis-agent.core.tests/ArchitectureTests.cs` reads core's source and fails on a file whose
namespace isn't its folder path, a file with two top-level types, a folder that isn't in the table, or a `using` against
the arrow. A new concern is a new row here and in the test's layers. Every other project's namespace is PascalCase too
(`LouisAgent.Api`, `LouisAgent.Orchestrator.Tools`); a type never shares its namespace's name (hence `MessageText` in
`Messages/`). The move was CL1 ([plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/CL1-project-layout.md)).

## Messages and logging

**No message text inline in C#.** Every message is a member of a message enum, and its `[Description]` is the text, a
template with **named** holes:

```csharp
public enum ConfigErrorMessage
{
    [Description("Unknown LLM_CACHE_TTL '{Value}'. Use 5m or 1h.")]
    UnknownCacheTtl,
}

throw new ArgumentException(MessageText.Format(ConfigErrorMessage.UnknownCacheTtl, new { Value = other }));
Log.Log(LogLevel.Warning, PricingLogMessage.RatesUrlUnusable, new { Url = url, Reason = ex.Message });   // holes kept as fields
Log.Log(LogLevel.Information, McpLogMessage.ToolNameTaken, new { Name = name }, tag: "MCP");   // [MCP] …
```

- **What counts:** exception messages, log lines, the error strings tools return to the model, and user-facing text
  (`/usage`, the usage line, CLI help). Enums by kind and area: the shared `ErrorMessage`, `ToolMessage` and
  `UserText` sit in core's `Messages/`; an area's own sit in its `Messages/` folder, named `{Area}ToolMessage`,
  `{Area}LogMessage` or `{Area}ErrorMessage` (the split by area, F18). A text two areas use stays shared. Each member is named
  for what happened, not for its wording.
- **Each host keeps its own texts** in its own `Messages/` folder
  ([src/CLAUDE.md](../../CLAUDE.md#each-hosts-own-texts)).
- **Not messages:** prompts (system prompt and procedures in `prompts/`; the orchestrator's triage and fix prompts),
  keywords another program matches on (`FIX-RESULT`, `DECISION`, `PASS`/`FAIL`, `VERIFIED`), the comms log's Markdown
  layout, and key=value or table formatting.
- **Text the model reads is pinned:** `ToolMessageSnapshotTests` renders every `*ToolMessage` enum's template (the shared one
  and each area's) into `Messages/ToolMessageSnapshot.json`, keyed by member name, as the model-request snapshot does for the
  tools and prompt; no two of them share a member name (a test holds that). A deliberate change regenerates it
  ([golden files](../../tests/CLAUDE.md#golden-files)). Numbers in messages are invariant (`12,345`).
- **Logging goes through `ILogger` (Microsoft.Extensions.Logging) with Serilog behind it**, configured in code by
  `Logging/AgentLogging` (`AgentHost` calls `Configure`; another agent on core calls it itself). Get a logger with
  `AgentLogging.For<T>()` and log a message member with `logger.Log(level, Member, new { Hole = value })`
  (`MessageLogging`): values bind to holes by name, and a missing one throws.
  - **Console: stderr only** (stdout carries the ACP and MCP protocols), `[INFO] …`, `[WARN] …`, `[ERROR] …`, or the
    call's `tag:` (`[TOOL]`, `[MCP]`, `[orchestrator]`).
  - **With `LOG_DIRECTORY`:** a run file per process (`agent-{host}-{time}-{pid}.log`, the console's lines with the
    time) and the records (`oversized-tool-results.jsonl`, `truncated-tool-calls.jsonl`, written by `ToolCallRecords`)
    as JSON lines in their own shape (`RecordFormatter`: `timestamp` and the record's fields in camelCase, nothing of
    Serilog's), since tools read them (F16).
- **A unit test checks every message enum:** each member has a `[Description]`, and its template's holes are well formed.
- **Screen text is not logging:** the CLI's usage line and the usage probe's errors are written to `Console.Error`
  directly (from their enums), so they stay out of the run file.

## Building an identity or a pack

The steps are [Build a pack](../../docs/guides/build-a-pack.md) and [Build an identity](../../docs/guides/build-an-identity.md),
each with its review checklist at the end; the rules they follow are
[ADR 0027](../../docs/adr/0027-engine-identities-and-packs.md) (core is the engine and names no product, shop or country;
`ArchitectureTests` holds it in the model-visible text and in the comments, this file included) and
[ADR 0028](../../docs/adr/0028-packs-stay-convertible-to-mcp.md) (`PackConvertibilityTests` holds the shapes; the rest
is reviewed by hand). Only a demo's own project under `demos/` knows its identities and packs.

## Building another agent on core

Pass `toolsets:` to the `AgentEngine` constructor to give an agent only your tool objects (plus `ExecuteSkill`) instead
of the toolsets a catalogue names, and build its system prompt from your own skill files (`CompositeSkillProvider` +
`MarkdownSkillProvider`). To record its usage, pass `usageSink: AgentHost.CreateUsageSink(options)` and
`usageMapper: clientFactory.CreateUsageMapper(llm)`, and open a `UsageScope` per turn (with a `UsageTags` run id if it
has one). `louis-agent.orchestrator` does exactly this; its `Program.cs` is the example.

## Changing the LLM

Configuration only — see [MODELS.md](../../docs/MODELS.md): `LLM_PROFILE` picks `config/.env.anthropic` or `config/.env.ollama`.
A new model also needs its prices in each demo's `config/prices.json` (else its cost is recorded as unknown); run
`src/tools/louis-agent.usage-probe` before trusting a new provider's usage numbers. Provider differences belong in
the provider's own library on core (`louis-agent.core.anthropic`, `.core.ollama`, `.core.openai`: an `ILlmProvider` with the
client, the usage mapper and the prompt caching, CL57), never in core, `AgentEngine` or the tools; a program composes
the providers it wants in its `HostServices`.
Where providers need different logic for the same concern, write it as a **strategy**: an interface, one implementation
per provider that differs (plus a default), chosen once by the provider's `ILlmProvider`, as `IUsageMapper` is
([Architecture: Provider strategies](../../docs/architecture/05-building-block-view.md#provider-strategies)). Never branch on the provider's name
anywhere else.
