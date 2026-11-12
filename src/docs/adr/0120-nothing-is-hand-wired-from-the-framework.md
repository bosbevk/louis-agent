# 0120. Nothing is wired by hand from the framework into an implementation

**Status:** Accepted · **Date:** 2026-11-12 (Louis, F127.6 step 9: "new ADR nothing is to be handwired from the
framework to implementation") · **Recorded:** 2026-11-12 · Amends [0084](0084-every-site-is-built-on-the-frameworks-web.md)
and [0111](0111-the-frameworks-website-lives-in-src-site.md)

## Context

Before F127.6, every site's `Program.cs` built the framework's parts itself: `new SiteFrames(…)`, `new PackViews()`
filled by hand, `new ChatHandoff("chat")`, `AddLouisAgentEvals()`, `AddLouisAgentAccounts()`, the chat's five services,
and a router listing `FrameworkPages.With(AccountLibrary.Assembly, EvalLibrary.Assembly)`. The code-review app and the
starter repeated it near line for line, so a new site copied the list and could forget a part: neither linked the eval
pages it registered, and only Puls drew their sidebar links, by hand. A part the framework added later reached no site
until each was edited.

## Decision

- **An implementation takes the framework's parts only through the framework's builders**, never by building or
  registering a part itself. A site names its areas, `builder.Services.AddSiteAreas(areas => …)`: `Chat()`,
  `Admin(layout, views => …)`, `Showroom(demos)`, and a library's own area as an extension (`Evals()`, `Accounts()`).
  The builder registers each area's services, pages, frames and admin links, and the chat's plumbing for every site.
- **What an implementation passes in is its own**: its layouts as types, its packs' views in the admin's lambda, its
  words and addresses. It never names a framework part (`SiteFrames`, `PackViews`, `ChatHandoff`, a library's
  `AddLouisAgent…`) and never lists the framework's page assemblies: its router reads `SiteAreas.PageAssemblies`.
- **One Composition Root per implementation** (Seemann): a static `Name` method (`CodeReviewAreas`, `PulsAreas`,
  `StarterAreas`, `LouisAgentDevAreas`) that its program and its tests both hand the builder, so the tests draw what
  the program draws.
- **A new part is a new area or a new builder method**, added to the framework once, and every site that names the area
  gets it.
- **louis-agent.dev is an implementation too.** It lives in `src/site/` (ADR 0111), beside the framework, but it is a
  site built on the framework like any demo's, and keeps this rule as they do.
- **The rule covers the framework's parts, not the site's own**: a site still registers its own services as it likes
  (Puls's `GitState`, a site's own API client), beside the call that names its areas.
- **Held by a test**: `SitesNameTheirAreasTests` fails a site whose program doesn't call `AddSiteAreas`, or whose
  program or router wires one of the builder's parts by hand. A host program follows the same rule as it is touched.

## Consequences

- A site's program is a few lines: its HTTP client, its own words and addresses, and one call naming its areas.
- The framework changes its parts' wiring in one place, and every site follows; the eval studio's links now reach every
  admin that names the evals area, drawn by the framework once.
- A site that needs a part no area offers asks the framework for an area or a builder method, rather than wiring it
  itself.
- One order rule is left: a site's own words win only when registered before `AddSiteAreas`, since the builder adds its
  defaults where nothing is set. That is wiring by order, and a `Words(…)` builder method takes it away (TODO CL62).
- One call that registers a lot is harder to see into than a list, so each area's summary and the web's design doc say
  what it registers.

## Links

[0084](0084-every-site-is-built-on-the-frameworks-web.md) · [0111](0111-the-frameworks-website-lives-in-src-site.md) ·
[0010](0010-folders-by-concern.md) · Gang of Four, [Builder](https://refactoring.guru/design-patterns/builder) · Mark
Seemann, [Composition Root](https://blog.ploeh.dk/2011/07/28/CompositionRoot/)

---
[Docs index](../README.md) · [ADRs](README.md)
