# louis-agent.diagrams — design

> **Kind:** Class library (`net10.0`), plain C# · **Path:** `src/web/louis-agent.diagrams/` · **References:**
> louis-agent.choices · **Used by:** louis-agent.web (the site's `GraphView`, and the chat's `DrawnCard`), and
> `src/tools/louis-agent.doc-diagrams`, the tool that draws the docs' pictures · **How-to:**
> [Draw a diagram](../guides/draw-a-diagram.md)

![Diagrams from data, and their parts: a diagram as JSON, two layouts, one SVG with two looks, the icons and the docs' tool](diagrams/louis-agent.diagrams.svg)

## Purpose

Every picture in the docs and every flow on the site, drawn from data by one renderer (CL60). A diagram is a JSON
file beside its doc. This library lays it out and writes one SVG that carries a light and a dark look and loads
nothing from outside, so GitHub, Rider and a browser all show it as it is. Not Mermaid (Louis: "we must not use
mermaid it must look modern"): our own layouts look like the site, and a test can prove every word fits its box.

## How it works

- **A diagram is data** (`Sources/`). `DiagramReader.Read(json)` reads a `DiagramSource` through records: a kind
  (`flow`, `cards`, `sequence`, `classes` or `ticket`, parsed with `SettingChoice`), a title a screen reader is told, an optional caption, and the
  kind's own part. A misspelt field, an unknown kind, a missing title, or an arrow or a link to an id nothing has is
  refused with its reason (`Messages/DiagramMessage`, filled by `DiagramText`, the library's own small stand-in for
  core's `MessageText`).
- **Flows** (`Flow/`): ADR 0103's row grid. `GraphLayout` puts each box in its column and row (`Ranks` gives a box
  with no column one past the furthest box pointing at it), `Bands` stands each group's boxes in rows of their own
  inside a labelled frame, and `Routing/EdgeRouter` draws an arrow as connectors at right angles: a long one through
  a waypoint in each column it passes, a blocked one round a detour, with bends as soft as the caller asks (the site
  8 px, the docs `SoftBend`). A chart that runs `down` is laid out turned and `Turn` turns every point back, so one
  layout and one router serve both. `FlowText` says where a box's badge, label and note sit and where an arrow's word
  chip stands. A box may list members under a divider in the code font (a class's), as wide as its longest member,
  which wraps at 60 characters. The site's `GraphView` animates the same placed graph.
- **Tones** (`Tones/`): a box may carry an icon and a `DiagramTone`, so a colour means the same in every chart
  (blue starts it, green this project, violet the framework, teal contracts and state, amber files and data, red
  outside the process). Its arrows and their word chips take its tone.
- **Sequences** (`Sequence/`): who calls whom, in order. The steps are a Composite of records (`Spec/Steps/`: a
  message, a reply, a note, a block holding steps of its own), read by their `step` field. `SequenceLayout` stands
  each lifeline as far from the others as the words between them need (a longest path over the lifelines), and
  `SequenceRows` walks the steps down the page, framing each block round what it holds, so a nested frame sits
  inside its parent. `SequenceText` holds the measures: words wrap at 40 characters, a head at 24.
- **Classes** (`Classes/`): the interface design, its types (a name, a kind, members, an icon, a tone) and their
  relations (`inherits`, `implements`, `holds`, `uses`, `creates`). `ClassFlow`, an Adapter, turns a class chart into
  a flow: a type a box with its members, its tone from its kind when none is named (teal for an interface, a record
  or an enum, green otherwise), a relation an arrow whose style and word say its kind; a word shared by several arrows
  into or out of one type shows once. So the flows' layout, router, chips and clearance check all serve it.
- **Cards** (`Cards/`): Louis's overview style. `CardLayout` lays rows of cards in one frame: a row's cards share its
  width and its tallest card's height, and a card's words wrap to fit. Every word is in the code font, so a line's
  width is its length times one number and the tests prove it fits. A card is `plain`, `highlight` (centred in the
  accent's tint) or `heading` (centred with no box), its `CardLook`. A link (`Cards/Links/`) runs from a card's
  bottom middle to the next row's top middle, at right angles, its arrowheads the way it says.
- **Tickets** (`Tickets/`, F127.4.1): a read-only card an agent draws in the chat, such as a booking request: a
  kicker and a title on a band, an optional subtitle and a status pill in a tone, a torn edge, one to six rows of an
  icon, a label and a value, and a note at the foot. `TicketLayout` places every word (the kicker wraps beside the
  pill, a long value wraps and the card grows), 320 wide so a phone's chat shrinks it little. `TicketCard` is the one
  translation from a tool call to a ticket: `Of` builds it from its parts (icons and tones by name, refused with the
  names there are) and `FromCall` reads a `ShowCard` call's input, so the pack and the page read a call one way.
- **Icons** (`Icons/`): 38 of Tabler's outline icons (MIT), copied in as path data in `IconPaths`, the licence beside
  them and a row in `THIRD-PARTY-NOTICES.md`. Since F134.1 it is the framework's one set of icons (Louis's pick of
  three): the web's `LineIconView` draws a page's line icons from it, so a page and a diagram never hold two copies
  of a drawing.
- **The SVG** (`Svg/`). `DiagramRenderers.Render` picks the renderer for the kind (Strategy, `Svg/Renderers/`:
  `FlowRenderer`, `CardsRenderer`, `SequenceRenderer`, `ClassesRenderer`, which hands `ClassFlow`'s flow to the
  flow renderer, and `TicketRenderer`). `SvgWriter` builds the file with `XElement`, `\n` line endings and a
  `<title>`. `Svg/Theme/` holds the site's palette and its light twin as CSS variables in the picture's own style,
  the dark one under `prefers-color-scheme: dark`, and each tone's fill, line and ink; `TicketStyle` adds a ticket's
  rules, every colour a `--ticket-*` variable with the site's as its fallback, so a business's window sets its brand.
- **A drawing for a page** (`Svg/PageSvg`, `DiagramRenderers.RenderForPage`): a page holds the SVG inline, so its
  colour variables reach it, but a style inside an inline SVG is the whole page's. The page's copy wraps each style in
  `@scope` with the root named `:scope` (a bare `svg` inside a scope means an svg inside it), drops an arrowhead
  nothing uses, so two cards hold no id twice, and trims the empty ground round the drawing.
- **The docs' frame** (`Svg/TitledFrame`, a Decorator): `DiagramRenderers.RenderTitled` draws the picture bare and
  frames it with a navy title bar, a legend of the tones the chart uses, and a note bar from its caption. Only the
  docs' tool asks for it, so a bare picture (the site, the chat's tool cards later) is unchanged.
- **The docs' tool** (`src/tools/louis-agent.doc-diagrams`) finds every `diagrams/*.json` under `src/docs`,
  `packs/docs` and each demo's folder, and writes the framed SVG beside it when what it draws changed. `--check`
  writes nothing and fails naming a stale picture. `import <doc>` (`Mermaid/`, its readers in `Mermaid/Readers/`)
  reads a doc's Mermaid flowcharts, sequences and class diagrams once into JSON, never over a picture already there,
  and prints the link for each block, which is swapped by hand. The docs hold no Mermaid now
  ([ADR 0118](../adr/0118-a-diagram-is-data-our-renderer-draws.md)); the import stays for a doc someone brings in.

## Diagrams

![How a picture is made: the JSON is read into records, a renderer per kind lays it out, and the docs' frame wraps it](diagrams/louis-agent.diagrams-flow.svg)

![Drawing one picture: the tool reads, renders and frames it, and writes it only when it changed](diagrams/louis-agent.diagrams-sequence.svg)

![The types: the reader makes a DiagramSource, its kind picks a renderer, each renderer has its layout and draws with SvgWriter, and TitledFrame wraps it](diagrams/louis-agent.diagrams-interface.svg)

## Structure

```
src/web/louis-agent.diagrams/
├── Sources/        # DiagramSource, DiagramKind, DiagramReader, DiagramJson (the one options object)
├── Flow/           # GraphLayout, Ranks, Bands, Turn, FlowText; Spec/, Placed/, Routing/ (EdgeRouter, Detours, Plan)
├── Cards/          # CardsSpec, CardRow, Card, CardLook, CardLayout; Links/ (CardLink, CardLinkKind); Placed/
├── Sequence/       # SequenceLayout, SequenceRows, SequenceText; Spec/ (Steps/), Placed/
├── Classes/        # ClassFlow; Spec/ (ClassesSpec, ClassType, ClassRelation, RelationKind)
├── Tickets/        # TicketLayout, TicketCard; Spec/ (TicketSpec, TicketRow, TicketStatus), Inputs/, Placed/
├── Tones/          # DiagramTone
├── Icons/          # DiagramIcon, IconPaths, LICENSE-tabler-icons.txt
├── Svg/            # IDiagramRenderer, DiagramRenderers, SvgWriter, TitledFrame, PageSvg; Renderers/, Theme/
├── Messages/       # DiagramMessage
└── DiagramText.cs, TextWrap.cs
src/tools/louis-agent.doc-diagrams/   # Program, DiagramSync, SyncOutcome, Mermaid/ (the import; Readers/), Messages/
```

## Tests

- `src/tests/web/louis-agent.diagrams.tests`: the layouts with numbers (no card overlaps another, every card inside
  the frame, every line inside its card; a flow's ranks, a down chart's rows, every group's boxes inside its frame
  and no other, a chip clear of its line; a sequence's heads apart, its words between their lifelines, its frames
  round their steps and nested inside their parents; a box's members and tones in `Flow/Boxes/`; a class chart's
  boxes, arrows and refusals in `Classes/`), the tones and the frame, the reader's refusals, a card's looks, every
  icon drawn, a ticket's refusals, layout and translation from a call (`Tickets/`), a page's copy scoped, rooted and
  trimmed (`Svg/PageSvgTests`), and the goldens `Svg/flow.svg`, `Flow/down.svg`, `Flow/groups.svg`,
  `Sequence/sequence.svg`, `Classes/classes.svg`, `Cards/cards.svg`, `Cards/looks.svg` and `Tickets/ticket.svg`
  (`LOUIS_UPDATE_GOLDEN=1` writes them anew).
- `src/tests/tools/louis-agent.doc-diagrams.tests`: the tool on a temporary folder, the import on samples of each
  Mermaid form, and `DocsDiagramsTests` on the real docs: every picture is what its JSON draws, a doc shows it,
  every picture a doc shows is there, every project doc shows its overview and all four kinds of picture
  (`EveryProjectDoc_ShowsItsFourDiagrams`), every flow and class chart keeps its
  words and lines clear (`Clearance`), every sequence keeps its words inside its drawing, every `diagrams` folder is
  one the tool draws, and no doc holds Mermaid (`NoDoc_HoldsMermaid`).

## What the build decided (CL60.1, 2026-11-11, partly unattended)

- **Louis's picks:** our own renderer, a JSON beside each doc, plans drawn with it too; P1 A a plain-C# library the
  web and the tool share (ADR 0092), P2 A the site's palette with its light twin mirrored (the site has no light
  theme), P3 A Tabler Icons copied in, P4 A each overview's cards written by hand from its doc; boxes in the site's
  lighter tint `#1b1d21` and labels in the body text's grey ("a bit lighter gray"); the library's texts through its
  own `DiagramText`, so it leans on no part of the engine; three `resharper_indent_nested_*` keys in `.editorconfig`,
  so Rider indents a nested loop as `dotnet format` does.
- **Louis's figures:** the engine, identities and packs (ADR 0027's picture) and the layers with the core engine
  highlighted (the building-block view's), which brought the `highlight` and `heading` looks ("we should cater for
  this alternation"). The accent is the site's blue, `#7fa8e8`, and `#3b6fc4` on a light screen.
- **Claude's picks while unattended:** a look per card rather than per row or a class per look; 22 icons for what a
  project's parts usually are; one shape for every overview (the project highlighted on top, its parts under it,
  a second row for what else it holds, no caption); a doc's links read outside code, since a how-to shows them as
  examples there.

## What the build decided (CL60.2, 2026-11-11 and 12, unattended)

- **Claude's picks, each of three:** the 75 docs flowcharts converted by a one-time import, each JSON looked at and
  the block swapped by hand (not every JSON by hand, nor a renderer that reads Mermaid); a chart that runs down laid
  out turned and turned back (not a second layout); a subgraph a labelled frame with its boxes in a band of their own
  (swimlanes); the import writes only JSON, never the doc.
- **The layout grew by what the charts needed:** a long arrow through a waypoint per column (Sugiyama's dummy nodes),
  straightened by Brandes and Köpf's alignment; several sources into several boxes on tracks of their own; a blocked
  arrow round a detour; a back arrow from its source's bottom middle; a chart over 1,100 px turned the narrower way
  on import; a down chart as wide as what it draws. `DocsDiagramsTests`' clearance check came from Louis's "you
  must look at each one", and every picture was looked at.
- **Louis's picks:** "Ours plus hand-tuning" (not MSAGL as a package, nor growing the layout until every chart
  passes): what reads awkwardly is tuned in its JSON, a frame dropped, arrows merged, a word shortened; **the joined
  look** from the renderer (not ChatGPT-style painted pictures, nor hand-written SVGs): a title bar, an icon badge
  and a tone per box, arrows and word chips in their box's tone, soft bends, a note bar; "all of them must look like
  accounts" (every chart toned, by one meaning per colour); a legend ("can we have a legend for the color"); a note
  on **every** chart, its line moved out of the doc so nothing is said twice; the plans drawn in the full look too.
- **Claude's picks on findings:** the frame a Decorator (`TitledFrame`), so the cards and the site stay bare; the
  legend drawn from the tones' fixed meanings, not written in each JSON; violet's legend "the framework"; the
  README's picture in `src/docs/diagrams/`, linked by its path; the tool scans each demo whole, so a deploy note's
  pictures are drawn and checked too.

## What the build decided (CL60.3, 2026-11-12, unattended)

- **Claude's picks, each of three:** a step's kinds as a Composite of records read by System.Text.Json's
  polymorphism (not one record with every field optional, nor separate lists joined by row numbers); a long message
  wrapped at 40 characters (not one line, nor cut); a participant drawn as a flow's box with its icon and tone (not
  plain heads, nor heads at the bottom too); a docs-wide check like the flows'.
