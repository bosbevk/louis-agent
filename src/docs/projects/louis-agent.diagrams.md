# louis-agent.diagrams — design

> **Kind:** Class library (`net10.0`), plain C# · **Path:** `src/web/louis-agent.diagrams/` · **References:**
> louis-agent.choices · **Used by:** louis-agent.web (the site's `GraphView`, and the chat's `DrawnCard`), and
> `src/tools/louis-agent.doc-diagrams`, the tool that draws the docs' pictures · **How-to:**
> [Draw a diagram](../guides/draw-a-diagram.md)

![Diagrams from data, and their parts: a diagram as JSON, two layouts, one SVG with two looks, the icons and the docs' tool](diagrams/louis-agent.diagrams.svg)

## Purpose

Every picture in the docs and every flow on the site is drawn from data, by one renderer. A diagram is a JSON file
beside its doc. This library lays it out and writes one SVG that carries a light and a dark look and loads nothing
from outside, so GitHub, Rider and a browser all show it as it is.

It is not Mermaid. Louis: "we must not use mermaid it must look modern". Our own layouts look like the site, and a
test can prove that every word fits its box.

*Decided in:* CL60, [ADR 0118](../adr/0118-a-diagram-is-data-our-renderer-draws.md).

## How it works

This section has four parts, to be read in order. Part 1 follows one picture from its file to its SVG. Part 2
goes through the five kinds of picture, one at a time. Part 3 is what every kind shares, tones and icons. Part 4
is a drawing held inside a web page, which needs care of its own.

### 1. One picture, from its file to its SVG

![How a picture is made: the JSON is read into records, a renderer per kind lays it out, and the docs' frame wraps it](diagrams/louis-agent.diagrams-flow.svg)

**Step 1: the file is read into records** (`Sources/`). `DiagramReader.Read(json)` reads a `DiagramSource`. It has
a kind, a title that a screen reader is told, an optional caption, and the kind's own part. The kind is one of
`flow`, `cards`, `sequence`, `classes` or `ticket`.

The reader refuses what is wrong, with the reason: a misspelt field, an unknown kind, a missing title, or an arrow
or a link to an id that nothing has. The reasons are the library's own messages (`Messages/DiagramMessage`), filled
by `DiagramText`, its own small stand-in for core's `MessageText`. That keeps the library from leaning on any part
of the engine.

**Step 2: the kind picks a renderer** (`Svg/`). `DiagramRenderers.Render` picks the renderer for the kind, a
Strategy. There is one per kind in `Svg/Renderers/`: `FlowRenderer`, `CardsRenderer`, `SequenceRenderer`,
`TicketRenderer`, and `ClassesRenderer`, which turns a class chart into a flow and hands it to the flow renderer.
Each renderer has a layout that places every box and word, and then draws. Part 2 explains each layout.

**How a renderer writes the file** (`SvgWriter`). It is a Builder: a renderer adds boxes, paths and words one by
one and asks for the file at the end. So no renderer writes markup or escapes text by hand, and the style and the
arrowhead every picture needs are written in one place. The file is built with `XElement` and has a `<title>`.
Every number is written in the invariant culture and every line ends in a plain `\n`, so the same drawing is the
same bytes on every machine.

**The picture carries both looks** (`Svg/Theme/`). The site's palette and its light twin are CSS variables in the
picture's own style, the dark one under `prefers-color-scheme: dark`. Each tone's fill, line and ink are there
too.

**Step 3: the docs' frame goes round it** (`Svg/TitledFrame`, a Decorator). `DiagramRenderers.RenderTitled` draws
the picture bare and then frames it: a navy title bar, a legend of the tones the chart uses, and a note bar from
its caption. Only the docs' tool asks for the frame. A bare picture, as the site draws and the chat's cards draw,
is unchanged.

#### The tool that draws the docs' pictures

![Drawing one picture: the tool reads, renders and frames it, and writes it only when it changed](diagrams/louis-agent.diagrams-sequence.svg)

`src/tools/louis-agent.doc-diagrams` finds every `diagrams/*.json` under `src/docs`, `packs/docs` and each demo's
folder. It writes the framed SVG beside each one, and only when what it draws changed.

It has two more ways to run. `--check` writes nothing and fails, naming any stale picture. `import <doc>` reads a
doc's Mermaid flowcharts, sequences and class diagrams once into JSON. It never writes over a picture already
there, and it prints the link for each block, which is swapped into the doc by hand.

The docs hold no Mermaid now. The import stays for a doc someone brings in.

*Decided in:* CL60.1, CL60.2, [ADR 0118](../adr/0118-a-diagram-is-data-our-renderer-draws.md).

### 2. The five kinds of picture

#### A flow: boxes and arrows on a grid

A flow (`Flow/`) is drawn on ADR 0103's row grid.

**Placing the boxes.** `GraphLayout` puts each box in its column and row. A box that names no column gets one from
`Ranks`: one past the furthest box that points at it. `Bands` handles groups: it stands each group's boxes in rows
of their own, inside a labelled frame.

**Drawing the arrows.** `Routing/EdgeRouter` draws an arrow as connectors at right angles. A long arrow goes
through a waypoint in each column it passes. A blocked arrow goes round a detour. The bends are as soft as the
caller asks: the site asks for 8 px, the docs for `SoftBend`.

**A chart that runs down.** It is laid out turned, and `Turn` turns every point back. So one layout and one router
serve both directions.

**The words.** `FlowText` says where a box's badge, label and note sit, and where an arrow's word chip stands. A
box may list members under a divider in the code font, as a class's box does. It is then as wide as its longest
member, and a member wraps at 60 characters.

The site's `GraphView` animates the same placed graph.

#### A sequence: who calls whom, in order

A sequence (`Sequence/`) is made of steps. A step is a message, a reply, a note, or a block that holds steps of
its own. They are a Composite of records (`Spec/Steps/`), read by their `step` field.

`SequenceLayout` stands each lifeline as far from the others as the words between them need. It works that out as
a longest path over the lifelines. `SequenceRows` then walks the steps down the page and frames each block round
what it holds, so a nested frame sits inside its parent. `SequenceText` holds the measures: words wrap at 40
characters, and a head at 24.

#### A class chart: the interface design

A class chart (`Classes/`) has types and relations. A type has a name, a kind, members, an icon and a tone. A
relation is one of `inherits`, `implements`, `holds`, `uses` or `creates`.

It has no layout of its own. `ClassFlow`, an Adapter, turns it into a flow. A type becomes a box with its members.
A type with no tone named takes its kind's: teal for an interface, a record or an enum, and green otherwise. A
relation becomes an arrow whose style and word say its kind. A word shared by several arrows into or out of one
type is shown once.

So the flow's layout, router, word chips and clearance check all serve a class chart too.

#### Cards: a project's overview

Cards (`Cards/`) are Louis's overview style. `CardLayout` lays rows of cards in one frame. A row's cards share its
width and take its tallest card's height, and a card's words wrap to fit.

Every word is in the code font. So a line's width is its length times one number, and the tests can prove it fits.

A card has a look, its `CardLook`: `plain`, `highlight` (centred, in the accent's tint) or `heading` (centred, with
no box). A link (`Cards/Links/`) runs from a card's bottom middle to the next row's top middle, at right angles,
with its arrowheads the way it says.

#### A ticket: a card an agent draws in the chat

A ticket (`Tickets/`) is a read-only card an agent draws in the chat, such as a booking request. It has a kicker
and a title on a band, an optional subtitle, a status pill in a tone, a torn edge, one to six rows of an icon, a
label and a value, and a note at the foot.

`TicketLayout` places every word. The kicker wraps beside the pill. A long value wraps, and the card grows. It is
320 wide, so a phone's chat shrinks it little.

`TicketCard` is the one translation from a tool call to a ticket. `Of` builds a ticket from its parts, with icons
and tones given by name, and a wrong name is refused with the names there are. `FromCall` reads a `ShowCard` call's
input. So the pack and the page read a call one way.

A ticket's colours are `--ticket-*` CSS variables with the site's as their fallback (`TicketStyle`), so a
business's window can set its brand on it.

*Decided in:* [ADR 0103](../adr/0103-a-diagram-is-data-on-a-row-grid.md) (flows), CL60.3 (sequences), CL60.4
(classes), CL60.1 (cards), F127.4.1 (tickets).

### 3. What every kind shares

**Tones** (`Tones/`). A box may carry an icon and a `DiagramTone`, so a colour means the same in every chart. Blue
starts it. Green is this project. Violet is the framework. Teal is contracts and state. Amber is files and data.
Red is outside the process. A box's arrows and their word chips take its tone.

**Icons** (`Icons/`). There are 36 of Tabler's outline icons (MIT), copied in as path data in `IconPaths`, with
the licence beside them and a row in `THIRD-PARTY-NOTICES.md`. It is the framework's one set of icons: the web's
`LineIconView` draws a page's line icons from it too, so a page and a diagram never hold two copies of a drawing.

*Decided in:* CL60.1, CL60.2, F134.1 (the one set).

### 4. A drawing held inside a web page

A page holds a drawing's SVG inline, so the page's colour variables reach it. But that brings a problem: a style
inside an inline SVG applies to the whole page.

`DiagramRenderers.RenderForPage` makes the page's copy (`Svg/PageSvg`) to deal with it. It does three things. It
wraps each style in `@scope`, with the root named `:scope`, since a bare `svg` inside a scope means an svg inside
it and would miss the root. It drops an arrowhead that nothing uses, so two cards on one page hold no id twice. And
it trims the empty ground round the drawing.

*Decided in:* F127.4.1.

## Diagrams

The flow and the sequence are shown in part 1, where they are explained. This is the interface design.

![The types: the reader makes a DiagramSource, its kind picks a renderer, each renderer has its layout and draws with SvgWriter, and TitledFrame wraps it](diagrams/louis-agent.diagrams-interface.svg)

## Structure

This section is for looking a file up.

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
