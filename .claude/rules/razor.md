---
paths:
  - "**/*.razor"
  - "**/*.razor.cs"
---

# Razor: the how of every rule

Loaded when a `.razor` or `.razor.cs` file is read or edited. The C# rules in `csharp.md` apply to the code-behind,
and the style rules in `css.md` to a component's styles.

The rules come in the order you meet them: where a file goes, what a `.razor` may hold, how a page gets what it
shows, the words on it, and its styles, scripts and themes.

## Where a file goes

- **A file that would be the same on every site belongs in `louis-agent.web`**, even if one site uses it today.
  What differs comes in from the site through a slot, a parameter or a setting. Never a branch on which site this
  is.
- **The framework names no site**, in comments too, and everything in it is in English. Another language is a
  site's own. `FrameworkNamesNoSiteTests` holds it.
- **A site's layout stays at 40 lines or fewer**, blank lines not counted. `SiteLayoutsStaySmallTests` holds it.
- **A folder holds at most six source files**, a component's markup and its code-behind counting as one. A bigger
  folder gets subfolders by concern. `FolderSizeTests` holds it.

*Decided in:* [ADR 0084](../../src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md),
[ADR 0010](../../src/docs/adr/0010-folders-by-concern.md).

## What a `.razor` holds

- **Markup only, and no `@code` block.** A component's C# goes in its `.razor.cs` code-behind. Logic that needs no
  component, such as text, search or formatting, goes in a plain class with NUnit tests. `RazorHasNoCodeTests`
  holds it.
- **A page over about a hundred lines splits into components**, each with one concern.
- **No nested ternaries in markup.** A three-way choice, `a ? x : b ? y : z`, moves to a plain class with a switch
  on the shape, tested on its own. `StepMarks.StateOf(step)` is one.

*Decided in:* [ADR 0034](../../src/docs/adr/0034-code-a-person-can-maintain.md),
[ADR 0051](../../src/docs/adr/0051-flat-conditionals-and-named-endpoints.md).

## How a page gets what it shows

**The web shows the live results.** A page renders what it last fetched from the API. After any write it makes,
such as a grade, a re-score or a kept name, it fetches again.

- It never adds its own write to a copy of the data.
- It never keeps a count the API keeps.
- It never asks the person to reload.

A change travels through a scoped state's `Changed` event.

*Decided in:* [ADR 0042](../../src/docs/adr/0042-the-web-shows-the-live-results.md).

## The words on a page

- **Text comes from one place.** A host's own texts are its message enums and its `ChatText`. Where each host
  keeps them is in the [hosts' guide](../../src/CLAUDE.md#each-hosts-own-texts).
- **No semicolon in words a visitor reads** (Louis, 2026-11-08, of one in the site's text: "we should never do
  this"): a page's words, a component's, a product's texts. Two clauses become two sentences, or a colon opens
  a list. The site's page tests refuse one in a page's markup (`DemosPageTests`, `TriagePageTests`,
  `CodingAgentPageTests`), and the guide's voice tells the model the same for its answers.

## Styles, scripts and themes

- **A component's own styles sit beside it.** A rule only this component draws goes in `X.razor.scss` next to
  `X.razor`, scoped and bundled by the build. A rule a site's own page draws, or a theme overrides, stays a global
  partial. The how is in `css.md`. `ScopedStylesTests` holds it.
- **A script is a module beside its component**: `X.razor.js`, with `export function`s and no global. It is held
  through `Scripts/JsModule` and let go in `DisposeAsync`. A button calls a method with `@onclick`, never a global
  written inline. A site's page lists only `first-paint.js` of ours. `FrameworkModulesTests` holds it.
- **A theme** is added as the hosts' guide says, under
  [Adding a web theme](../../src/CLAUDE.md#adding-a-web-theme).
- **A component's markup is pinned to a golden file** by a bUnit test.
  [Write and run tests](../../src/docs/guides/write-tests.md) says how one is regenerated.

*Decided in:* [ADR 0094](../../src/docs/adr/0094-a-components-styles-sit-beside-it.md),
[ADR 0093](../../src/docs/adr/0093-a-script-is-a-module-beside-its-component.md).
