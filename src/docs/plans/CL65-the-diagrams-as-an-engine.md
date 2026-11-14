# CL65: The diagrams as an engine

> **Plan.** A cleanup, a parent with four children. The diagrams become two projects: what a diagram is, and the
> engine that builds one. The engine puts every picture together with one builder, every picture says what type
> of diagram it is, and what the old looks left behind goes.

**Status:** approved 2026-11-14 (Louis: "approve go unattended"). **Branch:** `feature/F134.1-the-start-is-a-stage`,
since the work rests on F134.7 to F134.13, which are built there and not merged. **Children:** each has its own
plan, written when it starts.

## Context

Louis, 2026-11-14, as the last kind was redone: "when done do a project refactor". Then, while the work before it
was built, four things about it, in his words:

- "read here when you refactor, our graph types must match", with GeeksforGeeks' introduction to UML.
- "when we come to the refactoring, think of it as an diagram engine so we can split it into engine -->
  engine.diagrams of some sort."
- ".diagrams must be split into a diagrams --> and what builds the diagrams."
- "the type of diagram must always display."

**What was looked up.**

- *The project today.* `louis-agent.diagrams` holds about 5,600 lines of code in 54 folders. About 700 lines say
  what a diagram is: the JSON records of each kind, the reader and its checks, the tones and the refusals. About
  4,900 build one: layouts, routing, drawings, keys, looks and the SVG writer.
- *Which way it depends.* No record uses a layout or a drawing. The one thread across is small: the reader's
  messages and the drawings' words are one enum, with one internal formatter.
- *Who uses it.* The framework's web, the docs' tool, and one pack's tests. The web uses both sides.
- *How six kinds put a picture together.* Each kind's drawing has an `Of` method that does the same eight things
  in the same order: place, width, the key's layout, the light and the title, the body, the key, the caption,
  the styles and the write. Four of the eight are the same everywhere and written six times. The ticket is its
  own.
- *The kinds against UML's types.* `sequence`, `classes` and `composite` are one UML type each. `flow` draws
  several: of the docs' 91, about 13 are objects in memory, about 20 architecture, 6 have decisions, and 43 could
  not be told by their file's name. `components` is named like UML's component diagram and shows a composite
  structure. `cards` and `ticket` are not UML.
- *The rule the split must keep.* ADR 0092: a plain name sits below and references nothing heavy, and a dotted
  name sits on the thing before the dot. So `louis-agent.diagrams` and `louis-agent.diagrams.engine` are that
  rule applied. No ADR is added or changed.
