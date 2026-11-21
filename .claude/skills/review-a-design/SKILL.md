---
name: review-a-design
description: Reviews how a built site or a theme looks and feels, as a designer would, and hands over ranked findings, each measured on the running page. Use when Louis asks to review a site's design, its look and feel, its branding as it is seen, or a theme. Holds what is read first, what is measured and how, how findings are ranked, how an option is drawn on the real page, and the review page handed over. A look at one change before it is shown is check-a-page, and an audit of a brand's words and positioning is research-the-market.
---

# Review a design

A design review looks at pages that are built and says where they break their own brand, a standard, or what
strong sites of their kind do. Every finding is measured on the running page, so Louis can check it. Seven stops
come first.

1. **A review changes nothing in the repo.** It looks, measures and reports. An option is drawn in the browser,
   over the running site, never in a file of the repo. Building a pick is work of its own, on Louis's word, with
   its plan (`/plan-a-feature`).
2. **The brand guide and the theme's own rule are the brief.** Read them before the first page is opened
   (`/design-a-graphic` says where they are). A finding is a place where the page breaks its guide, its theme's
   rule or a standard, or reads worse than strong sites of its kind. It is never the reviewer's taste.
3. **Measured, never eyed.** A size, a gap, a colour or a contrast ratio is read from the running page, in light
   and in dark, at 1920 by 1080 and at 390 wide. The picture shows it and the number proves it.
4. **Nothing from memory.** A standard, a platform's rule and how a strong site does it are read at their source
   on the web that day, and named. What could not be confirmed is said plainly, with what was used instead.
5. **One finding at a time, with three options he can see.** Each goes to Louis on its own card through
   `/put-a-finding`, and each option is drawn on the real page, in light and dark. A slip with one right answer
   is told as a clear fix, and is not put on a card.
6. **Measure again before a finding is put.** One reviewer misses things and gets things wrong: NN/g asks for
   three to five. So each finding is measured a second time on the day it is put, and what no longer holds is
   said and taken off the review page.
7. **A pick is a design choice, not a go to build.** It is written down with what building it needs, and the
   build waits for his word.

## The steps

1. **Say what is reviewed**: which site, which theme, which pages, and what is left out. NN/g: the narrower the
   scope, the more detailed the review.
2. **Read the brief**: the brand guide, the theme's rule, and the theme's values in
   `src/web/LouisAgent.Web.Themes/Styles/theme-{name}/_light.scss` and `_dark.scss`.
3. **Read the craft on the web**, that day: two or three strong sites of the same kind on their own pages, with
   their design system's pages where they publish one, and the standards a page is held to (*References*).
4. **Run the site locally** as `/check-a-page` says, and take a picture of every page in light and dark at both
   sizes. Read every picture.
5. **Measure** (*What is measured*), and keep each script.
6. **Find each finding's cause in the code**: the file and the line that set the value, and whose it is. A
   theme's value, a framework component and one site's page are fixed in three different places.
7. **Rank the findings** (*How a finding is ranked*) and write the review page (*The review page*). Hand it over.
8. **Put the findings to Louis, one at a time**, the one that changes the most first (*How a finding is put*).
9. **When every finding is picked**, bring the review page along with a row per finding and its pick, and say
   what is owed: the build, as a plan.

## What is measured

The measures are scripts run in the page. In a cloud session Playwright runs them (`/check-a-page`, *In a cloud
session*). A value is read with `getComputedStyle`, never from the Sass, since a later rule can win over the one
you read: the code is where the cause is looked for, after the page has said what is drawn.

| What | How it is read | Held against |
|---|---|---|
| The sizes of type | Each heading's, body's and small text's size and weight, per page | NN/g's scale and hierarchy: each step clearly apart from the next |
| Every text's contrast | Its colour laid over the first solid ground behind it, then W3C's formula | WCAG 1.4.3: 4.5 to 1, and 3 to 1 for large text (24 px, or 18.5 px bold). Placeholder text counts |
| A control's edge and the focus ring | The same formula, edge against the ground beside it | WCAG 1.4.11: 3 to 1 |
| Lines, corners, shadows and blur | Every distinct value in use, counted, with where each is | The theme's own rule |
| Where blocks of words start and end | Each block's left and right edge, per page | The page's other blocks, and the other pages |
| The page at 390 wide | `scrollWidth` against the width, and how much of the width the words get | No sideways scroll |
| The mark and the name | Their drawn sizes wherever they stand | The brand guide |
| What a shared link shows | The `og:` tags in the page as it is served | The platform's own docs |

