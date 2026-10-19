# FX19 · A value from the model can reach git as an option, and some options run commands

> **Status:** done · **Area:** core, the git tools (`Tools/Git/GitTools`); `ArchitectureTests` · **Found:** 2026-10-18,
> reading the code and probing git for [the platform plan](../plans/platform.md) (at `main` 7894953) ·
> **Fixed:** 2026-10-19 · **Planned:** the plan's Phase 1, one step, so no plan file of its own

## Symptom (probed on git 2.43.0 in a scratch repository; not seen in a run)

Each value passed as one argument, as the model could send it:

- `git show --output=<file> --stat` exits 0 and writes the file, wherever it points; `ShowCommit(commitRef)` built
  exactly `show {commitRef} --stat` (`louis-agent.core/Tools/Git/GitTools.cs:37-41` at `7894953`) and is described as
  read-only.
- `git config core.fsmonitor '<command>'`, then `git status`, runs the command; `SetConfig(key, value)` accepted any key
  (`GitTools.cs:273-278`).
- `git fetch` and `git push` take `--upload-pack`, `--receive-pack` and `--exec`, which run commands, and
  `Fetch(remote)` put the model's value where git reads options (`GitTools.cs:173-176`).

## Cause

- **One string, escaped by hand.** 38 calls to `RunGit(string)` on 36 lines built one argument string for
  `ProcessStartInfo.Arguments` (`GitTools.cs:338-348`, `:386-389`), against
  [ADR 0004](../adr/0004-arguments-as-data.md). `RunGitList` already passed a list, and the history tools put `--`
  before the model's values (`GitTools.cs:329`, `:280-281`). The test's red run showed the split too:
  `CreateBranch("x --upload-pack=touch <file>")` came back `unknown option 'upload-pack=touch'`.
- **A list alone isn't enough.** A list stops a value being split, but a value that starts with `-` is still read as an
  option. A test with a branch named `x --upload-pack=touch /tmp/p` is refused ("is not a valid branch name") only
  because it starts with `x`, so it would pass while the hole stays open.
- **Some config keys run commands.** `core.fsmonitor`, `core.sshCommand`, `core.hooksPath` and `alias.*` make git run
  commands, and `SetConfig` took any of them.

## Fix

As designed in [the platform plan](../plans/platform.md), Phase 1 (*FX19 · Git arguments as data*):

- **One runner.** Every tool goes through `RunGitList` (a list on `ProcessRunner`, which uses `ArgumentList`);
  `RunGit(string)`, `EscapeForShell` and the `ToolMessage.GitFailed` only it used are gone. The working folder
  (`AGENT_WORKING_DIRECTORY`) and the 30-second timeout are as before.
- **Two guards on every value the model names** (a ref, branch, tag, remote, URL, path, config key), both, so a
  command that takes neither marker is still covered:
  1. our own options first, then `--end-of-options`, then the model's values; paths after `--` (`Stage`, `Unstage`,
     `DiscardUnstaged`, the diffs). Every form was pinned on real git by its test: `checkout`, `branch -d`,
     `reset --hard`, `stash apply|pop`, `push`, `pull`, `fetch`, `merge`, `rebase`, `tag`, `tag -d`, `remote get-url`,
     `remote add`, `cherry-pick`, `config --get` and `config` all take `--end-of-options` (git 2.24 or later; the
     containers' git is 2.43.0, `docker exec puls git --version`, and the machine's 2.56.0);
  2. a value that starts with `-` is refused before git runs, with `ToolMessage.GitValueRefused` naming the parameter
     and the value. `CreateBranch`'s name is `-b`'s own argument, so this guard alone covers it.
- **Free text travels whole**, whatever it starts with: a commit, merge, stash or tag message as `-m`'s argument, and a
  config value after `--end-of-options`. Quotes are characters again, not something to escape.
- **`SetConfig` sets only `user.name` and `user.email`** (`ToolMessage.GitConfigKeyRefused` names the keys that run
  commands); its description and `GetConfig`'s say so, and `GetRemoteUrl`'s no longer points at `SetConfig` for a
  remote's URL. `Stash` with a message uses `stash push -m` (`save` is deprecated; the same effect).
- **The ratchet.** `ArchitectureTests.NoFileInCore_BuildsAProcessCommandLineAsOneString` fails any core file that
  starts a process (`ProcessStartInfo`) and assigns its `Arguments` string.
