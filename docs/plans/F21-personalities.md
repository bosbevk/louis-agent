# F21 · Personalities per session: plan

> Approved 2026-10-13 · Feature: [F21 Personalities](../features/F21-personalities.md) · Branch: `feature/F20-puls-look`
> (F20 paused after its step 5; F21 is core work, built before F20 continues)

## Context

Louis: a personality must be passed when a session is created, a different one for sub-agent sessions; Puls's must feel
like South Africa, in Afrikaans that isn't stiff. Today every session gets `Skills/personality.md`.

## Design

- **Files:** `Skills/personalities/{name}.md`, embedded like the other built-in skill files (a file on disk overrides).
  `personality.md` stays the default when no name is given. A name is lowercase letters, digits and dashes.
- **Core:** `AgentHost.LoadSkills(agent, toolsets, personality)`; `AgentEngineFactory` keys its engines by toolsets and
  personality; an unknown personality is an error the API turns into 400.
- **API:** `POST /sessions` takes `personality`; `GET /personalities` lists the available ones.
- **Sub-sessions:** `StartSubSession` gets an optional `personality` argument, passed to the peer's `POST /sessions`.
- **Puls:** `puls.md` (Puls's own voice) and `puls-specialist.md` (the specialists it delegates to); Puls opens its chats
  with `puls`, and its router tells it to give its specialists `puls-specialist`. Claude drafts both, Louis edits.

## Steps

One commit each, with its tests; the review stop after each.

| Step | What | Check |
|---|---|---|
| 0 | Feature doc and this plan | — |
| 1 | The personality in `LoadSkills`; the name check; embedded `personalities/` | unit tests |
| 2 | The factory key; `POST /sessions` with `personality`; `GET /personalities` | API tests |
| 3 | `StartSubSession(…, personality)` | unit tests; golden file regenerated |
| 4 | `puls.md` and `puls-specialist.md`; Puls opens with `puls` and delegates with `puls-specialist` | build; a session by hand (free) |
| 5 | Docs: *Adding a personality* (CLAUDE.md), ARCHITECTURE | — |

## Verification

`dotnet test` for core, API and orchestrator per step; opening sessions by hand costs nothing. Louis tries Puls's voice in
a real chat himself.

## Concepts

Prompt composition and context engineering (GOAL #5): the system prompt as parts chosen per session (personality,
router, toolset guides), each part a stable prefix for caching; persona design for a product's voice.

## Approved during the build

(None yet.)
