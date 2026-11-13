# Louis Agent: Development Guide

## How to work with Louis

Louis is a software engineer learning to become an AI engineer through this project; learning matters more than speed.
The goal and the skill map are in [GOAL.md](GOAL.md).

1. **Louis drives the code.** Implement only what Louis asks for, one step at a time; planning, reviews and learning
   write-ups happen in the Claude project, so don't start the next feature on your own.
2. **Small steps, small features.** Each step is one commit (or PR) with its test, small enough to review in about
   ten minutes. A feature holds at most about ten steps; anything bigger is a parent with dotted children (`F49.1`),
   each its own plan, branch, pull request to `main`, done-when and write-up, so `main` moves every few days.
3. **Plan first, and keep the plan.** Before the first step, the plan Louis approved is saved to
   [plans/](plans/README.md) and committed on its branch; it changes only under *Approved during the build*. When
   the work is done the plan goes: its lasting parts move to an ADR, the project's design doc or a write-up, and the
   file is deleted (`.claude/rules/docs.md` has the lifecycle).
4. **Explain every change** in plain words, the way a friend would: what it means first, then what changed, why this
   design and not the alternatives, and what to look at closely; say what a term means the first time; name code only
   where Louis needs to find it. This holds for every explanation: a step, a finding, a review, an answer.
5. **Stop after each step** and wait for Louis to review and understand it before starting the next one.
6. **Teach, don't just do.** Name the AI-engineering concept behind each step and where it sits in the GOAL.md skill map.
7. **Measure, don't assume.** Ask before anything that spends real money (a run against the API) and before pushing.
8. **Write it up, and bring the docs along.** Every feature, fix and cleanup ends with its write-up for Louis to read
   ([learning/](learning/)) and with the docs and diagrams of every project it touched matching the code again.
9. **Source of truth:** [TODO.md](../../TODO.md) (what's next), [plans/](plans/README.md) (how we're building it now),
   [fixes/](fixes/README.md) (what broke and why), [projects/](projects/README.md) (how each project works) and
   [adr/](adr/README.md) (why). Update them rather than duplicating them.
10. **Ask about every finding, with three options.** When a step turns up a choice the plan or the brief doesn't
    settle (a design point, a rule that clashes with the brief, a layout or naming question, a test that can't be
    written as asked), don't choose: put each finding to Louis on its own, with three options, each with what it
    changes and what it costs, and the one you recommend and why. Wait for his pick. A mechanical call (a local
    variable's name, a test's wording) is not a finding.
