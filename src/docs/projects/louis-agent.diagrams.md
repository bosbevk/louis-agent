# louis-agent.diagrams — design

> **Kind:** Class library (`net10.0`), plain C# · **Path:** `src/web/louis-agent.diagrams/` · **References:**
> louis-agent.choices · **Used by:** louis-agent.web (the site's `GraphView`), and `src/tools/louis-agent.doc-diagrams`,
> the tool that draws the docs' pictures · **How-to:** [Draw a diagram](../guides/draw-a-diagram.md)

![Diagrams from data, and their parts: a diagram as JSON, two layouts, one SVG with two looks, the icons and the docs' tool](diagrams/louis-agent.diagrams.svg)

## Purpose

Every picture in the docs and every flow on the site, drawn from data by one renderer (CL60). A diagram is a JSON
file beside its doc. This library lays it out and writes one SVG that carries a light and a dark look and loads
nothing from outside, so GitHub, Rider and a browser all show it as it is. Not Mermaid (Louis: "we must not use
mermaid it must look modern"): our own layouts look like the site, and a test can prove every word fits its box.

## How it works

- **A diagram is data** (`Sources/`). `DiagramReader.Read(json)` reads a `DiagramSource` through records: a kind
  (`flow` or `cards`, parsed with `SettingChoice`), a title a screen reader is told, an optional caption, and the
  kind's own part. A misspelt field, an unknown kind, a missing title, or an arrow or a link to an id nothing has is
  refused with its reason (`Messages/DiagramMessage`, filled by `DiagramText`, the library's own small stand-in for
  core's `MessageText`).
- **Flows** (`Flow/`): ADR 0103's row grid. `GraphLayout` puts each box in its column and row, `EdgeRouter` draws an
  arrow as connectors at right angles, `FlowText` says where a box's label and note sit. The site's `GraphView`
  animates the same placed graph.
- **Cards** (`Cards/`): Louis's overview style. `CardLayout` lays rows of cards in one frame: a row's cards share its
  width and its tallest card's height, and a card's words wrap to fit. Every word is in the code font, so a line's
  width is its length times one number and the tests prove it fits. A card is `plain`, `highlight` (centred in the
  accent's tint) or `heading` (centred with no box), its `CardLook`. A link (`Cards/Links/`) runs from a card's
  bottom middle to the next row's top middle, at right angles, its arrowheads the way it says.
- **Icons** (`Icons/`): 22 of Tabler's outline icons (MIT), copied in as path data in `IconPaths`, the licence beside
  them and a row in `THIRD-PARTY-NOTICES.md`.
- **The SVG** (`Svg/`). `DiagramRenderers.Render` picks the renderer for the kind (Strategy: `FlowRenderer`,
  `CardsRenderer`). `SvgWriter` builds the file with `XElement`, `\n` line endings and a `<title>`. `Svg/Theme/`
  holds the site's palette and its light twin as CSS variables in the picture's own style, the dark one under
  `prefers-color-scheme: dark`.
- **The docs' tool** (`src/tools/louis-agent.doc-diagrams`) finds every `diagrams/*.json` under `src/docs`,
  `packs/docs` and each demo's `docs`, and writes the SVG beside it when what it draws changed. `--check` writes
  nothing and fails naming a stale picture.

## Structure

```
src/web/louis-agent.diagrams/
├── Sources/        # DiagramSource, DiagramKind, DiagramReader, DiagramJson (the one options object)
├── Flow/           # GraphSpec, GraphNode, GraphEdge, GraphLayout, EdgeRouter, FlowText; Placed/
├── Cards/          # CardsSpec, CardRow, Card, CardLook, CardLayout; Links/ (CardLink, CardLinkKind); Placed/
├── Icons/          # DiagramIcon, IconPaths, LICENSE-tabler-icons.txt
├── Svg/            # IDiagramRenderer, DiagramRenderers, FlowRenderer, CardsRenderer, SvgWriter; Theme/
├── Messages/       # DiagramMessage
└── DiagramText.cs, TextWrap.cs
src/tools/louis-agent.doc-diagrams/   # Program, DiagramSync, SyncOutcome, Messages/DocDiagramsUserText
```

## Tests

- `src/tests/web/louis-agent.diagrams.tests`: the layouts with numbers (no card overlaps another, every card inside
  the frame, every line inside its card), the reader's refusals, a card's looks, every icon drawn, and the goldens
  `Svg/flow.svg`, `Cards/cards.svg` and `Cards/looks.svg` (`LOUIS_UPDATE_GOLDEN=1` writes them anew).
- `src/tests/tools/louis-agent.doc-diagrams.tests`: the tool on a temporary folder, and `DocsDiagramsTests` on the
  real docs: every picture is what its JSON draws, a doc beside it shows it, every picture a doc shows is there, and
  every project doc shows its overview.

## What the build decided (CL60.1, 2026-11-11, partly unattended)

- **Louis's picks:** our own renderer, a JSON beside each doc, plans drawn with it too; P1 A a plain-C# library the
  web and the tool share (ADR 0092), P2 A the site's palette with its light twin mirrored (the site has no light
  theme), P3 A Tabler Icons copied in, P4 A each overview's cards written by hand from its doc; boxes in the site's
  lighter tint `#1b1d21` and labels in the body text's grey ("a bit lighter gray").
- **Louis's figures:** the engine, identities and packs (ADR 0027's picture) and the layers with the core engine
  highlighted (the building-block view's), which brought the `highlight` and `heading` looks ("we should cater for
  this alternation"). The accent is the site's blue, `#7fa8e8`, and `#3b6fc4` on a light screen.
- **Claude's picks while unattended:** a look per card rather than per row or a class per look; 22 icons for what a
  project's parts usually are; one shape for every overview (the project highlighted on top, its parts under it,
  a second row for what else it holds, no caption); a doc's links read outside code, since a how-to shows them as
  examples there.

## Limits and plans

- The docs' flows come in CL60.2, the sequences in CL60.3 and the class diagrams in CL60.4; until then those docs keep
  Mermaid. CL60.4 adds the rule and a test that no doc holds Mermaid.
- A word's width is reckoned from the code font; a reader whose screen falls back to another font may see a line run
  a little wider.
- F137 takes the same idea to the site: its cards read from JSON.

## Related docs

[Draw a diagram](../guides/draw-a-diagram.md) · [louis-agent.web](louis-agent.web.md) ·
[ADR 0103](../adr/0103-a-diagram-is-data-on-a-row-grid.md) · [ADR 0092](../adr/0092-a-project-holds-what-is-reused-together.md) ·
[CL60's plan](../plans/CL60-docs-diagrams-as-svg.md)

---
[Docs index](../README.md) · Related: [Projects](README.md) · [Guides](../guides/README.md)
