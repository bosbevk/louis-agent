# 0100. The site tells the whole architecture, and shows what's planned as planned

**Status:** Accepted · **Date:** 2026-11-08 · **Feature:** F109

## Context

The framework's public site began as a static project of its own (ADR 0066) that said only what worked and kept
what's planned to one honest line. By F109 it had moved into the louis-agent demo's web app, as the landing of the
coding agent's site on the framework's `SiteShell` (F109.1), deployed as a second Container App beside Puls (F109.2,
ADR 0098), with the framework's guide in its corner (F109.3, ADR 0099). Its words stopped at the engine: the films
told one turn through core, the pages named the engine, identities, packs and the two demos, and nothing said the
hosts are libraries, that every site is built on the framework's web, that a pack is four folders, that there are
accounts, an eval harness, a demo as someone's own project, or where it all runs.

Louis asked (2026-11-08) that "the whole website should explain the whole architecture, and the identity running on
our landing page must be able to answer in detail everything mentioned on the website", that "the whole framework" be
explained and held by a test, that the site say it is 100% C#, from South Africa and a solo project, show Puls's
numbers in rand with how that works, have a section on identities and one on what's coming, and that the repository,
now private, no longer be linked. The site is the demo of what is sold: a website with an agent that knows the
business behind it.

## Decision

- **Every part that ships has its place on the site.** A test (`SiteMapTests`) reads the solution file and asks that
  every project that is not a test or a test helper is named on some page; `IdentitiesPageTests` holds the
  identities to the `identity/` folders and `PacksPageTests` the packs to `packs/tools/`. A new project fails the
  site until the site says what it is.
- **The pages are written from the docs, and the guide reads the same docs.** A page's claims are held to the repo by
  tests the way *Build your own* holds its program (`BuildYourOwnTests`): the rand to the rates file
  (`ProofPageTests`), the packages named to the project files (`StandsOnTests`), the planned features to the TODO's
  open rows (`WhatsComingTests`), and the guide's voice to the pages (`GuideKnowsTheSiteTests`), with the docs each
  page came from, so a question about a page goes to its source.
- **What's planned is shown, labelled, and never as shipped.** A *What's coming* page lists the planned features by
  their TODO id, each marked planned and the list dated; an id with no open row fails the test, so what ships leaves
  the page. This replaces ADR 0066's "at most one honest line".
- **Each film keeps one job.** The short film on the landing is the map of the whole framework in six scenes; the
  detailed film on *How it works* stays one turn through every layer of core. Both are CSS, exported as ADR 0077
  says.
- **Up front, not in the footer:** 100% C#, built in South Africa by one person, on Microsoft.Extensions.AI; the
  Measured page shows the rand beside the dollars at the rates file's rate and date, and says how.
- **Nothing public links to the private repository** or calls the source public; the guide in the corner is how a
  visitor reads the docs.

## Consequences

- A change to the solution, the identities, the packs, the rates file, the packages or the TODO can fail the site's
  tests until the site follows; the site cannot drift from the repo the way it did before F57.
- The site holds more words, so a visitor can read the whole framework; the guide answers every page from the same
  sources, with the doc named, within the cap of ADR 0099.
- The *Try it* button leads to the coding agent's chat until the triage demo (F106.3) gives it a page of its own, where
  a visitor watches a real triage with a popup per step and the guide explaining it.
- The brand mark waits for its files; the site keeps its wordmark until then.

## Links

- [F109.1 to F109.5 in the work log](../work-log.md); the plan's record was in `src/docs/plans/F109-the-site-in-the-demos-web-app.md` until this ADR took its lasting parts
- [ADR 0066](../../../demos/louis-agent/docs/adr/0066-the-site-is-static-razor.md), superseded in part · [ADR 0077](../../../demos/louis-agent/docs/adr/0077-films-on-github-are-rendered-webp.md) · [ADR 0084](0084-every-site-is-built-on-the-frameworks-web.md) · [ADR 0098](0098-the-demos-app-runs-beside-puls-split-later.md) · [ADR 0099](0099-the-guide-answers-without-a-sign-in-within-a-cap.md)
- [The code-review app](../../../demos/louis-agent/docs/projects/louis-agent.code-review.md) · [The expert identity](../../../demos/louis-agent/docs/projects/louis-agent.identity.expert.md) · [Testing and shipping](../learning/18-testing-and-shipping.md)

---
[Docs index](../README.md) · Related: [ADRs](README.md)
