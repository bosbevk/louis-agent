# 0054. A move may copy whole line ranges, when a byte check proves nothing changed

**Status:** Accepted · **Date:** 2026-10-29 (Louis's pick of three, CL26 step 4) · **Recorded:** 2026-10-29

## Context

[0052](0052-edit-never-script.md) says code and comments change only by editing the file, never through a home-made
script, after a script that added comments mangled a test file. CL26 had to move 1,884 lines of Puls's `app.css`
(and 542 of the code-review app's) into area files without changing a character. Three ways were weighed: copy
whole line ranges with a command and prove the result (taken); have Claude write each file by hand, copying the text
(rejected: ~1,900 lines retyped by a model is the slip 0052 guards against); have Louis cut and paste in Rider
(rejected: a person's time on a mechanical move).

The first try showed why the proof matters: Git Bash's `sed` turned the file's CRLF line endings into LF, and the
check caught it before anything was committed.

## Decision

- **A pure move may be done by copying whole line ranges with a command** (`sed -b -n 'a,bp'`, binary mode, so no
  byte is touched), only when nothing in the moved text changes.
- **It is proved before it is committed:** the new files, joined in their order, are compared byte for byte with
  the old file (`cmp`), and the step says so. Each cut lands between top-level rules or declarations, never inside one.
- **Everything else in 0052 stands:** any change to the text itself (a rename, a comment, a fix), and the file that
  receives the list of new files (`app.css`'s imports), is made by editing, through Rider or Claude Code's edit tool.

## Consequences

- A big file can be split by concern in one step, with a proof a reviewer can trust, instead of a hand copy nobody
  can check line by line.
- The proof is part of the step: a move without its byte check is a script that rewrote a file, which 0052 forbids.

## Links

[0052](0052-edit-never-script.md), which this amends · [0053](0053-stylesheets-by-concern.md) (the split it was made
for) · [Finish a step](../guides/finish-a-step.md)

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
