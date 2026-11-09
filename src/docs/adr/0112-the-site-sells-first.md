# ADR 0112: The site sells first, the framework is the proof

**Status:** Accepted (2026-11-09, F128.1) · **Amends:** [ADR 0100](0100-the-site-tells-the-whole-architecture.md)

## Context

Louis, 2026-11-09: "Our goal is to sell websites with agents, and custom agents, for now. The framework takes second
place to that." A review of every page found the offer seventh on the landing, after six sections about the
framework, a developer's hero, a menu led by the framework, and a guide that invited developer questions. Louis picked,
each of three: the hero sells the website with an agent (1A), the landing tells its story offer first (2A), two plans on
a pricing page (3A), the menu leads with what we sell (5A), and a "how it works for you" band with the facts a buyer
trusts (7A). During the build: "the page names must change now" (the framework's pages under `/framework`), and "we
will make a framework focused version at framework.louis-agent.dev" (F130).

## Decision

- **louis-agent.dev speaks to a business first.** Its hero is the offer (a website with an agent, from R500 a month,
  "Ask for yours" and "See one work"); its landing runs the offer, what the agent does, how it works for you and why
  to trust it, custom agents and the demos, and only then the framework as the proof; its last strip asks a business
  for theirs. Its menu reads What we build · Pricing · Demos · Contact · The framework.
- **The selling pages are their own:** `/what-we-build` and `/pricing`, named for what they sell; the contact page and
  the guide quote the same two plans, held by tests.
- **The framework's pages sit under `/framework/…`,** so they can move whole to `framework.louis-agent.dev` (F130); an
  old address answers a permanent redirect to its new one, section and all, from the site's host (`MovedPages`).
- **A claim stays held to the repo** (ADR 0100): what the site says about where it runs, the guide's packs and the
  count of design records is checked by `WhatIsTrueTests`, and every link on the site by `SiteLinksTests`.

## Consequences

- The framework's own story is still on the site, behind *The framework* in the menu and after the offer on the
  landing, until F130 gives it a site of its own.
- A search engine sees the business pages' titles and descriptions first; old framework addresses keep their place in
  search through the redirects.
- F128.2 adds a law practice's, a restaurant's and a doctor's demo to the demos band and page.

## Links

- [ADR 0100: the site tells the whole architecture](0100-the-site-tells-the-whole-architecture.md)
- [ADR 0107: every section has an address](0107-every-section-has-an-address.md)
- [ADR 0113: the guide is Louis, and says it is the AI](0113-the-guide-is-louis-and-says-it-is-the-ai.md)
- [louis-agent.site](../projects/louis-agent.site.md)
- F128.1 in the work log
