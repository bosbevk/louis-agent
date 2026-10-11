# Plan: CL2 Split AgentEngine (cleanup)

> **For:** the TODO's *Split `AgentEngine`, right after the project layout* · **Rules:**
> [docs/CLAUDE.md, Project layout](../CLAUDE.md#project-layout) · **Branch:** `refactor/split-agent-engine` ·
> **Approved:** 2026-10-11

## Context
CL1 moved `AgentEngine` whole into `Agent/` (1,078 lines) and left `Guardrails/` empty; the TODO's next item is to split
it. It mixes seven concerns: construction and the client pipeline, the tool-call guards (cut-off calls, oversized
results summarised by the model, tools kept on the loop's final request), the tool registry, a console chat UI, the
turn loop, the skill tools (`ExecuteSkill`, `CreateSkillFile`, `ReloadSkills`) and the agent-built tools (`CreateTool`,
`/approve`, `/reject`). The pure safety checks live in toolsets: `ResolvePath`/`IsSensitive` in `WorkspaceTools` (also
used by the API's endpoints) and the SSRF check in `WebTools`. **Behaviour unchanged**; unlike CL1 this changes code,
so the first step pins down what the model sees.

**Decided with Louis (2026-10-11):** real classes, with `AgentEngine` as a façade (its public members hosts and tests
call stay, as one-line delegates); the console UI moves to the CLI project.

## Design
**Guardrails/** (layer 1: pure checks, no model, no usage):
- `WorkspaceBoundary` (`ResolvePath` against a root, from `WorkspaceTools`) and `SensitiveFiles.IsSensitive`.
- `SsrfGuard`: `IsPublicAddress`, the URI check and the guarded `SocketsHttpHandler` (from `WebTools`).
- `WorkspaceTools` and `WebTools` call them; the API's `GitEndpoints`, `HistoryEndpoints`, `WorkspaceEndpoints` call
  `SensitiveFiles` / `WorkspaceBoundary` directly instead of reaching into a toolset.

**Agent/** (the engine keeps the loop; each concern gets a class):
| Class | From AgentEngine | State it owns |
|---|---|---|
| `AgentEngine` | ctor + pipeline, `NewHistory`, `ProcessPromptAsync`, `StreamPromptAsync`, `BeginTurn`, cut-off continuation, `SkillDocumentation`, options/thinking/display | the client pipeline, skills, history rules |
| `ToolRegistry` | `ByName`, `AddTool`, `AddPublicMethodsAsTools`, `RegisterDiscoveredTool`, `ToolsChanged` | the sorted tool list |
| `ToolCallGuards` | `LogAndInvokeAsync`, `KeepToolsOnFinalRequest`, `MarkTruncatedToolCalls*`, `SummariseToolResultAsync`, `MaxToolResultChars`, `TruncatedCallMessage` | the inner client (for summaries) |
| `SkillTools` | `ExecuteSkill`, `CreateSkillFile`, `ReloadSkills`, `RunShellCommand`, `FormatSkillResult`, their regexes | skills directory, created files |
| `AgentBuiltTools` | `CreateTool`, `ListAgentTools`, `LoadApprovedTools`, `HandleUserCommand` (`/approve`, `/reject`), `RegisterScriptTool`, `RunScriptTool` | the script tools |

The guards that call the model (summarising an oversized result) stay in `Agent/`, since `Guardrails/` sits below
`Llm` and `Usage`; CLAUDE.md's `Guardrails/` row says so. Tool names and descriptions are unchanged: tools are still
registered with explicit names (`nameof(...)`) and keep their `[Description]`s.

**CLI:** `src/louis-agent.cli/ConsoleChat.cs` takes `RunAsync`, `RunSinglePromptAsync`, `StreamToConsoleAsync`,
`WriteUsageLine`; `DescribeArguments` stays in core (Rider's ACP server uses it).

Moved code keeps its inline strings; the messages-and-NLog cleanup (next) converts them. Tests that call moved internals
(`KeepToolsOnFinalRequest`, `SummariseToolResultAsync`, `MaxToolResultChars`, …) follow them to their class's test
folder.

## Steps (each one commit with its test; branch `refactor/split-agent-engine`; review stop after step 2 and at the end)
0. **Plan** as `docs/plans/CL2-split-agent-engine.md`, the plans table.
1. **A snapshot test of what the model sees:** the tool JSON (names, descriptions, schemas, order) and the system prompt
   of a default engine, compared with a committed golden file. Every later step must leave it unchanged.
2. **`Guardrails/`:** `WorkspaceBoundary`, `SensitiveFiles`, `SsrfGuard`; the toolsets and the API call them. *Tests:*
   the existing path, secret-file and SSRF tests move to `tests/…/Guardrails/` and pass unchanged.
3. **`ToolRegistry`.** *Tests:* `ToolOrderTests`, discovered tools.
4. **`ToolCallGuards`.** *Tests:* the round-limit, cut-off and summary tests.
5. **`SkillTools`.** *Tests:* skill authoring, `ExecuteSkill`.
6. **`AgentBuiltTools`.** *Tests:* `ScriptToolTests`, `/approve` and `/reject`.
7. **`ConsoleChat` into the CLI.** *Check:* `dotnet run --project src/louis-agent.cli -- "…"` on Ollama (free) prints
   as before.
8. **Docs and finish:** CLAUDE.md (tree, `Guardrails/` row, *Adding a C# tool* if registration moved), ARCHITECTURE's
   `AgentEngine` section, the core project doc; TODO tick and Done line.

## Approved during the build (2026-10-12)

- **Line endings, fixed first (its own commit):** the golden file showed that what the model is sent took the
  checkout's line endings (the system prompt from `Skills/*.md`, two multi-line `[Description]`s). The engine now
  normalises both to LF (`Normalise`, `Function`), so every machine sends the same bytes; on Windows the requests
  changed once.
- **`HandleUserCommand` stays on the engine:** it also answers `/usage`; it hands `/tools`, `/approve` and `/reject` to
  `AgentBuiltTools`.
- **`ResolveShell` moved to `BashTools`** beside `FindBash`: the skills and the agent-built tools both use it.

## Verification
- `dotnet build` and `dotnet test louis-agent-solution.sln` after every step; the snapshot test and `ArchitectureTests`
  pass throughout; the test count only grows.
- Free by hand: one CLI prompt on Ollama, the API image builds; no paid run (the snapshot proves the requests the
  model sees are byte-identical).

## Concepts
Separation of concerns inside the agent loop (#6), guardrails as their own layer (#15 safety), a golden-file test as the
proof of a behaviour-neutral refactor (#18).
