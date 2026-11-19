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
only the prompt folders. And `louis-agent.site` links core's `prompts/shared/principles.md` by path, so it moves
too.

## Design

### 1. The tree, today and after

Today (Puls, the guide, the showroom, a pack and core):

```
louis-agent.identity.puls/prompts/
  agent/puls/voice.md, voice-af.md, voice-en.md
  specialists/puls-cook.md, puls-analyst.md, ...
  minions/puls-minion.md, puls-minion-find-recipe.md, puls-minion-web-price-lookup.md
louis-agent.identity.guide/prompts/agent/louis-agent-guide/voice.md + skills/
louis-agent.identity.showroom/prompts/koringland-attorneys/agent/koringland-attorneys/voice.md   (and two more)
louis-agent.pack.web/prompts/toolsets/web.md
louis-agent.core/prompts/shared/principles.md, toolsets/agents.md, toolsets/self-extension.md
```

After CL68:

```
louis-agent.identity.puls/agents/puls/
  voice.md, voice-af.md, voice-en.md
  lookup.md                                  (what every lookup agent loads first, was puls-minion.md)
  subagents/cook/voice.md                    (level: subagent)
  subagents/analyst/voice.md ...
  subagents/find-recipe/voice.md             (level: lookup)
  subagents/web-price-lookup/voice.md        (level: lookup)
louis-agent.identity.guide/agents/louis-agent-guide/voice.md + skills/
louis-agent.identity.showroom/agents/koringland-attorneys/voice.md   (and two more, each name once)
louis-agent.pack.web/toolsets/web.md
louis-agent.core/shared/principles.md, toolsets/agents.md, toolsets/self-extension.md
```

The triage and coding identities and the core tests' made-up `testprod` identity move the same way.

### 2. What changes, and why this shape

**The wrapper goes (CL68.1).** `prompts/` holds nothing but the level folders, so each project embeds its prompt
folders (`agents/`, `toolsets/`, `shared/`) from its own root, and the path in the project is exactly the path the
engine reads. `agent/` becomes `agents/`. Since the recipes and shop-prices packs embed other files too,
`EmbeddedPrompts.Of` hands back a provider that shows only the prompt folders. The showroom's three identities
share one provider, and the catalogue counts a provider it already holds once, so each business's name appears once.

**A subagent lives in its agent's folder (CL68.2).** `specialists/` goes. The cook is
`agents/puls/subagents/cook/voice.md`. The address `puls.cook` (ADR 0117) and the folder now say the same thing,
and who owns the cook is visible in the tree. The engine reads a role's level from its header instead of from the
folder it sits in, and `specialist` becomes `subagent` (`RoleLevel.Subagent`, `level: subagent`). ADR 0017 chose
a folder per level so a role couldn't open at the wrong level. Today the header is read and checked for every
prompt anyway, so a test holds the same rule: a subagent's `voice.md` must say `subagent` or `lookup`, an agent's
must say `agent`. This is *package by feature*, everything one agent has in one place, where ADR 0017 was
*package by layer*.

**A minion becomes a lookup agent (CL68.3).** `minions/` goes, and its files join `subagents/` marked
`level: lookup`. The shared base (`puls-minion.md`) becomes `agents/puls/lookup.md`. The name changes through the
code (`MinionAsk` to `LookupAsk`, `RoleLevel.Minion` to `RoleLevel.Lookup`, `MINION_LLMS` to `LOOKUP_LLMS`, the
minion message texts) and in ADR 0021. Nothing is kept under the old name (ADR 0049).

### 3. The pictures

The interface design: the address, the role catalogue and the header, with what each child changes.

![An address names an agent or its subagent, the role catalogue turns it into a folder, and the level comes from the header](diagrams/CL68-agents-and-their-subagents-interface-design.svg)

The architecture: every project's prompts lose the wrapper, and the engine reads them through one catalogue.

![Identities hold agents and their subagents, packs hold toolsets, core holds the principles; the engine reads them all through EmbeddedPrompts and the catalogue](diagrams/CL68-agents-and-their-subagents-architecture.svg)

The main case: Puls asks its cook.

