# 0084. Every site is built on the framework's web

**Status:** Accepted · **Date:** 2026-11-06 (Louis, on where a pack's screens live: "move the puls layout to shared and do the
same as with admin, then we can spawn new sites, make a rule", "the shared library becomes a framework web", "pulse and code
review must use the same framework", "code review must adapt to puls") · **Recorded:** 2026-11-06

## Context

The engine is a framework and each product is a demo on top of it ([ADR 0074](0074-the-folders-say-what-ships.md)), but the web
wasn't. `louis-agent.web.shared` held 2,200 lines, Puls's app held 13,100 and the code-review app its own chat page, so a new site
would start by copying one of them. The name said "shared between two apps", and the project called `louis-agent.web` was
really one demo's code-review app.

Weighed for the name: *`louis-agent.web`* (taken), which reads like `louis-agent.core` and frees a misleading name, for the cost of
two renames; *`louis-agent.web.framework`* and *`louis-agent.web.core`*, one rename each, which leave a demo looking like the
framework. Other frameworks split this the same way: an [ABP theme](https://abp.io/docs/latest/framework/ui/blazor/theming) is a
Razor class library that holds layouts and global styles, and the app picks it and overrides parts.

## Decision

**Every site is built on the framework's web. A web file that would be the same on every site belongs in `louis-agent.web`. A site
holds only its words, its brand and colours, its addresses, its own pages, and which packs and views it uses. The framework names
no site.**

- **Ask of any web file: would this be the same on another site?** If so it belongs in the framework, even if only one site uses
  it today. That's the difference from the old rule, which waited until two apps needed a piece.
- **What differs comes in from the site:** through a slot, a parameter, or a setting a site fills in `Program.cs`, never a branch on
  which site this is.
- **Everything in `louis-agent.web` is in English.** Another language is a site's own: Puls is the one in Afrikaans, and it fills its
  words in from outside.
- **A site's layout only fills the framework's frame,** so it stays small, about 40 lines. `AdminShell` is that frame, with ordinary
  slots the compiler checks (Template Method). The frame couldn't be handed a settings object without losing that check, and a demo
  that writes about fifteen lines is a fair price.
- **A framework page names a framework layout, which hands over to the site's** (Bridge). A page's layout is fixed when it's compiled,
  and a page naming none gets the site's default, so a framework page names `AdminFrame`, which draws inside what `SiteFrames` says.
- **Every pack brings its controls the same way:** it names its screens through one seam, and a real component comes from its own
  project under `packs-web/`, found by name. No pack puts a panel in the chat.
- **The eval pages are the framework's, on every site.** A site makes one call for them and lists one Sass name. What only a site knows comes in as
  data: the kinds of website its sources chart sorts by are a list it hands that call, a small named rule each (Specification, Evans and Fowler), so the
  chart counts and never names a shop. Their links are the site's choice, as a site with no evals has nothing to show there.
- **Where two sites differ, one shape goes into the framework and the other adapts,** rather than the framework growing a
  switch. For today's two, Puls's shape is the one that goes in.

Two tests hold it, and they read the disk, so a new site comes under them with nobody listing it. `FrameworkNamesNoSiteTests` reads
every code, markup, style, script and project file in `louis-agent.web`, comments included. `SiteLayoutsStaySmallTests` is a
ratchet: what's still too long is listed, and the list can only shrink.

## Consequences

- **Every site carries the eval pages' weight,** about 270 files, even a site with no evals, as a Razor class library ships whole. That's the price of one
  web, and we'd split them into their own library if a site ever needed to go without.
- **The name test finds names, not words.** An Afrikaans sentence added to the framework wouldn't fail it. Words come in through
  `ChatText`, and a review has to catch one that doesn't.
- **The coding agent's landing page is its web app's, on the framework's site frame** (F109.1, 2026-11-08; before it, a
  static Razor site of its own, `louis-agent.site`, that read the framework's colours and referenced nothing else). Its pages
  draw in `SiteLayout` on the framework's `SiteShell`, its look hangs off the frame's root so the chat keeps the framework's,
  and it links to the chat at `/chat`. Louis: "the static site must become the landing page for the coding agent, it will
  be it's demo"; "that custom layout can be reused, it should be in the framework".
- **Old commits and docs mean the other project** when they say `louis-agent.web`. The changelog, write-ups and fix docs stay as
  they were written.
- **The framework needs a stylesheet of its own,** which reverses "the shared components only emit class names" once F59.4 is built.

## Links

[0027](0027-engine-identities-and-packs.md) · [0034](0034-code-a-person-can-maintain.md) ·
[0074](0074-the-folders-say-what-ships.md) ·
[Write-up 20](../learning/20-extensibility.md#the-frameworks-web-f591-2026-11-06) ·
[ABP themes](https://abp.io/docs/latest/framework/ui/blazor/theming)

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
