# Draw a diagram

> **How-to.** Put a picture in a doc: write its JSON beside the doc, draw it with the tool, link it, and let the test
> hold it. **Elsewhere:** how the drawing works in [louis-agent.diagrams](../projects/louis-agent.diagrams.md); why a
> diagram is data in [ADR 0103](../adr/0103-a-diagram-is-data-on-a-row-grid.md) and
> [ADR 0118](../adr/0118-a-diagram-is-data-our-renderer-draws.md).

## What you will do

A picture in the docs is two files that sit in a `diagrams` folder beside the doc. One is a JSON file, which you
write. The other is an SVG, which the tool draws from it. The doc links the SVG.

There are four steps, and this guide takes them in order:

1. **Write the JSON.** You say what the picture holds. You never place anything by hand.
2. **Draw it.** One command draws every picture that changed.
3. **Link it** in the doc.
4. **Check it** with the test, and look at it.

One rule comes before all of them: **never edit an SVG by hand.** The test compares each SVG with what its JSON
draws, byte for byte, so a hand edit fails it.

## Step 1: write the JSON

### Where the file goes, and what every file has

Put the file in `diagrams/` next to the doc, and name it for what it shows. The overview of core's design doc is
`src/docs/projects/diagrams/louis-agent.core.json`.

Every file has the same four things at the top.

| Field | What it is |
|---|---|
| `kind` | Which kind of picture this is. The next part helps you choose. |
| `title` | What the picture is. A screen reader is told it, and the docs' frame shows it. |
| `caption` | Optional in the file, but every chart in the docs has one: the one thing to take from the picture. |
| the kind's own part | A field named after the kind (`cards`, `flow`, `sequence`, `classes`, `components`, `ticket`) that holds the picture. |

### Choose the kind

| Kind | It shows | Use it for |
|---|---|---|
| `cards` | Rows of cards, with links between rows | A project's overview: what it is and its parts |
| `flow` | Boxes and arrows | How something works |
| `sequence` | Who calls whom, in order | One case, step by step |
| `classes` | Types, their members and how they relate | An interface design |
| `components` | One component, the boxes it holds, and what stands around it | What a component is made of: what holds what, what is handed down and what is said back up |
| `ticket` | A read-only card, such as a booking request | What an agent draws in the chat |

The six parts below take one kind each. Each starts with a whole example, then says what its pieces mean.

### Cards: a project's overview

```json
{
  "kind": "cards",
  "title": "louis-agent.core at a glance",
  "cards": {
    "rows": [
      { "cards": [
        { "id": "core", "icon": "chip", "title": "The engine", "code": "louis-agent.core", "look": "highlight",
          "text": "Everything that makes an agent an agent." }
      ] },
      { "cards": [
        { "id": "loop", "icon": "message", "title": "The loop", "text": "Runs the model's tool calls." },
        { "id": "guards", "icon": "shield", "title": "Guards", "text": "Keep the context safe." }
      ] }
    ],
    "links": [
      { "from": "core", "to": "loop" },
      { "from": "core", "to": "guards", "kind": "both" }
    ]
  }
}
```

**Reading the example.** The picture has two rows. The first holds one card, the project. The second holds two,
its parts. Two links join the project to each part, and the second has arrowheads both ways. The picture draws the
project as a headline on a light, and hangs its two parts from one lit rail under it.

**A card.** In the example each card has an `id`, an `icon`, a `title` and a `text`. A card may also have a
`code`, which is a project's or a type's name shown in a pill under the title. And it may have a `look`:

| `look` | What it draws |
|---|---|
| `plain` | A glass card: its icon in a badge beside its title, and its text. This is the default. |
| `highlight` | A headline on a light, with no box: the project. Give it a row of its own. |
| `heading` | A quieter headline, with no light. |

A plain card that no link touches is drawn quiet, lower and without a badge, in a picture that has links. So link
the project to its main parts, and leave what else it has unlinked.

