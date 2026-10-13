# F21 · Personalities per session

> **Status:** building (2026-10-13) · **Milestone:** — · **Depends on:** F5 (sessions with their own engine, sub-sessions)
>
> **In the TODO:** **F21 Personalities** under *Puls* ([TODO](../../TODO.md)) · **Plan:** [plans/F21](../plans/F21-personalities.md)

Every session gets the same personality today: `Skills/personality.md` ("Louis's Minion"), whatever the host. Puls, a
general assistant for South Africans, speaks in that voice too, and so do the specialists it hands work to. F5 let a
session choose its toolsets; F21 lets it choose its personality, the same way.

## User stories

**F21-S1 — A voice per app.** As *Puls*, I want my chats to open with my own personality, so that Puls sounds like
someone from South Africa talking everyday Afrikaans, not stiff textbook Afrikaans, and not Louis's coding minion.
- *Given* `POST /sessions` with `"personality": "puls"` *then* the session's system prompt starts with
  `Skills/personalities/puls.md` instead of `personality.md`.
- *Given* no personality *then* `personality.md`, as today (CLI, Rider, orchestrator unchanged).
- *Given* an unknown name, or one that isn't lowercase letters, digits and dashes *then* 400 (a name never reaches outside
  the folder).

**F21-S2 — A voice for the specialists.** As *Puls*, I want to give the specialists I delegate to their own short,
neutral personality (`puls-specialist`), since their answers come back to me, not to the user.
- *Given* `StartSubSession(…, personality: "puls-specialist")` *then* the sub-session opens with it.

## Design notes

A session keeps its personality for its whole life (its prefix stays cacheable, as with toolsets). The engine factory
caches one engine per toolsets and personality. `GET /personalities` lists them, as `GET /toolsets` does.

## What the build changed

(Filled in as the steps land.)

---
[Features](README.md) · [Plan](../plans/F21-personalities.md)
