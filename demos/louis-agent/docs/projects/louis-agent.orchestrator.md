# louis-agent.orchestrator — design

> **Kind:** console app · **Path:** `demos/louis-agent/hosts/louis-agent.orchestrator/` · **References:** louis-agent.core · **Talks to:**
> louis-agent.api over HTTP + SSE · **Docker:** `Dockerfile.orchestrator` (its demo compose file was removed in CL10) ·
> **Status:** proof of concept

![The orchestrator: it watches a log, reads its runbook, and has louis-agent fix it](diagrams/louis-agent.orchestrator.svg)

## Purpose

A second agent, built on the same core, that watches one microservice's error log, decides what each exception needs
from a **runbook per API method**, has louis-agent fix the ones the runbook allows, and then checks louis-agent's work
itself before calling anything fixed. It never edits code.

## How it works

![The orchestrator: one error, from the feed to a decision](diagrams/louis-agent.orchestrator-how-it-works.svg)

1. **Poll** (`Program.cs`): read new events from the service's error log (the triage pack's `JsonLinesErrorFeed`,
   since F106.3.1, remembers handled ids in `processed.txt`, so each is triaged once; a failed triage stays unhandled
   for the next run). `--once` runs one pass.
2. **Triage** each event with a fresh history: the runbooks are the system prompt, the event is fenced as untrusted data,
   and the model's only tools are `ServiceTools` and `FixChecks` (via `AgentEngine`'s `toolsets:`).
3. **Fix** (`CallLouisAgentFix`): check `main` out (stashing leftovers), open a session on louis-agent.api, send the fix
   request, stream the reply, send "continue" up to three times if it ends without a `FIX-RESULT`, then delete the
   session.
4. **Verify** — a claim is not proof: the commit exists on the fix branch, which starts from `main` and is not on `main`;
   the failing request now returns 200; the whole test suite passes.
5. **Record**: the `DECISION` line, `fixes.jsonl` / `escalations.jsonl` / `acknowledged.jsonl`, and every message both
   ways in the Markdown `agent-comms.md`.
6. **Cost** (F3): louis-agent's usage per reply (from `message_delta`), the triage's from its own `UsageScope`; the comms
   log shows both per error and a run total split by agent, and `fixes.jsonl` / `decisions.jsonl` carry them. A
   timed-out request fails that error, not the run (FX7): only Ctrl+C ends a run early. Costs are shown in the display
   currency (F14: rates from demo-api's `/v1/rates`, one rate for both agents), with the rate and the USD charged under the
   run total; the jsonl files keep USD.

## Diagrams

The flow is under *How it works*; these are the pieces.

![The orchestrator: the poll, its engine and tools, and what it writes down](diagrams/louis-agent.orchestrator-diagrams.svg)

![One error fixed: louis-agent claims the fix, and the orchestrator's own checks decide whether to trust it](diagrams/louis-agent.orchestrator-sequence.svg)

![The types: three decisions for the model, a client for the API, code that checks the fix, and the logs it writes](diagrams/louis-agent.orchestrator-interface.svg)

## Structure

| Path | What's in it |
|---|---|
| `Program.cs` | Start-up, the poll loop, the triage prompt, `Decision` parsing, runbook loading (`OrchestratorSkills`) |
| `ServiceTools.cs` | The fix and the two decisions: `CallLouisAgentFix`, `EscalateToHuman`, `AcknowledgeError`; the fix prompt; returning to `main` |
| `Tools/Checks/FixChecks.cs` | The checks of a fix the coding agent claims (F106.3.1, split out by responsibility): `VerifyFixCommit`, `ReplayRequest`, `RunServiceTests` |
| `LouisAgentClient.cs` | Sessions on louis-agent.api, SSE parsing into `AgentReply` (text, tool calls with inputs and results), `FIX-RESULT` parsing |
| the triage pack's `Feed/JsonLinesErrorFeed` | Reads the error log, skips handled and malformed events, accepts only plain-token ids; moved out of this project in F106.3.1, so every department's workflow reads the same feed |
| `CommsLog.cs` | The Markdown conversation log: per error, messages, replies, checks, summary; a run summary table |
| `OrchestratorOptions.cs` | Settings |
| `Messages/` | `OrchestratorToolMessage` (what its tools return and send louis-agent), `OrchestratorLogMessage` (its log lines: stderr, tagged `[orchestrator]`, and the run file with `LOG_DIRECTORY`), `OrchestratorErrorMessage` |
| `src/engine/louis-agent.core/prompts/agent/louis-agent/voice-orchestrator.md` | The workflow every error goes through: its mode, built into core (CL9) |
| `Skills/<service>/` | `service.md` (owners, policies) and one runbook per API method — see [Runbooks](orchestrator-runbooks.md) |

## Configuration

`ORCHESTRATOR_SERVICE_ROOT` (required), `ORCHESTRATOR_SERVICE`, `ORCHESTRATOR_SERVICE_PROJECT`, `LOUIS_AGENT_URL`,
`LOUIS_AGENT_API_KEY` (falls back to `AGENT_API_KEY`), `ORCHESTRATOR_STATE_DIRECTORY`, `ORCHESTRATOR_COMMS_LOG`,
`ORCHESTRATOR_POLL_SECONDS`; the model from the usual `LLM_*` settings. Its demo compose file, which set them all, was
removed in CL10, so a run sets them by hand.

## Extending it

The steps are in [Change the orchestrator](../guides/change-the-orchestrator.md). In short: a behaviour change is a
runbook where it can be; a tool is a public method on `ServiceTools` or `FixChecks`.

## Tests

`demos/louis-agent/tests/louis-agent.orchestrator.tests` — see [tests](../../../../src/docs/projects/tests.md): the feed, the SSE client and `FIX-RESULT`, decisions,
runbook loading, the comms log, and the git checks against a throwaway repository. The whole loop against a real model
ran in the demo, removed in CL10; it returns with the evals.

## Limits and plans

- POC choices: a JSON-lines error log instead of an error tracker, a local branch instead of a pull request, polling
  only → [orchestrator design](../specs/ORCHESTRATOR_DESIGN.md) and the TODO backlog.
- Estimates, budgets, stopping at account limits, results as tool calls →
  [F6](../../../../src/docs/features/F06-estimates.md), [F7](../../../../src/docs/features/F07-budgets.md),
  [F8](../../../../src/docs/features/F08-reliability.md).

## Related docs

[Runbooks](orchestrator-runbooks.md) ·
[Agent to agent](../../../../src/docs/AGENT_COMMUNICATION.md#agent-to-agent-the-orchestrator-and-louis-agent) ·
[Orchestrator design](../specs/ORCHESTRATOR_DESIGN.md)

---
[Projects](../../../../src/docs/projects/README.md) · Previous: [louis-agent.mcp](../../../../src/docs/projects/louis-agent.mcp.md) · Next: [louis-agent.usage-probe](../../../../src/docs/projects/louis-agent.usage-probe.md)
