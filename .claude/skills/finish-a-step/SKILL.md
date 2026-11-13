---
name: finish-a-step
description: Take a built step from its test passing to a commit. Use it when a step's code is written and its test passes, before the step is shown to Louis and before any commit. It holds the order of the tests, the golden files, the docs and pictures, the formatting with Rider's tools, what Louis is shown and the commit, and what to do after a Rider reformat.
---

# Finish a step

The step's test passes. This takes it to a commit. Four stops come first.

1. **Show the step before you commit it, and wait for Louis's ok.** Louis, 2026-10-14: "show me all changes before
   committing". He reviews the step while it can still change.
2. **The tests decide whether you may commit, and they run after the last format.** A Rider reformat can write an
   older copy of a file back over a fresh edit. Never commit on a test command whose result a pipe has hidden.
3. **Commit the step on its own, test and code together, then stop.** Never push without his word.
4. **Format only the commit's files, with the formatter.** Never a script that rewrites source, and never a
   whole-solution `dotnet format`.

## The list

1. **Build and test** the projects the step touches (in Rider, through its MCP server; elsewhere `dotnet build
   <project>` and `dotnet test <project>`); the whole solution (`dotnet build louis-agent-solution.sln`) before a PR.
2. **Goldens.** If tools, their descriptions, the tool texts (the `*ToolMessage` enums) or the prompts under
   `prompts/` changed, regenerate the golden files (`LOUIS_UPDATE_GOLDEN=1`) and review their diff
   ([golden files](../../../src/docs/guides/write-tests.md#regenerate-a-golden-file)).
3. **Docs.** Update the feature or fix doc with what the step learned. Then, for each project the step touches, open
   its design doc and its how-to (both show under `docs/` in the project in Rider) and change what the step made
   untrue: a renamed class, a moved folder, a new part, a new setting. If one of the doc's pictures no longer shows
   the code, change its JSON and redraw it with the tool, never the SVG by hand
   (`/draw-a-diagram`); `dotnet run --project src/tools/louis-agent.doc-diagrams -- --check`
   names an SVG that wasn't redrawn after its JSON changed. On the last step, use `/finish-a-feature`, or
   `/fix-a-bug` for a fix. The plan changes only under *Approved during the build*. New text goes in
   the doc of its type.
4. **Format** every C# file in the commit, in this order and once:
   1. the commit's files (`git diff --name-only`, or Rider's git status tool);
   2. Rider's own formatter on each, through its MCP server: it is the one that wraps lines to 120, which
      `dotnet format` never does;
   3. Rider's inspections on each (its file-problems or lint tool), instead of re-reading the file;
   4. the `//` and `///` lines over 120 wrapped by hand: Rider's formatter never wraps a comment (JetBrains issue
      RSRP-225809), and where no Rider runs (a cloud session) every long line is wrapped by hand;
   5. `dotnet format louis-agent-solution.sln --include <its .cs files>` once, last: it moves usings and is what CI
      checks. Never a whole-solution `dotnet format` in a loop.

   Only the files in the commit are formatted, whole; untouched files keep their old layout, never a big reformat of
   its own. CI's *Formatting* step checks the C# files of the commits each push brings (merges left out).
5. **Show the step** before committing (Louis, 2026-10-28): the files it changes, the commit message (what changed and
   why) and the explanation (what it means, why this design, the test failing before the code and passing after),
   and wait for Louis's ok. He reviews the step while it can still change; a commit is harder to reshape than a diff.
6. **Commit** the step on its own, test and code together, and stop. Never push without asking.

## Which tests, and when

Louis, 2026-11-13: "before committing only touched unit tests, before doing a PR main must be merged and full
suite unit tests passes".

- **Before a commit: only the tests of what the step touched.** `louis-agent.core.tests` is among them when the
  step moves files or adds names. `/do-a-step` says which tests a step reaches.
