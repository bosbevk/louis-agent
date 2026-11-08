# ADR 0103: A diagram is data on a row grid, drawn with connectors

**Status:** Accepted (2026-11-08, FX85)

## Context

F111 gave every explaining page a diagram drawn from data by the framework's web: a page declares boxes and arrows,
`GraphLayout` places them and `GraphView` writes the SVG. The first drawing used curves, columns stacked in data
order and a word at each curve's middle. Louis's review of the live site found what that does on real data: curves
between a tall column and a short one crossed, a word landed on a box, notes ran out of their boxes, a box a few
pixels off its neighbour drew a jog, and bends sat at different places between the same two columns. His rules, in
his words: "lines may not cross", "have connectors on lines so they still bend but make direct lines", "alignment is
key to pretty, and never overlap", "conformity", "this must not happen, our framework must cater for this". The
last one is the point: a site author declares data and must get a readable drawing every time, not after a hand fix.

## Decision

The framework's web draws every diagram by these rules, and a site only declares the data:

- **Data in.** A page declares a `GraphSpec`: boxes with an id, a label, a note and a column; arrows between ids,
  with a word when one helps. Nothing else: no coordinates, no hints. The page's own test holds the ids to the
  parts the page lists.
- **A row grid.** Every box sits on one grid of rows, each row as tall as the tallest box in the diagram, a shorter
  column centred on the grid, a shorter box in the middle of its row. Two middles then differ by nothing or by half
  a row at least, so no connector can ever jog by a few pixels. Notes wrap inside the box, at a fixed width of the
  code font.
- **Ordered by neighbours.** Inside a column the boxes stand where their neighbours are (the barycentre heuristic of
  layered drawing), swept by sources then by targets, and the data's order stands when it already has no crossing.
- **Connectors, one track per gap.** An arrow is straight pieces that turn at right angles, each bend rounded. Every
  arrow between two columns bends at the one track in the middle of the gap, leaves its box's middle and arrives at
  its target's middle, so a box has one line out that branches at the track and the arrows into a box join there.
  Orthogonal routing: a crossing becomes a junction.
- **An arrow inside a column drops straight** from a box's bottom middle to the next box's top middle, its word
  beside it: a pipeline reads top to bottom. No box stands without an arrow; the site's test holds it.
- **Back arrows under, loops to the right.** The arrows back to one box share one track past the last column, one
  lane under the drawing and one entry in the box's bottom middle, a second target's set nested outside. An arrow
  from a box to itself loops out of its right side, its word beside it; the back tracks start past both.
- **Words that fit.** A word sits on a flat piece, by its source or by its target when the source has more than one
  word out, and the columns stand far enough apart for the widest word with a margin either side; the drawing
  widens rather than overlaps, and the page scales it.
- **The box is the site's listing.** A box looks like the site's code listing and inverts under the pointer; the
  text comes out of the code-behind as markup and is styled through `::deep`.

`GraphLayoutTests` holds every rule with numbers, so no browser is needed to prove a drawing.

## Consequences

- A site adds a diagram by declaring data beside its page; it gets alignment, no crossings, no overlaps and words
  that fit without doing anything, and a new rule for prettiness goes into the layout once, for every site.
- Arrows between two columns share a track, so when several sources feed several targets between the same two
  columns the drawing shows that the group feeds the group, not which one feeds which; the page's words carry that.
  A diagram that needs to show it splits into two.
- A row is as tall as the tallest box, so a diagram with one long note has taller rows throughout; keep notes short.
- The layout grows in height for lanes and in width for loops and words; the page scales the SVG, so a wide diagram
  reads smaller on a phone. Six columns is the practical limit at 880.

## Links

- [ADR 0084: every site is built on the framework's web](0084-every-site-is-built-on-the-frameworks-web.md)
- [ADR 0034: code a person can maintain](0034-code-a-person-can-maintain.md)
- `src/web/louis-agent.web/Graphs/GraphLayout.cs`, `EdgeRouter.cs`, `Components/Graphs/GraphView.razor`
- [FX85](../fixes/README.md) in the fixes list, F111 in the work log
