# FX19 · A value from the model can reach git as an option, and some options run commands

> **Status:** open · **Area:** core, the git tools (`Tools/Git/GitTools`) · **Found:** 2026-10-18, reading the code and
> probing git for [the platform plan](../plans/platform.md) (at `main` 7894953) · **Planned:** the plan's Phase 1, one
> step, so no plan file of its own

## Symptom (probed on git 2.43.0 in a scratch repository; not yet seen in a run)

Each value passed as one argument, as the model could send it:

- `git show --output=<file> --stat` exits 0 and writes the file, wherever it points; `ShowCommit(commitRef)` builds
  exactly `show {commitRef} --stat` (`louis-agent.core/Tools/Git/GitTools.cs:37-41`) and is described as read-only.
- `git config core.fsmonitor '<command>'`, then `git status`, runs the command; `SetConfig(key, value)` accepts any key
  (`GitTools.cs:273-278`).
- `git fetch` and `git push` take `--upload-pack`, `--receive-pack` and `--exec`, which run commands, and
  `Fetch(remote)` puts the model's value where git reads options (`GitTools.cs:173-176`).

## Cause

- **One string, escaped by hand.** 38 calls to `RunGit(string)` on 36 lines build one argument string for
  `ProcessStartInfo.Arguments` (`GitTools.cs:338-348`, `:386-389`), against
  [ADR 0004](../adr/0004-arguments-as-data.md). `RunGitList` already passes a list, and the history tools put `--`
  before the model's values (`GitTools.cs:329`, `:280-281`).
- **A list alone isn't enough.** A list stops a value being split, but a value that starts with `-` is still read as an
  option. A test with a branch named `x --upload-pack=touch /tmp/p` is refused ("is not a valid branch name") only
  because it starts with `x`, so it would pass while the hole stays open.
- **Some config keys run commands.** `core.fsmonitor`, `core.sshCommand`, `core.hooksPath` and `alias.*` make git run
  commands, and `SetConfig` takes any of them.

## Fix (to do)

Designed in [the platform plan](../plans/platform.md), Phase 1 (*FX19 · Git arguments as data*): every call through
`RunGitList`, with `RunGit(string)` and `EscapeForShell` deleted; our own options first, then `--end-of-options`, then
the model's values, and paths after `--`; a model value that starts with `-` refused with its reason, except free text
that travels as an option's own argument (a `-m` message); `SetConfig` limited to `user.name` and `user.email`; and an
`ArchitectureTests` check that no file in core sets a `ProcessStartInfo`'s `Arguments` string.

## Tests (to do)

In `tests/louis-agent.core.tests/Tools/Git/`, against a real temporary repository, written first and seen failing (the
plan's Phase 1, step 2); the golden files regenerated and their diff reviewed.

## Confirm

The tests run git itself, so they are the confirmation: no file written, no key set, no command run.

## Lesson

Argument injection (OWASP LLM05:2025; CWE-88): a list stops splitting, not options; `--end-of-options`, from git's
`gitcli` docs, and an allow-list rather than a deny-list. Skill map #15 (safety and security).

---
[Fixes](README.md) · Previous: [FX18](FX18-auth-fails-closed.md) · Next: [FX20](FX20-skill-files-wait-for-approval.md)