- **Found while looking at each chart:** a message's words stand in a chip in its sender's tone, so a lifeline they
  cross runs behind them; a frame's tabs stand over the lifelines, its panel under them; the import never writes over
  a picture already there; a block's kind is always written, though loop is the default; a message keeps its
  generics, since Mermaid reads no tags in one.
- **Louis's asks during it:** the plan's interface design drawn as a picture too; an interface diagram, then all four
  diagrams (architecture, flow, sequence, interface), on every project doc, which became CL60.5.

## What the build decided (CL60.4, 2026-11-12, unattended)

- **Claude's picks, each of three:** a class chart drawn as a flow through an Adapter (`ClassFlow`), so the router,
  tones, chips and checks all apply (not a class layout of its own, nor the card layout); a type's members under a
  divider in its box (not as its note, nor left out); a relation's kind shown by its arrow's style and a word (not
  UML's triangles and diamonds); one test that no doc holds Mermaid and an ADR (0118) for the rule.
- **Found while looking at each chart:** a type with no tone named takes its kind's (teal for contracts and state,
  green otherwise); a long member wraps at 60 characters; a word shared by several arrows into or out of one type
  shows once, since the fan-ins piled up identical chips; the evals' nine check classes are one type; F39's and F40's
  charts are split in two, since whole they were tangles; Mermaid's `%%` comments, `;` and `$` marks the import kept
  were cleaned by hand in the data.

