# 0053. A stylesheet is split by concern, at most 200 lines of rules a file

**Status:** Accepted · **Date:** 2026-10-29 (Louis, during CL26: "we need to split app.css, and make it a rule to also
split the css") · **Recorded:** 2026-10-29

## Context

[0034](0034-code-a-person-can-maintain.md) and [0044](0044-the-size-limit-counts-code.md) keep a C# file to 200 lines
of code, so a person can read it and find what they need. The stylesheets had no such rule: Puls's `app.css` had
grown to 1,884 lines (the chat, the landing page and the whole admin portal in one file) and the code-review app's
to 542. Restyling the Evals page meant searching one long file for the old rules and adding the new ones somewhere
in the middle.

Considered (Louis's picks, three options each):

- **How to split:** plain area files in folders by concern, imported from `app.css` in ITCSS order (Harry Roberts'
  *Inverted Triangle CSS*: settings, base, layout, components; taken); Blazor's CSS isolation, a `.razor.css` beside
  each component (rejected for now: a rewrite of ~1,900 lines across ~80 components, `::deep` for styles that reach
  into a child, scope attributes in every bUnit golden, and the shared pieces still need a global file); both, area
  files now and isolation for new components (rejected: two places to look for a style).
- **The limit:** 200 lines in both web apps (taken); 200 in Puls only, the code-review app on a waiting list
  (rejected: a known exception); 300 (rejected: a second number to remember, and a file can still grow into a
  grab-bag).

## Decision

- **A `.css` file holds at most 200 lines of rules.** A line inside a `/* … */` comment, or a blank line, doesn't
  count; a rule with a comment beside it does. `FileSizeTests` holds it with the C# files.
- **A web app's `app.css` is its `@import` list**, one area file a line, under folders by concern (`settings/`,
  `base/`, `layout/`, `chat/`, `admin/`… for Puls; `base/`, `layout/`, `workspace/`, `git/`, `chat/` for the
  code-review app). **The import order is the cascade:** the tokens and the plain elements first (ITCSS's settings
  and base), then the rest in the order its rules always came, so a later rule still wins where it did. A split that
  would move rules past each other is done only where it can be checked in the browser.
- **A new rule goes in the file of its concern**; a file that grows past 200 lines is split, and its new file is
  imported where its lines were. A theme stays a file of its own in `themes/`, loaded by the page.
- `Stylesheet` (`tests/louis-agent.web.testing`) reads a stylesheet as the browser does; each app's tests check
  that every area file is imported and every import is there, and style tests find their rules through it.

## Consequences

- A style is found by its concern, and a change to one screen touches one or two short files.
- The browser makes one request per area file. It's small, cached and local, so it costs nothing a person notices.
- The layout folders and the chat folders alternate in the import list for now; a file is regrouped into its ITCSS
  layer only when its rules are next touched and checked in the browser.

## Links

CL26's plan, `docs/plans/CL26-eval-card.md` (deleted when CL26 was done; git keeps it) ·
[0034](0034-code-a-person-can-maintain.md) · [0044](0044-the-size-limit-counts-code.md), which this amends ·
[0054](0054-a-move-may-copy-line-ranges.md) · Harry Roberts, *ITCSS* · Microsoft, *ASP.NET Core Blazor CSS isolation*

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
