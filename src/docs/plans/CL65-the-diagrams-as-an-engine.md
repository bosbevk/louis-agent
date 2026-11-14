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
| **CL65.3 Every picture says its type**, built 2026-11-14 | The `type` in the JSON and the reader, the builder's step, each of the 91 flows read and given its type, the guide. | Every picture in the docs shows its type, and a flow without one is refused. |
| **CL65.4 What the old looks left**, built 2026-11-15 | The old rules in the shared style and the frame's. The two layout rules nothing draws by. The long lines out of the class that gives a box its column. One folder for the kinds' styles. One rule for a word's pill. The diagrams' own interface picture. The `components` kind's name, put to Louis. | The list in F134's plan is empty, and the docs' tool writes no picture. |

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
  - *Ours is kept, behind an interface.* Louis, after that pick: "we should have an interface to save ours
    while we test the ms library". So the sums sit behind one interface with two implementations: ours in
    double precision, which stays the one in use, and one on Microsoft's types. Every picture is drawn with
    both and the two are compared. Which one is in use after that is his pick, with the list of pictures that
    differ in front of him. What the interface covers is a finding for this child's own plan.
  - *Ours is `Box<T>`.* Louis: "our own will be Box<T> decision behind our interface". Our implementation
    is generic over the number type, by .NET's generic math, with `double` the type in use, so no number
    changes. Run with `float`, it also shows what in a difference is single precision's doing and what is
    the library's.
  - *Its decision is an ADR, with a C# standard.* Louis, during CL65.3: "CL 65.6 must be an ADR, we cannot
    write spaghetti code." and "and a csharp standard". So the rule that the engine's sums go through its
    geometry types, behind the interface, is an ADR, and how such code is written is a part of
    `.claude/rules/csharp.md`. The ADR's wording is put to him in a few lines before its file is written.
    Both are written with this child, and a rule test holds them.
  - *The rule is wider, and written now.* Of the first wording, which bound the engine's sums: "a rule for all
    C# not just sums it is for everything". Put to him as three shapes, one rule, a catalogue of the smells we
    refuse, or an amendment to ADR 0034. His own fourth: "how about one adr and list in csharprules?" So
    [ADR 0123](../adr/0123-ask-the-type.md) is the one rule, for all C#, and the list of shapes with their
    cures is the part *Ask the type* in `.claude/rules/csharp.md`. This child is the first code brought under
    it whole, and its rule test is the first that holds it. Which box is in use, ours or the one on
    Microsoft's types, stays this child's own question and is not in the ADR.
  - *A touched file is always fixed, so this child comes next.* Louis: "always fix touched files in
    csharprules", "for this". CL65.4 and CL65.5 touch engine files, and fixing those whole needs the box and
    the face that only this child builds. Put to him as three orders. His pick: "1", this child next, before
    CL65.4 and CL65.5. So the order of what is left is CL65.6, CL65.4, CL65.5.
  - *What the interface covers, and which family is in use.* Put to him as three: a box and a point made by
    one geometry, a box only, or all four kinds of knowledge behind it. His pick: "1", a box and a point,
    with a face for words and a path writer as plain types of ours. And while its plan was written: "keep
    ours and MS's but use ours.", "since we have it". So both families stay behind the interface, ours draws
    every picture, and a test draws each with Microsoft's too, so the kept one is run. Its plan is
    CL65.6's own.
- **2026-11-14, CL65.6 is built.** A box and a point stand behind one geometry, in two families, with ours in
  use. A face says how wide words are and a path writer writes a path. Every placed record holds a box, and the
  list of renderers hands a picture its geometry. A rule test holds it. No picture of the docs changed, and a
  test draws each with both families. Its picks are in its plan as of the commit before the one that deleted
  it, and its lessons in [the write-up](../learning/18-testing-and-shipping.md). What it left for CL65.4's list:
  - *One way to write a path.* An arrow the flow's router routes is written with room round every letter, and
    every other path with none.
  - *An arrow's numbers* are kept to the one decimal the picture writes, as when an arrow's way was a text.
  - *The look's glass and its accent* are defined with the same stops in five drawings. One place for them
    wants a small type for a fade, and the `Svg` folder already holds six files.
  - *The `ticket` kind's name,* put to Louis: since FX156 it is drawn as a glass card and nothing of a ticket
    (Louis: "glass but not a ticket"), and the kind, its JSON and its classes still say `ticket`.
  - *Not for a child, only noted:* drawn in single precision, a few of the docs' flows route an arrow another
    way. Which of the router's decisions turn on it was not looked into.
- **2026-11-14, found in CL65.3, for CL65.4's list.** A cards picture whose file has no `"links"` is not
  refused: the reader fails on it with .NET's own error, which names nothing a person wrote.
- **2026-11-14, CL65.2 is built.** One builder puts six kinds' pictures together, one placer a key's rows, and
  each renderer stands beside its drawing. No picture changed. Its picks are in its plan as of the commit
  before the one that deleted it, and its lessons in [the write-up](../learning/18-testing-and-shipping.md).
