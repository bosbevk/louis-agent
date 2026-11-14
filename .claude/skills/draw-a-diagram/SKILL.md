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
   (`cards`, `flow`, `sequence`, `classes`, `components`, `composite` or `ticket`), a `title`, a `caption`,
   and the kind's own part. A `flow` also says its `type`, straight after its `kind`: `component` for parts,
   `activity` for the steps of a procedure, `object` for instances in memory, `package` for sets of projects,
   `deployment` for what runs where. The guide's *Say the type* has all eight.
2. **Draw it.** `dotnet run --project src/tools/louis-agent.doc-diagrams` draws every picture that changed.
3. **Link it** in the doc as a markdown image whose alt text says what the picture shows.
4. **Check it.** `dotnet test src/tests/tools/louis-agent.doc-diagrams.tests`, then look at the SVG yourself.

## A shape on a flow's box

A box of a `flow` names a `shape` only where the shape means something, and most name none.

- `data`, a slanted box: a file, a folder, a log, a database or a store. A class only when its label names the
  file or the store beside it.
- `decision`, a box with pointed ends: a box that asks a question, or says a choice is made there, each arrow out an outcome.
- `terminal`, a pill: a person. In a picture that tells steps in order, also what sets it off and what it ends in.

## The overview's shape

A project's overview is a `cards` picture in one shape. The project is the `highlight` card, alone in the first
row, and is drawn as a headline. Its main parts are in the row under it, each linked from it, so they hang from
its rail. What else it has goes in a further row with no links, and is drawn quiet. A link joins neighbouring rows
only.

## A sequence's tones

A `sequence` shows a tone on a participant's badge and nowhere else: every call is the look's violet running to
blue, and every answer is grey. So give each participant the tone that says what it is, and don't count on a
call's colour to say who sent it. The picture writes its own title and its own key.

## A class chart's kinds

In a `classes` picture a type's badge shows a letter read from its `kind`: write `interface`, `record`, `enum`,
`abstract` or `static` in it, or the badge shows C for a class. A relation's `kind` decides its line: `holds`,
`inherits` and `implements` are the violet structure, `creates` is blue and `uses` is thin grey. Choose `holds`
for what a type is built of and `uses` for what it only calls.

## Where a project doc's pictures go

Every project doc shows four: its overview (`cards`) just above its *Purpose*, and a `flow`, a `sequence` and a
`classes` picture, each beside the part of *How it works* that it explains or in the doc's `## Diagrams` section.

A project that has components shows them too: a `components` picture for each of its main components, since
one picture is of one component. Name each `{project}-components-{what}`. It shows what holds what, what is
handed down and what is said back up. Read the component's markup and its code-behind before you write it:
every box, pin and neighbour is one the code has.

A class worth opening up gets a `composite` picture: one class, the parts its constructor builds, a port for
each thing asked of it and each thing it is handed. Name it `{project}-{class}`. Read the class's fields, its
constructor and its public methods first: every part, port and connector is one the code has.
