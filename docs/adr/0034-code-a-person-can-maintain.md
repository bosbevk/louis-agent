# 0034. Code a person can maintain: one concern per file, at most 200 lines, no code in razor

**Status:** Amended by [0044](0044-the-size-limit-counts-code.md) · **Date:** 2026-10-20 (Louis, during CL15's layout reviews) · **Recorded:** 2026-10-20

## Context

Most of this repository is written by agents, and an agent writes big files without cost: on 2026-10-20, 38 source
files were over 200 lines (`RunExplorer.razor` 985, `PaymoTools` 851, `AcpServer` 736) and 45 `.razor` files held an
`@code` block. Louis reviews every step and has to keep the code himself: "without this I as a human will not be able to
maintain this" and "AI likes big files". [0010](0010-folders-by-concern.md) already sorted folders by concern; it said
nothing about how big a class may grow or where a component's logic goes.

Considered: a review habit alone (rejected: a rule in a prompt states intent, and a session forgets it); 300 lines (19
files to split, but a class three screens long); 150 lines (47 files, more files to navigate); `partial` classes or
`#region` to bring files under the limit (rejected: they hide the size and split nothing).

## Decision

- **A folder, class or component holds one concern.** A folder with two kinds of thing, or more than about six files,
  gets subfolders by concern.
- **A file holds at most 200 lines**, a test file 400. A bigger class splits by responsibility: its methods grouped by
  the fields they use and the reason they would change, each group a class named for what it does, the original
  keeping only the flow; pure logic gets its own tests, and I/O stays at the edge. `Helpers`, `Utils` and `Manager`
  classes, `#region` and `partial` (except a code-behind or a source generator's, such as `[GeneratedRegex]`) don't
  count as a split.
- **A `.razor` holds markup only.** A component's C# goes in its `.razor.cs` code-behind; logic that needs no
  component goes in a plain class with NUnit tests.
- **Tests hold the rules, as ratchets:** `FileSizeTests` and `RazorHasNoCodeTests` fail on a new offender and on a
  listed one that no longer offends, so each list only shrinks.
- **A move proves itself:** the goldens and snapshots stay byte-equal, and a host no golden covers is pinned first (the
  ACP transcript, CL15 S.6a).

## Consequences

- Every review checks the folders and the sizes, not only the code; the rule is in `docs/CLAUDE.md`, so Rider sessions
  follow it unasked.
- More files, each readable on its own; a split asks for names, which is where a missing concern shows.
- 33 files stay on `Oversized` and 5 admin pages on `PendingCode` (rebuilt in Phase 3 step 7b); the rest of the list
  is a TODO line, split a group at a time.

## Links

[CL15 plan](../plans/CL15-engine-identities-packs.md) (*Approved during the build*) · [0010](0010-folders-by-concern.md)
· [Write-up 18](../learning/18-testing-and-shipping.md)

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
