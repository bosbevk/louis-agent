# CL68 Agents and their subagents

A plan: how every project's `prompts/` folder becomes `agents/`, with each agent's subagents inside it. It's a
parent with three children, and each child gets its own plan, branch, pull request and done-when when it starts.
This file says what changes and why, draws the end state, and lists the children in order.

![CL68 in three children: the prompts/ wrapper goes, specialists become subagents inside their agent, and minions become lookup agents](diagrams/CL68-agents-and-their-subagents-overview.svg)

## Context

Louis, 2026-11-19, looking at the guide's tree (`prompts/agent/louis-agent-guide/skills/`) and the showroom's
(`prompts/koringland-attorneys/agent/koringland-attorneys/voice.md`): "this prompt structure needs to be called
agents. prompts can go. and why does the showroom have this structure".

**Why the showroom names each business twice.** Every identity's and every pack's prompts are merged into one
catalogue, and inside it the path is the address. The engine looks for a voice at `agent/{name}/voice.md`
(`RoleCatalog.AgentFolder`), and `AgentCatalog` stops at start-up when two providers hold the same path. The
three showroom businesses share one project. If each of them served the whole folder, all three would bring all
three voices, and the catalogue would refuse the repeats. So each one is rooted in its own subfolder
(`EmbeddedPrompts.Of(assembly, business)`), and under that root its voice still has to sit at its catalogue
address. That's how the business ends up named twice: once to split the project, and once as the address.

Then, while picking a layout (three options, Louis picked the one that groups everything under its agent), he
reshaped the words themselves:

- "With C specialists fall away, you can just call an agent by role?" Reading the engine showed that a
  specialist runs on the same loop as the agent you talk to. Only its prompt is smaller: no routing, no identity
  skills, no list of agents to call. So Louis: "So it is actually a SubAgent". That's also the word Claude Code
  uses for an agent with its own instructions file, its own tools and its own context, kept under `agents/`
  ([Claude Code subagents](https://code.claude.com/docs/en/sub-agents)).
- The shape he wrote: "agents/{name}/Subagents/{name}". On the card he picked a folder per subagent
  (`subagents/cook/voice.md`) over one file each, so a subagent has the same shape as an agent and can hold its own
  `skills/` later. The folder is lowercase like every other folder in the repo.
- "Minions --> lookup-agents sub-agents". A minion becomes a **lookup agent**: a subagent like the cook, in the
  same `subagents/` folder, marked `level: lookup` in its header. It still runs the way a minion does today: one
  task, one tool handed down, and a small model if one is set.

What was read before this plan: `EmbeddedPrompts`, `SkillFileSource`, `SkillFiles`, `RoleCatalog`, `RoleFiles`,
`RoleLevel`, `SkillLoader`, `AgentEngineFactory.Build`, `AgentAddresses`, `MinionAsk`, `AgentCatalog`'s
uniqueness check, `PromptHeader` (the header already carries `metadata.level`, ADR 0063), ADRs 0017, 0019, 0021,
0063, 0114 and 0117, and every project's csproj. Two things matter for the build. The recipes and shop-prices packs
embed files that aren't prompts (`recipe-sites.md`, the price JSON), so a provider at the project root has to show
only the prompt folders. And `LouisAgent.Site` links core's `prompts/shared/principles.md` by path, so it moves
too.

**The scope: the engine, not Puls.** Louis, when the plan was first shown: "no we are not fixing pulse we only fix
the engine to work like this". And on the card for what happens to Puls, he picked "Leave Puls broken until it
moves" over moving its files or having the engine read both layouts. So CL68 moves core, the packs, the framework's
own identities in `src/site/` (the guide, the framework Orbi, the showroom) and the core tests' made-up `testprod`
identity, which has an agent, specialists and minions and so carries the engine's tests for every level. The demos
in `demos/` (Puls, the coding agent, the triage team) are left as they are. Once CL68.1 is merged their prompts
no longer load, so their tests fail until each moves to its own repo or is moved on purpose later. CI runs the whole
solution's unit tests (`.github/workflows/ci.yml`), so from CL68.1 on CI is red on those demos' tests, and the Puls
deploy workflow, which waits for green CI and also ships the framework's site, doesn't run.

## Design

### 1. The tree, today and after

Today (the core tests' `testprod`, the guide, the showroom, a pack and core):

```
LouisAgent.Core.Tests/prompts/
  agent/testprod/voice.md, voice-af.md, voice-en.md, skills/
  specialists/testprod-specialist.md
  minions/testprod-minion.md, testprod-minion-lookup.md
LouisAgent.Identities.Guide/prompts/agent/louis-agent-guide/voice.md + skills/
LouisAgent.Identities.Showroom/prompts/koringland-attorneys/agent/koringland-attorneys/voice.md   (and two more)
LouisAgent.Packs.Web/prompts/toolsets/web.md
LouisAgent.Core/prompts/shared/principles.md, toolsets/agents.md, toolsets/self-extension.md
```

After CL68:

```
LouisAgent.Core.Tests/agents/testprod/
  voice.md, voice-af.md, voice-en.md, skills/
  lookup.md                                  (what every lookup agent loads first, was testprod-minion.md)
  subagents/specialist/voice.md              (level: subagent)
  subagents/lookup/voice.md                  (level: lookup)
LouisAgent.Identities.Guide/agents/louis-agent-guide/voice.md + skills/
LouisAgent.Identities.Showroom/agents/koringland-attorneys/voice.md   (and two more, each name once)
LouisAgent.Packs.Web/toolsets/web.md
LouisAgent.Core/shared/principles.md, toolsets/agents.md, toolsets/self-extension.md
```

The framework Orbi moves like the guide. Puls, the coding agent and the triage team stay in `prompts/` as they
are. The pictures below use Puls and its cook as the example of the shape, since they are the agents the address
rules were written for, but Puls's own files don't move.

### 2. What changes, and why this shape

**The wrapper goes (CL68.1).** `prompts/` holds nothing but the level folders, so each project embeds its prompt
folders (`agents/`, `toolsets/`, `shared/`) from its own root, and the path in the project is exactly the path the
engine reads. `agent/` becomes `agents/`. Since the recipes and shop-prices packs embed other files too,
`EmbeddedPrompts.Of` hands back a provider that shows only the prompt folders. The showroom's three identities
share one provider, and the catalogue counts a provider it already holds once, so each business's name appears once.

**A subagent lives in its agent's folder (CL68.2).** `specialists/` goes. A subagent is
`agents/{identity}/subagents/{name}/voice.md`, so in this shape a cook would be
`agents/puls/subagents/cook/voice.md`. The address `puls.cook` (ADR 0117) and the folder say the same thing, and
who owns the cook is visible in the tree. The engine reads a role's level from its header instead of from the
folder it sits in, and `specialist` becomes `subagent` (`RoleLevel.Subagent`, `level: subagent`). ADR 0017 chose
a folder per level so a role couldn't open at the wrong level. Today the header is read and checked for every
prompt anyway, so a test holds the same rule: a subagent's `voice.md` must say `subagent` or `lookup`, an agent's
must say `agent`. This is *package by feature*, everything one agent has in one place, where ADR 0017 was
*package by layer*.

**A minion becomes a lookup agent (CL68.3).** `minions/` goes, and its files join `subagents/` marked
`level: lookup`. The shared base (`{identity}-minion.md`) becomes `agents/{identity}/lookup.md`. The name changes through the
code (`MinionAsk` to `LookupAsk`, `RoleLevel.Minion` to `RoleLevel.Lookup`, `MINION_LLMS` to `LOOKUP_LLMS`, the
minion message texts) and in ADR 0021. Nothing is kept under the old name (ADR 0049).

### 3. The pictures

The interface design: the address, the role catalogue and the header, with what each child changes.

![An address names an agent or its subagent, the role catalogue turns it into a folder, and the level comes from the header](diagrams/CL68-agents-and-their-subagents-interface-design.svg)

The architecture: every project's prompts lose the wrapper, and the engine reads them through one catalogue.

![Identities hold agents and their subagents, packs hold toolsets, core holds the principles; the engine reads them all through EmbeddedPrompts and the catalogue](diagrams/CL68-agents-and-their-subagents-architecture.svg)

The main case: an agent asks its subagent, shown as Puls asking a cook.

![Puls calls AskAgent with puls.cook, the address becomes the cook's folder, its header says subagent, and its engine is built with its toolsets](diagrams/CL68-agents-and-their-subagents-communication-puls-asks-its-cook.svg)

What is in memory while the cook answers.

![The catalogue holds each provider once, and the cook's sub-session holds its own engine built from its voice and header](diagrams/CL68-agents-and-their-subagents-objects-in-memory.svg)

### 4. The namespace map

| Type or file | Folder | Namespace | Child |
|---|---|---|---|
| `EmbeddedPrompts` (the prompt folders from the root; the subfolder overload goes) | `src/engine/LouisAgent.Composition/Packs/` | `LouisAgent.Composition.Packs` | CL68.1 |
| A provider that shows only the prompt folders (its pattern put to Louis when CL68.1 starts) | `src/engine/LouisAgent.Composition/Packs/` | `LouisAgent.Composition.Packs` | CL68.1 |
| `AgentCatalog` (a shared provider counted once) | `src/engine/LouisAgent.Core/Agent/Catalog/` | `LouisAgent.Core.Agent.Catalog` | CL68.1 |
| `ShowroomIdentity` (one shared provider) | `src/site/LouisAgent.Identities.Showroom/` | `LouisAgent.Identities.Showroom` | CL68.1 |
| `RoleCatalog.AgentsFolder`, `SkillFiles` (`agents/`) | `src/engine/LouisAgent.Core/Skills/`, `Skills/Roles/` | `LouisAgent.Core.Skills`, `.Roles` | CL68.1 |
| `PromptFolderTests`, `SkillsLayoutTests` | `src/tests/engine/LouisAgent.Core.Tests/` | `LouisAgent.Core.Tests` | CL68.1 |
| `RoleLevel.Subagent`, `RoleCatalog.SubagentsFolder`, `RoleCatalog.LevelOf` from the header | `src/engine/LouisAgent.Core/Skills/` | `LouisAgent.Core.Skills` | CL68.2 |
| `AgentAddresses` (an address to its subagent's folder) | `src/engine/LouisAgent.Core/Delegation/Addresses/` | `LouisAgent.Core.Delegation.Addresses` | CL68.2 |
| A layout test: a subagent's level matches where it sits | `src/tests/engine/LouisAgent.Core.Tests/` | `LouisAgent.Core.Tests` | CL68.2 |
| `RoleLevel.Lookup`, `LookupAsk` (was `MinionAsk`), its messages | `src/engine/LouisAgent.Core/Delegation/` | `LouisAgent.Core.Delegation` | CL68.3 |
| `LOOKUP_LLMS` (was `MINION_LLMS`) in the options and `src/site/config/` | `src/engine/LouisAgent.Core/Config/`, `src/site/config/` | `LouisAgent.Core.Config` | CL68.3 |

### 5. What is reused

Nothing new is installed. The embedded provider stays `ManifestEmbeddedFileProvider`
([its constructors](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.fileproviders.manifestembeddedfileprovider.-ctor?view=net-8.0));
the header reader (`PromptHeaders`, ADR 0063) already gives every prompt its level; the address (`AgentAddress`,
ADR 0117) already reads `puls.cook`.

## The children

Each starts with its own plan, on its own branch off `main`, and ends with its pull request.

| Child | What it does | Done when |
|---|---|---|
| **CL68.1** `prompts/` goes, `agent/` is `agents/` | Each project embeds its prompt folders from its root; `agent/` becomes `agents/` everywhere it's named (the core constants, the identities, the agent-written skills folder `skills/agents/functions/`, the site's link to the principles); the showroom flat, one provider for its three businesses. About six steps | No `prompts/` folder is left in core, the packs or `src/site/`, `PromptFolderTests` holds that, the showroom names each business once, and every unit test outside `demos/` passes |
| **CL68.2** Subagents inside their agent | `specialists/` goes; each specialist is `agents/{identity}/subagents/{name}/voice.md`; the level comes from the header; `specialist` becomes `subagent` in code, headers and texts; ADR 0017 superseded by a new ADR (proposed to Louis first). About eight steps | `testprod.specialist` opens `agents/testprod/subagents/specialist/voice.md`, no `specialists/` folder is left outside `demos/`, a test refuses a level that doesn't match where a file sits, and every unit test outside `demos/` passes |
| **CL68.3** Minions are lookup agents | `minions/` goes; each minion is a subagent marked `level: lookup`; the base is `agents/{identity}/lookup.md`; minion becomes lookup through the code, the settings and ADR 0021; the write-up. About seven steps | No `minion` name is left in the engine, its settings or the framework's prompts, a lookup agent still runs with one tool handed down, and the write-up is in `src/docs/learning/` |

**Open choices, each put to Louis when its child starts:**

- CL68.1: how the provider shows only the prompt folders (a filter around the embedded provider, one provider per
  folder joined together, or the packs moving their other files out of the embedded set).
- CL68.2: a subagent's name inside the engine. The address is `{identity}.{name}` and the folder is `{name}`, but
  sessions, `ROLES_ENABLED` and the stored runs know a role as `{identity}-{name}`. Either the role is called by its
  address everywhere, or it keeps the dashed name with the folder named by the short one, or the folder carries the
  dashed name.

## Verification

Each step's own tests while it's built, and before each child's pull request, after `main` is merged in, the full
unit suite, where only the tests of the demos left behind may fail, each named in the pull request. By hand, for free: start louis-agent.dev's host, talk to the guide, try the three showroom businesses and
let the guide ask one of them, and read their prompts on the admin's *Prompts* page. No paid run is needed: no prompt's words change,
only where they sit. The golden files are regenerated where a path shows in what the model reads, and the diff is
checked to be paths only.

## Concepts

- **CL68.1, prompts as content (skill 3, prompt design):** prompts are files the engine loads, and the path is
  their address. A tree that reads the way the engine reads it is easier to change safely.
- **CL68.2, subagents (skill 10, multi-agent orchestration):** a specialist and the main agent are the same
  thing with different context. What sets a subagent apart is what it's given (its instructions, its tools, a
  fresh context), not a different kind of engine. That's the same idea behind Claude Code's subagents.
- **CL68.3, cheap workers (skills 1 and 10):** a lookup agent is a subagent cut down to one task and one tool so
  a small model can do it. That's the cost side of delegation.

## Approved during the build

- **2026-11-22, the louis-agent demo moves too** (Louis's pick A of three, when CL68 started again after F170): the
  coding agent and the triage team move with the engine in each child, so every test stays green and every pull
  request merges on green. The plan's "the demos stay as they are" was for Puls, who has since left the repository,
  and the louis-agent demo stays. Chosen over leaving them broken (every merge after CL68.1 on red) and over the
  engine reading both layouts (ADR 0049).
- **2026-11-22, CL68.1's open choice:** each project embeds its prompt folders by name and the provider reads from
  the root, with no filter (Louis's pick A), since the files that weren't prompts left with Puls.
- **2026-11-22, CL68.1 done**, its plan folded. It also made each identity's skill a folder holding its `SKILL.md`
  (Louis: "each skill in it's folder, and SKILL.md"; the voice stays at the agent's root), with ADR 0063 amended so a
  skill's name is its folder's, and the source lists only markdown, since read from the root the embedded provider
  also lists its own manifest. The write-up is `learning/03-prompt-design.md`.
