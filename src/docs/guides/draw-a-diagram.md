# Draw a diagram

> **How-to.** Put a picture in a doc: write its JSON beside the doc, draw it with the tool, link it, and let the test
> hold it. **Elsewhere:** how the drawing works in [louis-agent.diagrams](../projects/louis-agent.diagrams.md); why a
> diagram is data in [ADR 0103](../adr/0103-a-diagram-is-data-on-a-row-grid.md).

A picture in the docs is a JSON file in a `diagrams` folder beside the doc, and an SVG the tool draws from it. You
write the JSON; the tool writes the SVG; the doc links the SVG. Never edit an SVG by hand: the test compares it with
what its JSON draws, byte for byte.

## Write the JSON

Put it in `diagrams/` next to the doc, named for what it shows (`src/docs/projects/diagrams/louis-agent.core.json`).
Every file has a `kind`, a `title` (what a screen reader is told), an optional `caption` and the kind's own part.

**Cards**, the overview style: rows of cards top to bottom, and links between rows.

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

- A card's `code` is optional: a project or a type, in a chip beside its title.
- `look` is `plain` (the default), `highlight` (centred, in the accent's tint) or `heading` (centred, no box).
- A link's `kind` is `down` (the default), `up` or `both`.
- `icon` is one of `chip`, `user`, `users`, `box`, `server`, `database`, `world`, `message`, `shield`, `code`,
  `folder`, `file`, `browser`, `checks`, `chart`, `cloud`, `settings`, `branch`, `mail`, `search`, `book`,
  `terminal`, `stack`, `info` (the last two are the frame's). Another one is a new member of `DiagramIcon` with Tabler's path data in `IconPaths`.
- A project doc's overview takes one shape: the project highlighted on top, its main parts under it with an arrow
  each, and a second row with no arrows for what else it holds.

**Flow**, boxes and arrows on ADR 0103's row grid:

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

- A box's column comes from its arrows (one past the furthest box that points at it); set `column` only to move it.
- A `\n` breaks a label over lines. `direction` is `right` (the default) or `down`.
- A group is a labelled frame round its boxes. An arrow's `style` is `solid` (the default) or `dashed`.
- **A box's tone says what it is, the same in every chart:** `blue` who starts it (a caller, a program, the browser),
  `green` this project's own parts, `violet` the framework (the engine, core, the framework's web), `teal`
  contracts, state and data shapes, `amber` files, databases and logs, `red` what runs outside the process (a model,
  another server). A decision stays `plain` (the default) with the `search` icon. An arrow takes the tone of the
  box it leaves.
- **The frame:** the docs' tool draws a title bar from `title` (its part before the first `: ` in bold), a legend of
  the tones the chart uses, and a note bar from `caption` (its part before the colon in bold). Every chart has a
  caption: the one thing to take from it, so the doc doesn't say it again.

**Sequence**, who calls whom, in order: its participants, then its steps.

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

- A step is a `message` (a solid arrow), a `reply` (dashed, in the answerer's tone), a `note` over one lifeline or a
  span, or a `block`: `loop`, `opt` or `alt`, its own `steps`, and for an `alt` each other case in `else`.
- A message to oneself (the same `from` and `to`) turns back on its lifeline. `numbered` puts 1, 2, 3 on the arrows.
- A participant takes an icon and a tone as a flow's box does; an `actor` with no icon wears a person's.

A misspelt field, a link to an id no card has, or a missing title is refused, and the tool says which.

## Draw it

```bash
dotnet run --project src/tools/louis-agent.doc-diagrams
```

It draws every `diagrams/*.json` under `src/docs`, `packs/docs` and each demo's folder, and writes only the SVGs that
changed. `-- --check` writes nothing and fails naming a stale one.

**A Mermaid flowchart or sequence someone brings in** is read once into JSON: `-- import <doc>` writes a JSON beside
the doc for each one in it (never over a picture already there), draws it, and prints the lines it replaces and the
link to put there. Swap the block by hand, then give the JSON its title, caption, icons and tones. No doc keeps a
Mermaid flowchart or sequence.

## Link it

In the doc, as a markdown image with alt text that says what the picture shows:

```markdown
![The engine, and its parts: the loop and the guards](diagrams/louis-agent.core.svg)
```

A project doc's overview goes just above its *Purpose*, or after its opening quote when it has none.

## Check it

```bash
dotnet test src/tests/tools/louis-agent.doc-diagrams.tests
```

`DocsDiagramsTests` fails when a picture isn't what its JSON draws, when no doc shows it, when a doc shows a picture
that isn't there, when a project doc lacks its overview, when a flow's word sits on a box or a line or a line crosses
a box it doesn't join, when a sequence's words, notes or frames leave its drawing, when a `diagrams` folder sits
where the tool never looks, or when a doc holds a Mermaid flowchart or sequence. Look at the SVG in Rider's markdown preview or a browser, in a light and a dark look, before you commit;
a chart wider than about 1,600 px reads small in a doc's column, so try it the other way round. Commit the JSON and
the SVG together.

---
[Docs index](../README.md) · Related: [Guides](README.md) · [louis-agent.diagrams](../projects/louis-agent.diagrams.md)
