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
today becomes `SkillTool`'s. `SettingChoice` parses the language. `PromptHeaders` (ADR 0063) reads every
`SKILL.md`'s YAML front matter, as it reads an identity's skills, and `ReadSkill` (ADR 0114) loads `write-a-skill` on
demand. No new package.

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

**2026-11-26, a skill's parameters are a JSON Schema file beside its `SKILL.md`.** Louis asked which way is correct,
"research this what is the correct way?". No standard puts typed parameters in a `SKILL.md`: Claude Code reaches a
skill through one tool that takes its name ([a deep dive](https://leehanchung.github.io/blogs/2025/10/26/claude-skills-deep-dive/)),
Agent Skills has a script describe itself through `--help` ([Using scripts](https://agentskills.io/skill-creation/using-scripts)),
Microsoft's Agent Framework takes the types from the script's code ([Microsoft Learn](https://learn.microsoft.com/en-us/agent-framework/agents/skills)),
and only one project's extension reads them from the front matter ([pi-typed-args](https://cdn.jsdelivr.net/npm/pi-typed-args@0.2.0/docs/skills.md)).
Every tool standard takes a function's inputs as a JSON Schema. So `SKILL.md` stays exactly the Agent Skills spec,
its `metadata` plain strings, and its inputs are `parameters.schema.json` in the same folder, handed to the model
unchanged and used by `UseSkill` to check arguments. His pick, over a `parameters` key of our own in the YAML and
over no typed parameters. Section 2's "its `metadata` adding the typed `parameters`" is replaced by this.

**2026-11-26, the script a skill runs is named in `metadata.script`.** Louis: "research what is the standard?". The
spec gives `scripts/` as the optional folder for code an agent can run ([agentskills.io](https://agentskills.io/specification)),
names a script by its path from the skill's folder, as `scripts/extract.py`, and leaves which languages to the
implementation; `metadata` is its place for a client's own properties. So `metadata.script: scripts/count.py` names
what `UseSkill` runs, a plain string, and the language comes from the extension (`.py`, `.ps1`, `.sh`), parsed once;
any other is refused with the list. Section 2's "the script's `language`" is replaced by this.

**2026-11-26, F97.1 and F97.2 are built as one child, F97.1.** Cut as planned, F97.1 would have changed the shape while
`CreateTool` and `CreateSkillFile` still wrote files nothing read, so `main` would ship a broken "make a tool" unless
a second reader bridged it, which ADR 0049 rules out. Louis picked one child over cutting F97.1 as the whole new shape
and over the bridge. So F97.1 delivers both rows of the children's table and both done-whens, in about twenty steps
under one plan and one pull request; F97.2 is gone, and the Claude child keeps its number, F97.3.

**2026-11-26, the arguments are checked by our own code, with no package.** .NET has no JSON Schema validator built in.
Of JsonSchema.Net (MIT, but its NuGet release carries a maintenance fee for revenue-earning users over US$10,000 a
year), Corvus.Text.Json.Validator (Apache-2.0) and Bebop.JsonSchema (MIT), Louis picked none: `UseSkill` checks that
every required property is there and that each value has the type its property names, which is what a skill's flat
parameters use. Section 10's "No new package" stands.

**2026-11-26, after comparing the plan with the research, three more picks.** Louis asked to "compare this plan with
research on how it is suppose to work". Claude Code reaches every skill through one `Skill` tool whose description
lists the skills' names and descriptions ([Mikhail Shilkov](https://mikhail.io/2025/10/claude-code-skills/)); a model
picks the wrong tool more often as tools grow, from 92.4% right at 15 tools to 78.2% at 640 ([arXiv 2607.15593](https://arxiv.org/pdf/2607.15593)),
and the coding agent starts with 100; Voyager keeps a skill only after a check confirms it worked
([Voyager](https://arxiv.org/abs/2305.16291)), and Anthropic's `skill-creator` tests a skill before it is done
([skills.sh](https://www.skills.sh/anthropics/skills/skill-creator)). So:

- **The door only.** An approved skill is not listed by its own name in any chat; `UseSkill` is the one way to call a
  skill, and its description, frozen with the chat, lists every approved skill's name and one-line description. This
  replaces pick 3's "from the next chat it is in the list under its own name", over keeping it and over a limit.
- **An example run on creation.** `CreateSkill` takes one example call, runs it, and keeps the draft only if it
  succeeds; the example is saved in the folder, and `/skills` and `/approve` show it with its answer.
- **`UseSkill(name)` with no arguments reads the skill**: its instructions, its parameters and its example; with
  arguments it runs it. A wrong call's answer gives the parameters too.

**2026-11-26, F97.3 is re-aimed at tool search, and ADR 0033's words are settled.** With no skill called by its own
name, F97.3 now brings Anthropic's tool search (`defer_loading`) to the agent's built-in tools, a hundred of them on the
coding agent, since the research says selection improves when tools load on demand; a paid proof on Haiku 5.5 comes
first, as before. Louis's pick, over dropping it and over parking it in the TODO. And he approved the amendment of
ADR 0033 in these words, written in F97.1's step 19: "What the agent makes is a skill: a `skills/{name}/` folder with a
`SKILL.md` in the Agent Skills shape, a `parameters.schema.json` and its script, which reads its arguments as JSON on
stdin. `CreateSkill` keeps a draft only once its example call has run; a draft waits in `skills/drafts/` and only the
chat that made it may use it; `/approve` moves it up for every chat, `/reject` deletes it. A chat reaches every skill
through `UseSkill`, always in its tool list, whose description lists the approved skills; with a name alone it reads a
skill. The chat is told of an approval or rejection on its next message."

**2026-11-26, a toolset's guide is a built-in skill.** Louis, of core's flat `toolsets/agents.md` and
`toolsets/self-extension.md`: "this should work the same, why is this hanging here?", then "these are just built in
skills", and "research against the standards". In Agent Skills a skill is always a folder with a `SKILL.md`, and when it
loads is decided by whoever uses it: a Claude Code subagent's `skills:` field preloads the skills it names at startup
([Claude Code](https://code.claude.com/docs/en/skills.md), [issue #32910](https://github.com/anthropics/claude-code/issues/32910)),
as an identity's skill says `metadata.load` here (ADR 0114). So core and every pack keep their built-in skills in
`skills/{name}/SKILL.md`, the shape of every other skill, and a toolset names the skill it always loads. The `skills`
toolset's guide, misnamed `self-extension`, is named for its toolset, and the agent's `write-a-skill` is one more of
core's built-in skills. His pick, over `toolsets/{toolset}/SKILL.md` and over only fixing the name. The fourteen
guides move in F97.1; the text the model reads doesn't change.
