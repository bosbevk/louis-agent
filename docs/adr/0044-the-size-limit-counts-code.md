# 0044. The size limit counts code: blank and comment lines are free

**Status:** Accepted · **Date:** 2026-10-27 (Louis, on F18's formatting commit: "we must maybe increase linecount to
250?") · **Recorded:** 2026-10-27

## Context

[0034](0034-code-a-person-can-maintain.md) caps a file at 200 lines, a test file at 400, counting every line. On
2026-10-27 the rules started asking for more lines that hold no logic: a summary on every class and method, with
`<remarks>` and `<seealso>` on a class; a `//` line on a step that isn't obvious; blank lines between groups of steps;
one property per line in an initializer; wrapping at 120 columns. The first formatting pass pushed four files over 200
(`ProtocolWriter`, `EvalRunner`, `AgentEngine`, `RiderMcpClient`) without one new line of code. 33 source files were
over 200 lines, 16 over 250.

Considered: keep 200 and split whatever the comments push over (rejected: a split for comments splits no concern);
raise the limit to 250 and 500 (rejected: the same comments keep filling the new room, and a class could hold more
logic than before); count code lines only, as `cloc` counts code, comment and blank lines apart (taken).

## Decision

- **The limit counts code lines:** 200 in a source file, 400 in a test file. A blank line, or a line whose text starts
  with `//` (a `///` doc line included), doesn't count. A code line with a comment after it counts.
- Everything else in 0034 stands: one concern per file, a bigger class splits by responsibility, the ratchet lists.

## Consequences

- Writing a summary, a remark or a blank line never forces a split; the limit catches a class with too much logic,
  which is what 0034 wanted.
- A file can read longer than 200 lines on screen; its comments are what make the extra length worth reading.
- `FileSizeTests` counts the same way, and the files that drop under the limit leave its `Oversized` list in the same
  commit.

## Links

[0034](0034-code-a-person-can-maintain.md) · `docs/CLAUDE.md`, *Separate by concern* · [F18 plan](../plans/F18-evals.md)

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
