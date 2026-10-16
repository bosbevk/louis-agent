# 0017. Skill files follow the levels: a folder per level, and a role opens only at its own

**Status:** Accepted · **Date:** 2026-10-16 · **Recorded:** 2026-10-16

## Context

A session runs at a level: the agent the user talks to (level 0), a specialist it delegates to (level 1), and, with
F30, a minion a specialist hands one task to (level 2). Each level reads its own prompt files: a voice and the router at
level 0, a role at level 1, a function at level 2. `Skills/` was one flat folder of four kinds of file, with
`personalities/` mixing levels 0 and 1 (`puls.md` beside `puls-cook.md`), so nothing stopped a role being opened at
the wrong level, and what a session knows couldn't be read from the tree.

Considered: folders by kind only (`personalities/`, `guides/`, `functions/`), which still lets a role open at the
wrong level; and the level written in each file's front matter, which the loader must parse and a misplaced file would
still pass.

## Decision

We keep a prompt file in the folder of the level that reads it: `Skills/agent/` (level 0, with its function skills in
`agent/functions/`), `Skills/specialists/` (level 1), `Skills/minions/` (level 2). What every level shares, the toolset
guides, lives in `Skills/toolsets/`. A session's level is its depth, and it loads its personality only from that
level's folder; a name from another level is refused as unknown. Names are unique across the levels. A layout test
fails a file outside the known folders, a name in two level folders, or a toolset without its guide.

## Consequences

- What a session's prompt is made of follows from its level and toolsets, and the tree shows it.
- A wrong-level role is a 400 before any engine is built, not a confusing answer.
- The `-skills.md` suffix goes inside the folders, so the toolset guides and function skills are renamed once, and
  ADR 0011's `Skills/{toolset}-skills.md` becomes `Skills/toolsets/{toolset}.md` (the decision in 0011 stands).
- A file kept on disk at an old path stops overriding its embedded copy; the start-up log shows where each file came
  from.
- Every new level or kind of prompt file needs its folder, and the layout test, before its first file.

## Links

[CL8 plan](../plans/CL8-skills-by-level.md) · F30 (minions, level 2) ·
[ADR 0011](0011-prompts-are-content.md) · [Adding a personality](../CLAUDE.md#adding-a-personality)
