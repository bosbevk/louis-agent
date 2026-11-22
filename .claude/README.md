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
2. **A skill puts its must-dos first.** When a long session is compacted, Claude Code puts back the first 5,000
   tokens of each skill it used, newest first, and 25,000 in all. So an older skill can be dropped whole, and is
   loaded again when its job comes back.

A rule a program can check is held by a hook as well, since a hook runs whether or not a skill loaded. Two are in
`hooks/`, named in `settings.json`: `set-the-author.sh` runs when a session starts and makes Louis the author
where none is set or Claude is, and `refuse-claude-lines.sh` runs before every shell command and refuses a commit
as Claude, or a commit or a pull request with a line that names Claude.

## One step, and what is loaded when

![One step of a feature: the always-loaded files are there from the start, a rules file arrives with the first file touched, and a skill with each job](../src/docs/diagrams/claude-instructions-one-step.svg)

## What loads with a kind of file

| File | Loads for | Holds |
|---|---|---|
| [src/engine/LouisAgent.Core/CLAUDE.md](../src/engine/LouisAgent.Core/CLAUDE.md) | `src/engine/LouisAgent.Core/` | Adding a C# tool or a toolset, the project layout, messages and logging, changing the LLM |
| `rules/csharp.md` | `**/*.cs` | The how of every C# rule: conditionals, endpoints, records, enums, size, formatting |
| `rules/csharp-summaries.md` | `**/*.cs` | How a summary is written, with its examples |
| `rules/tests.md` | `src/tests/**`, `packs/tests/**`, `demos/*/tests/**` | What every test is, which tests run when, the golden files |
| `rules/razor.md` | `**/*.razor`, `**/*.razor.cs` | Markup only, split by a hundred lines, the live results |
| `rules/css.md` | `**/*.css`, `**/*.scss` | 200 lines of rules a file, `app.css` the import list in cascade order, the byte-checked move |
| `rules/prompts.md` | the prompt folders' markdown (`**/agents/**/*.md`, `toolsets/`, `shared/`, `specialists/`, `minions/`), `**/*ToolMessage.cs` | No name or date in a text the model reads, a prompt's header, the golden files |
| `rules/docs.md` | `**/*.md` | One type per doc, the plan and fix-doc lifecycle, write-ups, English, plain words, the 200-line rule |
| `rules/hosts-and-web.md` | `src/hosts/**`, `src/web/**`, `src/site/**`, `packs/web/**`, `demos/*/hosts/**`, `demos/*/web/**` | A host's own texts, a web app's folders, a page and its components, running a host from Rider |

## The skills

The skills are grouped by the order the work runs. A group is a heading in this table and in `CLAUDE.md`'s, not
a folder: Claude Code finds a skill only as `skills/{name}/SKILL.md`. A file beside a `SKILL.md` is the skill's
own: its test jobs in `tests.md`, or detail it reads only when needed, as `answer-a-message` keeps `questions.md`.

| Group | Skill | Its job |
|---|---|---|
| Plan | `plan-a-feature` | A plan is written, changed or finished |
| Plan | `review-a-plan` | A plan is reviewed, proofread or checked for what it misses |
| Plan | `put-a-finding` | A choice comes up that the plan or the brief doesn't settle |
| Build | `do-a-step` | A step starts |
| Build | `finish-a-step` | A step's test passes, and it is to be shown and committed |
| Build | `fix-a-bug` | Something broke, and a fix starts |
| Build | `finish-a-feature` | The last step of a feature or a cleanup |
| Ship | `ship-it` | A commit, a push or a merge |
| Ship | `open-a-pull-request` | A pull request is opened, or its body written |
| Write | `write-the-docs` | A doc, a guide, a README, an ADR or a write-up is written or rewritten (the technical writer) |
| Write | `write-the-framework` | A framework doc or page is checked against the code, or a framework page changes (the framework writer) |
| Write | `draw-a-diagram` | A picture in the docs is added or changed |
| Site and brand | `research-the-market` | A slogan, a heading, positioning or a brand audit is asked for, or copy is to be written |
| Site and brand | `design-a-page` | A page or a section is planned, reordered or redesigned, always on our framework |
| Site and brand | `design-a-graphic` | A mark, a card, an image, a mockup or a palette is made or changed |
| Site and brand | `write-the-copy` | Words a visitor reads on a site are written or changed, the framework's pages aside (the story and web writer) |
| Site and brand | `check-a-page` | Something a visitor sees changed, or was deployed |
| Site and brand | `review-a-design` | A built site's or a theme's design, look and feel is reviewed |
| Sell | `answer-a-message` | A customer or a prospect sent a message, and a reply is drafted |
| Maintain | `write-a-skill` | A skill is written or changed |

## Changing an instruction

- **Where it goes.** Must it hold on every turn? `src/docs/CLAUDE.md`. Is it how one kind of file is written? That
  kind's rules file. Is it a procedure for a job? Its skill.
- **What holds it.** Every file here is at most 200 lines (`ClaudeMdSizeTests`). Every skill has a name and a
  description, and `CLAUDE.md` names exactly the skills there are (`SkillFilesTests`). Every file here is in the
  solution (`SolutionLayoutTests`).
- **A new file is picked up by a session that is already open.** A new skill's description joins the
  session's list of skills, and a new rules file loads with the next matching file. A skill with `paths`
  shows only once a matching file is worked on.
