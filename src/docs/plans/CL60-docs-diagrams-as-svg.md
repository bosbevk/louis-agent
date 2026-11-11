# CL60 The docs' diagrams as modern SVG, drawn by our own renderer

> **Plan, parent**, approved by Louis on 2026-11-11. Branch `refactor/CL60-svg-diagrams`. Its children each get their own plan,
> branch and PR when they start; this file changes only under *Approved during the build*.

## Context

Louis, 2026-11-11, while F127.3.1 was built: "we must replace all architecture diagrams of our docs" with a picture
like the one he attached (the engine as a card on top, identities and packs as two cards under it, each with an
outline icon, a title with a code chip and one line of description, in one rounded frame with a caption); then
"all docs with flow diagrams should get one of these also, not as is, but for the project"; "can we make all our
graphs to SVG"; "can we use something better than mermaid, it looks shit, our svg graphs must look cool"; and "we
need it all redone in SVG images but we must not use mermaid, it must look modern".

Today the docs hold **126 Mermaid diagrams in about 60 files**: 72 flowcharts, 43 sequence diagrams and 11 class
diagrams, drawn by whatever renders the markdown (GitHub, Rider), each in its own look. The site already draws its
diagrams from data with our own engine (`GraphSpec`, `GraphLayout`, `GraphView`; ADR 0103: a row grid, no crossings,
rounded connectors), which Louis shaped rule by rule on the live site.

## Louis's picks so far (2026-11-11)

| # | Question | Pick |
|---|---|---|
| 1 | The overview picture's form | One SVG per project, light and dark in the same file |
| 2 | When | Its own cleanup, CL60, with its own plan |
| 3 | All diagrams to SVG | Yes, into CL60 |
| 4 | The tool | **Our own renderer**: the framework web's diagram engine, with the layouts it lacks |
| 5 | Where a diagram's source lives | **JSON beside the doc** (`diagrams/name.json`), the SVG written beside it |
| 6 | Plans' four diagrams | **Plans use the new tool too**; this plan is the last drafted in Mermaid |

## The whole, in plain words

Every diagram in the docs becomes a small JSON file of what it shows (boxes and arrows, lifelines and messages, or
classes and their members) and an SVG our renderer draws from it, in one modern look, light or dark as the reader's
screen is. Every project doc also opens with an overview picture of its project in the card style Louis showed. A
test draws every JSON again and compares it with the SVG beside it, byte for byte, so a picture can never drift
from its source, and when the last Mermaid block is gone a test keeps it gone.

## The children, in build order

| Child | What | Done when |
|---|---|---|
| **CL60.1 The renderer and the overviews** (done; [what it decided](../projects/louis-agent.diagrams.md#what-the-build-decided-cl601-2026-11-11-partly-unattended)) | The diagram sources as records, the SVG writer with the theme, the card layout and its icons, the flow layout reused, the tool that writes every SVG, the test that holds them; an overview picture at the top of every project doc | Every project doc opens with its overview SVG, light and dark; the test fails on any SVG that doesn't match its JSON |
| **CL60.2 The flows** (done; [what it decided](../projects/louis-agent.diagrams.md#what-the-build-decided-cl602-2026-11-11-and-12-unattended)) | The 72 flowcharts redrawn as flow sources, the layout grown for what they need that the site's diagrams never did (a group around boxes, more than six columns, a note box) | No `flowchart` block left in the docs |
| **CL60.3 The sequences** ([plan](CL60.3-the-sequences.md)) | A sequence layout (lifelines, messages, replies, a note, a loop or an alt box), and the 43 sequence diagrams redrawn | No `sequenceDiagram` block left |
| **CL60.4 The classes, and the rule** | A class layout (a card per type, its members, the arrows that mean implements, holds and calls), the 11 class diagrams redrawn, the plans README's "as mermaid" rule rewritten, a test that no Mermaid block is left anywhere, an ADR | No Mermaid in the repo; the ADR records the rule |

Each child is about ten steps; CL60.2 and CL60.3 may split again if a group of docs needs more.

## Picks made in CL60.1's plan (2026-11-11, all A)

The drawing code is its own plain-C# library, `louis-agent.diagrams`; the palette is the site's own, light and dark;
the icons are Tabler Icons (MIT); an overview's cards are written by hand. See
[what CL60.1 decided](../projects/louis-agent.diagrams.md#what-the-build-decided-cl601-2026-11-11-partly-unattended).

## Approved during the build

- **CL60.1 is done** (2026-11-11, unattended): its plan is deleted on its own branch, as a done plan is, its picks and
  findings folded into [louis-agent.diagrams](../projects/louis-agent.diagrams.md) and its row in the work log; the
  links above point there. Each child after it has its own plan and its own branch, from the one before.
- **CL60.2 is done** (2026-11-12, unattended): all 75 flowcharts are pictures in Louis's joined look (a title bar,
  tones and icons, a legend, a note on every chart), held by a test that no doc holds a Mermaid flowchart. It took
  twelve steps, two over the guide's ten, since the look came mid-build. Its plan is deleted, its picks and findings
  folded into [louis-agent.diagrams](../projects/louis-agent.diagrams.md) and its row in the work log. A finding:
  the plans README asked for the architecture and object diagrams as Mermaid flowcharts, which the new test now
  refuses. Of three (rewrite that part of the rule now; let plans keep Mermaid until CL60.4, against Louis's "plans
  drawn the same way"; leave it, so the next plan fails), Claude's pick: **those two are flow pictures now**, and
  CL60.4 still rewrites the rest.

---
[Docs index](../README.md) · [Plans](README.md) · Next: [CL60.3](CL60.3-the-sequences.md)