![Puls calls AskAgent with puls.cook, the address becomes the cook's folder, its header says subagent, and its engine is built with its toolsets](diagrams/CL68-agents-and-their-subagents-communication-puls-asks-its-cook.svg)

What is in memory while the cook answers.

![The catalogue holds each provider once, and the cook's sub-session holds its own engine built from its voice and header](diagrams/CL68-agents-and-their-subagents-objects-in-memory.svg)

### 4. The namespace map

| Type or file | Folder | Namespace | Child |
|---|---|---|---|
| `EmbeddedPrompts` (the prompt folders from the root; the subfolder overload goes) | `src/engine/louis-agent.composition/Packs/` | `LouisAgent.Composition.Packs` | CL68.1 |
| A provider that shows only the prompt folders (its pattern put to Louis when CL68.1 starts) | `src/engine/louis-agent.composition/Packs/` | `LouisAgent.Composition.Packs` | CL68.1 |
| `AgentCatalog` (a shared provider counted once) | `src/engine/louis-agent.core/Agent/Catalog/` | `LouisAgent.Core.Agent.Catalog` | CL68.1 |
| `ShowroomIdentity` (one shared provider) | `src/site/louis-agent.identity.showroom/` | `LouisAgent.Identities.Showroom` | CL68.1 |
| `RoleCatalog.AgentsFolder`, `SkillFiles` (`agents/`) | `src/engine/louis-agent.core/Skills/`, `Skills/Roles/` | `LouisAgent.Core.Skills`, `.Roles` | CL68.1 |
| `PromptFolderTests`, `SkillsLayoutTests` | `src/tests/engine/louis-agent.core.tests/` | `LouisAgent.Core.Tests` | CL68.1 |
| `RoleLevel.Subagent`, `RoleCatalog.SubagentsFolder`, `RoleCatalog.LevelOf` from the header | `src/engine/louis-agent.core/Skills/` | `LouisAgent.Core.Skills` | CL68.2 |
| `AgentAddresses` (an address to its subagent's folder) | `src/engine/louis-agent.core/Delegation/Addresses/` | `LouisAgent.Core.Delegation.Addresses` | CL68.2 |
| A layout test: a subagent's level matches where it sits | `src/tests/engine/louis-agent.core.tests/` | `LouisAgent.Core.Tests` | CL68.2 |
| `RoleLevel.Lookup`, `LookupAsk` (was `MinionAsk`), its messages | `src/engine/louis-agent.core/Delegation/` | `LouisAgent.Core.Delegation` | CL68.3 |
| `LOOKUP_LLMS` (was `MINION_LLMS`) in the options and each demo's `.env.example` | `src/engine/louis-agent.core/Config/`, `demos/*/config/` | `LouisAgent.Core.Config` | CL68.3 |

### 5. What is reused

Nothing new is installed. The embedded provider stays `ManifestEmbeddedFileProvider`
([its constructors](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.fileproviders.manifestembeddedfileprovider.-ctor?view=net-8.0));
the header reader (`PromptHeaders`, ADR 0063) already gives every prompt its level; the address (`AgentAddress`,
ADR 0117) already reads `puls.cook`.

## The children

Each starts with its own plan, on its own branch off `main`, and ends with its pull request.

| Child | What it does | Done when |
|---|---|---|
| **CL68.1** `prompts/` goes, `agent/` is `agents/` | Each project embeds its prompt folders from its root; `agent/` becomes `agents/` everywhere it's named (the core constants, the identities, the agent-written skills folder `skills/agents/functions/`, the site's link to the principles); the showroom flat, one provider for its three businesses. About six steps | No `prompts/` folder is left in the repo, `PromptFolderTests` holds that, the showroom names each business once, and every unit test passes |
| **CL68.2** Subagents inside their agent | `specialists/` goes; each specialist is `agents/{identity}/subagents/{name}/voice.md`; the level comes from the header; `specialist` becomes `subagent` in code, headers and texts; ADR 0017 superseded by a new ADR (proposed to Louis first). About eight steps | `puls.cook` opens `agents/puls/subagents/cook/voice.md`, no `specialists/` folder is left, a test refuses a level that doesn't match where a file sits, and every unit test passes |
| **CL68.3** Minions are lookup agents | `minions/` goes; each minion is a subagent marked `level: lookup`; the base is `agents/{identity}/lookup.md`; minion becomes lookup through the code, the settings and ADR 0021; the write-up. About seven steps | No `minion` name is left in code, settings or prompts, `puls.find-recipe` still runs with one tool, and the write-up is in `src/docs/learning/` |

**Open choices, each put to Louis when its child starts:**

- CL68.1: how the provider shows only the prompt folders (a filter around the embedded provider, one provider per
  folder joined together, or the packs moving their other files out of the embedded set).
- CL68.2: a subagent's name inside the engine. The address is `puls.cook` and the folder is `cook`, but sessions,
  `ROLES_ENABLED`, the evals' labels and the stored runs know it as `puls-cook`. Either the role is called
  `puls.cook` everywhere, or it stays `puls-cook` with the folder named `cook`, or the folder carries the old name.

## Verification

Each step's own tests while it's built, and the full unit suite before each child's pull request, after `main` is
merged in. By hand, for free: open a Puls chat and ask the cook something, start the guide and the three showroom
businesses, and read their prompts on the admin's *Prompts* page. No paid run is needed: no prompt's words change,
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

Nothing yet.
