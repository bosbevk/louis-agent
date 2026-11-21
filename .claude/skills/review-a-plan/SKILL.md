---
name: review-a-plan
description: Reviews a plan in src/docs/plans before Louis approves it or before its first step, proofreading it against his picks and the code and finding what it leaves out, each gap researched with three ways to close it. Use when Louis asks to review, proofread or check a plan, asks what a plan is missing, or a plan was just written or changed by his picks. Holds what is read first, the two passes, the pre-mortem, how gaps are researched, and the review file handed over.
---

# Review a plan

A plan is cheapest to fix before its first step. A review reads it as written, against what Louis picked and what
the code is, and says what is wrong and what is missing. Five stops come first.

1. **The review never edits the plan.** It is a file of its own beside the plan. Louis decides what changes, and an
   approved plan changes only under *Approved during the build* (`/plan-a-feature`).
2. **Nothing from memory.** A name of code is opened in the code, a fact about a tool in its own docs on the web
   that day, a pick in Louis's words. What could not be checked is said plainly, with what would check it.
3. **Not your own plan, alone.** A plan reviewed by the thread that wrote it misses what that thread assumed. When
   the plan is this thread's own, say so before you start, and ask Louis for a fresh thread to review it. If he
   wants it reviewed here anyway, say so in the file's first paragraph, and run the pre-mortem twice as hard.
4. **A gap is not a decision.** Each gap gets its research and three ways to close it, and goes to Louis on its own
   card through `/put-a-finding`. A gap that changes the scope is never closed in the review.
5. **Proofreading comes before gaps.** A plan that contradicts itself has to be read right before you can say what
   it lacks.

## The steps

1. **Read what the plan answers to.** The plan as it stands in git (its commit) or in the project's files, its
   pictures, Louis's words in its *Context* and every pick he made since (the thread, the cards), the TODO row,
   and `/plan-a-feature` for the shape a plan must have.
2. **Pass one, proofread.** Read it top to bottom and list what is untrue or out of step (*What pass one checks*).
3. **Check the names in the code.** Every type, file, project, test and setting the plan says exists today, opened
   in `origin/main` after `git fetch origin main`. Every one it adds is marked new and absent today.
4. **Pass two, gaps.** Walk the main case end to end as the person it is for, then the cross-cutting concerns
   (*What pass two asks*), then run the pre-mortem.