**A link.** It joins two cards by their ids, a card to one in the row just above or just under it. A link that
skips a row, or joins two cards of one row, is refused with the two cards it names. Its `kind` says which way its
arrowheads point: `down` (the default), `up` or `both`.

**The icons.** `icon` names one of the 36 icons the framework holds. They come in five groups.

| Group | The names |
|---|---|
| What a project's parts usually are | `chip`, `user`, `users`, `box`, `server`, `database`, `world`, `message`, `shield`, `code`, `folder`, `file`, `browser`, `checks`, `chart`, `cloud`, `settings`, `branch`, `mail`, `search`, `book`, `terminal` |
| The docs' frame's own | `stack`, `info` |
| A ticket's facts | `calendar`, `clock`, `phone`, `home`, `tag`, `paw` |
| What a showroom's demos wear | `bed`, `dining`, `scales`, `exchange` |
| A deck's arrows | `chevron-left`, `chevron-right` |

To add an icon, add a member to `DiagramIcon` and its Tabler path data to `IconPaths`. This is the framework's one
set of icons. A page's line icons are drawn from it too, by `LineIconView` in the framework's web.

**The shape of an overview.** Every project doc's overview takes one shape, so they all read alike. The project is
highlighted on top, alone in its row. Its main parts are in the row under it, with a link to each, so they hang
from its rail. A further row, with no links, holds what else the project has, and is drawn quiet.

**How links become rails.** You write links, and the picture draws rails. In the gap between two rows, the cards
above that are linked to exactly the same cards below share one rail, and every other card above has a rail of
its own. So two cards that both feed one card share a rail, and two that feed different cards have one each.

**What the picture says about itself.** A cards picture writes its `title` small in its top left corner. Keep a
card's title and its text short: a row of three gives a card about 40 characters a line, and a row of four about
28.

### Flow: boxes and arrows

A flow is boxes and arrows on the row grid of [ADR 0103](../adr/0103-a-diagram-is-data-on-a-row-grid.md).

```json
{
  "kind": "flow",
  "title": "The CLI: from your program to the engine",
  "caption": "One chat, one engine: the console reads a line and the engine answers it.",
  "flow": {
    "nodes": [
      { "id": "you", "label": "Your program", "icon": "terminal", "tone": "blue", "shape": "terminal" },
      { "id": "chat", "label": "ConsoleChat\nreads a line", "icon": "message", "tone": "green", "group": "cli" },
      { "id": "engine", "label": "AgentEngine", "icon": "chip", "tone": "violet" }
    ],
    "edges": [
      { "from": "you", "to": "chat" },
      { "from": "chat", "to": "engine", "word": "StreamPromptAsync", "style": "dashed" }
    ],
    "direction": "down",
    "groups": [ { "id": "cli", "label": "louis-agent.cli" } ]
  }
}
```

**Reading the example.** There are three boxes and two arrows. The picture runs down the page. The first box is a
pill, since your program is what sets the story off. The middle box belongs to a group, which is drawn as a
labelled frame round it. The second arrow is dashed and carries a word.

**Where a box goes.** You don't place boxes. A box's column comes from its arrows: it stands one column past the
furthest box that points at it. Set `column` on a box only when you want to move it.

**The other fields.**

| Field | Values | What it does |
|---|---|---|
| a `\n` in a `label` | | Breaks the label over two lines. |
| `direction` | `right` (the default), `down` | Which way the chart runs. |
| `group` on a box, with `groups` | a group's id | Puts a labelled frame round the group's boxes. |
| `style` on an arrow | `solid` (the default), `dashed` | How the arrow is drawn. |
| `word` on an arrow | a few words | Shown in a pill on the arrow's own line. |
| `shape` on a box | `decision`, `data`, `terminal` | Draws the box in that shape. Left out, it is a plain box. |

**A box's tone says what it is, and means the same in every chart.**

