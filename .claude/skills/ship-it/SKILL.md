---
name: ship-it
description: Commits, pushes, opens a pull request or merges, only on Louis's word and one pull request at a time. Use for every commit, when Louis says push or merge, when a pull request is about to open, and when a branch is named or brought up to date with main. Holds the branch names, the commit message, who a commit is authored as, what runs before a pull request, and the checks.
---

# Ship it

A commit stays on the machine it was made on. A push leaves it, and a merge to `main` goes live. Six stops come
first.

1. **Never push, and never merge, without Louis's word, each time.** He merges, unless he says "merge". His yes
   for one push is not a yes for the next.
2. **Every commit is authored as Louis, a merge of `main` too.** Before the first one in a session, run
   `git config user.name` and `git config user.email`. A fresh clone says `Claude <noreply@anthropic.com>`, and
   every cloud thread starts from one. *The commit* has the two lines that set it.
3. **A merge to `main` is a release.** The deploy starts by itself on the merge and releases both sites,
   louis-agent.dev and framework.louis-agent.dev.
4. **Before a pull request, `main` is merged in and the full suite of unit tests passes.** Louis, 2026-11-13:
   "before doing a PR main must be merged and full suite unit tests passes".
5. **Never rebase a branch that is on the remote.** It rewrites pushed history, and Louis may have pushed to the
   branch himself. Check the remote before you say a branch was never pushed.
6. **One pull request at a time.** Louis, 2026-11-08: "maybe wait for PR's to complete and deploy before
   continuing". Let one pass its checks, merge and deploy before the next branch starts. Fix branches are not
   stacked on each other.

## The branch

**Branches** are off `main` and carry the plan's id: `feature/F22-role-tools`, `fix/FX14-sub-session-prompt-budget`,
`refactor/CL3-messages-and-logging`, `feature/F49.1-checks-contract`; `docs/{name}` for docs-only work.

A fix's branch, and where it starts, are in `/fix-a-bug`.

**A cloud thread is given its branch**, `claude/project-thread-…`, and may push to no other. The plan's id is then
carried by each commit's scope and by the pull request's title, not by the branch's name.

## The commit

**Commit messages** are `type(scope): step N - what changed` (`feat(f22): step 3 - ReadTable for spreadsheets`,
`fix(fx14): the prompt budget counts cached tokens`, `docs(f5): review of PR #27 - toolset guides`), and the body says
why. `type` is `feat`, `fix`, `refactor`, `test` or `docs`; `scope` is the id in lowercase (`f22`, `fx14`, `cl3`,
`f49.1`), or the area when there is none (`ollama`, `guide`); `step N -` only on a plan's steps.

**The template** at the repo's root, `.gitmessage`, holds the same shape as comment lines git drops. A clone turns it
on once with `git config commit.template .gitmessage`. Rider's own commit box doesn't read it; a `git commit` does.

- **Who it is authored as.** Louis, 2026-10-19: "when you make a commit, commit as https://github.com/bosbevk".
  The author is `Louis Marais <84603591+bosbevk@users.noreply.github.com>`. Louis's PC has it in the repo's own
  git config. A fresh clone does not, so set it there before anything is committed or merged:
  `git config user.name "Louis Marais"`, then
  `git config user.email "84603591+bosbevk@users.noreply.github.com"`. Never commit under another address, and
  never as Claude. Two hooks in `.claude/hooks/` back this up where Claude Code runs them: one sets the author
  when a session starts, and one refuses a commit as Claude or with a line that names Claude. Check all the
  same, since a hook can be off.
- **No line names Claude.** Louis, 2026-11-17, of `Co-Authored-By: Claude …`: "make a rule to take this out of
  commits and PR's". No `Co-Authored-By` trailer, no `Claude-Session` line, nor any other attribution line. A
  cloud thread is told to end its commits with both, and the same note says Louis's own rule comes first. This
  is that rule.
- **It follows the tests' own result**, and Louis's ok for the step. Both are in `/finish-a-step`.

## Before you say what main holds

Run `git fetch origin main` and look at `origin/main`. The local `main` can be hundreds of commits behind.
`git log HEAD..origin/main` lists what `main` has that the branch has not.

## Before a pull request

1. **Merge `main` in.** `git fetch origin`, then merge `origin/main` into the branch. A merge, never a rebase:
   `git ls-remote --heads origin <branch>` says whether the branch is on the remote, and one that is there is
   never rebased. Run the Docs tests at once after it: a merge can bring back a row this branch closed in the TODO or
   the work log.
2. **Build the whole solution.** `dotnet build louis-agent-solution.sln`.
3. **Run the full suite of unit tests**, and it passes:
   `dotnet test louis-agent-solution.sln --filter "Category!=Integration"`.
4. **Ask Louis for the push.**

**A cloud thread runs the full suite on GitHub** (Louis's pick, 2026-11-21). It merges `main` in, runs the tests
of what the branch touched, and on his word pushes and opens the pull request. CI then runs the full suite there,
and nothing merges until it is green. The whole solution doesn't build in a cloud container, so the full suite
can't run there.

## The pull request

Its title, its body's sections in their order and what is never in it are in `/open-a-pull-request`, and GitHub's
template, `.github/pull_request_template.md`, holds the same sections.

## The checks

CI runs on every push to a pull request, on Ubuntu: the build, the formatting of the C# files the commits touched,
the unit tests, then the integration tests. A push to a branch with no pull request runs nothing, and a merge to
`main` runs no CI either. A red check is the same failure its command shows locally.

## The merge, and after

- **Louis merges.** On his "merge", it is a merge commit, so each step stays its own commit on `main`.
- **Code waits for green checks, and docs don't** (Louis's pick, 2026-11-21). A pull request that changes only
  docs merges on his word once the Docs tests pass locally:
  `dotnet test src/tests/engine/LouisAgent.Core.Tests --filter "FullyQualifiedName~Docs"`. One that changes
  anything else waits until CI is green on its last push, about twenty minutes, on "finish unattended" too. Docs
  are the `.md` files and the pictures in a `diagrams/` folder. A file under `prompts/` is not a doc, since the
  model reads it. `git diff --name-only origin/main...HEAD` shows which kind a branch is.
- **The deploy follows the merge at once.** Wait for it to land. Where the change is one a visitor sees, look at
  the live page with `/check-a-page`.
- **Then the next branch starts**, from `origin/main` after a fetch.

## An example

A fix, on Louis's "merge that". Its commit message takes the shape above,
`fix(fx204): the guide's bar no longer crashes when an answer grows a form`, with the why in its body and no line naming Claude. Then `git fetch origin main` and a merge of
`origin/main`, the build of the whole solution, and the full suite of unit tests, every test project passing.
Only then the push, the pull request, and a merge commit, since he said "merge".

## References

- [The development guide's hard stops](../../../src/docs/CLAUDE.md#hard-stops), Louis's word for every push
- The commit template, `.gitmessage`, and GitHub's `.github/pull_request_template.md`
- [ADR 0075: test what you touch](../../../src/docs/adr/0075-test-what-you-touch.md), the full suite once
