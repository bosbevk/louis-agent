# Plan: CL1 Project layout (cleanup)

> **For:** the TODO's *Project layout, after F4, first of two cleanups* · **Rules:**
> [louis-agent.core/CLAUDE.md, Project layout](../../louis-agent.core/CLAUDE.md#project-layout) · **Branch:** `refactor/project-layout` ·
> **Approved:** 2026-10-11

## Context

The rules for the layout were written during F4: folders by concern, the namespace is the folder path in PascalCase,
one top-level type per file, dependencies pointing one way, tests mirroring the source, and an architecture test to
hold all of it. Today core mixes concerns (the agent loop lives in `tools/`, the LLM factory beside the skill
providers, usage and pricing in one folder), namespaces are lowercase (`louis_agent.core.tools`), and 15 files hold
more than one type.

**This is a move, not a rewrite.** Behaviour doesn't change; the tests pass untouched apart from their namespaces,
`using` lines and folders. Nothing paid runs.

**Decided with Louis (2026-10-11):**

- **PascalCase everywhere:** core, every host, the orchestrator, the usage probe and both test projects
  (`louis_agent.api` becomes `LouisAgent.Api`). Project and assembly names (`louis-agent.api`) don't change, so
  Docker, `InternalsVisibleTo` and the solution file are untouched.
- **One type per file, in this PR:** the 15 multi-type files are split as their folder moves.
- **`AgentEngine` moves whole** to `Agent/`; splitting it (the loop, the tool registry, the guards into
  `Guardrails/`) is the next TODO item, with its own plan. So `Guardrails/` stays empty here.

## Design

**Where each file goes** (core; `LouisAgent.Core` is the root namespace):

| Folder | Files (one type each after the split) |
|---|---|
| `Agent/` | `AgentEngine` |
| `Llm/` | `ILlmClientFactory`, `LlmClientFactory`, `IUsageMapper`, `StandardUsageMapper`, `UsageRecordingChatClient`, `IPromptCachingStrategy`, `NoPromptCaching`, `PromptCachingChatClient` |
| `Llm/Anthropic/` | `AnthropicUsageMapper`, `AnthropicPromptCaching` |
| `Usage/` | `UsageRecord`, `UsageTokens`, `UsageCost`, `UsagePurpose`, `IUsageSink`, `JsonlUsageSink`, `UsageScope`, `UsageTags`, `UsageReport`, `UsageTotal`, `UsageSession`, `UsageText`, `CacheGaps`, `CacheGapGroup`, `LifetimeCost` |
| `Pricing/` | the price table's types, `CostCalculator`, `PricingUsageSink`, `ExchangeRates`, the rates mappers, `DisplayCurrency` |
| `Skills/` | `MarkdownSkillLoader`, `ISkillProvider`, `CompositeSkillProvider`, `MarkdownSkillProvider`, the seven `*SkillsProvider`s |
| `Tools/` | `ProcessRunner` (used by several toolsets) |
| `Tools/{Workspace, Git, DotNet, Shell, Web, Scripts}/` | `WorkspaceTools`; `GitTools`; `DotNetTools`; `BashTools`, `PowerShellTools`, `PythonTools`; `WebTools` and the search providers; `ScriptTool` and its types (agent-built tools) |
| `Tools/Integrations/{Paymo, DevOps}/` | `PaymoTools`; `DevOpsTools` |
| `Protocols/Mcp/` | `RiderMcpClient` and its type, `RiderMcpToolDiscovery` |
| `Hosting/` | `AgentHost` and its context type, `AgentLog` |
| `Config/` | `AgentOptions`, `EnvFile`, `LlmOptions`, `CacheLifetime` |
| `Messages/` | `Messages`, `ErrorMessage`, `LogMessage`, `ToolMessage`, `UserText` |

**The other projects:** namespaces become `LouisAgent.Api`, `LouisAgent.AcpServer`, `LouisAgent.Cli`, `LouisAgent.Web`,
`LouisAgent.McpServer`, `LouisAgent.Orchestrator`, `LouisAgent.UsageProbe`, `LouisAgent.Core.Tests`,
`LouisAgent.Orchestrator.Tests` (`RootNamespace` in each csproj; the web app's Razor `_Imports`). Rule 4's host folders:
the API's three endpoint files go to `Endpoints/`, the orchestrator's `ServiceTools` to `Tools/`; nothing else moves.

**Dependencies** (rule 2), as layers; a folder may use its own layer and the ones below:

| Layer | Folders |
|---|---|
| 4 | `Hosting` |
| 3 | `Agent` |
| 2 | `Llm`, `Tools`, `Skills`, `Usage`, `Pricing`, `Protocols` (and `Tools` never uses `Llm`) |
| 1 | `Guardrails` |
| 0 | `Config`, `Messages` |

Two references point up today and move down: `EnvFile` calls `AgentHost.FindConfigDirectory` (the method moves to
`Config/`), and `LlmClientFactory` reads `AgentEngine.MaxOutputTokens` (the constant moves to `Llm/`, and the engine
reads it from there).

**The architecture test** (`tests/…/ArchitectureTests.cs`) reads core's source files: each file's namespace must be
`LouisAgent.Core` plus its folder path, a file holds one top-level type, and its `using LouisAgent.Core.*` lines follow
the layers. It starts with a **pending list** of today's violations and is a ratchet: it fails on a new violation, and
on a pending entry that no longer violates (so each move must shrink the list). The list is empty at the end.

**Moving files:** `git mv`, so history follows; a case-only rename (`config` → `Config`) goes through a temporary name,
which Windows needs.

## Steps (each one commit; tests pass after every one)

0. **The plan in the repo**, the plans table, and a TODO line for the `AgentEngine` split as the next item.
1. **The architecture test**, with the pending list of today's violations.
2. **The other projects in PascalCase:** `RootNamespace`s, namespaces, `using`s and `_Imports.razor`; the API's
   `Endpoints/` and the orchestrator's `Tools/`. Core keeps its namespaces for now (they're used through `using`).
3. **`Config/` and `Messages/`:** moved, split (`CacheLifetime`, the four message enums), `FindConfigDirectory` moved
   down.
4. **`Usage/` and `Pricing/`**, split.
5. **`Llm/` and `Llm/Anthropic/`**, split; `MaxOutputTokens` moved down.
6. **`Skills/` and `Protocols/Mcp/`.**
7. **`Tools/{Toolset}/`**, split (the search providers, `ScriptTool`'s types).
8. **`Agent/` and `Hosting/`**: the pending list is now empty.
9. **Tests mirror the source:** every test file in the folder of what it tests, with the matching namespace.
10. **Docs:** CLAUDE.md (the tree, the *Today* column becomes *Done*, the *Moving there* paragraph), ARCHITECTURE, the
    project docs and README; the TODO tick and a Done line. Historical docs (plans, fix docs, finished features) keep
    the paths they were written with.

The review stop: after step 3, when the pattern is set, and at the end before pushing.

## Approved during the build (2026-10-11)

- **`Messages` → `MessageText`:** a type can't share its namespace's name (`LouisAgent.Core.Messages.Messages` makes
  `Messages.Format` resolve to the namespace wherever usings sit before the namespace line), so the class is
  `MessageText`; the folder stays `Messages/`.
- **`Llm/AnthropicProvider/`, not `Llm/Anthropic/`:** a namespace `LouisAgent.Core.Llm.Anthropic` hides the Anthropic
  SDK's root namespace inside `LouisAgent.Core.Llm`, so `using Anthropic.Models.Messages;` there would not compile. A
  provider's folder is `Llm/{Name}Provider/`; the rule in docs/CLAUDE.md says so.
- **`RiderMcpToolDiscovery` in `Hosting/`, not `Protocols/Mcp/`:** it registers Rider's tools with the engine at
  start-up and only `AgentHost` calls it, so in `Protocols/` it would use `Agent`, a layer above. `Protocols/Mcp/` holds
  the client.
- **A `Logging/` folder at layer 0 for `AgentLog`, not `Hosting/`:** the agent loop and the usage ledger both write to
  it, so it must sit below them. The messages-and-NLog cleanup puts its `ILogger` setup there too.
- **`BuiltInSkillFiles` moves to `Skills/SkillFiles`:** the engine refuses to overwrite those files, and the list lived
  in `AgentHost`, a layer above the engine.

## Verification

- `dotnet build louis-agent-solution.sln` and `dotnet test louis-agent-solution.sln` after every step.
- The architecture test's pending list shrinks with each move and is empty after step 8.
- `git log --follow` on a moved file shows its history from before the move.
- The Docker images still build (`docker compose … build`), since project names didn't change.
- No paid run: nothing the model sees changes (tool names, descriptions and the system prompt come from method names
  and `Skills/*.md`, not namespaces). Checked by comparing the tool JSON and system prompt before and after with the
  existing `ToolOrderTests` approach.

## Concepts

- **Separation of concerns in an agent:** the loop, the model and its providers, the tools, the skills, the guardrails
  and the measurements each change for different reasons, so each has one home (skill #1 models and providers, #4 tool
  design, #18 testing and shipping).
- **Architecture as a test:** a rule the build checks stays true; a rule in a document drifts (#18).
- **Behaviour-neutral refactoring:** the same tests, the same tool JSON, history kept with `git mv`.