- **Docs.** [ADR 0004](../adr/0004-arguments-as-data.md) is amended with the two further parts of the rule; the git
  guide's `SetConfig` line; the core design doc, the crosscutting concepts' *Security* and F22's note of the gap.
- **From the review of PR #66 (2026-10-19): a refspec isn't an option.** Neither guard saw `Push`'s branch when it was
  `+main` (a forced push, with `force: false`) or `:main` (deletes the remote's branch), both around the tool's own
  force switch. `Push` and `Pull` now refuse a branch that starts with `+` or contains `:`
  (`ToolMessage.GitRefspecRefused`): a value can be read as more than a name without being an option.
- **Not this fix's door, noted:** `AmendCommit(noEdit: false)` would open git's editor in a process with no terminal and
  wait out the 30-second timeout; it did before, and it stays for a later look at the tool's shape.

## Tests

`tests/louis-agent.core.tests/Tools/Git/GitToolsTests.cs`, rewritten on `GitHistoryTests`' pattern (a repository made
for each test, a bare one beside it as `origin`, skipped without git; its five `[Ignore]`d cases from before are gone),
and the `ArchitectureTests` check. Written first and seen failing on the old code:

- `ShowCommit_RefusesAValueThatIsAnOption_AndWritesNoFile`: was `Success (no output).` and the file written outside the
  repository.
- `SetConfig_SetsOnlyTheUsersNameAndEmail`: was `Success (no output).` with `core.fsmonitor` set; now refused, and
  `user.name` / `user.email` set and read back.
- `AValueThatStartsWithADash_IsRefusedBeforeGitRuns`: `Fetch("--upload-pack=touch …")` ran (`Could not read from remote
  repository`); now `Fetch`, `CreateBranch("--orphan")`, `SwitchBranch("-")`, `AddRemote` (name and URL), `ResetHard`,
  `GetConfig("--global")` and `DiffUnstaged("--output=…")` are refused and nothing runs.
- `Push_RefusesARefspec_ThatWouldForceOrDelete` (from the review): red first, `+main` pushed with `force: false`; now
  `+main`, `:main` and `main:other` are refused and origin's `main` stays as it was, and `Pull` refuses the same.
- `AValueWithSpaces_ArrivesAsOneArgument`: was `unknown option 'upload-pack=touch'` (the string split); now git sees one
  name and refuses it.
- `FreeText_ArrivesWhole_WhateverItStartsWith`: a message of `-m "quoted" --and dashes; and a 'tail'` read back whole
  from a commit, a merge and a tag; a config value of `-Anna-`.
- The forms, happy path: `Branches_CreateSwitchAndDelete`, `Changes_StageCommitShowDiffUnstageAndDiscard`,
  `Stashes_PushListApplyAndPop`, `Tags_CreateListAndDelete`, `Remotes_AddGetUrlPushFetchAndPull` (push, fetch and pull
  against the bare repository), `History_MergeRebaseAndCherryPick`.
- `ArchitectureTests.NoFileInCore_BuildsAProcessCommandLineAsOneString`: red on `Tools/Git/GitTools.cs`. Its first
  form also matched two files whose records have an `Arguments` field, so it reads only files that start a process.

The golden files changed on purpose and their diff was reviewed: five descriptions (`SetConfig` and its `key`,
`GetConfig`, `GetRemoteUrl`, `Stash`) in `ModelRequestSnapshot.json`; `GitValueRefused` and `GitConfigKeyRefused` in,
`GitFailed` out, in `ToolMessageSnapshot.json`; `GitRefspecRefused` in after the review.

## Confirmed

The tests run git itself, so they are the confirmation: no file written, no key set, no command run, on git 2.56.0
here; the containers run 2.43.0, the version the roadmap probed, and `--end-of-options` needs 2.24.

## Lesson

Argument injection (OWASP LLM05:2025; CWE-88): a list stops splitting, not options; `--end-of-options`, from git's
`gitcli` docs, and an allow-list rather than a deny-list. And a test for a guard has to fail for the hole, not for a
side effect: the branch name `x --upload-pack=…` is refused by git whatever we do, so it proves nothing on its own; the
test that proved this fix was the file `git show --output` wrote. Skill map #15 (safety and security).

---
[Fixes](README.md) · Previous: [FX18](FX18-auth-fails-closed.md) · Next: [FX20](FX20-skill-files-wait-for-approval.md)
