# ADR 0118: A diagram is data our renderer draws

**Status:** Accepted (2026-11-11, Louis's picks for CL60: no Mermaid, our own renderer); held since CL60.4
(2026-11-12) by a test that no doc holds Mermaid.

## Context

The docs drew their diagrams in Mermaid: a code block GitHub turns into a picture its own way. The pictures came out
grey, cramped and different on every screen, and a flowchart with thirty boxes had lines through its words. The site
already drew its diagrams from data with our own engine (`louis-agent.diagrams`, [ADR 0103](0103-a-diagram-is-data-on-a-row-grid.md)),
so the docs had two ways to draw the same kind of thing, and the worse one was the one people read most.

Louis's picks for CL60 (2026-11-11): no Mermaid, our own renderer, the JSON beside each doc with its SVG beside it,
light and dark in one file, and plans drawn the same way. CL60.1 to CL60.4 built the kinds the docs need (flow, cards,
sequence, classes) and turned all 75 flows, 45 sequences and 11 class charts into pictures.

## Decision

- **A diagram in a doc is a JSON file our renderer draws**, in a `diagrams/` folder beside the doc, its SVG beside the
  JSON, shown with a plain image link. The docs' tool (`louis-agent.doc-diagrams`) redraws every picture whose JSON
  changed.
- **Four kinds cover the docs:** `flow` (boxes and arrows, the architecture and object diagrams), `cards`,
  `sequence` (who calls whom, in order) and `classes` (types, their members and how they relate; drawn as a flow).
- **No Mermaid anywhere**: no doc holds a Mermaid block of any kind. `NoDoc_HoldsMermaid` fails on one.
- **A plan's four diagrams are pictures too:** the interface design a `classes` picture, the architecture and the
  object diagram `flow` pictures, the communication a `sequence` picture.

## Consequences

- Every diagram looks the same on GitHub, in an editor and on the site, light and dark, with our tones and icons.
- A picture is checked by tests: no word leaves the drawing, no line crosses a box it doesn't join, every picture is
  shown by a doc and every link points at one.
- Drawing a diagram means writing JSON, not a few lines of Mermaid. An old Mermaid block can still be imported once
  (the tool's `import`), and then the JSON is the source.
- A diagram the renderer can't lay out well (thirty types with crossing arrows) is split into smaller pictures rather
  than drawn badly.
- The renderer is ours to keep: a new kind of diagram is a change to `louis-agent.diagrams`, with its tests.

## Links

[ADR 0103](0103-a-diagram-is-data-on-a-row-grid.md) (the site's diagrams from data) · [ADR 0013](0013-documentation-structure.md) ·
[Draw a diagram](../guides/draw-a-diagram.md) · the [louis-agent.diagrams](../projects/louis-agent.diagrams.md)
design doc · CL60 in the [work log](../work-log.md)

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