- *What the old looks left behind,* listed in [F134's plan](F134-the-showroom-looks-modern.md) as each kind was
  redone.

**His picks.** Four findings, three options each.

1. *Two projects* (of: two projects, one project with two folders, three projects).
2. *`louis-agent.diagrams` is what a diagram is, `louis-agent.diagrams.engine` is what builds it, both in
   `src/web/`* (of: these names; the engine keeping the plain name; these names in a home of their own).
3. *One fluent picture builder, each kind's renderer its director,* after his "then use builder pattern for
   implementation" (of: this; a builder a kind with one director; builders on both sides).
4. *A picture states its type* (of: this; a kind a UML type; fixing the one wrong name only). A kind stays a
   layout. A kind with one type has it filled in. A `flow` must say its own, and the reader refuses one that
   doesn't.

**Picked for him, and his to overrule.** `cards` shows "Overview", since UML has no name for it. `ticket` shows
nothing: it is a card in a visitor's chat, not a diagram in a doc.

**What could not be confirmed.** The UML type of 43 flows. CL65.3 reads each one. And whether the `components`
kind keeps its name: under pick 4 a kind's name is a layout's, so it need not be UML's, but it reads as UML's
component diagram. CL65.4 puts that to him.

## Design

**Two projects, one way.** `louis-agent.diagrams` says what a diagram is: its kinds, the records each kind's JSON
reads into, the reader and what it refuses, the tones and the icons' names. It references `louis-agent.choices`
and nothing else. `louis-agent.diagrams.engine` builds a diagram: every layout, the routing, the drawings, the
keys, the looks and the SVG writer. It references `louis-agent.diagrams`. A test holds that the first never
names the second. So something that only writes or reads a diagram's JSON, a pack's tool or an agent, takes the
small project and no drawing code.

**One builder.** `PictureBuilder` puts a picture together: its title, its type, its body, its key, its caption
and its styles, then `Build()`. It owns what every kind did the same way: the room above the drawing, the
picture's width, the light, where the caption stands, the order of the styles. A kind's renderer is the
director: it places with its own layout and hands the builder its body and its key. The kinds stay a Strategy,
one renderer picked by kind.

**A type on every picture.** A picture's JSON has a `type`, one of UML's names, or `overview`. The builder's type
step writes it at the right of the title's line, where the composite's stands today.

**What stays.** Every layout, every look, every picture's shapes. Children 1, 2 and 4 change no picture: the
docs' tool must write none, which is the proof. Child 3 changes every picture by one line of text, its type.

### Interface design

![The two projects' types: the source and its kinds on one side, the renderers, the builder and the writer on the other](diagrams/CL65-the-diagrams-as-an-engine-interface-design.svg)

### Architecture

![Four users, two projects: the engine references the diagrams, and the diagrams reference only the enum-name parser](diagrams/CL65-the-diagrams-as-an-engine-architecture.svg)

### Communication (one picture is built)

![The tool reads a picture with the diagrams' reader, and the engine's renderer directs the builder through its steps](diagrams/CL65-the-diagrams-as-an-engine-communication-one-picture-is-built.svg)

### Objects (one flow, being built)

![One source with its type, one placed graph, and one builder holding the title, the type, the body, the key and the styles](diagrams/CL65-the-diagrams-as-an-engine-objects-one-flow-being-built.svg)

## The children

Each is its own plan, its own done-when and its own write-up, in this order.

| Child | What it does | Done when |
|---|---|---|
| **CL65.1 Two projects**, built 2026-11-14 | Moves what builds a diagram into `louis-agent.diagrams.engine`. Splits the one message enum in two. Their tests follow, in two projects. | `louis-agent.diagrams` holds no layout and no drawing, a test holds the direction, and the docs' tool writes no picture. |
| **CL65.2 The picture builder**, built 2026-11-14 | `PictureBuilder`, and the six drawings built through it. The light, drawn in three places, and the keys' placing, written four times, become one each. The renderers get a folder that fits them. | No kind writes its own title, light or caption, and the docs' tool writes no picture. |
| **CL65.3 Every picture says its type** | The `type` in the JSON and the reader, the builder's step, each of the 91 flows read and given its type, the guide. | Every picture in the docs shows its type, and a flow without one is refused. |
| **CL65.4 What the old looks left** | The old rules in the shared style and the frame's. The two layout rules nothing draws by. The long lines out of the class that gives a box its column. One folder for the kinds' styles. One rule for a word's pill. The diagrams' own interface picture. The `components` kind's name, put to Louis. | The list in F134's plan is empty, and the docs' tool writes no picture. |

## Verification

- Each child runs `louis-agent.diagrams.tests`, the engine's tests once they exist, `louis-agent.doc-diagrams.tests`
  from its first step, `louis-agent.web.tests`, and `louis-agent.core.tests` when files or projects move.
- Children 1, 2 and 4: `dotnet run --project src/tools/louis-agent.doc-diagrams` writes no picture, and no golden
  changes.
- Child 3: every changed picture differs from the last commit's by its type's line and nothing else, checked by
  script, and a sheet of each kind is looked at.
- The full suite of unit tests runs before the pull request, with `main` merged in. No paid run.

## Concepts

- CL65.1: the Common Reuse Principle (Robert Martin, ADR 0092) and a Separated Interface (Fowler): nobody takes
  what they don't use, and the small project is what both sides agree on. Skill 20 (extensibility).
- CL65.2: Builder (Gang of Four, Refactoring.Guru) with a Strategy as its director: the steps written once,
  what goes into them picked by kind. Skill 20.
- CL65.3: a kind is how a picture is laid out, a type is what it shows, and data says which. Skill 5 (structured
  outputs).
- Children 1, 2 and 4: a refactor proved by an output that does not change by a byte. Skill 18 (testing and
  shipping).

## Approved during the build

- **2026-11-14, unattended from the start.** Louis: "approve go unattended". No stop after a step or a child;
  every pick is recorded in the child's plan, and a child's plan is written and committed before its first step.
- **2026-11-14, CL65.1 is built.** 130 files moved to `louis-agent.diagrams.engine` and 75 stayed. The one
  message enum is two. A rule test holds the direction. No picture changed. Its picks are in its plan as of
  the commit before the one that deleted it, and its lessons in
  [the write-up](../learning/18-testing-and-shipping.md). What it left for CL65.4: one test project tests both
  projects, and one design doc covers both.
- **2026-11-14, after CL65.** Louis, while CL65.1 started: "then after this finish F134.1."
- **2026-11-14, a fifth child: CL65.5, the pictures a project needs, and where they stand.** Louis, during
  CL65.2: "the project type and requirements must drive which diagrams is required", "if diagrams is missing
  add them", and "graphs must also be placed where it is used ort needed not a section".
  - *His pick of three:* the project's type sets the floor, and its requirements add. A fixed table gives each
    type of project its required diagrams, a doc names what its own requirements add, and a test holds both.
    The other two were the type alone, and each doc's own list alone.
  - *The floor proposed to him, which he did not change:* every project an overview and a class diagram; a
    library with a main path an activity and a sequence diagram; a host or a program a sequence and a
    deployment diagram; a web app or Razor library a component diagram; a console tool an activity diagram.
  - *What it replaces:* the one rule that asks every project doc for the same four kinds. Today 39 of 42
    project docs keep 108 pictures in a *Diagrams* section. That section goes, and each picture stands beside
    the text it explains. The projects' index and the docs' rules name that section and change with it.
  - *It follows CL65.3,* since a test can only ask for a type of diagram once every picture states its type.
  - *Done when* every project doc states its type from a fixed list, shows what its type and its own list
    ask for, and has no *Diagrams* section.
- **2026-11-14, a sixth child: CL65.6, the engine's arithmetic as small types.** Louis, stopping CL65.2 to
  ask: "with all these math in the egine, can we not have something like a calculator which does it neatly."
  Measured: 275 named numbers in 49 files, text measured by its length in 19 places with 12 different widths
  of a character, a box's middle or edge worked out inline in 56 places, and 20 path strings built by hand.
  - *His pick of three:* small types that carry their own sums, a box that knows its middle and edges, a face
    that knows how wide words are in it, a small writer for a path. The other two were one calculator class,
    which is the grab-bag our rule forbids, and a layout that works out every place from what stacks under
    what.
  - *Done when* no layout or drawing measures words by a number of its own or works out a box's edge inline.
  - *Whose types: Microsoft's.* He asked "is there not an arithmic calculator interface in MS". Microsoft's
    docs were read: generic math gives a type of ours its operators and does no sums itself;
    `System.Drawing.RectangleF` and `System.Numerics.Vector2` have a box's and a point's sums ready-made, in
    single precision. His pick of three: those types, over our own in double precision, which I had
    recommended, and our own generic over the number. Both ship with .NET itself, so no package is added. A
    number that passes through single precision may change in its last digits, so this child cannot be proved
    by no picture changing: each picture that changes is compared and looked at. Words still need a type of
    ours, since nothing of Microsoft's measures them without a font.
- **2026-11-14, CL65.2 is built.** One builder puts six kinds' pictures together, one placer a key's rows, and
  each renderer stands beside its drawing. No picture changed. Its picks are in its plan as of the commit
  before the one that deleted it, and its lessons in [the write-up](../learning/18-testing-and-shipping.md).

---
[Docs index](../README.md) · Related: [F134](F134-the-showroom-looks-modern.md) ·
[louis-agent.diagrams](../projects/louis-agent.diagrams.md) ·
[ADR 0092](../adr/0092-a-project-holds-what-is-reused-together.md) · [Plans](README.md)
