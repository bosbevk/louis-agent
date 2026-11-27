# Add a skill

> **How-to.** Give the agent a skill: a folder with a `SKILL.md`, its parameters and one script, made by the agent in
> a chat or dropped in by you, and approved before every chat has it. **Elsewhere:** why what the agent makes waits
> for a person in [ADR 0033](../adr/0033-what-the-agent-writes-has-a-scope.md); a toolset's own built-in skill in
> [Change the engine](change-the-engine.md).

![A skill: a folder the agent makes or you drop in, a draft until you approve it, then every chat's](diagrams/add-a-skill.svg)

A skill is something the agent can do that no built-in tool does: count the words in a text, convert a currency,
check a file's shape. It is a folder in the skills folder, `demos/louis-agent/skills/{name}/` (or wherever
`SKILLS_DIRECTORY` points), in the shape of the [Agent Skills](https://agentskills.io/specification) standard. The
agent reaches every skill through one tool, `UseSkill`: called with a name alone it reads the skill, and called with
the name and its arguments it runs it. This guide shows what goes in the folder, how the agent makes one, how you
approve it, and how you drop one in by hand.

## What goes in the folder

A skill's folder holds four things, and the agent's built-in standard, `write-a-skill`, holds it to each:

| File | What it is |
|---|---|
| `SKILL.md` | YAML front matter with the skill's `name`, its `description` and `metadata.script`, then a few plain lines saying what it's for, what it needs and what it gives back |
| `parameters.schema.json` | Its parameters, a JSON Schema object: each under `properties` with its type and description, and the ones a call must give under `required` |
| `scripts/…` | Its one script, a `.py`, `.ps1` or `.sh` file, at the path `metadata.script` names |
| `examples/example.json` | The call it was proved with, and its answer |

The name is a verb and what it acts on, in kebab case, at most 64 characters: `count-words`. It is the folder's name,
and the header says the same. The description says what the skill does and then when to use it, in the third person,
since it is what lists the skill to the model:

```
---
name: count-words
description: Counts the words in a text. Use it when the person asks how long a text is.
metadata:
  script: scripts/count.py
---
# Count words

Give it the text. It answers with the number of words, nothing else.
```

The script reads its arguments as one JSON object from stdin and prints its answer. It never builds a shell command
out of an argument, so a value can't become a command ([ADR 0004](../adr/0004-arguments-as-data.md)), and a call
whose arguments don't fit the parameters is refused before anything runs.

## How the agent makes one

![The agent makes a draft its own chat can use; you approve it for every chat or reject it](diagrams/add-a-skill-flow.svg)

In a chat with the `skills` toolset, the agent reads the standard with `UseSkill("write-a-skill")` and calls
`CreateSkill` with the skill's name, its `SKILL.md`, its parameters, its script and one example call. `CreateSkill`
runs the example, and keeps the skill only if it runs cleanly; otherwise nothing is kept and the answer says what to
fix. A kept skill is a draft, in `drafts/{name}/`, which git ignores. The chat that made it can use it at once with
`UseSkill`, and no other chat can.

## Approve or reject it

1. Type `/skills` in the chat to see the drafts waiting, each with the call it was proved with, and the skills
   already approved.
2. Type `/approve {name}` to move the draft up a level, so every chat has it, or `/reject {name}` to delete it. The
   model can't do either: a person widens what the agent made.
3. Carry on. The chat you typed it in is told on its next message, and every chat after lists the skill in
   `UseSkill`'s description.

## Drop one in yourself

Write the folder as above, straight into the skills folder, not into `drafts/`. Then ask the agent to run
`ReloadSkills`, which reads the folder again: `UseSkill` reaches the skill at once, and the next chat lists it.

## Which chats have skills

`UseSkill`, `CreateSkill` and `ReloadSkills` are the `skills` toolset, so only a chat that names it has them, such as
the coding agent's. A chat without it, such as a site visitor's, has none. An open chat keeps the tool list it started
with, because the model checks its earlier thinking against it; that is why a newly approved skill is listed from
your next chat, while `UseSkill` already reaches it in this one.

*Decided in:* F97.1, [ADR 0033](../adr/0033-what-the-agent-writes-has-a-scope.md), [ADR 0004](../adr/0004-arguments-as-data.md).

---
[Guides](README.md) · [Docs index](../README.md)
