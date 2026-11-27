# Skills: what the agent makes

> **Reference.** What this folder holds, and where the built-in prompts are instead. **Elsewhere:** why, in
> [ADR 0017](../../../src/docs/adr/0017-skills-by-level.md) and [ADR 0018](../../../src/docs/adr/0018-built-in-prompts-embedded.md).

This folder is the **skills folder** when `SKILLS_DIRECTORY` isn't set (the compose files set it to this folder,
mounted at `/skills`). It holds the skills the agent makes, kept on disk so they survive a rebuild. Each is a folder
named for the skill, in the shape of the Agent Skills standard:

| Path | What |
|---|---|
| `{name}/SKILL.md` | The skill: its name and description, how to use it, its parameters and its script, `metadata.script` naming the script's path, such as `scripts/count.py` |
| `{name}/parameters.schema.json` | Its parameters, a JSON Schema, the shape `UseSkill`'s arguments must fit |
| `{name}/scripts/` | Its one script, a `.py`, `.ps1` or `.sh` file that reads its arguments as one JSON object from stdin |
| `{name}/examples/example.json` | The call it was proved with, and its answer |
| `drafts/{name}/` | A skill the agent made in a chat with `CreateSkill`, waiting for `/approve` or `/reject`; only the chat that made it can use it until then |

A chat makes a skill with `CreateSkill`, which keeps it as a draft only once its example runs, and uses it at once
with `UseSkill`. Typing `/approve <name>` moves the draft's folder up a level, so every chat has it from then on;
`/reject <name>` deletes it. The approved skills are committed with the demo; the drafts are not, which the
repository's `.gitignore` holds.

**The built-in prompts are not here.** The principles, the modes, the subagents' voices and the toolsets' skills
live in [`src/engine/LouisAgent.Core/`](../../../src/engine/LouisAgent.Core)'s prompt folders and in each identity's,
and are read only from the assembly: change one there and rebuild.

`AgentHost` finds this folder by this file, so keep it.
