---
name: design-a-graphic
description: Works as the graphic designer for anything looked at rather than read. Use when a mark, an icon, a social card, a banner, an image, a mockup or a palette is made or changed, or when Louis asks how something should look. Holds the brand's fixed parts, the plan before drawing, the checks every picture passes, and how one is handed over.
---

# Design a graphic

Four stops come first.

1. **The product's own brand guide is the brief.** For louis-agent.dev that is `src/docs/brand/README.md`, which
   holds the mark, the name, the colour and the type, with section 5 of
   `src/docs/plans/F151-themes-and-styles.md` for what glass and flat look like. Another product has its own
   guide, and nothing of louis-agent.dev's is carried over to it. A product with no guide yet gets its brief
   first, as a finding with three options. Read the guide before drawing. What it fixes is not up for a new idea;
   a change to it is a finding with three options.
2. **Plan, then draw.** Before any file, write the plan in a few lines: what it is for and where it is seen, its
   size, the colours by name, the type, the one thing it should be remembered by. Check the plan against the brief,
   then draw.
3. **Research first, on the web.** For anything new, look at how two or three strong brands of the kind solved
   it, on their own pages that day, and say which and why. Never copy one. A size or a rule a platform sets, such
   as a link preview's, is read in that platform's own docs.
4. **Look at it.** A picture is checked by seeing it at the size it is seen, never by reading its code.

## What is fixed for louis-agent.dev

These are louis-agent.dev's fixed parts, from its guide. Another product's are in its own.

- **The mark** is the ball with the l: a flat violet disc `#6b4dff` with Geist's l at weight 650, or the glass
  ball on the site. It is never recoloured, tilted, stretched, or set beside the name as well as inside it. Never
  smaller than 16 px; clear space of half its height.
- **The name** is `louis-agent`, lowercase with its hyphen, in Geist 650, the mark standing in for its o.
- **Colour** is one purple, Ultraviolet, in five steps (`#e2dfff`, `#9282ff`, `#6b4dff`, `#3a22c4`, `#110a40`),
  on near black or a pale page. **No rainbow** (Louis, 2026-11-10): one accent to an element, and a gradient stays
  within one colour.
- **Type** is Geist for words and Geist Mono for numbers, code and small labels, never for body text.
- **Glass** (Louis's pick, Crystal): only cards are glass, glass is white, one fine edge line brightest bottom left
  and top right, never a thick side or light outside the edge; the mark is the only ball.
- **No robots, brains or sparkles**, and no stock-AI look.

## The principles every picture keeps

These are NN/g's five principles of visual design, made concrete.

- **Scale:** no more than three sizes of text on one picture.
- **Hierarchy:** one thing is seen first. Size, weight, colour and space say what comes second.
- **Balance:** asymmetry for movement, symmetry for calm; choose, don't fall into the middle.
- **Contrast:** text passes WCAG 2.2 AA, 4.5:1 for normal text and 3:1 for large (24 px, or 18.5 px bold), worked
  out, never rounded up. The brand's colours are no excuse: WCAG exempts a logo, not text set in brand colours.
- **Gestalt:** what belongs together sits together; a label sits close to what it names.

## What a picture is made with

| Picture | Made with | Where it goes |
|---|---|---|
| A doc's diagram | The diagrams tool, from JSON (`/draw-a-diagram`) | Beside its JSON |
| The social card | `film-export social-card`, from the site's own glass | `src/docs/brand/`, the site's `wwwroot` |
| The mark at a size | The SVG drawn to PNG by headless Chromium | `src/docs/brand/` |
| A mockup or concept | An HTML page published as an Artifact, or a design canvas | Linked in the thread, then in the plan |
| An icon in a page | `DiagramIcon`, the framework's Tabler icons | A control's `Icon` parameter |

A picture that a script draws keeps its script, so it can be drawn again when the look changes.

## Checks before it is shown

- Seen at its real size, and at the smallest size it is used (16 px for the mark, 390 px wide for a page).
- Seen on a light ground and a dark one, where it is used on both.
- Every text measured for contrast, and the ratio said.
- Nothing from the anti-pattern list: warm cream with terracotta, near black with one acid accent, a generic card
  kit, a single accented word in a heading, a tracked-out all-caps eyebrow, a dot between meta words.
- A shared picture is checked in the place it is shared: a link preview, a LinkedIn post.

## Handing it over

Show it before it goes into the repo: the picture, the plan it followed, the checks and their numbers, and three
options where there is a choice to make. It goes into the repo on its own branch through `/ship-it`, with the
brand guide's table of files brought along.

## An example

Redrawing the site's share card, the picture a link to louis-agent.dev shows. The plan names where it is seen (a
link preview, a LinkedIn post), its size, the colours by name, Geist for the words, and the one thing to remember
it by: the glass ball, the only ball on it. It is drawn by a script,
`dotnet run --project src/tools/LouisAgent.FilmExport social-card`, which photographs the site's own glass ball,
so it can be drawn again when the look changes. It is checked as a link preview, and its text's contrast is worked
out and said.

## References

- [The brand guide](../../../src/docs/brand/README.md) and the look of glass and flat in
  `src/docs/plans/F151-themes-and-styles.md`, section 5
- NN/g, [Principles of visual design](https://www.nngroup.com/articles/principles-visual-design/)
- W3C, [WCAG 2.2, contrast (minimum)](https://www.w3.org/WAI/WCAG22/Understanding/contrast-minimum.html)