| Tone | What the box is |
|---|---|
| `blue` | Who starts it: a caller, a program, the browser |
| `green` | This project's own parts |
| `violet` | The framework: the engine, core, the framework's web |
| `teal` | Contracts, state and data shapes |
| `amber` | Files, databases and logs |
| `red` | What runs outside the process: a model, another server |

An arrow takes the tone of the box it leaves.

**A shape, only where it means something.** Most boxes name none: a class or a project is a plain box. Name a
shape when the box is one of these three.

| Shape | Drawn as | Name it on |
|---|---|---|
| `decision` | A diamond | A box that asks a question, or whose own words say a choice is made there, with each arrow out carrying an outcome. It may stay `plain`. |
| `data` | A slanted box | Data at rest, not code: a file, a folder, a log, a database, a store. A class counts only when its label names the file or the store beside it. |
| `terminal` | A pill | A person, wherever one stands. In a picture that tells steps in order, also the caller, the request or the event that sets it off, and the outcome it ends in. |

A class or a program at the top of a picture of what a project holds is not a start, so it stays a box. A diamond
shows its words and no icon, and is twice as wide and as tall as they are, so keep a question short.

**What the picture says about itself.** A flow writes its own title inside the picture: the part of `title`
before the first `: ` large, and the rest on a line under it. Under the drawing it has a key, *How to read it*:
each tone and each shape the picture uses, and that a line has the colour of the box it leaves. You write none of
that.

**What the docs' frame adds.** The tool draws a frame round every picture in the docs. Round a flow, a component
diagram or a cards picture, which have their own title, it adds only a note bar from `caption`, with the part
before the colon in bold. Round every other kind it also has a title bar from `title`, with the part before the first `: `
in bold, and a legend of the tones the chart uses. That is why every chart has a caption: it is the one thing to
take from the picture, so the doc doesn't have to say it again.

### Sequence: who calls whom, in order

A sequence names its participants first, then its steps.

```json
{
  "kind": "sequence",
  "title": "louis-agent.cli: one line typed",
  "caption": "One line, two ways: a command is answered with no model, a message streams from the engine.",
  "sequence": {
    "participants": [
      { "id": "you", "label": "You", "actor": true, "tone": "blue" },
      { "id": "chat", "label": "ConsoleChat", "icon": "terminal", "tone": "green" },
      { "id": "engine", "label": "AgentEngine", "icon": "chip", "tone": "violet" }
    ],
    "steps": [
      { "step": "message", "from": "you", "to": "chat", "text": "a line" },
      { "step": "block", "block": "alt", "label": "it's a command",
        "steps": [ { "step": "reply", "from": "engine", "to": "chat", "text": "the answer, no model" } ],
        "else": [ { "label": "it's a message",
          "steps": [ { "step": "message", "from": "chat", "to": "engine", "text": "StreamPromptAsync" } ] } ] },
      { "step": "note", "over": ["chat", "engine"], "text": "the usage goes to stderr" }
    ],
    "numbered": false
  }
}
```

**Reading the example.** There are three participants. The first step is a message from you to the chat. The
second is a block with two cases: the line is a command, or else it is a message. The last is a note over two
lifelines.

**A participant.** It takes an icon and a tone, as a flow's box does. An `actor` with no icon wears a person's
icon.

**A step.** Each step says what it is in its `step` field.

| `step` | What it draws |
|---|---|
| `message` | A solid arrow from one participant to another. |
| `reply` | A dashed arrow, in the answerer's tone. |
| `note` | A note over one lifeline, or over a span of them. |
| `block` | A frame round steps of its own. Its `block` is `loop`, `opt` or `alt`. An `alt` lists each other case in `else`. |

A message to oneself, with the same `from` and `to`, turns back on its own lifeline. Setting `numbered` to true
puts 1, 2, 3 on the arrows.

### Classes: an interface design

A class chart shows types, their members and how they relate. It is drawn as a flow, so a type is a box and a
relation is an arrow.

