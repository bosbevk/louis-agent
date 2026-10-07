# Design: Orchestrators and Self-Healing

**Status:** partly proven by the orchestrator POC ([samples/README.md](../../samples/README.md)) · **Date:** moved here
from the TODO on 2026-10-07

How louis-agent fits into a self-healing setup: an **orchestrator** agent per microservice watches it, decides what each
problem needs, and calls louis-agent for anything that is a code change. Each decision below says what the POC showed
and what is still open; the open items are tracked in the [TODO](../../TODO.md) backlog.

## Two kinds of agent

- **louis-agent** stays a coding agent: given a problem, it locates the code, fixes it, tests it and hands back a
  branch (later a pull request). It never deploys.
- **An orchestrator** owns business state and decisions: it watches errors, uptime and performance, decides between a
  code fix, an operational action and escalating to a person, and decides *when* to ship. It calls louis-agent as a
  sub-agent for code changes.

The call goes to **louis-agent.api's session endpoint, not the MCP server**. The MCP server has no model of its own — it
hands out raw tools for the caller's model to drive — so through MCP the orchestrator's model would do the fix reasoning
one tool call at a time, with no louis-agent judgement involved. Through a session, louis-agent runs its own fix → test
loop and the orchestrator gets the result. (Details: [AGENT_COMMUNICATION.md](../AGENT_COMMUNICATION.md).)

## Decisions: one orchestrator per microservice

| # | Decision | POC result | Still open |
|---|---|---|---|
| 1 | **Scope per microservice**, the same way `AGENT_FUNCTION` selects a skill set: one orchestrator codebase, a configuration per service declaring its error-tracker project, repository, escalation rules and service-specific actions (e.g. payments never auto-deploys; orders may restart a stuck worker) | **Proven:** a folder per service (`Skills/order-service/service.md`) with one runbook per API method ([RUNBOOKS.md](../RUNBOOKS.md)); selected with `ORCHESTRATOR_SERVICE` | Error-tracker project ids and service actions (no real tracker yet) |
| 2 | **Don't run the watcher inside the service it watches**: if the service crashes, its watchdog shouldn't crash with it | **Proven:** the orchestrator is its own container | — |
| 3 | **Topology is a deploy choice**: one shared orchestrator loaded with the right configuration per run, or one instance per service (stronger isolation: a misconfigured orchestrator can't touch another service's repository or credentials), like `acp-server` per chat and `mcp-server` per client today | One instance in the demo | Choose for production |
| 4 | **Triggering**: scheduled polling and/or a webhook receiver for error-tracker and uptime alerts, per service | Polling only (`ORCHESTRATOR_POLL_SECONDS`, or `--once`) | Webhook receiver |
| 5 | **The call into louis-agent is an API session**: each service gets its own louis-agent.api with `WORKSPACE_ROOT` fixed to its repository; the orchestrator posts the problem (stack trace, endpoint, frequency) and streams the result | **Proven:** `CallLouisAgentFix` opens one session per fix, sends "continue" follow-ups when louis-agent runs out of tool rounds, and reads the result | A pull request instead of a local branch (needs a PR tool) |
| 6 | **Implementation tier**: originally Anthropic's Tool Runner (`BetaToolRunner`, C#), since the orchestrator has no multi-provider requirement and it matches the stack in place (`Axiz.Adobe` is a .NET/C# repository); its tools (`CallLouisAgentFix`, escalations to other agents) are plain HTTP/MCP calls, not in-process hand-offs | Built on `louis-agent.core` instead (an `AgentEngine` with its own `toolsets:`), reusing providers, the skills loader and the tool loop with no new dependency | **Decide** which to keep before building the real one |
| 7 | **Credentials follow the process split**: the orchestrator holds error-tracker, deploy and business-system credentials; louis-agent holds only git/build access to its one repository, so a bad orchestrator configuration can't push to production — it can only ask for a fix | The tool split holds: the orchestrator has no file-editing, git-write or shell tools; louis-agent has no tracker or deploy access. No real credentials yet | **Branch protection on `main` is required**: the token louis-agent needs to push a PR branch can usually push to `main` too |

**Worked example:** `order-service` throws a new exception → the orchestrator's check (or a webhook) picks it up from the
error tracker → the service's runbook says this error is auto-fixable → `CallLouisAgentFix` opens a session on the
service's louis-agent.api with the stack trace → louis-agent fixes it, tests it and hands back a branch (later a PR) →
the orchestrator — not louis-agent — decides whether to merge and deploy under the service's rules, or wait for a person.
**The POC ran this end to end with ten bugs** ([sample output](../../samples/sample-output/README.md)), with a local
branch instead of a PR and a person merging in the web app.

## Self-healing within louis-agent

The fix loop itself belongs to louis-agent: it is the normal fix → test → PR loop, triggered by an error instead of a
prompt. Default to the guarded path (a PR and a person's approval) before any automatic deploy, since deploys are hard
to reverse and affect shared systems.

| Item | Status |
|---|---|
| Exception-driven triage loop: error → locate code → fix → test → PR (auto-deploy only as an explicit per-environment opt-in) | **POC done** (error → runbook triage → branch → verified commit, replay and tests). Missing: PR, a real error tracker, the deploy opt-in |
| Error-tracker tools (e.g. `ExceptionlessTools`: new and trending errors, stack traces, endpoints, frequency), in the style of `PaymoTools` / `DevOpsTools` | Open — the POC reads a JSON-lines log |
| Rollback / kill switch: revert to the previous release if the error rate spikes after an agent-triggered deploy | Open |
| Synthetic monitoring and performance-regression checks feeding the same loop (not every problem throws) | Open |
| A scheduled maintenance agent (dependency bumps, security patches) | Open |
| A dependency / CVE watcher that starts the same fix → test → PR pipeline when an advisory lands | Open |

## Store operations (a separate orchestrator)

Beyond code fixes, a store-operations orchestrator would also act on business state directly — catalogue, stock,
pricing — without routing those through louis-agent, and own the deploy gate: it decides *when* to ship; louis-agent
only produces the fix. All open:

- Design it: watch store health (errors, uptime, performance) and choose between code fix, operational action and
  escalation.
- Catalogue, inventory and pricing tools for it.
- The approval and deploy-gate boundary between the two agents, so agent-written code and business-critical deploy or
  rollback decisions stay separate.

---
[Docs index](../README.md) · Related: [Orchestrator demo](../../samples/README.md) · [Runbooks](../RUNBOOKS.md) ·
[TODO](../../TODO.md)
