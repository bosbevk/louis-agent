# F97 A skill usable in the chat that made it

A plan: how a tool or a skill the agent makes becomes usable in the chat that made it, at once, without breaking
the chat, and how the agent's tools and skills become one kind of thing with one place on disk. It is a parent with
three children. This file holds what the three share: what was decided, how it works, and what each child delivers.
Each child gets its own plan, with its steps, when it starts.

![F97 at a glance: a skill the agent makes is used at once, built in three children](diagrams/F97-a-skill-usable-in-its-chat-overview.svg)

## Context

FX4 made a chat keep the tools it started with, so changing them can't break a thinking model's chat any more. On
2026-11-26 Louis walked FX4 on the coding agent at `:5082` and asked it to make a tool. The tool was made, but the
chat couldn't call it: it reaches only the next chat. His words: "this is fucking pointless, if a chat creates a tool
it should be able to fucking use it, not only in a new chat". Of the first options put to him he picked both a door
now and the tool's own name in the next chat, adding "skills must work the same, and our tools and skill folders in
the core needs to be standardised". Then: "we must think how must it work not what we have".

So the design below starts from what a person wants: I ask the agent for something it can't do yet; it builds itself
a way to do it and uses it straight away; if I keep it, every chat after has it. He said yes to that design, and
the choices were then put to him one at a time.

| The choice | Louis's pick |
|---|---|
| Are an agent-made tool and an agent-made skill one thing? | One kind: a skill, a `skills/{name}/` folder with a `SKILL.md` |
| How a skill's folder shows its state | By folder: approved in `skills/{name}/`, drafted in `skills/drafts/{name}/`, which git ignores |
| The door | `UseSkill(name, arguments)`, reaching this chat's drafts and every approved skill |
| How a script gets its arguments | JSON on stdin, which he asked to be researched first |
| How the chat learns of an `/approve` or `/reject` | A note in front of the person's next message |
| How the agent writes a skill | In our standard, YAML front matter, following a `write-a-skill` skill ("when the core creates a skill it must be in our standard, it must use the write skill skill. YAML") |
| Claude's deferred loading | F97's last child, with a paid proof first |

