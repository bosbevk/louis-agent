# 0019. A level-0 session is a mode: one file, in a folder per product

**Status:** Accepted · **Date:** 2026-10-17 · **Recorded:** 2026-10-17

## Context

After CL8 (ADR 0017, 0018), level 0 had three files doing three jobs: `personality.md`, the coding agent's voice for
every session that names none; `puls.md`, Puls's Afrikaans voice; and `default.md`, one router shared by both
products. The shared router made each product pay for the other's rows in every request's fixed prefix: Puls for git,
dotnet, Paymo and DevOps, the coding agent for the cook and Puls's Afrikaans briefing labels. Puls's language was fixed
in two places (the voice it sends, a "coming soon" language menu) with nothing tying them together. `AGENT_FUNCTION`
selected a placeholder skill file, and the code-review app and the microservice orchestrator were modes of their own
with no file that said so.

Considered: a voice and a router as two files paired in a `voices.json` (drawn first: a session still loads two files,
and a rule file has to keep them paired); one router with a section per product, which keeps both products paying for
both; a `language` field on a session beside its personality, two settings that must agree.

## Decision

We treat a level-0 session as a **mode**, and a mode is **one file**: its voice, then its routing, in
`Prompts/agent/{product}/{mode}.md` (`puls/voice-af`, `puls/voice-en`, `louis-agent/voice`, which
also does code review, `louis-agent/voice-orchestrator`). A session names `{product}/{mode}`; one that names
none gets `louis-agent/voice`. A product's routing appears in each of its files and a test keeps it identical. Puls's
language is its mode: the front end offers the languages for which `GET /personalities` has a `puls/voice-{code}`, and
opens new chats with it. `AGENT_FUNCTION` goes: with the `skills` toolset, a session loads every function skill the
agent wrote. What is embedded is built in: the names the agent may not take are the embedded files' names, not a
hand-kept list.

## Consequences

- A session's prompt is one mode file plus its toolsets' guides; nothing pairs files, and the folder shows each
  product's modes.
- Each product's fixed prefix holds only its own routing, so it costs less per request; the saving is measured from the
  start-up log, free, before any paid run.
- A product's routing is written in several files; the test fails when they differ, so a change is made in all of them.
- A new mode, or a new Puls language, is one file; the menu offers a language with no other change.
- Hosts that name no personality (CLI, Rider, MCP) keep the coding agent's mode through the default, unchanged.
- An `.env` that still sets `AGENT_FUNCTION` is ignored; `PERSONALITIES_ENABLED` names modes as `puls/voice-af`.
- Specialists (level 1) and minions (level 2) are unchanged: no routing, and their roles as in ADR 0017.

## Links

[CL9 plan](../plans/CL9-modes-per-product.md) (*Approved during the build*) · [ADR 0017](0017-skills-by-level.md) ·
[ADR 0018](0018-built-in-prompts-embedded.md) · [ADR 0011](0011-prompts-are-content.md)
