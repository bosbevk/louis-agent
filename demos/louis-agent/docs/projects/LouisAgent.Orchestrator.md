# LouisAgent.Orchestrator — design

> **Kind:** program (a console app) · **Path:** `demos/louis-agent/hosts/LouisAgent.Orchestrator/` · **References:** LouisAgent.Core · **Talks to:**
> LouisAgent.Api over HTTP + SSE · **Docker:** `Dockerfile.orchestrator` (its demo compose file was removed in CL10) ·
> **Status:** proof of concept
> **Also shows:** `component`, `activity`

![The orchestrator: it watches a log, reads its runbook, and has louis-agent fix it](diagrams/LouisAgent.Orchestrator.svg)

## Purpose

A second agent, built on the same core, that watches one microservice's error log, decides what each exception needs
from a **runbook per API method**, has louis-agent fix the ones the runbook allows, and then checks louis-agent's work
itself before calling anything fixed. It never edits code.

## How it works

![The orchestrator: one error, from the feed to a decision](diagrams/LouisAgent.Orchestrator-how-it-works.svg)

### 1. Poll

It starts in `Program.cs` by reading new events from the service's error log. The triage pack's `JsonLinesErrorFeed`
remembers the ids it has handled in `processed.txt`, so each event is triaged once, and a failed triage stays
unhandled for the next run. `--once` runs a single pass.

*Decided in:* F106.3.1.

### 2. Triage

Each event is triaged with a fresh history. The runbooks are the system prompt, the event is fenced off as untrusted
data, and the model's only tools are `ServiceTools` and `FixChecks`, handed to the engine through `AgentEngine`'s
`toolsets:`.

### 3. Fix

When the decision is to fix, `CallLouisAgentFix` checks `main` out (stashing anything left over), opens a session on
LouisAgent.Api and sends the fix request. It streams the reply, sends "continue" up to three times if the reply ends
without a `FIX-RESULT`, and then deletes the session.

### 4. Verify

A claim is not proof, so the orchestrator checks for itself. The commit must exist on the fix branch, which starts
from `main` and isn't on `main`. The request that failed must now return 200. And the whole test suite must pass.

### 5. Record

It writes the `DECISION` line, the records in `fixes.jsonl`, `escalations.jsonl` and `acknowledged.jsonl`, and every
message both ways in the Markdown `agent-comms.md`.

### 6. Cost

louis-agent's usage comes with each reply (from `message_delta`), and the triage's from its own `UsageScope`. The comms
log shows both for each error, and a run total split by agent, and `fixes.jsonl` and `decisions.jsonl` carry them. A
request that times out fails that error, not the run: only Ctrl+C ends a run early. Costs are shown in the display
currency, with rates from demo-api's `/v1/rates` and one rate for both agents, and the rate and the USD charged sit
under the run total. The jsonl files keep USD.

*Decided in:* F3, FX7, F14.

The pieces:

![The orchestrator: the poll, its engine and tools, and what it writes down](diagrams/LouisAgent.Orchestrator-diagrams.svg)

