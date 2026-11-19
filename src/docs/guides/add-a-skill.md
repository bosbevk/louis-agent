# Add a skill

> **How-to.** Give the agent a function skill: a markdown file it runs with `ExecuteSkill`, written by the agent or
> dropped in by you, and approved before every session sees it. **Elsewhere:** why what the agent writes waits for a
> person in [ADR 0033](../adr/0033-what-the-agent-writes-has-a-scope.md); a toolset's guide in
> [Change the engine](change-the-engine.md).

A function skill is a markdown file on disk, at `demos/louis-agent/skills/agent/functions/{name}.md`. The agent writes
one with `CreateSkillFile`, or you drop one in yourself.

## Write the file

A function skill needs no header. The built-in prompts open with their front matter, but a function skill is read
whole. Phase 7 plans to turn these files into `SKILL.md` folders with the same header.

Each procedure in the file is a `## Skill: Name` section with an `Execution:` fence in bash, PowerShell or Python:

````markdown
## Skill: MySkill

- Description: What it does.
- Parameters:
  - `name` (string): What it is.
- Execution:

```bash
echo "Hello, ${SKILL_ARG_name}"
```
````

Its arguments arrive as `SKILL_ARG_*` environment variables. Quote them, and never build a command out of them
([ADR 0004](../adr/0004-arguments-as-data.md)).

*Decided in:* FX20, CL9, [ADR 0063](../adr/0063-a-prompt-says-what-it-is.md).

## Approve what the agent wrote

What the agent writes doesn't go live for everyone straight away. It waits in
`demos/louis-agent/skills/agent/functions/pending/`, which git ignores, and works only in the engine that wrote it.

1. Type `/tools` in the chat to list what's still waiting.
2. Type `/approve {name}` to move it up a folder, so every session gets it, or `/reject {name}` to delete it.
3. Start a new chat (see the next part).

Tools the agent builds wait and are approved the same way.

## Which sessions get it

Every approved skill loads with the `skills` toolset, and no setting picks which ones. A session without that
toolset, such as a Puls chat with only `agents` and `web`, gets its mode and its toolsets' guides, and no skill file.
A name that a built-in prompt already uses, such as a toolset's guide, is refused, because what's embedded is built
in (`SkillFiles.IsBuiltIn`).

## Start a new chat afterwards

On Claude Haiku 5.5, `/approve`, `/reject` and a skill reload change the tool list under a running chat, and that
breaks the chat with a 400 ([FX4](../fixes/FX04-append-only-history.md)). Start a new chat after any of them.

---
[Guides](README.md) · [Docs index](../README.md)
