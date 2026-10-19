# FX20 · A skill file the agent writes is loaded at once, for every later session, with no person in between

> **Status:** open · **Area:** core, skill authoring (`Agent/Tools/SkillTools`, `AgentEngine`'s `/approve` and
> `/reject`) · **Found:** 2026-10-18, reading the code for [the platform plan](../plans/platform.md) (at `main`
> 7894953) · **Planned:** the plan's Phase 1, one step, so no plan file of its own

## Symptom (found in the code; the documented design, not a crash)

`CreateSkillFile` saves `Skills/agent/functions/{name}.md` and loads it at once, for every later session
(`louis-agent.core/Agent/Tools/SkillTools.cs:149-168,201-205`). A skill's `## Skill:` sections carry an `Execution:`
fence that `ExecuteSkill` runs as bash, PowerShell or Python (`SkillTools.cs:140`), and `ExecuteSkill` is registered in
every engine (`AgentEngine.cs:141`). So text the model wrote once, perhaps steered by a page it read, becomes code every
later session can run.

## Cause

It is the documented design: the tool's description (`SkillTools.cs:149-168`), `docs/CLAUDE.md`'s *Adding a skill*,
`docs/architecture/06-runtime-view.md:19-20` and `Prompts/toolsets/python.md:37`. Agent-built tools already wait for a
person: they wait in a `pending` folder for `/approve`, and that folder is git-ignored
(`louis-agent.core/Agent/Tools/AgentBuiltTools.cs:12-13,30-32,142-154`; `.gitignore:50-51`). Skill files have no such
step, and `/approve` and `/reject` reach agent-built tools only (`AgentEngine.cs:303-317`). This fix changes
documented behaviour on purpose.

## Fix (to do)

Designed in [the platform plan](../plans/platform.md), Phase 1 (*FX20 · Skill files wait for approval*), mirroring
agent-built tools: `CreateSkillFile` writes to `Skills/agent/functions/pending/{name}.md`, active in the engine that
wrote it; later sessions load only approved files; `/approve {name}` moves the file with no reload and `/reject {name}`
deletes it; `/tools` lists pending skill files beside pending tools; `.gitignore` gains the pending folder. Known
limitation 9 still holds until [FX4](FX04-append-only-history.md) (Phase 7).

## Tests (to do)

`tests/louis-agent.core.tests/Agent/SkillAuthoringTests.cs`, extended, written first and seen failing (the plan's
Phase 1, step 3); the golden files regenerated and their diff reviewed.

## Confirm

By the tests: a new engine on the same folder doesn't see a pending skill file until `/approve`.

## Lesson

Memory poisoning (OWASP Top 10 for Agentic Applications, ASI06): what the agent writes for later sessions is untrusted
until a person approves it. Skill map #15 (safety and security) and #16 (human in the loop).

---
[Fixes](README.md) · Previous: [FX19](FX19-git-arguments-as-data.md)
