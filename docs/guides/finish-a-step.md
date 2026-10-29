# Finish a step

> **How-to.** Get one step from "the test passes" to "committed": the build, the tests, the goldens, the docs, the
> formatting, showing it to Louis, the commit. **Elsewhere:** the rule itself in the
> [development guide](../CLAUDE.md#steps-branches-and-commits); the tests and the golden files in
> [Write and run tests](write-tests.md); the layout rules in `.claude/rules/csharp.md`.

## The list

1. **Build and test** the projects the step touches (in Rider, through its MCP server; elsewhere `dotnet build
   <project>` and `dotnet test <project>`); the whole solution (`dotnet build louis-agent-solution.sln`) before a PR.
2. **Goldens.** If tools, their descriptions, the tool texts (the `*ToolMessage` enums) or the prompts under
   `Prompts/` changed, regenerate the golden files (`LOUIS_UPDATE_GOLDEN=1`) and review their diff
   ([golden files](write-tests.md#regenerate-a-golden-file)).
3. **Docs.** Update the feature or fix doc with what the step learned; on the last step, follow *Finishing a feature*
   or *Finishing a fix* in their READMEs. The plan changes only under *Approved during the build*. New text goes in
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

## Rider's tools, by job

The rule is *Rider's tools first* ([development guide](../CLAUDE.md#steps-branches-and-commits)): for any job a Rider
tool covers, use it, never a hand edit, a script or a search-and-replace that does the same thing less well. The tool
names differ by Rider version, so a session starts with `/mcp` to see what it has, and runs Rider's *Auto-Configure*
(Settings → Tools → MCP Server) if the server is missing. *Brave mode* (terminal commands without a confirmation)
stays off. The research is `research/rider-mcp-tools.md` in the Claude project.

| Job | Rider 2025.2 | Rider 2026.x | Never |
|---|---|---|---|
| Format a file | `reformat_file` | `reformat_file` | re-typing the file; a Python, sed, awk or PowerShell script |
| See what the inspections flag | `get_file_problems` | `get_file_problems`, `lint_files` (all of them at once) | re-reading the file to look for problems |
| Compile | the terminal's `dotnet build` | `build_project` | — |
| Rename a symbol | `rename_refactoring` | `rename_refactoring` | search and replace |
| Before a signature change | `get_symbol_info` | `get_symbol_info`, `analyze_calls` | guessing the callers |
| The commit's files | `git_status` | `git_status` | — |

Why no script, ever (Louis, 2026-10-27): a script that added comments walked a sentence letter by letter and left 478
one-character comment lines in a test file. Code and comments change only by editing the file (an editor, Rider,
Claude Code's own edit tool); layout only through `dotnet format` or Rider's formatter; a rename only through Rider's
rename, which follows every reference ([ADR 0052](../adr/0052-edit-never-script.md)).

---
[Guides](README.md) · [Docs index](../README.md)
