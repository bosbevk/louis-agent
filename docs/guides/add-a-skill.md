# Add a skill

> **How-to.** Give the agent a function skill: a markdown file it runs with `ExecuteSkill`, written by the agent or
> dropped in by you, and approved before every session sees it. **Elsewhere:** why what the agent writes waits for a
> person in [ADR 0033](../adr/0033-what-the-agent-writes-has-a-scope.md); a toolset's guide in
> [Change the engine](change-the-engine.md).

A function skill is `Skills/agent/functions/{name}.md`, on disk: the agent writes one with `CreateSkillFile`, or you
drop one in. What the agent writes waits in `Skills/agent/functions/pending/` (git-ignored), active only in the engine
that wrote it, until you type `/approve {name}` in the chat, which moves it up; `/reject {name}` deletes it and `/tools`
lists what is pending (FX20, as agent-built tools do). Every approved one loads with the **skills** toolset, no setting
needed (CL9 retired `AGENT_FUNCTION`); a session without it (Puls's chats: `specialists` and `web`) gets its mode and
its toolsets' guides, no skill file. A name that is a built-in prompt (a toolset's guide) is refused: what is embedded
is built in (`SkillFiles.IsBuiltIn`).

Procedures use `## Skill: Name` with an `Execution:` fence (bash, powershell or python); arguments arrive as
`SKILL_ARG_*` environment variables, so quote them and never build commands from them
([ADR 0004](../adr/0004-arguments-as-data.md)):

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

On Claude Haiku 5.5, `/approve`, `/reject` and a skill reload change the tool list under a running chat and break it
with a 400 ([FX4](../fixes/FX04-append-only-history.md)): start a new chat afterwards.

---
[Guides](README.md) · [Docs index](../README.md)