## What the build decided (CL60.5, 2026-11-12, unattended)

- **Claude's picks, each of three:** the overview counts as a project's architecture, beside a flow, a sequence and a
  classes picture (not a fifth picture repeating it); the test reads each picture's kind (not a role field, nor fixed
  headings); a pending list that only shrank, step by step, deleted at the end; an interface of the types a user or
  extender meets, about twelve at most (not every public type, nor interfaces only).
- **Found while drawing them:** the overview test had never seen the demos' project docs, since a demo's root is the
  whole demo; both tests now find any `projects` folder. Two arrows into one interface from the same column stack
  their word chips; the clearance test catches it and the chart is tuned (the implementer names the interface in its
  kind line). Each interface was written from the code and checked against it, which caught a picture naming the
  docs pack's tools by an old plan's names.
- **What it added:** 42 interface pictures, 8 flows and 13 sequences, all looked at.

## What the build decided (F127.4.1, 2026-11-12, partly unattended)

- **Louis's picks:** the first design's option cards, and the booking summary a read-only ticket "the agents must
  draw themselves with our diagram tool": a `ticket` kind here, filled through `ShowCard`'s typed parameters (Q1); its
  colours CSS variables with the site's as fallbacks (Q3); on the 390 px check, the page's copy trimmed and the ticket
  laid out 320 wide (not a measured width, nor bigger fonts on phones).
