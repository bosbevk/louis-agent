---
name: design-a-page
description: Works as the web designer, always on LouisAgent.Web's controls (ADR 0125), never hand-made HTML. Use when a page or a section is planned, reordered or redesigned, when its flow, hook or next step changes, or when Louis asks how a page should work or what will catch visitors. Holds who the page is for, the order of its sections, the evidence behind it, and the checks on desktop and phone. Checking the built page is check-a-page.
---

# Design a page

Five stops come first.

1. **Built on our framework, always** (Louis, 2026-11-18). A site is configured framework components (ADR 0125):
   a page holds no HTML, only `LouisAgent.Web`'s controls (`Band`, `TileRow`, `Tile`, `LinkButton`, `ProductWindow`
   and the rest) given words and data. Before a section is planned, look up which control draws it. A shape the
   framework lacks is built there first, as a control any site could use, and then configured on the page. Styles
   belong to the theme; another thread may own them, so a redesign changes the order and the words, and asks
   before it changes a look.
2. **A page is for one reader and one next step.** Before any section, write who it is for and what they should
   do at the end. A page that speaks to two readers splits, or sends one of them elsewhere.
3. **The words come from the research.** Slogans, headings and claims follow `/research-the-market`, and are
   never made up here.
4. **Plan, then build.** The flow is drawn and agreed with Louis before a section moves, and the plan names the
   framework control for every section.
5. **Look at real pages first.** Before the flow is drawn, open two or three strong pages of the kind on the web
   that day, and note the order of their sections and their one next step. The evidence below is the baseline. A
   number from it is checked at its source before it is quoted to Louis.

## What the evidence says

- **Ten seconds.** People leave in 10 to 20 seconds unless the value is clear (NN/g). The hero says what it is
  and what changes for the reader, in plain words.
- **The first two screens.** 57% of viewing time is above the fold and 74% in the first two screens (NN/g). The
  hook goes there.
- **Try beats watch.** Interactive demos convert better than video, and the best sit above the fold or in the
  menu, with no sign-up in front (Navattic, vendor data). On our sites the guide and the demos are the hook.
- **People scan, and plain words convert.** The numbers, and the rules for the words that follow from them, are
  in `/write-the-copy`.
- **Proof with a face.** Photos are remembered, and numbers and logos alone are not (CXL).
- **Reassure where doubt comes.** A short line at the moment of hesitation, such as privacy beside a form,
  lifts the next step (NN/g).

## The order of a selling page

The 4 Ps give the spine: the problem, the promise, the proof, the proposal. The reader is the hero and we are
the guide (StoryBrand).

| # | Section | Its job |
|---|---|---|
| 1 | Hero | The problem and the promise in one look, and the hook to try |
| 2 | Show it | The demo, or the thing working, in the second screen |
| 3 | What it does | Each tile a job in the reader's words, starting with its verb |
| 4 | Proof | What is true today: a face, measured numbers, a client |
| 5 | Doubt answered | Privacy, where it runs, what happens next |
| 6 | The next step | One action, the same one all the way down |

Anything about us rather than the reader (the brand, the framework) goes lower, or to its own page.

## Copy on the page

- One idea a section, under a heading that says it.
- The words themselves are written by `/write-the-copy`'s rules.
- No claim the code doesn't back, and no number the ledger or an eval didn't give.
- Every place on a page has its own address (ADR 0107).
- The page's title and description are written for the search a reader makes (`get-a-site-found.md`).

## Checks

- **Read it in ten seconds**: cover everything below the first screen and ask what the page is and what to do.
- **1920 by 1080 and 390 wide**, no sideways scroll, with nothing fixed covering content (the guide's bar, a
  corner link). Use `/check-a-page`.
- **Light and dark, glass and flat**, since the visitor picks.
- **Keyboard** reaches every control, with a visible focus.

## Handing it over

The flow as a table or a mockup page published as an Artifact, each section with the framework control that draws it, the current flow beside it, the evidence for each
move, and three options where there is a choice. Then the build goes step by step through `/do-a-step`.

## An example

The free-websites offer on the landing page. The reader is a business owner, and the next step is to see how the
draw works. The hero had no place for such a line, and any site could want one, so the framework got it first:
`AnnouncementStrip`, put in `SiteHero`'s new `Announcement` slot. The landing only configures it, in
`Components/Landing/Hero.razor`, with its link to `/free-websites`, its status "Entries open" and its words "Two
businesses get a free website.". The look stayed the theme's, plain and not glass, as Louis asked.

## References

- [ADR 0125: a site is configured framework components](../../../src/docs/adr/0125-a-site-is-configured-framework-components.md)
- NN/g on [the fold](https://www.nngroup.com/articles/scrolling-and-attention/)
- `/write-the-copy`, for how people read a page and the rules for its words
- Navattic's [State of the Interactive Product Demo](https://www.navattic.com/report/state-of-the-interactive-product-demo-2025),
  vendor data
