# 0019. A level-0 session is a mode: a voice and its router, paired as data

**Status:** Accepted · **Date:** 2026-10-16 · **Recorded:** 2026-10-16

## Context

After CL8 (ADR 0017, 0018), level 0 had three files doing three jobs: `personality.md`, the coding agent's voice for
every session that names none; `puls.md`, Puls's Afrikaans voice; and `default.md`, one router shared by both
products. The shared router made each product pay for the other's rows in every request's fixed prefix: Puls for git,
dotnet, Paymo and DevOps, the coding agent for the cook and Puls's Afrikaans briefing labels. Puls's language was fixed
in two places (the voice it sends, a "coming soon" language menu) with nothing tying them together. `AGENT_FUNCTION`
selected a placeholder skill file (`code-review.md`, whose skills only echo) or `louis` (every function file), and the
code-review app, which never used those skills, was really a mode of the coding agent.

Considered: deriving a voice's router from its name's prefix (`puls-*`), which hides a rule in a naming habit; one
router with a section per product, which keeps both products paying for both; a `language` field on a session beside
its personality, two settings that must agree.

## Decision

We treat a level-0 session as a **mode**: a voice, and the router that goes with it. The voices are what a session
names (`puls-default-af`, `puls-default-en`, `louis-default-en`, `louis-code-review`); a router (`puls-orchestrator`,
`louis-router`) is never named, it comes with its voice. `Prompts/agent/voices.json` pairs each voice with its router and
names the default voice for a session that names none; the voices offered at level 0 are the ones it lists. Puls's
language is its voice: the front end offers the languages for which `GET /personalities` has a `puls-default-{code}`
voice, and opens new chats with it. `AGENT_FUNCTION` goes: with the `skills` toolset, a session loads every function
skill the agent wrote. What is embedded is built in: the names the agent may not take are the embedded files' names,
not a hand-kept list.

## Consequences

- Each product's fixed prefix holds only its own routing, so it costs less per request; the saving is measured from
  the start-up log, free, before any paid run.
- A new mode is a voice file and a line in `voices.json`; a new Puls language is a `puls-default-{code}` voice and its
  name in the front end, and the menu offers it with no other change.
- Hosts that name no personality (CLI, Rider, MCP) keep the coding agent's voice through the default, unchanged.
- The two Puls voices can drift apart in content; a test holds them to the same sections, and the wording of each is
  reviewed by a person.
- An `.env` that still sets `AGENT_FUNCTION` is ignored.
- Specialists (level 1) and minions (level 2) are unchanged: no router, and their roles as in ADR 0017.

## Links

[CL9 plan](../plans/CL9-modes-per-product.md) · [ADR 0017](0017-skills-by-level.md) ·
[ADR 0018](0018-built-in-prompts-embedded.md) · [ADR 0011](0011-prompts-are-content.md)
