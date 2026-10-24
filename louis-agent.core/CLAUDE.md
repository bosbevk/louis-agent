# louis-agent.core: development guide

> **How-to.** What to know before changing `louis-agent.core`: tools and toolsets, the layout and message rules,
> another agent on core, a new model provider. **Elsewhere:** how to work, steps, commits and tests in the
> [development guide](../docs/CLAUDE.md). Claude Code loads this file when it works on files in this folder.

## Adding a C# tool

1. Add a public method with `[Description]`s to a tool class (or a new class in `louis-agent.core/Tools/{Toolset}/`):

   ```csharp
   [Description("What this tool does")]
   public string MyTool([Description("What the parameter is")] string input, int limit = 10) => "result";
   ```

2. A new class is a new toolset: follow *Adding a toolset* below. The engine doesn't register tool classes itself.
3. Name it in its toolset's guide (`Prompts/toolsets/{toolset}.md`) so the model knows when to use it.
4. Test it in `tests/louis-agent.core.tests/Tools/{Toolset}/`.

**Every public method on a tool class becomes a tool.** Helpers must be `internal` or `private` (the test project and
`louis-agent.api` can see internals). Tool names must be unique across all classes: a duplicate stops the engine at
start-up.

Return strings the model can act on: say what went wrong and what to do instead. Keep output bounded — results over
50,000 characters get summarised; most toolsets cap their own output at 12,000.

