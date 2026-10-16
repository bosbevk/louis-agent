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

**Every toolset follows one pattern: guide → tools → guide.** `default.md` routes a task to a toolset, the toolset's
guide names its tools and when to use them, and each tool's `[Description]` stays short. The files a new toolset
touches, and nothing else:

1. **Tools:** a class in `louis-agent.core/Tools/{Toolset}/` (*Adding a C# tool*); every public method is a tool.
2. **Catalogue entry** in `Agent/ToolsetCatalog.cs`: the name, a one-line description, the settings it needs
   (`MissingSettings`), its tool class (`ToolType`), the factory for its tool object (`CreateTools`) and its guide
   (`GuideFile`).
3. **Guide:** `Prompts/toolsets/{toolset}.md`, plain markdown, embedded with the other built-in prompts: what the toolset is for, its tools grouped with one line
   each, and its workflows (it may also hold `## Skill:` procedures). `AgentHost.LoadSkills(agent, toolsets)` adds it
   to the prompt, through `MarkdownSkillProvider`, only when a session loads the toolset. **No provider class per
   toolset.**
4. **Built-in list:** the guide's name in `SkillFiles.BuiltIn`, so the agent can't overwrite it.
5. **Router:** a row in `default.md`'s toolset table.
6. **Tests:** the name in `DefaultSkillsProviderTests`' toolset list, the toolset's tests in
   `tests/louis-agent.core.tests/Tools/{Toolset}/`, and the golden files regenerated
   ([golden files](../tests/CLAUDE.md#golden-files)).

Every catalogue toolset follows it; `SkillFilesTests` checks that each has a guide that loads with it.

**Skill files we control are plain markdown, always.** That covers every file in `Prompts/`: the toolset guides, and the
function files (`agent/functions/code-review.md`, `time-logging.md`), which `AGENT_FUNCTION={name}` loads by the same
`agent/functions/{name}.md` path as one the agent wrote (that one from the repo's `Skills/`, on disk). `SkillFilesTests` fails if a provider class for one of them appears.

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
| `Agent/` | The loop: turns, history, system prompt, round limits; the tool registry, the tool-call guards that call the model (cut-off calls, oversized results summarised), the skill tools and the agent-built tools (`/approve`) |
| `Llm/` + `Llm/{Name}Provider/` | The model: client factory, `IChatClient` middleware (thinking, caching, usage recording), one folder per provider (`AnthropicProvider/`: a folder named `Anthropic` would hide the SDK's own `Anthropic` namespace) |
| `Tools/{Toolset}/` | What the model can do: one folder per toolset (`Workspace`, `Git`, `DotNet`, `Shell`, `Web`, `Scripts`, `Agents`, `Integrations/Paymo`, `Integrations/DevOps`), plus `Clock` (every session) and `Documents` (shared by two toolsets); `ProcessRunner` beside them |
| `Skills/` | Loading the markdown skills and personality into the prompt |
| `Guardrails/` | Pure safety checks, no model: the workspace boundary, sensitive files, the SSRF guard (the guards that call a model are in `Agent/`) |
| `Usage/` and `Pricing/` | The ledger (records, sinks, scopes, reports); prices, cost, rates and the display currency |
| `Protocols/Mcp/` | Talking to other agents and tools over MCP |
| `Hosting/` | Bootstrap shared by every host: `AgentHost`, and wiring done at start-up (Rider's MCP tools) |
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
4. **Hosts stay thin:** a host project maps the agent's stream to its protocol (`Endpoints/`, `Services/`) and holds no
   agent behaviour; another agent on core (the orchestrator) keeps its own `Tools/` and `Skills/` the same way.
5. **Tests mirror the source:** `tests/louis-agent.core.tests/Llm/AnthropicProvider/…` tests `Llm/AnthropicProvider/…`.
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