11. **Look up the pattern, then give three options.** A design choice (how to shape a class, pick an implementation,
    hold a rule) is never "your call" and never the first pattern that comes to mind: look up the patterns that fit
    (Refactoring.Guru, the Gang of Four, Fowler, Microsoft's .NET guidance), then put three options to Louis as rule
    10 says, each naming its pattern in plain words, with the sources and the one you recommend. The pick is named in
    the commit, the explanation and the `<remarks>` of each class that carries it ([ADR 0050](adr/0050-summaries-like-a-person.md)).

## The loop of a step

One step is one commit. How each part is done is in its skill. The waits in it hold
whether or not a skill is loaded.

1. **Do it** with `/do-a-step`. The test comes first and is seen failing for the expected reason, then the code.
   A docs-only or configuration-only step has no test first.
2. **Finish it** with `/finish-a-step`. Test what the step touches, regenerate the golden files if what the model
   reads changed, bring the docs and pictures along, and format the commit's C# files.
3. **Show it, and wait.** The files it changes, the commit message and the explanation, with the test failing
   before the code and passing after. No commit before Louis's ok.
4. **Commit it on its own**, test and code together, and stop (rule 5). Never push without asking (rule 7).
   The branch's name, the commit message and the pull request are in `/ship-it`.

**Which tests, and when.** Louis, 2026-11-13: "before committing only touched unit tests, before doing a PR main
must be merged and full suite unit tests passes". Before a commit, run only the tests of what the step touched.
Before a pull request, merge `main` into the branch first, and then the full suite of unit tests must pass.

**In Rider, Rider's tools come first** ([ADR 0052](adr/0052-edit-never-script.md)). A session starts with `/mcp` to see
what Rider offers, and for any job a Rider tool covers (format, inspect, build, test, rename, find usages, symbol
info, git status) it uses that tool, never a hand edit, a script (Python, sed, PowerShell) or a search-and-replace
that does the same thing less well: formatted with Rider's formatter, not re-typed; renamed with Rider's rename, not
replaced; problems from Rider's inspections, not from re-reading the file. A shell command is for what no Rider tool
does; *Brave mode* stays off. The tool-to-job table per Rider version is in [the skill](../../.claude/skills/finish-a-step/SKILL.md#riders-tools-by-job).

## Hard stops

Never without Louis's word, each time. His yes for one is not a yes for the next.

- **A push or a merge.** He merges, unless he says "merge" (rule 7).
- **A run that spends real money.** A test call of the guide or an agent is the exception: Louis, 2026-11-09, "I have
  250$ of credit do not worry about spending". Run it, and report what it cost after. Still ask before anything much
  larger, such as a long eval series.
- **A new package.** Three options first, with what speaks for and against each.
- **A new ADR, or a new rule in one.** Propose it in a few lines and wait.
- **A design choice the plan doesn't settle** (rules 10 and 11).
- **A commit.** Show the step first, and wait for his ok.
- **The next step.** Stop after each one (rule 5).

Three more ways of working, in his words:

- **Research first.** Louis, 2026-11-13: "do research first", and of planning, "the research also applies".
  Before an answer, a finding, a plan or a doc: read the code for what is there, and read a tool's own docs when
  the work rests on it. Nothing is said from memory. What could not be confirmed is said plainly.
- **Say what you are busy with.** Louis, 2026-11-08: "i need to always see what you are busy with?". One plain line
  before every batch of work, saying what it does. He follows the work as it happens and can't tell a stall from
  thinking.
- **Do the work yourself.** Louis, 2026-11-04: "do not pass on the work you do it". No build work is handed to another
  agent unless he asks for one.

## What is loaded with this file

Two rules files load in every session beside this one. If either is not in your context, read it before any change.

- [The design rules every change keeps](../../.claude/rules/design-rules.md): the decisions of the engine, safety,
  the code, the web and the docs, one line each with its ADR.
- [Where things are in the repo](../../.claude/rules/repo-map.md).

How all of Claude's instructions are laid out is in [.claude/README.md](../../.claude/README.md).

## Skills

A skill is a procedure that loads for its job. Use the one that fits.

| Job | Skill |
|---|---|
| A plan is written, changed or finished | `/plan-a-feature` |
| A choice comes up that the plan or the brief doesn't settle | `/put-a-finding` |
| A step starts | `/do-a-step` |
| A step's test passes, and it is to be shown and committed | `/finish-a-step` |
| Something a visitor sees changed, or was deployed | `/check-a-page` |
| Something broke, and a fix starts | `/fix-a-bug` |
| A commit, a push, a pull request or a merge | `/ship-it` |
| The last step of a feature or a cleanup | `/finish-a-feature` |
| A picture in the docs is added or changed | `/draw-a-diagram` |

## Where the rest is

Start with [Get started](guides/get-started.md). The how of a kind of file is a rules file in `.claude/rules/`,
and core has a `CLAUDE.md` of its own. Claude Code loads each when such a file is worked on.
[.claude/README.md](../../.claude/README.md) lists them, with what each holds and when it loads.

The how-tos are in [guides/](guides/README.md): [Add a skill](guides/add-a-skill.md), modes and roles in
[Build an identity](guides/build-an-identity.md#modes-roles-and-minions-the-rules),
[Debug and logs](guides/debug-and-logs.md), [Common errors](guides/common-errors.md).

**Known limitations** are the TODO's [list](../../TODO.md#known-limitations); when work removes one, delete it there.
**Further reading:** [the architecture](architecture/README.md), a demo's `config/.env.example` (settings), [the coding agent's mode](../../demos/louis-agent/identity/louis-agent.identity.coding/prompts/agent/louis-agent/voice.md).

---
[Docs index](README.md) · Previous: [Architecture](architecture/README.md) · Next: [Orchestrator runbooks](../../demos/louis-agent/docs/projects/orchestrator-runbooks.md)