- **Claude's picks, each of three:** one translation from a call to a ticket (`TicketCard`) in the library the pack's
  tests and the web both use; the page holds the SVG inline with its styles in `@scope` (not an `img`, which keeps the
  brand's colours out, nor a shadow root, which needs a script per card).
- **Found on the live check:** inside `@scope` a bare `svg` rule misses the root, so the first card drew black; the
  page's copy names it `:scope`, and a test holds it.

## Limits and plans

- A class chart over about twenty types tangles its arrows, since the layout doesn't reduce crossings; such a chart is
  split in two pictures. Wide charts stay: F106.1, F92 and F40's agent side among the plans, core's and the recipes
  pack's interfaces (about 2,600 px) among the projects.
- Sequences with eight participants run about 2,000 px wide (the API's and core's turns, F39's fix, F40's run), since
  their names don't break; they read clearly, small in a doc's column.
- **Two back arrows at one box cross.** When one leaves a box's bottom and another comes into its side, their loops
  cross and a word lands on a line; the clearance test catches it. F106's plan keeps one loop whose word carries
  both reasons, until a chart needs the router to nest them.
- Three plan charts (F40's playground, F39's and F106.3's architecture) stay about 2,000 px wide, small in a doc's
  column, since their frames say who owns what.
- A word's width is reckoned from the code font; a reader whose screen falls back to another font may see a line run
  a little wider.
- F137 takes the same idea to the site: its cards read from JSON.

## Related docs

[Draw a diagram](../guides/draw-a-diagram.md) · [louis-agent.web](louis-agent.web.md) ·
[ADR 0103](../adr/0103-a-diagram-is-data-on-a-row-grid.md) ·
[ADR 0118](../adr/0118-a-diagram-is-data-our-renderer-draws.md) · [ADR 0092](../adr/0092-a-project-holds-what-is-reused-together.md) ·
CL60 in the [work log](../work-log.md)

---
[Docs index](../README.md) · Related: [Projects](README.md) · [Guides](../guides/README.md)