```json
{
  "kind": "classes",
  "title": "F92 voice: the interface design",
  "caption": "Speech is a provider like a model: one interface per provider, and a factory that picks it.",
  "classes": {
    "types": [
      { "id": "provider", "name": "ISpeechProvider", "kind": "interface, step 1",
        "members": [ "SpeechProviderName Name", "ISpeechToTextClient Create(SpeechOptions options)" ] },
      { "id": "whisper", "name": "WhisperSpeechProvider", "kind": "step 2" },
      { "id": "factory", "name": "SpeechClientFactory", "kind": "step 1" }
    ],
    "relations": [
      { "from": "whisper", "to": "provider", "kind": "implements" },
      { "from": "factory", "to": "provider", "kind": "holds", "word": "one per provider" }
    ]
  }
}
```

**Reading the example.** There are three types. One implements the interface, with a dashed arrow. The factory
holds it, with a solid arrow that carries a word.

**A type.** Its `kind` is the small line under its name. Say what it is and the step that builds it. Its `members`
go under a divider, in the code font. A member over 60 characters wraps.

**A type's tone.** With no `tone`, a type is `teal` when its kind says interface, record or enum, and `green`
otherwise. Name another tone for a type that comes from elsewhere, such as `violet` for the framework's.

**A relation.** Its `kind` says how two types relate, and how the arrow is drawn.

| `kind` | Its arrow |
|---|---|
| `inherits` | Solid. Read it as "is a". |
| `implements` | Dashed. |
| `holds` | Solid. |
| `uses` | Dashed. |
| `creates` | Dashed. |

A `word` on a relation replaces the arrow's own word. When several arrows into one type, or out of one, share a
word, it is shown once.

`direction` and `groups` work as they do in a flow. Over about twenty types the arrows tangle. Split such a chart
into two pictures.

### Components: what a component is made of

A component diagram shows one component, everything it holds, and what stands around it. Use it for a project's
components: a page, a frame or a part that is put together from others.

```json
{
  "kind": "components",
  "title": "The showroom's start: what it is made of",
  "caption": "One thing held: which demo is forward. The deck and the dock are told it, and each says back what a visitor asked for.",
  "components": {
    "subtitle": "louis-agent.web, Components / Site / Showroom / Start",
    "root": {
      "id": "start", "name": "ShowroomStart", "kind": "component", "tone": "green",
      "state": {
        "says": "which demo is forward",
        "down": [ "stage.Accent", "deck.Forward", "dock.Current" ],
        "up": [ "deck.ForwardChanged", "dock.Chosen" ]
      },
      "holds": [
        { "id": "stage", "name": "LitStage", "kind": "general part", "tone": "green", "in": [ "Accent" ],
          "holds": [
            { "id": "deck", "name": "Deck", "kind": "general part", "tone": "green",
              "note": "one forward, the rest behind it in depth",
              "in": [ "Items", "Forward" ], "out": [ "ForwardChanged" ],
              "holds": [
                { "id": "window", "name": "FramedWindow", "kind": "general part", "tone": "green",
                  "in": [ "Address", "Lit" ],
                  "holds": [
                    { "id": "plain", "name": "PlainBody", "tone": "green",
                      "note": "when a demo names no window", "ghost": true }
                  ] }
              ] },
            { "id": "dock", "name": "Dock", "kind": "general part", "tone": "green",
              "in": [ "Items", "Current" ], "out": [ "Chosen" ] }
          ] }
      ]
    },
    "around": [
      { "id": "page", "name": "DemosPage", "side": "before", "kind": "page", "tone": "blue",
        "heading": "The site's page", "gives": [ "Featured", "Group" ] },
      { "id": "demos", "name": "ShowroomDemos", "side": "before", "tone": "teal",
        "heading": "What it reads", "note": "the site's list of demos", "reads": true },
      { "id": "script", "name": "Deck.razor.js", "side": "after", "to": "deck",
        "heading": "Runs in the browser", "code": "watch, bring",
        "note": "says which item a swipe left in the middle", "calls": "calls", "back": "Viewed" }
    ]
  }
}
```