**What was looked up.** Anthropic's Agent Skills make a skill a folder with a `SKILL.md`, whose frontmatter needs
only a name and a description, beside an optional `scripts/` folder; an agent reads only the name and description at
the start and the rest when a task needs it ([Anthropic](https://www.anthropic.com/engineering/equipping-agents-for-the-real-world-with-agent-skills),
[agentskills.io](https://agentskills.io/specification)). This repo already gave Claude's own skills that shape in
CL68.1. The standard's [Using scripts](https://agentskills.io/skill-creation/using-scripts) lets a script take its
input "via command-line flags, environment variables, or stdin"; [Cloudflare's runtime](https://developers.cloudflare.com/agents/skills/execution/)
hands it JSON, and [CodeQL](https://codeql.github.com/codeql-query-help/java/java-exec-tainted-environment) warns
against untrusted values in a child's environment. No source compared stdin with the others directly; that stdin
keeps a value out of what decides which program runs is our own reasoning. MCP can announce a changed tool list, but
users report hosts that keep the list they began with ([Cursor's forum](https://forum.cursor.com/t/mcp-notifications-tools-list-changed-not-acted-on-mid-session/161459),
[Claude Code #38324](https://github.com/anthropics/claude-code/issues/38324)), which is the fixed list FX4 gives a
chat. Anthropic's [tool search](https://platform.claude.com/docs/en/agents-and-tools/tool-use/tool-reference) leaves
a deferred tool out of the cache key, so loading one mid-chat keeps the cache; whether it also passes the thinking
check, and whether a tool defined after the start can be deferred, the docs don't say, so F97.3 proves it first.

## Design

### 1. What a person gets

A person asks for something the agent can't do. The agent makes a skill, says so, and uses it in the same answer. The
skill is a draft: only this chat can use it. The person types `/approve word-count`, and on their next message the
model is told it was approved. Every chat from then on lists `word-count` among its tools by its own name. If they
type `/reject word-count` instead, the draft is gone and a call to it is refused. At no point does the chat break.

### 2. One kind of thing: a skill

Today the agent can make two things that are one idea built twice. A *tool* is a `.tool.md` with typed parameters and
a script, kept in `skills/tools/`, read by `ScriptToolDefinition` and run as a `ScriptTool`. A *skill file* is a `.md`
with a script that gets its arguments as environment variables, kept in `skills/agents/functions/`, read by
`MarkdownSkillLoader` and run through `ExecuteSkill`. They have two names, two shapes of file, two folders, two
loaders, two runners and two ways of being called, and the model has to choose between them.

After F97 there is one: a skill, in our own standard. Core already has it for an identity's skills:
`agents/{identity}/skills/{name}/SKILL.md`, a folder named for the skill, its `SKILL.md` opening with YAML front
matter, `name` and `description` and our `metadata`, read once by `PromptHeaders` and refused with a reason when it is
wrong (ADR 0063, ADR 0114). It is also Agent Skills' shape. A skill the agent makes is the same file read by the same
reader, its `metadata` adding the typed `parameters` the model must pass and the script's `language`; its script lives
in `scripts/`. A skill someone writes by hand is the same thing, dropped in the folder.

The agent writes a skill the way our own skills are written. Louis, 2026-11-26: "when the core creates a skill it
must be in our standard, it must use the write skill skill. YAML". So core ships an on-demand skill for the agent,
`write-a-skill`, holding the standard our `write-a-skill` holds for Claude Code: the name a verb and what it acts on,
in kebab case; the description in the third person, what it does then when to use it, under 1,024 characters, with no
colon and space to break the YAML; the body's shape. `CreateSkill`'s description tells the model to read it first,
and `CreateSkill` refuses a `SKILL.md` whose header `PromptHeaders` won't read, saying what to change. The skill is
the procedure, and the reader is the check that holds it (ADR 0080, a tool where it makes a prompt more accurate).

### 3. Where a skill lives, and what state it is in

The folder says the state, and nothing inside a file does. An approved skill is in `skills/{name}/`, and every chat
has it. A draft is in `skills/drafts/{name}/`, which git ignores as `pending/` is ignored today, and only the chat
that made it can use it. `/approve` moves the folder up a level; `/reject` deletes it. A skill dropped in by hand is
approved by where it was put. Every path is named once, on `SkillFiles` in core, so no class builds a path of its own.

### 4. Reaching a skill: the door, then its own name

A chat's tool list is frozen from its first turn (FX4), and stays so. In it, always, is one door:
`UseSkill(name, arguments)`. It runs any skill the chat may use, its own drafts and every approved skill, and checks
the arguments against the skill's parameters before anything runs, so a wrong call is told what is missing. When
`CreateSkill` drafts a skill, its reply gives the name and the parameters, so the model calls the door at once. From
the next chat, an approved skill is also in the list under its own name, with its parameters as a schema, as an
approved tool is today; the door's description says to call a skill by its name where it is listed.

![F97's interface design: one Skill, one library and runner, CreateSkill and UseSkill over them](diagrams/F97-a-skill-usable-in-its-chat-interface-design.svg)

### 5. Arguments reach the script as JSON on stdin

The runner hands the script one JSON object on stdin and nothing in its environment or its command line, as an
agent-built tool does today (ADR 0004, arguments as data). Typed values, lists and nested objects pass whole. A bash
script reads it with `jq` or a few lines; the guide for writing skills shows how in each language.

### 6. The chat hears about an approval

`/approve` and `/reject` are answered by the host, not the model, so today the model never learns of them: in the
walk-through it told Louis to approve a tool he had just approved. After F97 the host keeps what each did and puts a
short note in front of the person's next message, so the model reads it as part of that message. It is appended,
never an edit of what was sent (ADR 0008), so the cache and the thinking stay valid.

![F97's communication: a skill made, used through the door, approved, and listed by name in the next chat](diagrams/F97-a-skill-usable-in-its-chat-communication.svg)

### 7. Where it runs, and what a chat holds

![F97's architecture: one skills folder with its drafts apart, one library in core, the door and the commands over it](diagrams/F97-a-skill-usable-in-its-chat-architecture.svg)

![What one chat holds: its frozen tools with the door, its own drafts, and a note waiting for the next message](diagrams/F97-a-skill-usable-in-its-chat-objects.svg)

### 8. The folders, today and after

```
skills/                              skills/
├── README.md                        ├── README.md
├── tools/                           ├── word-count/
│   ├── WordCount.tool.md            │   ├── SKILL.md
│   └── pending/                     │   └── scripts/count.py
│       └── CharCount.tool.md        └── drafts/                (git-ignored)
└── agents/functions/                    └── char-count/
    ├── {name}.md                            ├── SKILL.md
    └── pending/{name}.md                    └── scripts/count.py
```

Nothing reads the old shapes once F97.1 is in (ADR 0049): the files that exist are moved by hand in that child.

### 9. The namespace map

The children's own plans list every type with its step. In outline:

| Type | Folder and namespace | Child |
|---|---|---|
| `Skill`, `SkillParameter`, `SkillState` | `Skills/` in `LouisAgent.Core.Skills` | F97.1 |
| `SkillLibrary`, `SkillRunner`, `SkillTool` | `Skills/Library/`, `Skills/Running/` | F97.1 |
| `SkillFiles` (every path) | `Skills/` | F97.1, changed |
| `ScriptToolDefinition`, `ScriptToolParameter`, `ScriptTool`, `MarkdownSkillLoader` | removed | F97.1 |
| `ISkillProvider`, `MarkdownSkillProvider`, `CompositeSkillProvider` | changed to hand out `Skill` | F97.1 |
| `CreateSkill`, `UseSkill` (engine tools) | `Agent/EngineTools/` | F97.2 |
| `write-a-skill`, the agent's on-demand skill for writing one | core's embedded prompts | F97.2 |
| `AgentBuiltTools`, `SkillTools` | merged into one class of the skill tools | F97.2 |
| `UserCommands` (`/skills`, `/approve`, `/reject`, the note) | `Agent/Commands/` | F97.2 |
| An Anthropic strategy for a new skill by name | `LouisAgent.Core.Anthropic` | F97.3, if the proof holds |

### 10. What is reused

FX4's frozen tools and its `FrozenTool` refusal carry the door and a rejected skill. The script runners already in
core (`PythonRunner` and the PowerShell and bash ones) run the scripts. The typed-parameter schema `ScriptTool` builds
today becomes `SkillTool`'s. `SettingChoice` parses the language. `PromptHeaders` (ADR 0063) reads every `SKILL.md`'s YAML
front matter, as it reads an identity's skills, and `ReadSkill` (ADR 0114) loads `write-a-skill` on demand. No new
package.

### 11. The rules and tests that reach this work

About 50 files name `CreateTool`, 36 `ExecuteSkill` and 24 `CreateSkillFile`: the coding agent's
`toolsets/self-extension.md` prompt, the golden snapshots of what the model reads (regenerated in F97.2), ADRs 0028
and 0033, the guides and about 30 tests. `FolderSizeTests` caps a folder at six files, so `Skills/` gets subfolders
for the library and the runner. The packages change their public API (`ISkillProvider` hands out `Skill`), so Puls
follows on its next package update.

## The three children

| Child | What it delivers | Done when |
|---|---|---|
| **F97.1 One skill shape** | The `SKILL.md` folder in our YAML standard with typed parameters, read by `PromptHeaders`, one runner passing JSON on stdin, one library over `skills/*/` that skips `drafts/`, every path on `SkillFiles`, approved skills listed by their own name; the old types and the two old folders gone, the files that exist moved | An approved skill in the new shape is listed by its name in a new chat and runs with JSON on stdin; nothing in core names `.tool.md` or `agents/functions` |
| **F97.2 Draft, door, approve** | `CreateSkill` (replacing `CreateTool` and `CreateSkillFile`), following the agent's `write-a-skill` and refusing a header that won't read, drafts in `skills/drafts/`, `UseSkill` (replacing `ExecuteSkill`), `/skills`, `/approve` and `/reject` on folders, the note on the next message, the prompts and goldens | On Haiku 5.5 at `:5082`: a chat makes a skill and uses it in the same answer; `/approve` then a message, and the model knows; a new chat calls it by its name; `/reject` refuses it; no 400 throughout |
| **F97.3 Claude calls it by name** | A paid proof on Haiku 5.5; if a deferred tool added mid-chat passes the thinking check, an Anthropic strategy that adds a new skill as a deferred tool so Claude calls it by name in its own chat | The proof's result recorded; if it held, the walk-through again with the skill called by name in its own chat |

## Verification

Each child's tests run as its steps go, and the full suite before each pull request. F97.2 ends with the walk-through
above on the coding agent at `:5082`, on Haiku 5.5, which costs a few cents and is run on Louis's word, as are F97.3's
proof and its walk-through. **Done when** the three children's done-whens hold, ADR 0033 says the rule, and the
guides `add-a-skill.md` and the core design doc tell it.

## Concepts

F97.1 is *20 Extensibility*: one shape for what extends the agent, read from a published standard. F97.2 is *4 Tool
design* and *9 Sessions and state*: a fixed door that keeps a session's tools stable while what it can do grows, and
telling the model what changed outside its view. F97.3 is *13 Cost and token economics* and *2 Reasoning and extended
thinking*: what a provider lets you add without paying for the cache or breaking the thinking again.

## The decision is an ADR

Louis approved amending ADR 0033 on 2026-11-26 with this rule: what the agent makes is a skill, a `skills/{name}/`
folder with a `SKILL.md` and its script reading JSON on stdin; a draft waits in `skills/drafts/` and only its chat may
use it; `/approve` moves it up, `/reject` deletes it; a chat calls any skill it may use through `UseSkill`, and an
approved one by its own name from the next chat; the chat is told of an approval or a rejection on its next message.
The amendment also takes out the line FX4 made untrue. It is written in F97.2, when the rule is whole.

## Approved during the build