- **Before a pull request: `main` is merged in first, then the full suite of unit tests passes.** Fetch, merge
  `origin/main` into the branch, and run
  `dotnet test louis-agent-solution.sln --filter "Category!=Integration"`. A pull request is not opened on a
  branch that is behind `main`, or on a suite with a failing test.

## What Louis is shown

Item 5, part by part.

- **The files** the step changes, as a diff he can read.
- **The commit message**: what changed, and why in its body.
- **The explanation**, in plain words, the way a friend would. What it means first, then what changed, why this
  design and not the alternatives, and what to look at closely. A term is explained the first time it is used.
- **The test**, failing before the code and passing after, and which tests ran.
- **The concept** behind the step, and where it sits in the skill map of `src/docs/GOAL.md`.

## After a Rider reformat

Rider keeps an open file in its own memory, and may not have reloaded what was just written to disk. Its
formatter can then write the older copy back, and the edit is gone. So after `reformat_file`:

1. Build the touched project and run its tests again.
2. If a note says the file changed on disk and it shows an older version, make the edit again.
3. Commit only on the tests' own result. Use `set -o pipefail`, or check for the `Passed!` line. Never
   `dotnet test … | head -1 && git commit`, where the pipe hides a failure.

## Rider's tools, by job

The rule is *Rider's tools first* ([ADR 0052](../../../src/docs/adr/0052-edit-never-script.md)): for any job a Rider
tool covers, use it, never a hand edit, a script or a search-and-replace that does the same thing less well. The tool
names differ by Rider version, so a session starts with `/mcp` to see what it has, and runs Rider's *Auto-Configure*
(Settings → Tools → MCP Server) if the server is missing. *Brave mode* (terminal commands without a confirmation)
stays off. The research is `research/rider-mcp-tools.md` in the Claude project.

| Job | Rider 2025.2 | Rider 2026.x | Never |
|---|---|---|---|
| Format a file | `reformat_file` | `reformat_file` | re-typing the file; a Python, sed, awk or PowerShell script |
| See what the inspections flag | `get_file_problems` | `get_file_problems`, `lint_files` (all of them at once) | re-reading the file to look for problems |
| Compile | the terminal's `dotnet build` | `build_project`. Rider 2026.2 lists `build_solution_start` and `build_solution_state` instead | — |
| Rename a symbol | `rename_refactoring` | `rename_refactoring` | search and replace |
| Before a signature change | `get_symbol_info` | `get_symbol_info`, `analyze_calls` | guessing the callers |
| The commit's files | `git_status` | `git_status` | — |

Why no script, ever (Louis, 2026-10-27): a script that added comments walked a sentence letter by letter and left 478
one-character comment lines in a test file. Code and comments change only by editing the file (an editor, Rider,
Claude Code's own edit tool); layout only through `dotnet format` or Rider's formatter; a rename only through Rider's
rename, which follows every reference ([ADR 0052](../../../src/docs/adr/0052-edit-never-script.md)).

## Rebuild the container

Louis, 2026-10-15: "build the container always after doing something". He tests by hand in the running app, and an
old image hides the change. A restart is not enough, since the prompts are built into the assembly (ADR 0018).

After the step's tests pass, rebuild the image the change reaches, without being asked, and say whether it came up.
A change to the engine reaches every image in use.

| What changed | Rebuild |
|---|---|
| The framework's site | `docker compose -f src/site/docker/docker-compose.yml up -d --build` |
| The code-review API or its web app | `docker compose -f demos/louis-agent/docker/docker-compose.yml --env-file demos/louis-agent/config/.env up -d --build api` |
| Puls | `docker compose -f demos/puls/docker/docker-compose.puls.yml --env-file demos/puls/config/.env up -d --build` |

**Never while an eval run is going.** A run started from the eval studio runs inside the container. A rebuild makes
the container again, and that ends the run. `GET /v1/admin/evals/runs/current` says whether one is going. Wait for
it to end, then rebuild.
