---
name: write-a-skill
description: Write a new Claude Code skill for this repo, or change one that is here. Use it when Louis asks for a skill, when a procedure keeps being explained again in threads, or when a skill under .claude/skills is added, renamed, split or rewritten. It holds the research first, whether the job is a skill at all, the name and the description, the body's shape, how it is wired in, and how it is tried before it is shown.
---

# Write a skill

A skill is a procedure Claude loads for one job. Only its description is in view all the time, and its body loads
when the job comes up. Four stops come first.

1. **A hard stop is never in a skill.** The body loads on Claude's judgement, so a rule that must hold on every
   turn, such as "never push without Louis's word", goes in `src/docs/CLAUDE.md` (`.claude/README.md`).
2. **Research first** (Louis, 2026-11-13: "do research first"). Read Anthropic's skill guidance and the repo's
   own skills before a line is written, and say what each part rests on. Nothing about how skills work is said
   from memory.
3. **Look for one that is already there.** A job that an existing skill nearly covers changes that skill. Two
   skills that both fit one job make Claude guess.
4. **Show it before it is committed.** The whole file, its sources, and what it was tried on. A new skill is
   committed only on Louis's word, like any step.

## Is it a skill?

Where a piece of guidance goes decides when Claude sees it. Pick by the question it answers.

| It answers | It goes in |
|---|---|
| What must hold on every turn | `src/docs/CLAUDE.md` |
| How one kind of file is written | Its rules file in `.claude/rules/`, loaded by `paths` |
| How to do one job, in order | **A skill** |
| How a tool or a project is used, for a person too | A guide in `src/docs/guides/`, which the skill points at |
| Why a design is the way it is | An ADR, only on Louis's yes |

A one-off answer is none of these. A skill earns its place when the same procedure has been needed more than
once, or when getting it wrong costs a lot.

## The steps

1. **Research.** Read these, and note what you take from each.
   - Anthropic's *Skill authoring best practices*:
     `https://platform.claude.com/docs/en/agents-and-tools/agent-skills/best-practices`.
   - Claude Code's *Skills* page, for the header's fields and how skills load:
     `https://code.claude.com/docs/en/skills`.
   - Two or three skills in `.claude/skills/`, for the shape this repo uses.
   - For a job with its own craft (writing, design, research), the sources of that craft, as `write-the-copy`
     cites GOV.UK and NN/g. A skill's rules come from somewhere a reader can check.
2. **Name it** as a verb and what it acts on, lowercase with hyphens, as the others are: `fix-a-bug`,
   `draw-a-diagram`. At most 64 characters, never `claude` or `anthropic` in it, and never a vague word such as
   `helper` or `tools`. The folder and the `name` are the same (`SkillFilesTests`).
3. **Write the description.** It is all Claude sees when it decides whether to load the skill, so it says what
   the skill does and when to use it, with the words a person would use for that job. Start with the job, then
   "Use it when…", then what it holds. Keep the use case first, since the listing cuts a long description, and
   stay under 1,024 characters.
4. **Write the body** in this order (the shape every skill here has):
   - One or two lines on what the job is, then **the stops first**, numbered, at most seven. After a long
     session is compacted only the start of a loaded skill is kept, so what must never be missed is at the top.
   - The **steps**, numbered, one action each, in the order they are done.
   - The **detail** after the steps, under headings that say what each part is for: a table to look things up,
     the rules of the craft with their sources, an example.
   - **Checks**, then **Handing it over**: what is shown to Louis, and where the work goes next.
5. **Wire it in.**
   - A row in the *Skills* table of `src/docs/CLAUDE.md`, as `` `/name` ``, and a row in `.claude/README.md`'s
     table. `SkillFilesTests` holds that `CLAUDE.md` names exactly the skills there are.
   - The file in the solution's `.claude\skills` folder, as the others are (`SolutionLayoutTests`).
   - A rules file or a guide that should send Claude to it gets one line with the link.
6. **Try it.** Start a fresh session, which knows nothing of this one, and give it two or three real jobs the
   skill is for, said the way Louis would say them. Watch two things: does the skill load from its description
   alone, and does the work follow its steps? Each miss goes back into the file, then try again.
7. **Run the tests** of what it touched:
   `dotnet test src/tests/engine/louis-agent.core.tests --filter "FullyQualifiedName~Docs"`.

## How the body is written

- **Only what Claude doesn't already know.** Every paragraph costs room in the context each time the skill loads.
  Cut what any good engineer would do anyway, and keep what is true only here: our commands, our paths, our
  tests, Louis's rules.
- **Louis's own words, with their date**, for a rule that came from him, so a later reader knows it is his and
  not a guess: "Louis, 2026-11-17: …".
- **Exact where a step is fragile, free where it takes judgement.** A command that must run one way is written
  out in full, in a code span. A step that needs judgement gets the rule and its reason, not a script.
- **One word for one thing** across the whole file. A step is not also a task and a stage.
- **Plain words, in short sentences**, as every doc here is written (`.claude/rules/docs.md`). A table where the
  reader scans, paragraphs where it explains.
- **Detail one link away, never two.** A long how-to stays in its guide, and the skill names the guide and the
  part to read. Claude may read only the start of a file it is sent to from another link.
- **At most 200 lines** (`ClaudeMdSizeTests`). A skill that grows past that splits by job, or moves its how-to
  into a guide.
- **Nothing that dates**, such as "until the new menu ships". What is true today goes in, and the plan holds the
  rest.
- **No hard stop and no secret.** A stop that must always hold is in `CLAUDE.md`. A key, a password or a private
  address is never in any file here.

## The header's fields

Claude Code reads more fields than `name` and `description`. Use one only when the job needs it.

| Field | Use it when |
|---|---|
| `paths` | The skill is only for some files, as `draw-a-diagram` is for `**/diagrams/*.json` |
| `disable-model-invocation: true` | Only a person should start it, with `/name`, never Claude on its own |
| `argument-hint` | It takes what it works on after the command, as `[pull request number]` |

## Checks before it is shown

- The description, read alone, says what it does and when. Would Claude pick it for the jobs it was tried on,
  and not for a nearby job another skill owns?
- The stops are at the top, and none of them is a hard stop that belongs in `CLAUDE.md`.
- Every command in it was run once, and every path in it exists.
- Every rule names where it comes from: Louis's words, an ADR, a test, or a source on the web.
- At most 200 lines, and the Docs tests pass.

## Handing it over

Show Louis the whole file, the research it rests on with links, and the jobs it was tried on with what happened.
A choice the research doesn't settle, such as which of two skills a job belongs to, goes to him through
`/put-a-finding`. On his word it is committed on its own through `/ship-it`.
