# 0052. Code changes only by editing; layout only through the formatter; a rename only through Rider

**Status:** Accepted (Louis, 2026-10-27 and 2026-10-28) · **Date:** 2026-10-27 (Louis, after a script that added
comments walked a sentence letter by letter and left 478 one-character comment lines in a test file); 2026-10-28
(renames: "a rename goes through Rider's rename refactoring"; Rider's tools: "rider has mcp and we already call it,
why don't we just let the agent call those?"); 2026-10-29 ("make use of the Rider MCP fully without the need to list
every tool") · **Recorded:** 2026-10-29 (CL25, from the rule text in `docs/CLAUDE.md`)

## Context

Three ways of changing source files went wrong in the same week. A home-made script (Python) that rewrote source
files to add comments broke a test file. A search-and-replace rename misses references the compiler knows about and
hits strings it shouldn't. And a session that formats by hand, or re-reads a file to look for problems, does slowly
and badly what Rider already does well: its formatter wraps lines to 120 (which `dotnet format` never does), its
inspections find the problems, its rename follows every reference. Rider offers these through its MCP server, which
Claude Code in Rider already calls, but a rule that lists each tool by name goes stale with every Rider version
(2025.2 has twenty tools; 2026.x adds `build_project`, `lint_files` and `analyze_calls`), and a session only uses
what the list names.

## Decision

- **Code and comments change only by editing the file**: an editor, Rider, Claude Code's own edit tool. Never a
  home-made script (Python, sed, awk, PowerShell) that rewrites source files.
- **Layout changes only through the formatter**: Rider's *Reformat Code* (the one that wraps to 120) and then
  `dotnet format`, which has the last word because CI checks with it. A `//` or `///` line over 120 is wrapped by
  hand, since Rider's formatter never wraps a comment (JetBrains issue RSRP-225809), as is every long line where no
  Rider runs. Only the files in a step's commit are formatted, whole; never a big reformat of its own.
- **A rename goes through Rider's rename refactoring**, which follows every reference, never search and replace.
- **In Rider, Rider's tools come first.** A session starts with `/mcp` to see what Rider offers, and for any job a
  Rider tool covers (format, inspect, build, test, rename, find usages, symbol info, git status) it uses that tool,
  never a hand edit, a script or a search-and-replace that does the same thing less well. A shell command is for
  what no Rider tool does. *Brave mode* (terminal commands without a confirmation) stays off. The tool-to-job table
  per Rider version is a how-to ([Finish a step](../guides/finish-a-step.md)), so a new Rider version changes a
  table, not this rule.

## Consequences

- A change is as good as the editor that made it: no half-applied rewrite, no missed reference.
- A cloud session, with no Rider, wraps long lines by hand and runs `dotnet format`; it never writes a script for it.
- The step list in `docs/CLAUDE.md` is one rule instead of a tool list, and the guide keeps the names.

## Links

[0034](0034-code-a-person-can-maintain.md) · [Finish a step](../guides/finish-a-step.md) · `research/rider-mcp-tools.md`
in the Claude project · [JetBrains RSRP-225809](https://youtrack.jetbrains.com/issue/RSRP-225809)

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