**Reading the example.** The root is the start. It holds a lit stage, which holds a deck and a dock, each drawn
inside the one that holds it. The deck holds a framed window, and the window a ghost. The start holds one thing
of its own, which demo is forward. That goes down to three pins and comes back up from two. A page stands before
the root and hands it two things. A list stands before it too and is only read. A script stands after it, beside
the deck it belongs to.

**How to read the picture.**

- **A box inside a box is held by it.** The boxes one box holds are stacked in the order you name them.
- **A pin on a box's top edge is what crosses it.** A filled pin with an arrow down is handed down to the box: a
  parameter. A hollow pin with an arrow up is said back up: a callback.
- **A pill under a box's name is the one thing it holds.** A line runs from it down to each pin it feeds, and back
  up from each pin that changes it.
- **A ghost is drawn dashed.** It is what stands in that place when the usual box does not.

The picture draws its own key, "how to read it", with a row for each of these it really uses.

**A box.**

| Field | What it is |
|---|---|
| `id` | Its id. A state and a neighbour name a box by it. It is used once in the picture. |
| `name` | Its name, as the code has it. |
| `kind` | The few words in its chip, such as "general part". |
| `note` | A line under its name: what it does. A long one wraps. |
| `aside` | A few words at the right of its name, such as the folder it is in. |
| `tone` | Its colour. It means what it means in every picture: `green` is this project's own. |
| `in` | The names of what is handed down to it. |
| `out` | The names of what it says back up. |
| `holds` | The boxes inside it. |
| `state` | The one thing it holds: what it `says`, and the pins that carry it `down` and `up`, each written `box.Pin`. |
| `ghost` | `true` draws it dashed. |

**A neighbour**, in the list `around`.

| Field | What it is |
|---|---|
| `id`, `name`, `kind`, `note`, `tone` | As a box's. Its name is written in the code's face. |
| `side` | `before` the root, at the left, or `after` it, at the right. |
| `to` | The id of the box it stands by. The root, when you leave it out. |
| `heading` | The words over it. Give it to the first of a group of neighbours. |
| `code` | A line of code under its name, such as a script's functions. |
| `gives` | What it hands to its box. Each is a row with a line of its own. |
| `reads` | `true` when its box only reads it. It is drawn as one dashed line. |
| `calls`, `back` | The word on the line from the box out to it, and on the line from it back. |

**The title is in the picture.** A component diagram writes its `title` and its `subtitle` inside itself. So the
docs' frame gives it no title bar and no legend, only the note bar from `caption`.

**What the tool places.** Everything. A box is as wide as its words, its pins and the boxes it holds need. A
neighbour stands level with the box it talks to. A line from a pill goes straight where it can, and otherwise
down the left side of the box that holds the pin's box.

### Ticket: a card an agent draws

A ticket is a read-only card, such as a booking request. An agent draws one in the chat with the site pack's
`ShowCard` tool, which fills these same parts. A doc may show one too.

```json
{
  "kind": "ticket",
  "title": "BOOKING REQUEST: The garden room",
  "ticket": {
    "kicker": "BOOKING REQUEST",
    "title": "The garden room",
    "subtitle": "Seemeeu Guesthouse · Yzerfontein",
    "status": { "label": "Not sent yet", "tone": "amber" },
    "rows": [
      { "icon": "calendar", "label": "Dates", "value": "Fri 14 to Sun 16 Nov" },
      { "icon": "users", "label": "Guests", "value": "2 adults" }
    ],
    "note": "The guesthouse confirms by email. Nothing is paid here."
  }
}
```

**Reading the example.** It has a kicker and a title on a band, a subtitle, and a status that is drawn as a pill.
Two rows follow, each an icon, a label and a value. The note is at the foot.

**The rows.** There are one to six. A long value wraps, and the card grows.