**The texts a tool returns are `ToolMessage` members** (*Messages and logging*), errors and results alike:
`return MessageText.Format(ToolMessage.FileNotFound, new { Path = relativePath });`. Its descriptions are attributes.
A new tool or text changes the golden files on purpose: regenerate them and review the diff
([golden files](../tests/CLAUDE.md#golden-files)).

## Adding a toolset

**Every toolset follows one pattern: guide → tools → guide.** A mode's routing sends a task to a toolset, the toolset's
guide names its tools and when to use them, and each tool's `[Description]` stays short. The files a new toolset
touches, and nothing else:

1. **Tools:** a class in `louis-agent.core/Tools/{Toolset}/` (*Adding a C# tool*), or in the pack's project when the
   toolset is a capability of its own (*Building an identity or a pack*); every public method is a tool.
2. **Catalogue entry:** a `ToolsetDefinition` (the name, a one-line description, the settings it needs
   (`MissingSettings`), its tool classes (`ToolTypes`), the factory for their objects (`CreateTools`) and its guide
   (`GuideFile`)) in `Agent/Toolsets/ToolsetCatalog.cs` for core's, or in the pack's `Toolsets` for a pack's. `AgentCatalog`
   lists both, core's first, and stops start-up on a tool name two toolsets or the engine already use.
3. **Guide:** `Prompts/toolsets/{toolset}.md`, plain markdown, embedded with the other built-in prompts (a pack's in
   its own `Prompts/`): what the toolset is for, its tools grouped with one line each, and its workflows. Core's guides
   may also hold `## Skill:` procedures; a pack's may not, because a guide must be able to travel as an MCP server's
   instructions (ADR 0028): a procedure a pack needs becomes a tool. `SkillLoader.Load(agent, toolsets)` adds the
   guide to the prompt, through `MarkdownSkillProvider`, only when a session loads the toolset. **No provider class per
   toolset.**
4. **Reserved by itself:** the guide is embedded, so the agent can't take its name (`SkillFiles.IsBuiltIn`).
5. **Routing:** a row in the routing of each mode whose product uses it (`agent/louis-agent/voice.md` in core; an
   identity's `agent/{name}/voice.md` in its own project). A pack can't add a row to a router it doesn't own, so a
   pack's guide opens with the one line that routes to it, read only where the toolset loads (the TODO's small cleanups).
6. **Tests:** `SkillFilesTests` checks the coding agent's routing names core's toolset, the toolset's tests in
   `tests/louis-agent.core.tests/Tools/{Toolset}/` (a pack's in `tests/louis-agent.pack.{name}.tests/`), and the golden
   files regenerated ([golden files](../tests/CLAUDE.md#golden-files)).

Every catalogue toolset follows it; `SkillFilesTests` checks that each has a guide that loads with it.

**Skill files we control are plain markdown, always.** That covers every file in `Prompts/` (the principles, the modes,
the roles, the toolset guides) and the function skills the agent writes into `Skills/agent/functions/`. `SkillFilesTests` fails if a provider class for one of them appears.

**The exception: our own implementation.** A provider class of its own is only for skills we don't control, when we
must read someone else's skill format; a toolset's tools may be registered differently only when a tool class can't
express them (`skills`: `CreateSkillFile`, `ReloadSkills`, `CreateTool` and `ListAgentTools` are registered by
`AgentEngine`, because they need its registry; its guide is still a plain file). The difference and its reason go in
the class's doc comment and in the feature's plan. A copy of `MarkdownSkillProvider` under another name is not one.

## Project layout

**Folders split the code by concern, and the namespace is the folder path.** Each concern of an agent has one home, so
a change to one (a new provider, a new tool, a new guard) touches one folder:

| Folder (core) | Concern |
|---|---|
| `Agent/` | The engine (`AgentEngine`, the facade that wires one agent) and its tool registry at the top; one folder per thing it does, one layer: `Turns/` (one turn: `TurnRunner`, the `ChatPipeline` middleware order, the tool-call guards that call the model, a call's text for displays), `Commands/` (what the user types that the model never sees: `/tools`, `/approve`, `/reject`, `/usage`), `EngineTools/` (what the engine registers itself: the skill tools and the agent-built tools), `Toolsets/` (a toolset's definition, context and core's catalogue), `Catalog/` (the contracts an identity or a pack implements, and the catalogue built from them: the seam a later contracts project would be cut from) |
| `Llm/` | The model: the client factory at the top (`ILlmClientFactory`, `LlmClientFactory`, with the thinking middleware); `Caching/` (how a provider marks a request for its prompt cache: the strategy, the no-op default, the middleware); `UsageMapping/` (a provider's usage into our record: the mapper and the recording middleware); one folder per provider under `Providers/`, `{Name}Provider/` (`Providers/AnthropicProvider/`: a folder named `Anthropic` would hide the SDK's own `Anthropic` namespace) |
| `Tools/{Toolset}/` | What the model can do: one folder per toolset (`Workspace`, `Git`, `DotNet`, `Shell`, `Web`, `Scripts`, `Agents`, `Integrations/Paymo`, `Integrations/DevOps`), plus `Clock` (every session), `Documents` (shared by two toolsets) and `Currency` (listed by the web toolset, no web of its own); `ProcessRunner` beside them |
| `Sessions/` | What a host sets for one session's turn and the tools read: the user's place (F22) and the attachments at the top; `SubSessions/` (the sub-sessions a chat keeps, F26, and the live feed of a child's events). Not what the model can do, and not the loop |
| `Skills/` | Loading the markdown skills and a session's role into the prompt: the contract at the top (`ISkillProvider`, the embedded files, `SkillFolder`: where a file comes from), its implementations in `Providers/`, and in `Roles/` what a session is (`RoleCatalog`, `RoleLevel`, `RoleHeader`, CL13; `RoleFiles`: which roles this instance offers) |
| `Guardrails/` | Pure safety checks, no model: `Auth/` (the hosts' key check, failing closed, FX18), `Network/` (the SSRF guard and the address it blocks), `Workspace/` (the workspace boundary, sensitive files). The guards that call a model are in `Agent/` |
| `Usage/` | The ledger: `Records/` (a record, its tokens and cost, its purpose, the turn's scope and tags, the session), `Sinks/` (where records go: the monthly JSONL file), `Reports/` (reading a ledger back: totals, the usage text every host shows, the lifetime cost, the cache gaps) |
| `Pricing/` | `Prices/` (the price table and its loader, a model's prices, the cost calculator, the sink that prices each record), `Rates/` (exchange rates and their loader, the display currency; `Mappers/`: a provider's reply into our format) |
| `Protocols/Mcp/` | Talking to other agents and tools over MCP |
| `Hosting/` | Bootstrap shared by every host: `AgentHost` (the flow), its context, the engine factory, the ledger and the tools-only client at the top; `EnvFiles/` (the .env files a host loads), `Prompts/` (the system prompt assembled from skill files), `Rider/` (Rider's MCP tools at start-up), `SessionLlms/` (the LLM profiles a session may name, `SESSION_LLMS`) |
| `Config/`, `Messages/`, `Logging/` | Settings; message enums; logging (`AgentLogging`: Serilog behind `ILogger`) |

The rules:

1. **Namespace = folder path**, PascalCase (`LouisAgent.Core.Llm.AnthropicProvider`), one top-level type per file, the file
   named after the type. No `Helpers/`, `Utils/`, `Common/` or `Interfaces/` folders: an interface lives beside its
   implementations, a helper beside its one user.
2. **Dependencies point one way:** hosts → `Hosting` → `Agent` → `Llm`, `Tools`, `Skills`, `Usage`, `Pricing`,
   `Protocols` → `Guardrails` → `Config`, `Messages`, `Logging`. A tool never references the agent loop or a model;
   `Agent` never references a provider's SDK (provider differences stay in `Llm/{Name}Provider/`, as in *Changing the
   LLM*).
3. **Prompts are content, not code:** system prompt text and procedures stay in `Prompts/`, in the folder of the level
   that reads them (`agent/`, `specialists/`, `minions/`; `toolsets/` for what every level shares), embedded and read
   only from the assembly; C# only loads them. `SkillsLayoutTests` fails on a misplaced file.
4. **Hosts stay thin:** a host project maps the agent's stream to its protocol and holds no agent behaviour. Its
   `Program.cs` is start-up only; its routes are in `Endpoints/`, one file per group, and the rest sits in a folder per
   concern (the API: `Sessions/`, `Admin/`, `Benchmarks/`, `Config/`, `Messages/`), never a `Services/` folder, which
   names a kind of thing, not a concern; another agent on core (the orchestrator) keeps its own `Tools/` and `Skills/` the same way.
5. **Tests mirror the source:** `tests/louis-agent.core.tests/Llm/Providers/AnthropicProvider/…` tests `Llm/Providers/AnthropicProvider/…`.
6. **An architecture test enforces 1 and 2:** namespaces match folders, and no type depends against the arrow.

**Held by a test:** `tests/louis-agent.core.tests/ArchitectureTests.cs` reads core's source and fails on a file whose
namespace isn't its folder path, a file with two top-level types, a folder that isn't in the table, or a `using` against
the arrow. A new concern is a new row here and in the test's layers. Every other project's namespace is PascalCase too
(`LouisAgent.Api`, `LouisAgent.Orchestrator.Tools`); a type never shares its namespace's name (hence `MessageText` in
`Messages/`). The move was CL1 ([plan](../docs/plans/CL1-project-layout.md)).

## Messages and logging

**No message text inline in C#.** Every message is a member of a message enum, and its `[Description]` is the text, a
template with **named** holes:

```csharp
public enum ErrorMessage
{
    [Description("Unknown LLM_CACHE_TTL '{Value}'. Use 5m or 1h.")]
    UnknownCacheTtl,
}

throw new ArgumentException(MessageText.Format(ErrorMessage.UnknownCacheTtl, new { Value = other }));
Log.Log(LogLevel.Warning, LogMessage.RatesUrlUnusable, new { Url = url, Reason = ex.Message });   // holes kept as fields
Log.Log(LogLevel.Information, McpServerLogMessage.ToolNameTaken, new { Name = name }, tag: "MCP");   // [MCP] …
```

- **What counts:** exception messages, log lines, the error strings tools return to the model, and user-facing text
  (`/usage`, the usage line, CLI help). Enums by kind (`ErrorMessage`, `LogMessage`, `ToolMessage`, `UserText`), each
  member named for what happened, not for its wording.
- **Each host keeps its own texts** in its own `Messages/` folder
  ([src/CLAUDE.md](../src/CLAUDE.md#each-hosts-own-texts)).
- **Not messages:** prompts (system prompt and procedures in `Prompts/`; the orchestrator's triage and fix prompts),
  keywords another program matches on (`FIX-RESULT`, `DECISION`, `PASS`/`FAIL`, `VERIFIED`), the comms log's Markdown
  layout, and key=value or table formatting.
- **Text the model reads is pinned:** `ToolMessageSnapshotTests` renders every `ToolMessage` template into
  `Messages/ToolMessageSnapshot.json`, as the model-request snapshot does for the tools and prompt; a deliberate change
  regenerates it ([golden files](../tests/CLAUDE.md#golden-files)). Numbers in messages are invariant (`12,345`).
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

The steps, for a person, are in [Build a pack](../docs/guides/build-a-pack.md) and
[Build an identity](../docs/guides/build-an-identity.md); this section keeps the rules they follow.

Core is the engine and names no product, shop or country (CL15, [ADR 0027](../docs/adr/0027-engine-identities-and-packs.md));
`ArchitectureTests` holds it in the model-visible text and in the comments. Who an agent is lives in an identity, what
it can do in a pack, and only `src/louis-agent.composition` knows them all. The one identity in `identities/` and the
one pack in `packs/` are the worked examples (this guide names neither: core's source, this file included, names no
product, held by the same test); F40's demo takes over as the simpler one.

**An identity** is a project `identities/louis-agent.identity.{name}` with one class implementing `IAgentIdentity`
(`Agent/Catalog/`, as `IAgentPack` and `AgentCatalog` are):
its `Name` (what `IDENTITIES` loads it by), its `Prompts` (a `ManifestEmbeddedFileProvider` over its `Prompts/` folder:
its modes under `agent/{name}/`, its roles under `specialists/` and `minions/`, as *Adding a mode or a role* lays them
out) and the `Packs` it names, by name, never by reference. Its csproj needs core's two embedding lines
(`GenerateEmbeddedFilesManifest` and the `EmbeddedResource` glob), or its prompts silently vanish: its tests read one
prompt through `SkillFileSource` to prove the manifest works, after a `[SetUpFixture]` registers the identity's prompts
and its packs' with `SkillFileSource.Use`, as a host's `Build` does with the catalogue's.

**A pack** is a project `packs/louis-agent.pack.{name}` with one class implementing `IAgentPack`: its `Name`, its
`Toolsets` (one `ToolsetDefinition` each, *Adding a toolset*), its `Prompts` (its guides) and the packs it `Requires`,
by name. It reads its own settings in its own options class (`ShopsOptions.FromEnvironment`), never through
`AgentOptions`, and reaches core's shared services through `ToolsetContext` (the browser, the search provider, the
rates). Its result texts are its own message enums in its `Messages/` folder (`ShopToolMessage`, `ShopLogMessage`), with
their own golden; `HostMessageEnumTests` finds them.

**Then the composition root:** a member in `IdentityKind` or `PackKind`, its `Create` arm in `Plugins`, a project
reference in `louis-agent.composition.csproj`, and the test projects that need it. A host picks with `IDENTITIES` and
`PACKS` at start-up; `AgentCatalog` refuses a name nothing provides, a name used twice, a tool name two toolsets share
and a `Requires` cycle, before any engine is built.

**The review checklist for a pack** ([ADR 0028](../docs/adr/0028-packs-stay-convertible-to-mcp.md)):
`PackConvertibilityTests` holds the shapes (text returned, plain data taken, a `CancellationToken` when it waits, an
MCP-safe name, no identity named in a description, guides as prose in the pack's prompts, the MCP SDK keeping name,
schema and description). Review the rest by hand, because a test can't see it:

- A failure is text with its reason, never a thrown exception: served over MCP, the SDK would replace the message.
- Whatever a tool reads from the session (the user's place) also arrives as a parameter, so an MCP client can pass it.
- Outside data it returns is framed as data (`--- untrusted … ---`), and its own output is bounded: a call served over
  MCP skips the engine's guards.
- It is ready when built: anything slow or async happens before the catalogue is built, once, and its toolsets and
  tools never change afterwards.
- It references no other pack and no identity (`ArchitectureTests` reads the csproj), and names the packs it needs.

## Building another agent on core

Pass `toolsets:` to the `AgentEngine` constructor to give an agent only your tool objects (plus `ExecuteSkill`) instead
of the built-in coding tools, and build its system prompt from your own skill files (`CompositeSkillProvider` +
`MarkdownSkillProvider`). To record its usage, pass `usageSink: AgentHost.CreateUsageSink(options)` and
`usageMapper: clientFactory.CreateUsageMapper(llm)`, and open a `UsageScope` per turn (with a `UsageTags` run id if it
has one). `louis-agent.orchestrator` does exactly this; its `Program.cs` is the example.

## Changing the LLM

Configuration only — see [MODELS.md](../docs/MODELS.md): `LLM_PROFILE` picks `config/.env.anthropic` or `config/.env.ollama`.
A new model also needs its prices in `config/prices.json` (else its cost is recorded as unknown); run
`tools/louis-agent.usage-probe` before trusting a new provider's usage numbers. Provider differences belong in
`LlmClientFactory` (the client, and `CreateUsageMapper` for how it reports usage), never in `AgentEngine` or the tools.
Where providers need different logic for the same concern, write it as a **strategy**: an interface, one implementation
per provider that differs (plus a default), chosen once in `LlmClientFactory`, as `IUsageMapper` is
([Architecture: Provider strategies](../docs/architecture/05-building-block-view.md#provider-strategies)). Never branch on the provider's name
anywhere else.
