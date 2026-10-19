# 0031. Orchestration is an engine function, and the orchestrator stays on core

**Status:** Accepted · **Date:** 2026-10-19 (decided in the [platform plan](../plans/platform.md), Phase 6 F39) ·
**Recorded:** 2026-10-19

## Context

Delegation exists twice today: Puls hands a task to a specialist (F21, F26, F30), and the orchestrator POC asks
louis-agent for fixes ([0012](0012-one-orchestrator-per-microservice.md)). Depth rules live in the API host, a child's
answer comes back whole and unfenced, one `AGENT_API_KEY` works both ways, and the orchestrator reads its results with
a regex. ADR 0012 left open whether the real orchestrator keeps core or moves to Anthropic's Tool Runner.

## Decision

- **Delegation is one function in the engine:** agents as tools, results typed from a C# record's schema and checked
  in code, one policy per turn tree (depth, limits, who may delegate to whom).
- **The orchestrator is an identity on it**, voorman, with its own voice, runbooks and fixer role.
- **The tier stays `louis-agent.core`**, which closes 0012's open question: the Tool Runner is Anthropic only (against
  [0002](0002-messages-api-tier-with-our-own-loop.md) and [0003](0003-provider-strategies.md)), and would lose the free
  local triage and the shared ledger. Louis answered *yes* (question 8, 2026-10-19).
- **MCP is for tools**; A2A comes later at the edge, as an adapter behind `IChildTransport`.
- **Credentials per peer and an allow-list** of who may delegate to whom (OWASP Agentic ASI03 and ASI07).

## Consequences

- Puls's delegation and voorman share one tested path.
- What would reopen the tier: Phase 6 showing core's loop failing at something the Tool Runner does well that can't be
  fixed in core.

## Links

[Platform plan](../plans/platform.md) (Phase 6) · [0012](0012-one-orchestrator-per-microservice.md) ·
[0021](0021-minions-one-task-one-tool.md) · [0027](0027-engine-identities-and-packs.md)

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