**The band.** The `kicker` wraps beside the status pill. The `title` takes the band's full width. The status's
`tone` is one of the tones, and `plain` when left out.

**Its size and colours.** A ticket is 320 wide, so a phone's chat shrinks it little. Its colours are CSS variables
with the site's colours as fallbacks, which a business's window sets for its brand: `--ticket-band`,
`--ticket-band-ink`, `--ticket-paper`, `--ticket-ink` and `--ticket-accent`.

**Where it is drawn.** A page holds a ticket inline, through `DiagramRenderers.RenderForPage`, with its styles
kept to the drawing. The docs' tool draws it like any other picture.

### What is refused

The reader refuses a file with a misspelt field, a link to an id that no card has, or a missing title. The tool
says which.

In a component diagram it also refuses an id used twice, a state that names a pin its box can't reach (a box
it doesn't hold, or a pin that isn't in that box's `in` for a way down or its `out` for a way up), and a
neighbour that stands by a box the picture doesn't have.

## Step 2: draw it

```bash
dotnet run --project src/tools/louis-agent.doc-diagrams
```

The tool draws every `diagrams/*.json` under `src/docs`, `packs/docs` and each demo's folder. It writes only the
SVGs that changed.

To see whether anything is stale without writing, add `-- --check`. It writes nothing and fails, naming any stale
picture.

**If someone brings in a doc with a Mermaid diagram.** No doc keeps a Mermaid block
([ADR 0118](../adr/0118-a-diagram-is-data-our-renderer-draws.md)), so it is read once into JSON. Run the tool with
`-- import <doc>`. For each Mermaid flowchart, sequence or class diagram in the doc, it writes a JSON beside the
doc, never over a picture already there. It draws it, and prints the lines it replaces and the link to put there.
Swap the block by hand. Then give the JSON its title, caption, icons and tones.

## Step 3: link it

In the doc, link the SVG as a markdown image. Its alt text says what the picture shows:

```markdown
![The engine, and its parts: the loop and the guards](diagrams/louis-agent.core.svg)
```

**Where the pictures go in a project doc.** Every project doc shows four pictures:

| Picture | Kind | Where it goes |
|---|---|---|
| The overview: the project and its parts | `cards` | Just above the doc's *Purpose*, or after its opening quote when it has none |
| How it works | `flow` | Beside the part of *How it works* that it explains, or in the doc's `## Diagrams` section |
| Its main case | `sequence` | The same |
| Its interface design: the types someone using or extending it meets, about twelve at most | `classes` | The same |

Name the last three `{project}-flow`, `{project}-sequence` and `{project}-interface` when they are new.

**A project that has components shows them too** (Louis, 2026-11-14: "all projects which has components should
have an component diagram in their documentation", and of one picture for a whole project: "of it's main
components? plural"). Each of its main components gets a `components` picture of its own, since one picture is
of one component: a frame, a page that is put together from parts, a part others are built on. Name each
`{project}-components-{what}`, such as `louis-agent.web-components-showroom-start`, and put it beside the part
of *How it works* that tells of that component.

## Step 4: check it

```bash
dotnet test src/tests/tools/louis-agent.doc-diagrams.tests
```

`DocsDiagramsTests` fails when any of these is true:

- a picture isn't what its JSON draws
- no doc shows a picture, or a doc shows a picture that isn't there
- a project doc lacks its overview or one of its four kinds of picture
- a flow's word sits on a box or a line, or a line crosses a box it doesn't join
- a sequence's words, notes or frames leave its drawing
- a `diagrams` folder sits where the tool never looks
- a doc holds a Mermaid block

Then look at the picture yourself, in Rider's markdown preview or a browser, in a light look and a dark one,
before you commit. A chart wider than about 1,600 px reads small in a doc's column, so try it the other way round.

Commit the JSON and the SVG together.

---
[Docs index](../README.md) · Related: [Guides](README.md) · [louis-agent.diagrams](../projects/louis-agent.diagrams.md)
