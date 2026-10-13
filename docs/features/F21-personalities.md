# F21 · Personalities per session

> **Status:** built (2026-10-13), on `feature/F20-puls-look` · **Milestone:** — · **Depends on:** F5 (sessions with their own engine, sub-sessions)
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

- **Sub-sessions have no router** (approved during the build): a session at depth 1 gets no `default.md`. The engine key
  is toolsets, personality and router; an agents-only Puls chat is about 2,200 tokens of prefix, a researcher about 1,900.
- **A specialist's personality is its role**, one per kind (approved during the build): `puls-researcher`, `puls-reader`,
  `puls-historian`, `puls-analyst` and `puls-specialist`, mapped from toolsets in `agents-skills.md`. `puls.md` is Puls's
  own voice. All are first drafts for Louis to edit.
- **Checked before building:** an unknown personality is refused before the engine is cached (a .NET `Lazy` that throws
  keeps the exception).
- **Gaps found** (tools the roles need to do their jobs): planned as [F22 Role tools](F22-role-tools.md).

---
[Features](README.md) · [Plan](../plans/F21-personalities.md)