- **2026-11-14, CL65.3 is built.** Every picture shows its type at the right of its title's line, and a flow's
  file says which of eight it is. Its picks are in its plan as of the commit before the one that deleted it,
  and its lessons in the same write-up. Three things from it are open for Louis.
  - *The doubtful types,* of the 95 flows read: 54 component, 16 activity, 16 object, 6 package, 3 deployment.
    - `03-context-and-scope`: component. It shows people and outside systems round ours. UML's nearest is a
      use case diagram, but its lines are protocols, not what a person uses it for.
    - `louis-agent.site-how-it-runs`: component. It could be a deployment diagram, but its boxes are
      projects, not machines.
    - `ARCHITECTURE-the-build-and-the-verbs`: deployment. It could be an activity diagram of a deploy.
    - `puls-track-between-the-phases`: activity, as what comes after what. A roadmap is none of UML's types.
    - `claude-instructions-layers`, `README-how-it-s-built`, `tests-diagrams` and the `phase-10` picture:
      package, since their boxes are sets of files or projects.
    - The four project pictures titled *How it works* whose boxes are parts and not steps, core's, the API's
      and the two providers': component.
  - *A body narrower than its picture* stands at the left, under the title, in the seven pictures that grew
    for their title and type to stand side by side. Whether it should stand in the middle is open.
  - *Whether "every picture shows its type" is a rule in an ADR.* It is held by a test and said in the design
    doc. An ADR needs his word, so none is written.
- **2026-11-15, CL65.4 is built.** The engine holds only what a picture is drawn with today. Gone: the two
  layout rules nothing drew by, with the strategy round them; the rules of 26 classes no drawing writes, from
  the style every picture carries and the docs' frame's; the palette's banner and three colours; the second way
  a path was written. In one place now: the kinds' styles, what two kinds style alike, the long lines, and each
  fade, a small type named beside its style. Three rule tests hold it: no kind styles what a shared look
  styles, every rule of the shared style is for a class a drawing writes, and no drawing says how a fade runs.
  Every picture that changed was held against the commit before with its one change taken out. F134's list is
  empty. Its picks are in its plan as of the commit before the one that deleted it, and its lessons in
  [the write-up](../learning/18-testing-and-shipping.md). Open for Louis:
  - *Picked unattended, his to overrule.*
    - The strategy is folded into two static classes, since its second rule went (ADR 0055). The other ways:
      keep the two interfaces as a seam, or keep the old rules as tested rules nothing draws by.
    - A cards picture with no `links` has none. The plan said it is refused. There is one thing no links can
      mean, and CL65.3's rule is a default where there is one answer.
    - One test project and one design doc stay for the two diagrams projects.
    - A kind's own class stands beside a shared one, as `pic-pill cls-chip`: the shared rule draws it, and
      the kind's class colours it and lets a test find it.
    - Nine fades are named where the plan said the look's three, each ending in `Fade`.
    - The diagrams' interface picture is two pictures, the way in and a kind's own parts. One picture of all
      of it had 24 boxes and 30 lines and could not be read.
  - *Noted and left.* The six messages that say what a tone means are still named `Legend…`, from when the
    docs' frame had a legend. They are a key's words now. And this plan's own picture of a flow being built
    still says `FlowRules.Straight`, which is gone: the layout asks its two rules themselves.
  - *The two names* are put to him under the next heading. Neither is renamed.

## Put to Louis: the names of two kinds

CL65.4 looked both up and renamed neither. A kind's name is in a picture's JSON, in the reader and in the guide,
so each waits for his pick. Nothing is backward compatible before a sale (ADR 0049), so a rename keeps no old
name beside the new one.

### 1. The `components` kind

**What it means.** The kind's name promises one UML type and the picture shows another.

**What was found.**

- A kind that is one UML type is named for it: `sequence`, `classes`, `composite`. By that pattern `components`
  reads as UML's component diagram.
- It draws one component, the boxes it holds and what stands around it. Since CL65.3 it shows "Composite
  structure diagram" beside its title, the same type as the `composite` kind (`DiagramTypes.Own`).
