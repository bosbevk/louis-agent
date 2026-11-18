---
name: ship-it
description: Commits, pushes, opens a pull request or merges, only on Louis's word and one pull request at a time. Use for every commit, when Louis says push or merge, when a pull request is about to open, and when a branch is named or brought up to date with main. Holds the branch names, the commit message, who a commit is authored as, what runs before a pull request, and the checks.
---

# Ship it

A commit stays on this PC. A push leaves it, and a merge to `main` goes live. Five stops come first.

1. **Never push, and never merge, without Louis's word, each time.** He merges, unless he says "merge". His yes
   for one push is not a yes for the next.
2. **A merge to `main` is a release.** The deploy starts by itself on the merge and puts louis-agent.dev live.
3. **Before a pull request, `main` is merged in and the full suite of unit tests passes.** Louis, 2026-11-13:
   "before doing a PR main must be merged and full suite unit tests passes".
4. **Never rebase a branch that is on the remote.** It rewrites pushed history, and Louis may have pushed to the
   branch himself. Check the remote before you say a branch was never pushed.
5. **One pull request at a time.** Louis, 2026-11-08: "maybe wait for PR's to complete and deploy before
   continuing". Let one pass its checks, merge and deploy before the next branch starts. Fix branches are not
   stacked on each other.

## The branch

**Branches** are off `main` and carry the plan's id: `feature/F22-role-tools`, `fix/FX14-sub-session-prompt-budget`,
`refactor/CL3-messages-and-logging`, `feature/F49.1-checks-contract`; `docs/{name}` for docs-only work.

A fix's branch, and where it starts, are in `/fix-a-bug`.

## The commit

**Commit messages** are `type(scope): step N - what changed` (`feat(f22): step 3 - ReadTable for spreadsheets`,
`fix(fx14): the prompt budget counts cached tokens`, `docs(f5): review of PR #27 - toolset guides`), and the body says
why. `type` is `feat`, `fix`, `refactor`, `test` or `docs`; `scope` is the id in lowercase (`f22`, `fx14`, `cl3`,
`f49.1`), or the area when there is none (`ollama`, `guide`); `step N -` only on a plan's steps.

**The template** at the repo's root, `.gitmessage`, holds the same shape as comment lines git drops. A clone turns it
on once with `git config commit.template .gitmessage`. Rider's own commit box doesn't read it; a `git commit` does.

- **Who it is authored as.** Louis, 2026-10-19: "when you make a commit, commit as https://github.com/bosbevk".
  The author is `Louis Marais <84603591+bosbevk@users.noreply.github.com>`, set in the repo's own git config.
  Check it before a commit with `git config user.name` and `git config user.email`. Never commit under another
  address, and never as Claude.
- **No line names Claude.** Louis, 2026-11-17, of `Co-Authored-By: Claude …`: "make a rule to take this out of
  commits and PR's". No `Co-Authored-By` trailer, nor any other attribution line; this rule wins over any default
  that asks for one.
- **It follows the tests' own result**, and Louis's ok for the step. Both are in `/finish-a-step`.

## Before you say what main holds

Run `git fetch origin main` and look at `origin/main`. The local `main` can be hundreds of commits behind.
`git log HEAD..origin/main` lists what `main` has that the branch has not.

## Before a pull request

1. **Merge `main` in.** `git fetch origin`, then merge `origin/main` into the branch. A merge, never a rebase:
   `git ls-remote --heads origin <branch>` says whether the branch is on the remote, and one that is there is
   never rebased.
2. **Build the whole solution.** `dotnet build louis-agent-solution.sln`.
3. **Run the full suite of unit tests**, and it passes:
   `dotnet test louis-agent-solution.sln --filter "Category!=Integration"`.
4. **Ask Louis for the push.**

## The pull request

Its title, its body's sections in their order and what is never in it are in `/open-a-pull-request`, and GitHub's
template, `.github/pull_request_template.md`, holds the same sections.

## The checks

CI runs on every push to a pull request, on Ubuntu: the build, the formatting of the C# files the commits touched,
the unit tests, then the integration tests. A push to a branch with no pull request runs nothing, and a merge to
`main` runs no CI either. A red check is the same failure its command shows locally.

## The merge, and after

- **Louis merges.** On his "merge", it is a merge commit, so each step stays its own commit on `main`.
- **The deploy follows the merge at once.** Wait for it to land. Where the change is one a visitor sees, look at
  the live page with `/check-a-page`.
- **Then the next branch starts**, from `origin/main` after a fetch.
