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
| the kind's own part | A field named after the kind (`cards`, `flow`, `sequence`, `classes`, `ticket`) that holds the picture. |

### Choose the kind

| Kind | It shows | Use it for |
|---|---|---|
| `cards` | Rows of cards, with links between rows | A project's overview: what it is and its parts |
| `flow` | Boxes and arrows | How something works |
| `sequence` | Who calls whom, in order | One case, step by step |
| `classes` | Types, their members and how they relate | An interface design |
| `ticket` | A read-only card, such as a booking request | What an agent draws in the chat |

The five parts below take one kind each. Each starts with a whole example, then says what its pieces mean.

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
its parts. Two links join the project to each part, and the second has arrowheads both ways.

**A card.** In the example each card has an `id`, an `icon`, a `title` and a `text`. A card may also have a
`code`, which is a project's or a type's name shown in a chip beside the title. And it may have a `look`:

| `look` | What it draws |
|---|---|
| `plain` | An ordinary card. This is the default. |
| `highlight` | Centred, in the accent's tint. |
| `heading` | Centred, with no box. |

**A link.** It joins two cards by their ids. Its `kind` says which way its arrowheads
point: `down` (the default), `up` or `both`.

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
highlighted on top. Its main parts are under it, with an arrow to each. A second row, with no arrows, holds what
else the project has.

### Flow: boxes and arrows

A flow is boxes and arrows on the row grid of [ADR 0103](../adr/0103-a-diagram-is-data-on-a-row-grid.md).

```json
{
  "kind": "flow",
  "title": "The CLI: from your program to the engine",
  "caption": "One chat, one engine: the console reads a line and the engine answers it.",
  "flow": {
    "nodes": [
      { "id": "you", "label": "Your program", "icon": "terminal", "tone": "blue" },
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

**Reading the example.** There are three boxes and two arrows. The picture runs down the page. The middle box
belongs to a group, which is drawn as a labelled frame round it. The second arrow is dashed and carries a word.

**Where a box goes.** You don't place boxes. A box's column comes from its arrows: it stands one column past the
furthest box that points at it. Set `column` on a box only when you want to move it.

**The other fields.**

| Field | Values | What it does |
|---|---|---|
| a `\n` in a `label` | | Breaks the label over two lines. |
| `direction` | `right` (the default), `down` | Which way the chart runs. |
| `group` on a box, with `groups` | a group's id | Puts a labelled frame round the group's boxes. |
| `style` on an arrow | `solid` (the default), `dashed` | How the arrow is drawn. |
| `word` on an arrow | a few words | Shown in a chip on the arrow. |

**A box's tone says what it is, and means the same in every chart.**

| Tone | What the box is |
|---|---|
| `blue` | Who starts it: a caller, a program, the browser |
| `green` | This project's own parts |
| `violet` | The framework: the engine, core, the framework's web |
| `teal` | Contracts, state and data shapes |
| `amber` | Files, databases and logs |
| `red` | What runs outside the process: a model, another server |

A decision stays `plain`, the default, and takes the `search` icon. An arrow takes the tone of the box it leaves.

**What the docs' frame adds.** The tool draws a frame round every picture in the docs. It has a title bar from
`title`, with the part before the first `: ` in bold. It has a legend of the tones the chart uses. And it has a
note bar from `caption`, with the part before the colon in bold. That is why every chart has a caption: it is the
one thing to take from the picture, so the doc doesn't have to say it again.

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
