# F177 Work with us: the client slideshow as a page of the site

> **Plan.** How the page at `/work-with-us` is built. Louis approved the design on 2026-11-24 with "build it and
> merge", after the mockup in the project thread. The plan is deleted when the work closes.

## Context

Louis applies for freelance AI-integration work and wanted a slideshow to hand a client. On 2026-11-24 he asked
"how could I get this slideshow on our site? in a menu item?", and picked three things on cards: the page is shown in
the framework's Slides layout, it says "we" like the rest of the site, and its menu item is "Work with us" at
`/work-with-us`.

What was read first. The framework already lays a page out as slides: `PageLayout.Slides`, which a page asks for with
`PageLayoutChoice`, makes every section one screen and settles the page on one at a time (`SiteShell.razor.scss`).
Every control the slides need is there: `Band`, `TileRow`, `Tile` with its number and note, `Steps`, `ButtonRow`,
`LinkButton`, `AskButton`, and the landing's own `WhoBuildsIt`. So no control is built, and no package is added.

What the build leaves out, and why. Orbi's notes about the site (`site-map`, `site-pages`) are not changed here. A
change to them turns the guide's recorded eval red until a live run records it again, and that run needs a model key
and Louis's go. It is a row in the TODO, to be done with FX228, which waits for the same run.

## Design

One page, `WorkWithUsPage`, of nine bands in the order of the deck: the opening, what we build, who builds it, see it
working, how we work, how you pay, what you get, running costs, and the next step. The page holds no HTML, only the
framework's controls (ADR 0125). Its lists of words come from a dictionary factory keyed by an enum of their parts
(ADR 0132), in `Content/WorkWithUs`. A small component of the site, `WorkTiles`, draws one list as a row of tiles, as
the trust page's `FactBand` does.

The words follow the site's rules: only what is true today, no price, "we", short sentences. The deck's slide about
low prices is not on the page.

| Type | Folder | Namespace | Step |
|---|---|---|---|
| `WorkWithUsWords` | `src/site/LouisAgent.Site/Content/WorkWithUs` | `LouisAgent.Site.Content.WorkWithUs` | 1 |
| `WorkTile`, `PaymentShare` | the same | the same | 1 |
| `BuildKind`, `ProofPlace`, `WorkStep`, `PaymentHalf`, `HandoverItem`, `RunningCost` | `…/Content/WorkWithUs/Parts` | `LouisAgent.Site.Content.WorkWithUs.Parts` | 1 |
| `WorkWithUsWordsTests` | `src/tests/site/LouisAgent.Site.Tests/Content/WorkWithUs` | `LouisAgent.Site.Tests.Content.WorkWithUs` | 1 |
| `WorkTiles` | `src/site/LouisAgent.Site/Components/WorkWithUs` | `LouisAgent.Site.Components.WorkWithUs` | 2 |
| `WorkWithUsPage` | `src/site/LouisAgent.Site/Pages` | `LouisAgent.Site.Pages` | 2 |
| `WorkWithUsPageTests` | `src/tests/site/LouisAgent.Site.Tests/Pages` | `LouisAgent.Site.Tests.Pages` | 2 |

## Steps

1. **The words.** The six lists as `PartWords`, with the test that each is one over its own parts.
2. **The page.** The nine bands in the Slides layout, at its address, with its heading, its title, its description
   and every section a named area.
3. **The menu and the sitemap.** "Work with us" before Contact, and the page listed for search engines.
4. **Looked at, and closed.** The page at 1920 by 1080 and 390 wide, the site's design doc, the write-up, the TODO
   and work-log rows, and this plan deleted.

## Done when

`/work-with-us` answers with nine sections, one screen each, the menu leads to it, the site's tests pass, and CI is
green on the pull request.

## Approved during the build

- 2026-11-24, on a card: fix the framework's Slides layout first, as its own fixes, before the page ships in it
  (FX246, a paged section's heading on page one, and FX247, the guide's bar as the corner button on a page of slides,
  Louis's pick on a second card).