- [GeeksforGeeks' introduction to UML](https://www.geeksforgeeks.org/system-design/unified-modeling-language-uml-introduction/),
  the page Louis named, says a component diagram shows "the organization and dependencies" of a system's
  components, and a composite structure diagram "the internal structure of a class or other structured
  classifier". Ours is the second, so the type on the picture is right and the kind's name is what misleads.
- One picture of the docs uses the kind, `louis-agent.web-components-showroom-start`. CL65.5 is to draw one
  for each main component of every project that has components. 28 C# files name the kind or its records.
- *Not confirmed:* whether UML also lets a component diagram show the inside of one component. The page that
  would say, uml-diagrams.org, could not be read: its certificate failed.

**Three options.**

1. **Name it for its layout: `nested`** (recommended). A kind is a layout by pick 4 above, and this layout is
   boxes inside a box. The renderer's folder is already `Svg/Renderers/Parts/Nesting`.
   - *How:* the wire name, the JSON's field, `DiagramKind.Components`, `ComponentsSpec` and its records,
     `ComponentsStyle`, the `Components` folder, the one picture and the guide, renamed through Rider.
     "Components" stays the docs' word for what such a picture is of, as in `{project}-components-{what}`.
   - *What it changes:* a picture's JSON. No drawing.
   - *What it costs:* a rename over about 28 files, and the kind's name no longer says what it is for. An
     author learns from the guide that a nested picture is how a component is shown.
2. **Keep `components`, and say in one line what it is.** The guide's table already gives its type. The kind's
   summary and the guide gain a line: it is named for what it shows, a thing's components, and is not UML's
   component diagram.
   - *What it changes:* two sentences.
   - *What it costs:* the one kind named like a UML type it does not show stays. "Our graph types must match"
     is met by the type on the picture and not by the name an author types.
3. **One kind for the one type: fold it into `composite`,** with the layout a field, `"layout": "pins"` or
   `"nested"`.
   - *How:* one kind whose record can carry both shapes, and two layouts picked by the field.
   - *What it changes:* the reader, both records, both renderers' way in, two pictures.
   - *What it costs:* the most. It undoes pick 4 for this one type, and the two records share little: a
     composite has parts, ports and connectors, and a components picture has boxes in boxes and neighbours.

**Why the first.** Pick 4 says a kind is a layout and a type is what a picture shows. Under it this is the one
kind whose name is a type, and the wrong one. Only one picture uses it today and CL65.5 adds more, so the
rename is cheapest now.

### 2. The `ticket` kind

**What it means.** The diagrams call a thing a ticket that everything else calls a card, and that no longer
looks like a ticket.

**What was found.**

- Since FX156 the kind is drawn as a glass card. Louis: "it must not look like a ticket", "glass but not a
  ticket".
- The word is still the kind's wire name, its types (`TicketSpec`, `TicketCard`, `TicketLayout`,
  `TicketRenderer`, `TicketStyle`), the classes it writes (`ticket-paper`, `ticket-well`) and the seven
  variables a page sets its colours with (`--ticket-accent` and the rest). 46 files of the diagrams, the web,
  the site's pack and their tests say it.
- The parts outside the diagrams already say card: the pack's tool is `ShowCard` with `CardRow`s, and the
  web's component is `DrawnCard`, "the diagrams library's ticket".
- No picture of the docs has the kind. A card is made from an agent's tool call.
- The plain name `card` is one letter from the `cards` kind, the overview picture.
- The pattern is one word for one thing across the code, Evans's ubiquitous language
  ([Fowler](https://martinfowler.com/bliki/UbiquitousLanguage.html): "a common, rigorous language").

**Three options.**

1. **`chat-card`** (recommended). The wire name `chat-card`, the types `ChatCard…`, the classes `chat-card-…`,
   the variables `--chat-card-…`. It says where the card is drawn and can't be read as `cards`.
   - *How:* Rider's rename for the types, and the wire name, the classes and the variables by hand with the
     goldens regenerated and read.
   - *What it changes:* about 46 files, two goldens by class names only. No drawing.
   - *What it costs:* the rename, and a page that sets the seven variables sets them by new names. The
     chat's tool cards, which the diagrams are to draw one day too, will want a name that sits beside it.
2. **`card`, and the overview picture takes its own name.** `ticket` becomes `card` and `cards` becomes
   `overview`, which is what its layout, its drawing and its type are called already (`OverviewLayout`,
   `OverviewDrawing`, "Overview").
   - *What it changes:* the same 46 files, and the 45 pictures of the docs that are `cards`, each a new kind
     and field in its JSON. No drawing.
   - *What it costs:* the most. Every name then says what it is, and 45 pictures are rewritten to get there,
     proved as CL65.3's were, by taking the one change out again.
3. **Keep `ticket` as the record's name and not the look's.** A ticket is a kicker, a title, rows of facts, a
   status and a note. How it is drawn is the look's matter. One line in the record's summary says so.
   - *What it changes:* one sentence.
   - *What it costs:* nothing in code. The word Louis refused for the look stays in what a page's author
     types, `--ticket-accent`, and the diagrams keep a word the web and the pack don't use.

**Why the first.** It takes the word out everywhere a person meets it, with no clash and without rewriting 45
pictures that are fine. The second is the tidier end and costs a rewrite of every overview for a name.

---
[Docs index](../README.md) · Related: [F134](F134-the-showroom-looks-modern.md) ·
[louis-agent.diagrams](../projects/louis-agent.diagrams.md) ·
[ADR 0092](../adr/0092-a-project-holds-what-is-reused-together.md) · [Plans](README.md)
