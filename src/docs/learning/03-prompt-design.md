# 3 · Prompt design: a prompt's path is its address

> **Skill:** #3 in the [goal](../GOAL.md)'s map, with #18 (a tree held by tests that read the disk) · **Stage:**
> CL68.1, *`prompts/` goes, `agent/` is `agents/`*, CL68's first child, five steps on one branch (2026-11-22),
> [ADR 0063](../adr/0063-a-prompt-says-what-it-is.md) amended · **Written by** Claude, for Louis to read
> (2026-11-22) · **Next:** CL68.2, a specialist as a subagent inside its agent's folder; CL68.3, a minion as a
> lookup agent

What this answers: why the folder a prompt sits in matters to a prompt's design, what CL68.1 changed in every
project that holds prompts, what came up on the way that the plan didn't see, and what it taught.

![Every project's prompts at its root: agents/ with each agent's voice and its skills as folders, toolsets/ and shared/, read through one catalogue](diagrams/03-prompt-design.svg)

## Why the tree is part of the design

A prompt here is a file the engine loads, and the engine finds it by its path. A voice is `agents/{name}/voice.md`,
a toolset's guide `toolsets/{name}.md`, the principles `shared/principles.md`. So the tree isn't filing. It is the
address the engine reads, and when the tree in Rider and the address in the engine differ, every change means
translating between them.

Before CL68.1 they differed in two ways. Every project wrapped its prompts in a `prompts/` folder the engine never
saw, and the showroom named each business twice, `prompts/wheatfield-kitchen/agent/wheatfield-kitchen/voice.md`,
once to split its project and once as the address. Louis, looking at the guide's tree: "this prompt structure needs
to be called agents. prompts can go. and why does the showroom have this structure".

## What changed

Every project's prompt folders now sit at its root, and its project file names them, `agents\**\*.md` or
`toolsets\**\*.md`. The engine's provider reads from the root, so the path in the project is the path the engine
reads. `agent/` became `agents/`, a folder of agents. The showroom's three businesses share one provider over their
project, each voice at `agents/{business}/voice.md`, and the catalogue reads a provider it is given twice only once.
Then, seeing the guide's `skills/` folder, Louis asked for one more thing: "each skill in it's folder, and SKILL.md",
the shape Claude Code's skills have, while the voice "acts like claude.md so it stays in root". So a skill is now
`skills/{name}/SKILL.md`, named for its folder, which ADR 0063 now says.

Seventeen projects moved, the louis-agent demo's coding agent and triage team among them, since the demo stays in
this repository and every merge waits for green. Every file moved with `git mv`, so each keeps its history, and no
prompt's words changed but one line of a guide that names where the agent saves a skill.

## What the plan didn't see

The plan's choice for reading from the root rested on there being nothing but prompts in a project's embedded files,
which held once the recipes packs left with Puls. It held, except for one file nobody had thought of: read from its
root, the embedded provider also lists its own manifest. Every project then held the same path, and the catalogue,
which refuses a path two places hold, refused it. Louis picked the cure from three: a prompt is markdown, so the
source lists only `.md` files.

Three more came from the repository's own rules. On Windows `agent/` and the core tests' `Agent/` code folder are the
same folder, so the rename had to come before the wrapper went. The words `agents` and `skills` are also two core
toolsets' wire names, so a test that wrote either as a string was caught by the rule that keeps repeated names in
enums. And the files each step touched had to leave without anonymous message holes, so fourteen became records.

One gift came out of it. Two of the guide's skill tests had failed on this PC only for weeks. Moving them showed why:
they looked for `"\n## An example\n"` in a file Windows had checked out with `\r\n`. The guide's test now evens the
line breaks, as the framework's twin already did.

## Words with names

Updating the framework site for the new tree turned up one more thing. *Build your own* held its steps as a list of
string literals, and Louis, reading it: "put this in a dictionary factory and make it a rule", and then as a
framework function. So a page's words are now `PartWords<TPart, TValue>`, in `LouisAgent.Composition`: a dictionary
keyed by an enum of the page's parts, built by a factory that asks a switch for each part when the class loads and
refuses one with no words. It is the same idea as the prompts' tree, from the other side. A step that has a name can
be pointed at, and a gap fails at start-up instead of showing on the page. ADR 0132 holds it, and a page converts when
a step touches it.

## What it taught

A prompt's design includes where it lives. When the address the engine reads is the path a person sees, a new
agent, a new skill or a moved one is a folder you can read, not a mapping you have to remember. And a tree like this
is only held if the tests read the disk the same way the engine does: the rule that no project wraps its prompts and
the rule that every skill is a folder each read the project files, so the next project can't drift.

---
[Learning write-ups](README.md) · [Goal](../GOAL.md) · [ADR 0133](../adr/0133-subagents-live-inside-their-agent.md) · [ADR 0063](../adr/0063-a-prompt-says-what-it-is.md) · [Build an identity](../guides/build-an-identity.md)
