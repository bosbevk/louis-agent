---
paths:
  - "**/*.razor"
  - "**/*.razor.cs"
---

# Razor: the how of every rule

Loaded when a `.razor` or `.razor.cs` file is read or edited. The why: [ADR 0034](../../src/docs/adr/0034-code-a-person-can-maintain.md)
(code a person can maintain) and [ADR 0042](../../src/docs/adr/0042-the-web-shows-the-live-results.md) (the web shows the
live results). The C# rules in `csharp.md` apply to the code-behind.

- **Every site is built on the framework's web** ([ADR 0084](../../src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md)):
  a file that would be the same on every site belongs in `louis-agent.web`, even if one site uses it today. What differs comes in
  from the site through a slot, a parameter or a setting, never a branch on which site this is. The framework names no site, in
  comments too, and everything in it is in English: another language is a site's own. A site's layout stays about 40 lines. `FrameworkNamesNoSiteTests` and `SiteLayoutsStaySmallTests` hold it.
- **A component's own styles sit beside it** ([ADR 0094](../../src/docs/adr/0094-a-components-styles-sit-beside-it.md)):
  a rule only this component draws goes in `X.razor.scss` next to `X.razor`, scoped and bundled by the build; a rule
  a site's own page draws or a theme overrides stays a global partial. The how is in `css.md`; `ScopedStylesTests` holds it.
- **A `.razor` holds markup only, no `@code` block:** a component's C# goes in its `.razor.cs` code-behind, and logic
  that needs no component (text, search, formatting) goes in a plain class with NUnit tests. `RazorHasNoCodeTests`
  holds it.
- **A page over about a hundred lines splits into components**, each one concern; a folder with more than about six
  files gets subfolders by concern.
- **No nested ternaries in markup** (ADR 0051): a three-way choice (`a ? x : b ? y : z`) moves to a plain class with a
  switch on the shape (`StepMarks.StateOf(step)`), tested on its own.
- **The web shows the live results:** a page renders what it last fetched from the API and fetches again after any
  write it makes (a grade, a re-score, a kept name); it never adds its own write to a copy, never keeps a count the
  API keeps, and never asks the person to reload. Change travels through a scoped state's `Changed` event.
- **Text from one place:** a host's own texts are its message enums and its `ChatText`
  ([Hosts guide](../../src/CLAUDE.md#each-hosts-own-texts)); F46 moves every text to localisation files, enums stay
  the keys.
- **No semicolon in words a visitor reads** (Louis, 2026-11-08, of one in the site's text: "we should never do
  this"): a page's words, a component's, a product's texts. Two clauses become two sentences, or a colon opens
  a list. The site's page tests refuse one in a page's markup (`DemosPageTests`, `TriagePageTests`,
  `CodingAgentPageTests`), and the guide's voice tells the model the same for its answers.
- **A theme** is added as [Hosts guide: Adding a web theme](../../src/CLAUDE.md#adding-a-web-theme) says; a bUnit
  test pins each component's markup to a golden file ([Write and run tests](../../src/docs/guides/write-tests.md)).
- **A script is a module beside its component** ([ADR 0093](../../src/docs/adr/0093-a-script-is-a-module-beside-its-component.md)):
  `X.razor.js` with `export function`s and no global, held through `Scripts/JsModule` and let go in `DisposeAsync`; a
  button calls a method with `@onclick`, never a global inline; a site lists only `first-paint.js` of ours.
