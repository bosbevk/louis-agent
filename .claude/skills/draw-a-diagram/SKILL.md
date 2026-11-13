---
name: draw-a-diagram
description: Draw, change or fix a picture in the docs. Use it when a doc needs a picture, or when a diagram's JSON file under a diagrams folder is added or edited. A picture is a JSON file that the tool draws as an SVG.
paths:
  - "**/diagrams/*.json"
---

# Draw a diagram

Three things are never done. Read them first.

1. **Never edit an SVG by hand.** A test compares each SVG with what its JSON draws, byte for byte.
2. **Never put a Mermaid block in a doc** (ADR 0118). A picture is a JSON file our own tool draws.
3. **Never commit one without the other.** The JSON and its SVG go in the same commit.

## The four steps

The guide has every field and a whole example of each kind of picture: `src/docs/guides/draw-a-diagram.md`. Read it
before writing a kind you have not written in this session.

1. **Write the JSON** in the `diagrams/` folder beside the doc, named for what it shows. Every file has a `kind`
   (`cards`, `flow`, `sequence`, `classes` or `ticket`), a `title`, a `caption`, and the kind's own part.
2. **Draw it.** `dotnet run --project src/tools/louis-agent.doc-diagrams` draws every picture that changed.
3. **Link it** in the doc as a markdown image whose alt text says what the picture shows.
4. **Check it.** `dotnet test src/tests/tools/louis-agent.doc-diagrams.tests`, then look at the SVG yourself.

## Where a project doc's pictures go

Every project doc shows four: its overview (`cards`) just above its *Purpose*, and a `flow`, a `sequence` and a
`classes` picture, each beside the part of *How it works* that it explains or in the doc's `## Diagrams` section.
