# Plan: CL7 Docs tidy (cleanup)

> **For:** the TODO's *Docs tidy* (cleanup) · **Branch:** `docs/CL7-docs-tidy` · **Approved:** 2026-10-14

## Context

After CL4–CL6, three things at the top of `docs/` no longer earn their place there (Louis, 2026-10-14):

- **`docs/todo/`** isn't a task list: it holds the session-architecture picture, a 1.1 MB PNG and the same picture as a
  43 KB SVG, used only by `specs/SESSION_ARCHITECTURE.md`.
- **`AGENT_INTERACTION.md`** (how a turn works) covers the same ground as arc42's
  [runtime view](../architecture/06-runtime-view.md), which already has *A chat turn* and links to it.
- **`RUNBOOKS.md`** is about the orchestrator proof of concept only, so it belongs beside the orchestrator's project
  doc, not at the top of `docs/`.

**A move, not a rewrite.** Text moves unchanged, every link to a moved file or anchor is repointed, and a link check
runs after each step. Nothing the model sees changes.

## Steps (each one commit)

0. **This plan,** its row in the plans table, and the TODO line.
1. **The picture:** the SVG moves to `docs/specs/images/`, the PNG goes, and the spec shows the SVG.
2. **How a turn works → the runtime view:** its sections move into `architecture/06-runtime-view.md` under their own
   headings, so their anchors survive. Its *Extending* links go into the section's opening line. `AGENT_INTERACTION.md`
   goes, and its links point to the runtime view.
3. **Runbooks → `docs/projects/orchestrator-runbooks.md`,** its links repointed. The docs index and the reading chain
   are updated, and the TODO line moves to the CHANGELOG.

## Verification

- Link check after every step: zero broken links.
- Each moved doc compared line by line with its new home.
- `docs/` top level after: CLAUDE, README, GOAL, QUICK_START_AGENT, SETUP, MODELS, AGENT_COMMUNICATION, BENCHMARKS.