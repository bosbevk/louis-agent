# 0013. Documentation: one type per doc, guides by folder, arc42 and ADRs

**Status:** Accepted · **Date:** 2026-10-14 · **Recorded:** 2026-10-14

## Context

The docs are read by people and by Claude Code, which loads the development guide into every session and reads other
docs whole. Docs that mixed a tutorial, a reference and a changelog made every question load all of them; a 510-line
guide cost ~15k tokens a session; the architecture lived in one long doc, and its decisions were scattered.

## Decision

- **One type per doc** ([Diátaxis](https://diataxis.fr), CL4): tutorial, how-to, reference or explanation, plus *plan*
  for the project's own planning docs; each opens with what it answers and where the rest is.
- **Guides by folder** (CL5): the development guide holds what every task needs; a folder's own how-tos live in its
  `CLAUDE.md`, which Claude Code loads only when working there. None in `Skills/`, which core embeds.
- **arc42, ADRs and C4** (CL6): the architecture in arc42's twelve sections in `docs/architecture/`, each lasting
  decision as an ADR here ([0001](0001-record-architecture-decisions.md)), the diagrams at C4's levels.

## Consequences

- A reader opens the kind of doc its question needs; the guide loaded every session dropped to ~6k tokens.
- A fact has one home; moving text means checking every link (a link check ran after every step).
- New text has to find its type and folder, which the docs index and the guide's rules say.

## Links

[CL4 plan](../plans/CL4-doc-types.md) · [CL5 plan](../plans/CL5-guides-by-folder.md) · [CL6 plan](../plans/CL6-arc42.md) ·
[Docs index: Writing docs](../README.md#writing-docs-one-type-per-doc)
