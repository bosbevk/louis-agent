# Plan: CL3 Messages and logging (cleanup)

> **For:** the TODO's *Messages and logging, after F4, second of two cleanups* · **Rules:**
> [louis-agent.core/CLAUDE.md, Messages and logging](../../louis-agent.core/CLAUDE.md#messages-and-logging) · **Branch:** `refactor/messages-and-logging` ·
> **Approved:** 2026-10-12

## Context
The second cleanup after F4 (TODO, *Quality and CI*; the rule is in docs/CLAUDE.md, *Messages and logging*). Message
text is still written inline across core and the hosts: ~39 exceptions built from literals, ~95 error strings tools
return to the model, the usage text, the comms log's headings, and 135 `Console.Error` writes (most in the ACP server
and `AgentHost`). Logging is `Console.Error` plus `AgentLog`'s own files (a stderr copy per run,
`oversized-tool-results.jsonl`, `truncated-tool-calls.jsonl`).

**Decided with Louis (2026-10-12):**
- **`ILogger` with Serilog**, not NLog: configured in code in `AgentHost` (no config file to ship in every image),
  CompactJsonFormatter for JSON lines, everything on stderr. CLAUDE.md's rule changes from NLog to Serilog.
- **The comms log stays as it is** (`agent-comms.md`); only the orchestrator's jsonl keys become consistent
  (`decisions.jsonl` mixes `error_id` with `Method`, `Outcome`).
- **Structured results between the agents** (F8-S4: `ReportFixResult` and the decision as tool calls instead of the
  `FIX-RESULT` text line) become their own feature after this cleanup and FX4, before F5; this plan only adds its TODO
  line.

## Design
**Messages** (the rule as written): every message is a member of `ErrorMessage`, `LogMessage`, `ToolMessage` or
`UserText`, its `[Description]` a template with named holes, rendered by `MessageText.Format`. Core's enums hold core's
texts; a host with its own texts (the orchestrator's comms log and tools, the ACP server, the usage probe) gets its own
`Messages/` folder with its own enums, using core's `MessageText`, and its own enum test (core can't know host texts).

**The texts the model reads must not change.** Tool error strings move into `ToolMessage`, rendered to the same bytes:
each conversion commit keeps the existing tests that assert those strings, and a new golden file
(`ToolMessageSnapshot.json`, like step 1 of CL2) renders every `ToolMessage` template with fixed sample values, so a
later wording change is deliberate and reviewed. The model-request snapshot (tool descriptions, system prompt) passes
throughout.

**Logging:** `Logging/AgentLogging` owns one Serilog configuration per process, set by `AgentHost` from `AgentOptions`:
- console sink to **stderr only** (stdout carries ACP and MCP), template `[{Level:u4}] {Message:lj}` so lines read as
  today (`[INFO]`, `[WARN]`; tool calls `[TOOL] -> …` become Information events with a `Tool` property);
