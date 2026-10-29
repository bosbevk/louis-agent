---
paths:
  - "**/*.razor"
  - "**/*.razor.cs"
---

# Razor: the how of every rule

Loaded when a `.razor` or `.razor.cs` file is read or edited. The why: [ADR 0034](../../docs/adr/0034-code-a-person-can-maintain.md)
(code a person can maintain) and [ADR 0042](../../docs/adr/0042-the-web-shows-the-live-results.md) (the web shows the
live results). The C# rules in `csharp.md` apply to the code-behind.

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
- **A theme** is added as [Hosts guide: Adding a web theme](../../src/CLAUDE.md#adding-a-web-theme) says; a bUnit
  test pins each component's markup to a golden file ([Write and run tests](../../docs/guides/write-tests.md)).