W3C's formula: a colour's luminance is `0.2126 R + 0.7152 G + 0.0722 B`, each channel made linear first, and the
ratio is `(lighter + 0.05) / (darker + 0.05)`. A text over a picture or a gradient has no one ground, so it is
flagged and looked at by hand. A ratio is never rounded up.

## How a finding is ranked

By NN/g's three questions. How many pages show it? How much does it cost the reader or the brand where it shows?
Does it come back every time, or is it met once? A finding is high, medium or low, and the page lists them in
that order. A finding that touches every page in both styles outranks a worse one on a single page.

## The review page

One HTML page, published as an Artifact, kept with its pictures and its scripts in the project's files under
`/mnt/project-files/reviews/{site}-design/`, never in the repo. It opens with what was reviewed, at which commit,
and how. Then what is strong, with a line on why: NN/g asks a review for the strengths as well as the problems,
and they say what a fix must not break. Then the findings, ranked, each with where it is, its picture, its
measure, its cause in the code and its source. It closes with what could not be checked.

## How a finding is put

1. Measure it again, and read the code again for its cause.
2. Research its three options (`/put-a-finding`), each from a source read that day.
3. Draw each option on the real page: a style or a small change to the page, set by the script in that browser
   only. Put today's page and the three options on one sheet, in light and in dark, and look at the sheet yourself
   before it is posted.
4. Post it with the recommended option first, what each option changes and what it costs, and the sources. Then
   the card.
5. Write his pick down where a later thread finds it: the review page, and the project's memory, with what
   building it needs. While he reads, the next finding is prepared.

## Checks before it is shown

- Every number on the page was read from the running site that day, in light and in dark.
- Every finding names its cause in the code and its source, and none is only a picture.
- Every option was drawn and looked at, and each script is kept beside its pictures.
- Nothing in the repo changed: `git status` is clean.
- What was not looked at is said: a size, a browser, a page behind a sign-in.

## Handing it over

Post the review page with a few lines: how many findings, what is strong, and the finding that matters most.
Then the first card. A skill that fell short on the way is said in the same reply.

## An example

The framework's own site under the flat theme, 2026-11-21. Flat's rule is one solid colour for each surface and
a hairline round it, with no gradient, shadow or blur. Vercel's Geist, Linear's notes on its redesign and
Tailwind's docs were read that day, with WCAG 2.2 and NN/g. The site ran on `http://127.0.0.1:5085` and Playwright
measured every page. Thirteen findings were ranked and put one at a time. Within one of them, the focus ring
measured 2.89 to 1 in light and 2.43 in dark, under the 3 to 1 of WCAG 1.4.11. The cause was in the code: the
chat's stylesheet sets a ring at 70 percent that wins over the full one in `base/_elements.scss`. Measuring again
showed that four things in the first review did not hold, before any of them reached a card: the pages wear
three corner sizes, not five, and the search key's hint is not shown on a phone at all. X's size for a link's
picture could not be read at its source, so the finding said so and used Meta's and LinkedIn's.

## References

- `/put-a-finding`, how a finding goes to Louis, `/check-a-page`, how a site runs locally, and
  `/design-a-graphic`, the brand's fixed parts
- [The brand guide](../../../src/docs/brand/README.md)
- NN/g, [Principles of visual design](https://www.nngroup.com/articles/principles-visual-design/),
  [How to conduct a heuristic evaluation](https://www.nngroup.com/articles/how-to-conduct-a-heuristic-evaluation/),
  [UX expert reviews](https://www.nngroup.com/articles/ux-expert-reviews/) and
  [Severity ratings for usability problems](https://www.nngroup.com/articles/how-to-rate-the-severity-of-usability-problems/)
- W3C, WCAG 2.2: [contrast (minimum)](https://www.w3.org/WAI/WCAG22/Understanding/contrast-minimum.html),
  [non-text contrast](https://www.w3.org/WAI/WCAG22/Understanding/non-text-contrast.html) and
  [G17](https://www.w3.org/WAI/WCAG22/Techniques/general/G17), the formula
