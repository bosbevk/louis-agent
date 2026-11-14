# Claude's instructions

This folder holds what Claude Code reads to work in this repo. This page says how it is laid out. It is for a
person who wants to change an instruction, and for Claude when it needs to find one. By Claude Code's docs, this
page is not one that Claude Code loads by itself.

## Three layers, by when each is needed

An instruction costs room in Claude's context on every turn it is loaded. So each one is loaded only when it is
needed, and the ones that must never be missed are loaded always.

![Three layers: what is always loaded, what loads with a kind of file, and what loads for a job](../src/docs/diagrams/claude-instructions-layers.svg)

| Layer | Files | When Claude Code loads it |
|---|---|---|
| Always | `src/docs/CLAUDE.md`, which the root `CLAUDE.md` imports. `rules/design-rules.md` and `rules/repo-map.md`, which have no `paths`. | Every session, whole |
| With a kind of file | `rules/csharp.md`, `rules/csharp-summaries.md`, `rules/razor.md`, `rules/css.md`, `rules/docs.md`, `rules/prompts.md`, `rules/tests.md`, `rules/hosts-and-web.md`, each with `paths`. Core's own `CLAUDE.md`, in its folder. | When a matching file, or a file in that folder, is read or edited |
| For a job | `skills/{name}/SKILL.md` | Its description is always in view. Its body loads when Claude judges the job matches, or when someone types `/name`. |

Two rules follow from the last row.

1. **A hard stop is never in a skill.** A skill's body loads on Claude's judgement, so what must hold on every turn,
   such as "never push without Louis's word", stays in `CLAUDE.md`.
2. **A skill puts its must-dos first.** When a long session is compacted, only the start of a loaded skill is kept.

## One step, and what is loaded when

![One step of a feature: the always-loaded files are there from the start, a rules file arrives with the first file touched, and a skill with each job](../src/docs/diagrams/claude-instructions-one-step.svg)

## What loads with a kind of file

| File | Loads for | Holds |
|---|---|---|
| [src/engine/louis-agent.core/CLAUDE.md](../src/engine/louis-agent.core/CLAUDE.md) | `src/engine/louis-agent.core/` | Adding a C# tool or a toolset, the project layout, messages and logging, changing the LLM |
| `rules/csharp.md` | `**/*.cs` | The how of every C# rule: conditionals, endpoints, records, enums, size, formatting |
| `rules/csharp-summaries.md` | `**/*.cs` | How a summary is written, with its examples |
| `rules/tests.md` | `src/tests/**`, `packs/tests/**`, `demos/*/tests/**` | What every test is, which tests run when, the golden files |
| `rules/razor.md` | `**/*.razor`, `**/*.razor.cs` | Markup only, split by a hundred lines, the live results |
| `rules/css.md` | `**/*.css`, `**/*.scss` | 200 lines of rules a file, `app.css` the import list in cascade order, the byte-checked move |
| `rules/prompts.md` | `**/prompts/**/*.md`, `**/*ToolMessage.cs` | No name or date in a text the model reads, a prompt's header, the golden files |
| `rules/docs.md` | `**/*.md` | One type per doc, the plan and fix-doc lifecycle, write-ups, English, plain words, the 200-line rule |
| `rules/hosts-and-web.md` | `src/hosts/**`, `src/web/**`, `src/site/**`, `packs/web/**`, `demos/*/hosts/**`, `demos/*/web/**` | A host's own texts, a web app's folders, a page and its components, running a host from Rider |

## The skills

| Skill | Its job |
|---|---|
| `plan-a-feature` | A plan is written, changed or finished |
| `put-a-finding` | A choice comes up that the plan or the brief doesn't settle |
| `do-a-step` | A step starts |
| `finish-a-step` | A step's test passes, and it is to be shown and committed |
| `check-a-page` | Something a visitor sees changed, or was deployed |
| `fix-a-bug` | Something broke, and a fix starts |
| `ship-it` | A commit, a push, a pull request or a merge |
| `finish-a-feature` | The last step of a feature or a cleanup |
| `draw-a-diagram` | A picture in the docs is added or changed |

## Changing an instruction

- **Where it goes.** Must it hold on every turn? `src/docs/CLAUDE.md`. Is it how one kind of file is written? That
  kind's rules file. Is it a procedure for a job? Its skill.
- **What holds it.** Every file here is at most 200 lines (`ClaudeMdSizeTests`). Every skill has a name and a
  description, and `CLAUDE.md` names exactly the skills there are (`SkillFilesTests`). Every file here is in the
  solution (`SolutionLayoutTests`).
- **A new file is picked up by a session that is already open.** A new skill's description joins the
  session's list of skills, and a new rules file loads with the next matching file. A skill with `paths`
  shows only once a matching file is worked on.
