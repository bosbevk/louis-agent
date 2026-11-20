# FX216 One address per page, for Google

A plan for a fix: Google indexes 37 of louis-agent.dev's pages and leaves 160 out, mostly because the site hands it
many copies of each page and every copy says it is the original. The fix gives every page one address that Google
reads as the original: a section's address points to its page, the sitemaps list pages only, and www and any other
host are sent to the site's own address.

![FX216 in five steps: the canonical is the page, the sitemaps list pages, one setting names the site's address, other hosts are sent there, and the apps and docs follow](diagrams/FX216-one-address-per-page-overview.svg)

## Context

Louis, 2026-11-20, showing Search Console for louis-agent.dev (160 not indexed: 95 "Duplicate, Google chose
different canonical than user", 53 "Page with redirect", 12 "Discovered, currently not indexed") and the framework
site's new sitemap reading "Couldn't fetch": "why is this". The live sites were checked that day from his computer,
since the cloud can't reach them. The full findings are in the project's `seo-indexing/google-indexing-findings.md`.

**What was found.** Every section has its own address (ADR 0107), like `/home/does` or `/cv/skills`, and each one
answers with its whole page, word for word, with a canonical link that names the section's own address.
`PageShell.Fill` builds the canonical from the typed `address`, not the page's `route`, so `/home/does/` and
`/HOME/does` are originals too. In the sitemap, 28 of louis-agent.dev's 39 addresses are sections, and 40 of the
framework site's 50 are. `www.louis-agent.dev` serves a second copy of the whole site with www canonicals. Neither
program passes `publicUrl` to `MapLouisAgentSite`, and ADR 0109 chose "the public address is the request's" on
purpose, so there would be no setting to keep right.

**What was fine.** http goes to https, both `robots.txt` files let everything in and name their sitemap, both sitemaps
answer 200 as XML, and an unknown address answers 404. The framework sitemap's "Couldn't fetch" is Google not having
read it yet.

**Louis's picks, on cards, 2026-11-20.**

- A section address tells Google its page is the original, and the sitemap lists pages only ("Canonical to its
  page", over making each section its own page or `noindex`).
- "Framework redirects to site address": `MapLouisAgentSite` reads each app's public address from a setting, sends
  any other host there with a 301, and every canonical uses it (over a Cloudflare rule, or only fixing the canonical).
- The old `/framework/...` and `/csharp-ai-agent-framework` addresses stay 404 ("Leave them 404").

**What was read.** `PageShell`, `SitePageHandler`, `SitePages`, `SiteRoutes`, `PageShellTests`, both `SitemapTests`,
`OpenDoorOptions` and `AdminOptions` (how a host reads a setting), both programs, both apps' yaml (each has health
probes), ADRs 0107 and 0109, and the guide *Get a site found*. Google's own advice: one canonical per piece of content,
only canonical addresses in a sitemap, and a 301 as the strongest canonical signal (Google Search Central:
*Consolidate duplicate URLs*, *Build and submit a sitemap*, *Redirects and Google Search*).

## Design

### 1. What changes, and why this shape

**A section's canonical is its page (step 1).** `SiteRoutes.PageFor` already gives the handler the page's route for
any address under it, so `PageShell.Fill` names `route`, not `address`, in the canonical and `og:url`. A section's
address still opens the page at that section for a person, and the breadcrumb is unchanged.

**The sitemaps list pages only (step 2).** Both sites' `SitemapTests` stop collecting sections and cards, and the two
files are written again. A sitemap that lists only originals is what Google asks for.

**One site address, read from a setting (steps 3 and 4).** A small options record, `SiteAddressOptions`, reads
`SITE_ADDRESS` the way `OpenDoorOptions` reads its settings, with the lookup replaceable for tests. `MapLouisAgentSite`
uses it when a program passes no `publicUrl`. With no setting, nothing changes: the canonical follows the request,
which is what a developer's `localhost` needs. With one, every canonical names it, and a page asked for on any other
host answers a 301 to the same path on the site's address.

The redirect happens in `SitePageHandler`, for page addresses only. Files, the API (`/v1/...`) and the health probes
are never redirected. That keeps the probes working (both apps have them, and Azure calls them on the container's own
address), and it keeps the API open to the chat on another site (F131) and to client sites on their own host names
(F164's idea).

**The settings and the records (step 5).** Both apps' yaml set `SITE_ADDRESS`. ADR 0109's "The public address is the
request's" no longer holds once a site names its address, and ADR 0107's "the sitemap lists every place" no longer
holds once it lists pages only. A new ADR amends both. It's proposed to Louis in a few lines when step 5 starts.

### 2. The pictures

![Interface design: SiteAddressOptions read once by SitePages, held by SitePageHandler, which uses SiteRoutes and PageShell](diagrams/FX216-one-address-per-page-interface-design.svg)

![Architecture: Cloudflare sends both hosts to the two Container Apps, each sets SITE_ADDRESS, and louis-agent.api's site handler does the rest](diagrams/FX216-one-address-per-page-architecture.svg)

![Communication: Googlebot asks www for a section, gets a 301 to the site's address, then the landing with the landing as canonical](diagrams/FX216-one-address-per-page-communication-www-section.svg)

![Objects in memory: one SitePageHandler holds the site's address, the routes, the rendered pages and index.html](diagrams/FX216-one-address-per-page-objects-in-memory.svg)

### 3. The namespace map

| Type or file | Folder | Namespace | Step |
|---|---|---|---|
| `PageShell` (canonical and `og:url` from the route) | `src/hosts/louis-agent.api/Site/` | `LouisAgent.Api.Site` | 1 |
| `PageShellTests` | `src/tests/hosts/louis-agent.api.tests/Site/` | `LouisAgent.Api.Tests.Site` | 1 |
| `SitemapTests`, `sitemap.xml` (louis-agent.dev) | `src/tests/site/louis-agent.site.tests/Pages/Meta/`, `src/site/louis-agent.site/wwwroot/` | `LouisAgent.Site.Tests.Pages.Meta` | 2 |
| `SitemapTests`, `sitemap.xml` (the framework site) | `src/tests/site/louis-agent.site-framework.tests/Meta/`, `src/site/louis-agent.site-framework/wwwroot/` | `LouisAgent.SiteFramework.Tests.Meta` | 2 |
| `SiteAddressOptions` (new) | `src/hosts/louis-agent.api/Config/` | `LouisAgent.Api.Config` | 3 |
| `SiteAddressOptionsTests` (new) | `src/tests/hosts/louis-agent.api.tests/Config/` | `LouisAgent.Api.Tests.Config` | 3 |
| `SitePages`, `SitePageHandler` | `src/hosts/louis-agent.api/Site/` | `LouisAgent.Api.Site` | 3, 4 |
| `SitePagesTests` | `src/tests/hosts/louis-agent.api.tests/Site/` | `LouisAgent.Api.Tests.Site` | 4 |
| `louis-agent-app.yaml`, `louis-agent-framework-app.yaml` | `src/site/deploy/azure/` | | 5 |
| The new ADR, ADRs 0107 and 0109's status, `get-a-site-found.md`, `louis-agent.api.md` | `src/docs/adr/`, `src/docs/guides/`, `src/docs/projects/` | | 5 |

### 4. What is reused

`SiteRoutes.PageFor` already maps an address to its page's route, so step 1 needs nothing new. The options record is
shaped like `OpenDoorOptions`. The redirect is ASP.NET Core's own `TypedResults.RedirectPermanent`. No package is added.

## Steps

One commit each, test first and seen failing, with a stop for Louis's review after each.

1. **A section's canonical is its page.** Test: `PageShellTests` fills `/proof/rand` under route `/proof` and expects
   the canonical and `og:url` `https://louis-agent.dev/proof` (today's test expects `/proof/rand`, and is changed).
   Code: `PageShell.Fill` uses `route`.
2. **The sitemaps list pages only.** Test: both `SitemapTests` expect one address per page; they fail against
   today's files. Code: the address rule's sections and cards come out of `Addresses()`, and
   `LOUIS_UPDATE_GOLDEN=1` writes both files again.
3. **The site's address is a setting.** Test: `SiteAddressOptionsTests` reads `SITE_ADDRESS`, ends it with a slash,
   and gives none when it's unset or not an absolute https address. Code: `SiteAddressOptions`, and
   `MapLouisAgentSite` uses it when no `publicUrl` is passed.
4. **Another host is sent to the site's address.** Test: `SitePagesTests` asks a page on `www.example.test` with
   `SITE_ADDRESS` set to `https://example.test/` and gets a 301 to the same path and query; a file and an API address on
   the same host are not redirected; with no setting, nothing is. Code: the guard in `SitePageHandler.HandleAsync`.
5. **The apps name their address, and the docs follow.** `SITE_ADDRESS` in both yaml files, the new ADR (proposed
   first), ADRs 0107 and 0109 marked amended, the guide *Get a site found* and the API's design doc. No test first,
   since it's configuration and docs; the deploy-workflow and docs tests run.

## Verification

- Each step runs the tests of what it touched. Before the pull request, `main` is merged in and the full unit suite
  passes.
- After the deploy, from Louis's computer (free): `/home/does` names `https://louis-agent.dev/` as its canonical,
  `https://www.louis-agent.dev/demos` answers 301 to `https://louis-agent.dev/demos`, and both sitemaps list pages only.
- In Search Console, Louis resubmits both sitemaps, asks for the landing and `/demos` under *URL inspection*, and
  presses *Validate fix* on the duplicates row. The numbers settle over a few weeks.
- No paid run.

## Concepts

This fix doesn't touch the agent, so it sits under skill 18, *Testing and shipping*, in `src/docs/GOAL.md`: what a
product needs before people can find it. Step 1 teaches what a canonical link is for, step 2 what a sitemap promises,
and steps 3 and 4 why one setting can beat "no setting to keep right" once more than one address reaches the app.

## Approved during the build

Nothing yet.

---
[Docs index](../README.md) · Related: [Plans](README.md) · [Get a site found](../guides/get-a-site-found.md) ·
[ADR 0107](../adr/0107-every-section-has-an-address.md) · [ADR 0109](../adr/0109-a-page-is-served-as-html.md)
