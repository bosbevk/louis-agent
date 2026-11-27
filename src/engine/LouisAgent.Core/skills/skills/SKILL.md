---
name: skills
description: The skills toolset's guide, loaded with it. How the agent uses its skills and makes a new one when no tool fits, the lightest option first.
---
# Skills

A skill is a folder the person can read and approve: its `SKILL.md`, the JSON Schema of its parameters, and one
script. `UseSkill` is the one way to every skill: its description lists the approved ones.

## Use a skill

- **Read it first** with `UseSkill(name)` alone: you get how to use it, its parameters and an example.
- **Run it** with `UseSkill(name, arguments)`, the arguments a JSON object that fits its parameters. A call that
  doesn't fit is answered with what's wrong and the parameters, and nothing runs.
- A name you may not use is answered with the ones you may.

## Decide before you make one

1. **A tool or a skill already does it?** Check your tools and the skills `UseSkill` lists first.
2. **A one-off need** (answer this question, transform this file once): run code with `PythonRun`,
   `PowerShellRun` or `BashRun`. Don't make anything.
3. **A procedure the person wants to keep**, or one you'll call again with other inputs: make a skill with
   `CreateSkill`.

## Make a skill

1. Read the standard first: `UseSkill("write-a-skill")`.
2. Call `CreateSkill` with its name, its `SKILL.md`, its parameters, its script and one example call. It is kept
   only if the example runs cleanly; otherwise nothing is kept and the answer says what to fix.
3. Use it at once in this chat with `UseSkill`. It is a draft: no other chat can use it yet.
4. Tell the person its name, what it does, and that they keep it for every chat by typing `/approve <name>`, or
   discard it with `/reject <name>`. You cannot approve a skill yourself.

`ReloadSkills` reads the skills folder again, after a skill was added or edited outside the agent.
