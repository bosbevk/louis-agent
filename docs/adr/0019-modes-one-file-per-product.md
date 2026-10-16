# 0019. A level-0 session is a mode, in a folder per product; a language shares its product's routing

**Status:** Accepted · **Date:** 2026-10-17 · **Recorded:** 2026-10-17

## Context

After CL8 (ADR 0017, 0018), level 0 had three files doing three jobs: `personality.md`, the coding agent's voice for
every session that names none; `puls.md`, Puls's Afrikaans voice; and `default.md`, one router shared by both
products. The shared router made each product pay for the other's rows in every request's fixed prefix: Puls for git,
dotnet, Paymo and DevOps, the coding agent for the cook and Puls's Afrikaans briefing labels. Puls's language was fixed
in two places (the voice it sends, a "coming soon" language menu) with nothing tying them together. `AGENT_FUNCTION`
selected a placeholder skill file, and the code-review app and the microservice orchestrator were modes of their own
with no file that said so.

Considered: a voice and a router as two files paired in a `voices.json` (drawn first: a rule file has to keep them
paired); one file per mode with the routing repeated in each and a test keeping the copies identical (built first, in
step 1: Puls's languages would carry the same routing twice); one router with a section per product, which keeps both
products paying for both; a `language` field on a session beside its personality, two settings that must agree.

## Decision

We treat a level-0 session as a **mode**, named `{product}/{mode}`, in `Prompts/agent/{product}/`:

- A **whole mode** is one file, its voice then its routing: `louis-agent/voice` (the coding agent, the default for a
  session that names none, with code review in it) and `louis-agent/voice-orchestrator` (the microservice orchestrator's
  workflow).
- A **language** is `{product}/voice-{xx}.md`, a two-letter code, with only the voice: `puls/voice-af`, `puls/voice-en`.
  It loads its product's shared `voice.md` (Puls's routing) after it, so the routing is written once; that shared file
  is not a mode of its own. Puls's front end offers the languages for which `GET /personalities` has a
  `puls/voice-{code}`, and opens new chats with it.
- Each product's routing holds only its own rows.

`AGENT_FUNCTION` goes: with the `skills` toolset, a session loads every function skill the agent wrote. What is embedded
is built in: the names the agent may not take are the embedded files' names, not a hand-kept list.

## Consequences

- A session's prompt is the principles (ADR 0020), its mode (and, for a language, its product's routing) and its
  toolsets' guides; nothing pairs files, and the folder shows each product's modes.
- Each product's fixed prefix holds only its own routing: the coding agent's is 801 tokens smaller (29,464 → 28,663,
  from the start-up log), even with the principles added.
- A new mode is one file; a new Puls language is one `puls/voice-{xx}.md` with the same sections as the others (a test
  holds them), and the menu offers it with no other change.
- Hosts that name no personality (CLI, Rider, MCP, the code-review app) keep the coding agent's mode through the
  default, unchanged.
- An `.env` that still sets `AGENT_FUNCTION` is ignored; `PERSONALITIES_ENABLED` names modes as `puls/voice-af`.
- Specialists (level 1) and minions (level 2) are unchanged: no routing, and their roles as in ADR 0017.

## Links

[CL9 plan](../plans/CL9-modes-per-product.md) (*Approved during the build*) · [ADR 0017](0017-skills-by-level.md) ·
[ADR 0018](0018-built-in-prompts-embedded.md) · [ADR 0011](0011-prompts-are-content.md)
