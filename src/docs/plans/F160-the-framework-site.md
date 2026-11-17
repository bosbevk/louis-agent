# F160 The framework's own site, framework.louis-agent.dev

A plan, a parent with three children: the framework gets a site of its own on its own subdomain, seven pages written
new that walk a visitor from what the framework is to building on it, with its own agent, louis-agent · AI, in the
corner. louis-agent.dev keeps selling; the framework's old pages there are thrown away.

## Context

**What Louis asked** (2026-11-17). "next we should rewrite Framework on the site", "we should mention all these
features in the Framework rework", "we have a Feature planned to redo the docs. So research how we can structure our
framework pages to accomodate for that", then "framework will also become a subdomain" and "no we give him his own
identity". His picks of that day, each of three options after research:

| # | The choice | His pick |
|---|---|---|
| 1 | The framework's pages and the docs | A story on top, the docs underneath: pages that link down into the docs by path ("A, but I want C for the docs feature one source of truth") |
| 2 | What builds the docs site | Our own, on the framework's web, with Markdig; no `llms.txt` |
| 3 | Where the docs live | `docs.louis-agent.dev` ("docs will become a subdomain") |
| 4 | The MCP scheme | `docs://{path}`; `feed://` became `feeds://` (CL67, merged) |
| 5 | The order | This feature first, its doc links pointing at GitHub; then F148, the docs site, F150 |
| 6 | The pages' outline | Seven pages in a visitor's order: Overview, Getting started, How it works, Features, Cost and evals, Examples, Comparison |
| 7 | The names | `louis-agent.web.docs` for the framework's docs parts; a site's projects are named `louis-agent.site-…` |
| 8 | What was there | "totally forget what was we will build it new and delete the old": no page, part, picture, test, sentence or address kept, no redirect |
| 9 | The pictures | "we redraw pics from our tool": every picture a spec for `louis-agent.diagrams` |
| 10 | Where the pages live | "framework will also become a subdomain": `framework.louis-agent.dev`, its own project and app, as a parent with children (A) |
| 11 | Its agent | "no we give him his own identity": louis-agent · AI, the framework speaking for itself, labelled AI (A) |

The research behind them (how Microsoft Agent Framework, Anthropic, LangChain and the AI SDK lay out their docs,
Diátaxis, Starlight, llms.txt's uptake, the MCP resources spec, Google's URL guidance) is in F160.1's *Context*.

## Design

### The shape, in plain words

**Three sites, one framework.** louis-agent.dev sells websites with agents (ADR 0112); `framework.louis-agent.dev`
tells the framework, for a developer; `docs.louis-agent.dev`, later, holds every doc. Each is its own app on the
framework's web with its own host, as the framework says a site is built (ADR 0084).

**The framework site** (`src/site/louis-agent.site-framework`, served by `louis-agent.site-framework-api`) has seven
pages at the root of its address: `/`, `/getting-started`, `/how-it-works`, `/features`, `/cost-and-evals`,
`/examples`, `/comparison`, each section its own address under its page (ADR 0107). Every section ends with links to
its docs by repo path, through `louis-agent.web.docs`'s doc link, so the docs site takes them over by one setting.

**Its agent** (`src/site/louis-agent.identity.framework`) is louis-agent · AI: the framework describing itself in the
first person, labelled AI so it can't be taken for a person (ADR 0113's rule holds), technical, answering from the
docs with a doc link and showing the page it talks about. It has the docs and site packs, not the leads pack: this
site doesn't sell. Its host runs it with its own model key and its own usage ledger.

### Architecture

![Three apps on the framework's web: louis-agent.dev sells with Louis · AI, framework.louis-agent.dev tells the framework with louis-agent · AI, docs.louis-agent.dev comes later; doc links lead to GitHub until then](diagrams/F160-the-framework-site-architecture.svg)

### Communication: a developer asks the framework's agent

![A developer on How it works asks louis-agent · AI a question; its host runs the turn on the framework identity, which searches and reads the docs and answers with a doc link](diagrams/F160-the-framework-site-communication-a-question.svg)

The interface design and the objects in memory are drawn in each child's plan, from the code that child touches.

## The children, in build order

| Child | What | Done when |
|---|---|---|
| F160.1 | The framework's pages: the old ones thrown away from louis-agent.dev; the site and its host on :5084; the doc link; `CodeListing` and `ComparisonTable`; the seven pages written new | The seven pages answer on :5084 in both themes, light and dark, at 1280 and 390 px; no HTML tag on them; every doc link a real file; `SiteMapTests` holds on the new site |
| F160.2 | louis-agent · AI: its identity, voice and skills, the docs and site packs, run by the framework site's host, in the corner | It answers a framework question from the docs with a doc link, on :5084; its golden prompt reviewed; one paid test call, its cost reported |
| F160.3 | framework.louis-agent.dev: its image, its Azure app, the deploy, DNS (Louis's step at Cloudflare), its sitemap and robots, louis-agent.dev's menu linking to it | The live address answers each page, prerendered; the guide answers there; louis-agent.dev's menu leads to it |

Each child has its own plan, branch, pull request to `main` and write-up; a child's plan is written when it starts.

## Verification

Each child's own. Across the three: no page of either site links to an address that answers nothing, and the two
agents keep to their own sites (Louis · AI never on the framework site, louis-agent · AI never on the selling one).

## Concepts, and where they sit in GOAL.md

- **One source of truth with several doors** (F160.1): pages link to a doc by path. *Skill 11, protocols.*
- **Information architecture by the reader's need** (F160.1): Diátaxis, a journey from overview to reference.
  *Skill 20, extensibility.*
- **An agent per site, from one framework** (F160.2): an identity is who, its packs what it can do (ADR 0027).
  *Skill 3, prompt design.*
- **Shipping a second app** (F160.3): an image, an app, a domain. *Skill 18, testing and shipping.*

## Approved during the build

(empty)

---
[Docs index](../README.md) · [Plans](README.md) · Related: [F160.1](F160.1-the-framework-pages.md) · [F148](F148-one-docs-endpoint.md) · [ADR 0084](../adr/0084-every-site-is-built-on-the-frameworks-web.md) · [ADR 0113](../adr/0113-the-guide-is-louis-and-says-it-is-the-ai.md)
