# Skills: what the agent writes

> **Reference.** What this folder holds, and where the built-in prompts are instead. **Elsewhere:** why, in
> [ADR 0016](../docs/adr/0016-skills-by-level.md) and [ADR 0017](../docs/adr/0017-built-in-prompts-embedded.md).

This folder is the **skills folder** when `SKILLS_DIRECTORY` isn't set (the compose files set it to this folder,
mounted at `/skills`). It holds only what the agent writes, kept on disk so it survives a rebuild:

| Path | What | Written by |
|---|---|---|
| `agent/functions/{name}.md` | A function skill: procedures `ExecuteSkill` runs | `CreateSkillFile` |
| `tools/` | Agent-built tools, pending and approved | `CreateTool`, `/approve` |
| `HelloWorld.md` | A test fixture, never loaded by a host | — |

**The built-in prompts are not here.** The voices, the router, the specialists' roles, the toolset guides and the
built-in function skills live in [`louis-agent.core/Prompts/`](../louis-agent.core/Prompts), one folder per level, and are
read only from the assembly: change one there and rebuild. A copy of a built-in file here is ignored, with a warning.

`AgentHost` finds this folder by this file, so keep it.