5. **Research each gap**: what we found with its source, three ways to close it with the pattern each follows, and
   the one recommended with why (`/put-a-finding`'s shape).
6. **Write the review file**, run its checks, and hand it over.

## What pass one checks

Each finding names where it is (section or line) and what is true instead.

| Look for | For example |
|---|---|
| A pick Louis made that the text or a picture doesn't show yet | The opening says three children; his pick made it four |
| Two parts that disagree | The namespace map puts a type in one project; the folder tree has no such project |
| A name that doesn't match the code | A class the plan calls existing that isn't in `origin/main` |
| A part named for a job it doesn't do | A test helper that turns out to stand in for something else; a planned tool used as built |
| A choice called open that is settled, or settled that is open | "Which package: to be decided" after his pick |
| A step without a test written first, or bigger than about ten minutes to review | "Step 6: the sender, the retries and the deploy" |
| More than about ten steps, or no review stop | Split into dotted children (`/plan-a-feature`) |
| A missing part of the shape | No *Verification*, no *Concepts*, no overview or one of the four pictures |
| A record out of step | The branch, the TODO row, the work-log row, the plans index |
| A rule of the repo broken by the design | A pack that references another pack; a choice held as a string (`.claude/rules/design-rules.md`) |
| A sentence that reads badly, or a list where a doc should talk | `.claude/rules/docs.md` |

## What pass two asks

**The main case, as the person.** Follow one real user from the first thing they do to the result, step by step,
and at each step ask what they see and what the system holds. A gap shows up as a step the plan has no part for:
the notification nobody sends, the input the reader can't read, the case the user meets on day one.

**The concerns that cross every plan.** Google's design docs always weigh security, privacy and observability, and
count *Alternatives considered* among the most important sections. The rest of this table is this repo's own rules.
For each row, the plan either answers it or says why it doesn't apply.

| Concern | The question |
|---|---|
| Failure | What happens when each outside service, the model or the network fails or is slow? |
| Security and privacy | What does it trust, what does it store, who can see it, and how is it deleted? |
| Cost | What does one use cost, who pays, and where is the cap (ADR 0007)? |
| Limits | Which limits of a platform or a tool does it rest on, and were they read in its docs? |
| Observability | How will we know it works in production, and see when it doesn't? |
| Measurement | What proves it is better, before and after (GOAL.md, *Measure, don't assume*)? |
| What Louis does by hand | Accounts, approvals, documents and keys outside the code, and how long they take |
| The alternatives | Did the plan say why not the obvious other way, with sources? |
| Reuse | Does the repo or a maintained package already hold a part it builds (ADR 0030)? |
| Too much | Does a part solve a problem no one has now? Google's reviewers watch for over-engineering (ADR 0055) |
| Tests | Would each step's test actually fail if the step were broken? |

**The pre-mortem.** Imagine the feature shipped and failed six months later. Write down every reason it could have
failed, then keep those the plan doesn't already guard against. This is Gary Klein's pre-mortem: assume the project
has just failed and give the plausible reasons for it. Starting from the failure makes it safe to name the doubts a
plan's author would rather not hear.

## The review file

It goes beside the plan in the project's files, `/mnt/project-files/plans/{id}/{id}-review-{date}.md`, never in
the repo. It talks, as every doc here does, and opens with one paragraph: what was read, at which commit, which
names were checked in the code, and whether it reviews its own thread's plan.

Then two sections, in this order. *A. Out of step* is pass one's findings, numbered, each with where it is and the
fix, most of them mechanical. *B. Gaps* is pass two's, numbered, each with what we found and its source, the three
ways with the pattern each follows, the recommended one and why, and whether it changes the scope. Then *The
pre-mortem*, and *Could not check*: each thing the review could not confirm, and the step or person that would.

As Louis picks, each pick is added under *Louis's picks*, with its date and his words, so the one file stays the
record a later thread reads.

## Checks before it is shown

- Every name of code it calls existing or missing was opened in `origin/main` today.
- Every fact about a platform or a package has its link, read today.
- Every pick of Louis's that the plan should show was checked against the plan and its pictures.
- Every gap has three ways, a recommendation and its reason, and none is decided in the file.
- The pre-mortem was run, and what it found is in *B* or named as already guarded.

## Handing it over

Post the review file with a few lines: how many findings are out of step, how many gaps, and the one gap that
matters most. The mechanical fixes of *A* can be made in the plan on Louis's word, as one change. Each gap in *B*
then goes to him on its own card through `/put-a-finding`, the one that changes the most first. A plan already
approved takes each pick under *Approved during the build*, with the date and his words.

## An example

On 2026-11-21 the plan for Facebook auto-answers (F168) was reviewed before Louis approved it. The review's first
paragraph says it was written by the plan's own thread, and names what it checked in `origin/main`. Pass one found
seven things out of step: a pick not yet drawn through the opening, the records in the wrong step, a test helper
named for a job it doesn't do, and a last step too big to review in ten minutes. Pass two, walking a person from
their first Messenger message to an answer, found seven gaps: the guide's open door caps sessions per address, and
every Messenger session comes from one address, so it would shut on the second person. Each gap got three ways and
a recommendation. The pre-mortem named six ways it could fail, each tied to a gap or to a guard the plan already
had. Louis's picks went under *Louis's picks* as they came, in `F168-review-2026-11-21.md` in the project's files.

## References

- `/plan-a-feature`, the shape a plan must have, and `/put-a-finding`, how a gap goes to Louis
- [The design rules](../../rules/design-rules.md) and [the docs rules](../../rules/docs.md)
- Malte Ubl, [Design docs at Google](https://www.industrialempathy.com/posts/design-docs-at-google/): the
  alternatives considered, and the cross-cutting concerns a review checks
- Gary Klein, [Performing a project premortem](https://hbr.org/2007/09/performing-a-project-premortem), Harvard
  Business Review, 2007
- Google, [What to look for in a code review](https://google.github.io/eng-practices/review/reviewer/looking-for.html):
  design first, then the details
- Anthropic, [Skill authoring best practices](https://platform.claude.com/docs/en/agents-and-tools/agent-skills/best-practices):
  checklists and a feedback loop for a job with many parts
