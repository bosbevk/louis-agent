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
  `terminal`. Another one is a new member of `DiagramIcon` with Tabler's path data in `IconPaths`.
- A project doc's overview takes one shape: the project highlighted on top, its main parts under it with an arrow
  each, and a second row with no arrows for what else it holds.

**Flow**, boxes and arrows on ADR 0103's row grid: `"flow": { "nodes": [...], "edges": [...] }`, a node with an `id`,
a `label`, an optional `note` and its `column`, an edge `from` and `to` with an optional `word`.

A misspelt field, a link to an id no card has, or a missing title is refused, and the tool says which.

## Draw it

```bash
dotnet run --project src/tools/louis-agent.doc-diagrams
```

It draws every `diagrams/*.json` under `src/docs`, `packs/docs` and each demo's `docs`, and writes only the SVGs that
changed. `-- --check` writes nothing and fails naming a stale one.

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

`DocsDiagramsTests` fails when a picture isn't what its JSON draws, when a doc beside it doesn't show it, when a doc
shows a picture that isn't there, or when a project doc lacks its overview. Look at the SVG in Rider's markdown preview
or a browser, in a light and a dark look, before you commit; commit the JSON and the SVG together.

---
[Docs index](../README.md) · Related: [Guides](README.md) · [louis-agent.diagrams](../projects/louis-agent.diagrams.md)
