# 0009. Message text lives in enum templates; logging goes through `ILogger`

**Status:** Accepted · **Date:** 2026-10-12 · **Recorded:** 2026-10-14

## Context

Messages were written inline wherever they were used: exceptions, log lines, the error strings tools return to the
model, and user-facing text (486 once collected: 313 in core, the rest in the hosts). The texts the model reads
couldn't be reviewed in one place, a change to one could slip through unnoticed, and logging went to `Console.Error`
and `AgentLog`'s own files with no structure.

## Decision

Every message is a member of a message enum (`ErrorMessage`, `LogMessage`, `ToolMessage`, `UserText`, and each host's
own), and its `[Description]` is the text: a template with named holes, filled by `MessageText.Format`. Logging goes
through `ILogger` (Microsoft.Extensions.Logging) with Serilog behind it, configured in code: stderr only, a run file
and the JSON-lines records as sinks, holes kept as fields. Serilog was chosen over NLog.

## Consequences

- Every text the model reads is pinned by a golden file (`ToolMessageSnapshot.json`) and reviewed in its diff.
- A test checks every enum member has a template with well-formed holes; a missing hole value throws.
- stdout stays free for the ACP and MCP protocols.
- Prompts, protocol keywords (`FIX-RESULT`, `DECISION`) and table formatting are not messages and stay where they are.

## Links

[CL3 plan](../plans/CL3-messages-and-logging.md) ·
[Messages and logging](../../louis-agent.core/CLAUDE.md#messages-and-logging)
