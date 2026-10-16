# 0020. Every agent loads the same principles first: never lie, cheat, steal or murder

**Status:** Accepted · **Date:** 2026-10-17 · **Recorded:** 2026-10-17

## Context

louis-agent runs several agents on one core: Puls's modes, the coding agent's, the specialists and (with F30) the
minions they hand work to, and the microservice orchestrator. Each prompt was built from its own voice or role, and
nothing said what holds for all of them. Rules against inventing a price or faking a result were scattered across roles
(the cook's "never invent a price") or missing, so a new role or mode started without them.

Considered: repeating the rules in every voice and role, which drifts as files are added; putting them in the routing,
which only level 0 loads.

## Decision

We keep one file, `Prompts/shared/principles.md`, and every prompt the hosts build starts with it, at every level and in
the orchestrator: Louis's sentence ("an agent may never lie, cheat, steal or murder") and what each word means for an
agent. It comes first, before the mode or role, so nothing later in the prompt can push it aside. It is built in (ADR
0018), so no copy on disk changes it.

## Consequences

- A new mode, role or minion gets the principles without anyone remembering to add them; a test fails if any level's
  prompt doesn't start with them, or the orchestrator's.
- Every request pays for the file's tokens (about 280), in the cached prefix.
- A prompt states intent; it doesn't enforce. The guards in code still hold the line: arguments as data (ADR 0004),
  the SSRF and sensitive-file guards, F30's `VerifyQuote` for prices.
- Changing the principles changes every agent's prompt at once: the golden file shows it, and the checks measure it.

## Links

[CL9 plan](../plans/CL9-modes-per-product.md) (*Approved during the build*, step 2) ·
[ADR 0004](0004-arguments-as-data.md) · [ADR 0018](0018-built-in-prompts-embedded.md)