![One error fixed: louis-agent claims the fix, and the orchestrator's own checks decide whether to trust it](diagrams/LouisAgent.Orchestrator-sequence.svg)

![The types: three decisions for the model, a client for the API, code that checks the fix, and the logs it writes](diagrams/LouisAgent.Orchestrator-interface.svg)

## Structure

| Path | What's in it |
|---|---|
| `Program.cs` | Start-up, the poll loop, the triage prompt, `Decision` parsing, runbook loading (`OrchestratorSkills`) |
| `Tools/ServiceTools.cs` | The fix and the two decisions: `CallLouisAgentFix`, `EscalateToHuman`, `AcknowledgeError`; the fix prompt; returning to `main` |
| `Tools/Checks/FixChecks.cs` | The checks of a fix the coding agent claims (F106.3.1, split out by responsibility): `VerifyFixCommit`, `ReplayRequest`, `RunServiceTests` |
| `Client/LouisAgentClient.cs` | Sessions on LouisAgent.Api, SSE parsing into `AgentReply` (text, tool calls with inputs and results), `FIX-RESULT` parsing |
| the triage pack's `Feed/JsonLinesErrorFeed` | Reads the error log, skips handled and malformed events, accepts only plain-token ids; moved out of this project in F106.3.1, so every department's workflow reads the same feed |
| `Comms/CommsLog.cs` | The Markdown conversation log: per error, messages, replies, checks, summary; a run summary table |
| `OrchestratorOptions.cs` | Settings |
| `Messages/` | `OrchestratorToolMessage` (what its tools return and send louis-agent), `OrchestratorLogMessage` (its log lines: stderr, tagged `[orchestrator]`, and the run file with `LOG_DIRECTORY`), `OrchestratorErrorMessage`; in `Holes/` the records that fill the log lines, by what the orchestrator is doing (starting up, the comms log, an error event, a fix, the checks; ADR 0119) |
| `demos/louis-agent/identity/LouisAgent.Identities.Coding/agents/louis-agent/voice-orchestrator.md` | The workflow every error goes through: its mode, one of the coding identity's prompts |
| `Skills/<service>/` | `service.md` (owners, policies) and one runbook per API method — see [Runbooks](orchestrator-runbooks.md) |

## Configuration

Where it runs: as its own program beside the service it watches, in its own container or on the PC.

![Where the orchestrator runs: beside the watched service, polling its error log, asking louis-agent's API for a fix and writing its records](diagrams/LouisAgent.Orchestrator-deployment.svg)

`ORCHESTRATOR_SERVICE_ROOT` (required), `ORCHESTRATOR_SERVICE`, `ORCHESTRATOR_SERVICE_PROJECT`, `LOUIS_AGENT_URL`,
`LOUIS_AGENT_API_KEY` (falls back to `AGENT_API_KEY`), `ORCHESTRATOR_STATE_DIRECTORY`, `ORCHESTRATOR_COMMS_LOG`,
`ORCHESTRATOR_POLL_SECONDS`; the model from the usual `LLM_*` settings. Its demo compose file, which set them all, was
removed in CL10, so a run sets them by hand.

## Extending it

The steps are in [Change the orchestrator](../guides/change-the-orchestrator.md). In short: a behaviour change is a
runbook where it can be; a tool is a public method on `ServiceTools` or `FixChecks`.

## Tests

`demos/louis-agent/tests/LouisAgent.Orchestrator.Tests` — see [tests](../../../../src/docs/projects/tests.md): the feed, the SSE client and `FIX-RESULT`, decisions,
runbook loading, the comms log, and the git checks against a throwaway repository. The whole loop against a real model
ran in the demo, removed in CL10; it returns with the evals.

## Limits and plans

POC choices: a JSON-lines error log instead of an error tracker, a local branch instead of a pull request, polling
only → [orchestrator design](../specs/ORCHESTRATOR_DESIGN.md) and the TODO backlog. Estimates, budgets, stopping at
account limits, results as tool calls → [F6](../../../../src/docs/features/F06-estimates.md),
[F7](../../../../src/docs/features/F07-budgets.md), [F8](../../../../src/docs/features/F08-reliability.md).

## Related docs

[Runbooks](orchestrator-runbooks.md) ·
[Agent to agent](../../../../src/docs/AGENT_COMMUNICATION.md#agent-to-agent-the-orchestrator-and-louis-agent) ·
[Orchestrator design](../specs/ORCHESTRATOR_DESIGN.md)

---
[Projects](../../../../src/docs/projects/README.md) · Previous: [LouisAgent.Mcp](../../../../src/docs/projects/LouisAgent.Mcp.md) · Next: [LouisAgent.UsageProbe](../../../../src/docs/projects/LouisAgent.UsageProbe.md)