- `LOG_DIRECTORY` set: the per-run text file (today's stderr copy) and the two record files as JSON lines
  (CompactJsonFormatter, filtered by a `Record` property), same names, so CLAUDE.md's *Common errors* still holds;
- classes use `ILogger<T>` from a static `SerilogLoggerFactory` (no DI container in core; the API's DI gets the same
  factory); a small extension `logger.Log(level, LogMessage.X, new { Url, Reason })` passes the values in the template's
  hole order (`MessageText.Holes`), so names can't drift from arguments.
- `AgentLog` goes once nothing calls it. The usage ledger stays its own `JsonlUsageSink` (data, not logs).

## Steps (each one commit with its test; branch `refactor/messages-and-logging`; review stop after step 2 and at the end)
0. **Plan** as `docs/plans/CL3-messages-and-logging.md`; TODO lines: CL3 in progress, and *Structured results between
   agents* (F8-S4 pulled forward) after FX4.
1. **Serilog behind `ILogger`:** packages (Serilog.Extensions.Logging, Sinks.Console, Sinks.File, Formatting.Compact),
   `AgentLogging`, the `Log(level, LogMessage, values)` extension, `AgentHost` configures it. *Tests:* stderr only,
   nothing on stdout; the run file and a record file written as JSON lines; holes bound by name.
2. **`ToolMessageSnapshot.json`** and its test (empty enum to start).
3. **Config, Pricing, Usage:** exceptions and log lines to the enums and `ILogger`.
4. **Llm, Skills, Protocols/Mcp, Guardrails.**
5. **Tools/{Toolset}:** the ~55 tool strings to `ToolMessage` (DevOps, Web, Shell, Git, DotNet, Workspace, Paymo).
6. **Agent:** `SkillTools`, `AgentBuiltTools`, `ToolCallGuards` (log lines and tool strings), the engine.
7. **Hosting:** `AgentHost`'s 29 lines; `AgentLog` removed.
8. **Hosts:** the ACP server (32), the API, the CLI, the MCP server, the usage probe; each host's own enums and test.
9. **Orchestrator:** its tools' strings, log lines and the comms log headings (`UserText`), and consistent snake_case
   keys in `decisions.jsonl` / `fixes.jsonl` (readers checked first).
10. **Docs and finish:** CLAUDE.md (Serilog instead of NLog, *Moving there* removed, *Running and debugging*), the
    project docs, TODO tick and Done line.

## Approved during the build (2026-10-12)

- **A tag instead of `[{Level:u4}]`:** Serilog's four-letter codes print `EROR`, and some lines always had their own
  bracket (`[TOOL]`, `[MCP]`, `[ACP]`). `LevelTagEnricher` fills a `Tag` property from the level (`INFO`, `WARN`,
  `ERROR`) unless the call passed `tag:`; the console template is `[{Tag}] {Message:lj}`.
- **One root logger with a forwarding sink:** a logger made before `Configure` (a static field) stayed bound to the
  old configuration. The root's only sink forwards to the current one, so `Configure` can replace it any time; a test
  holds it.
- **`MessageLogging.Log` takes any `Enum`,** not only `LogMessage`, so a host logs its own members the same way.
- **Host enums named per host** (`AcpLogMessage`, `AcpUserText`, `AcpModelText`, `ApiLogMessage`,
  `McpServerLogMessage`, `CliUserText`, `ProbeUserText`, `OrchestratorToolMessage`, `OrchestratorLogMessage`,
  `OrchestratorErrorMessage`), in each project's `Messages/`; `HostMessageEnumTests` checks them from source, since
  most hosts have no test project. The probe's cache-gaps texts (written into core's enums in F4) moved to
  `ProbeUserText`.
- **The stderr tee kept until step 8d,** when nothing wrote to `Console.Error` directly any more: the run file then
  became a Serilog file sink and `AgentLog` went, its records to `ToolCallRecords`. The run file uses `{Message:l}`,
  not `:lj`: the file formatter writes a non-string value (a `Uri`) as JSON, in quotes.
- **Numbers in messages are invariant:** an oversized result's count reads `12,345` on every machine (it was the
  culture's, `12 345` on en-ZA).
- **Step 8 in four commits (8a ACP server, 8b API/MCP/CLI, 8c usage probe, 8d run file) and step 9 in three** (9a the
  orchestrator's tool strings and exceptions, 9b its log lines, 9c `decisions.jsonl`'s keys).
- **The orchestrator logs through `ILogger`:** stderr instead of stdout, `[orchestrator]` (louis-agent's progress
  `[louis-agent]`), `[WARN]` instead of a `WARNING:` prefix, the time in the run file it now configures.
- **Not converted:** the comms log stays as it is (Louis, 2026-10-12: "drop it for now"); the prompts (`TriagePrompt`,
  `FixPrompt`) are content, not messages; `PASS`/`FAIL`/`VERIFIED`, `FIX-RESULT` and `DECISION` are keywords another
  program matches on, so they stay in code.
- **From the PR #20 review:** steps 5b and 5c convert what step 5 left inline, so the rule holds everywhere: core's
  tool results and refusals (85 more `ToolMessage`), `/tools` and `/usage` replies, the hosts' attachment notes, the
  API's answers (`ApiUserText`, `ApiModelText`), the MCP server's (`McpServerText`) and the last literal exceptions.
  Delimiters (the web content markers) and patterns matched in git's output stay in code.
- **The record files keep the shape `AgentLog` wrote** (`RecordFormatter`: `timestamp` and camelCase fields), not
  CompactJsonFormatter's (`@t`, `@mt`, `Tag`, `SourceContext`), because F16 reads them; Serilog.Formatting.Compact is
  dropped.

## Verification
- `dotnet build` and `dotnet test louis-agent-solution.sln` after every step; the model-request snapshot, the
  tool-message snapshot, the message-enum tests and `ArchitectureTests` pass throughout.
- `grep` for inline messages left (`throw new …("`, `Console.Error`, `return "Error:`) goes to zero step by step.
- By hand, free: the CLI on Ollama (stderr lines as before, stdout the answer only), Rider over ACP (stdout carries only
  the protocol), the API image builds and its log file appears in `logs/`.
- A `-Short` demo run (≈ $0.05) only if Louis wants to see the comms log and run files from a real run; not required.

## Concepts
Structured logging with message templates (#17 observability), separating what is said from where it is said, and
keeping model-facing text stable with a golden file (#4 tool design, #18).
