# ADR 0111: The framework's website lives in src/site, built on the framework like any site

**Status:** Accepted (2026-11-09, F124), amended by [0120](0120-nothing-is-hand-wired-from-the-framework.md) (the
site names its areas, its showroom among them) · **Amends:** [ADR 0074](0074-the-folders-say-what-ships.md)

## Context

From F109.1 the framework's website, louis-agent.dev, was the landing of the louis-agent demo's code-review app: its
pages, films, graphs and the guide in the corner lived in a demo, served by the coding agent's API with its whole
catalogue, a coding agent with shell, files and git included. Louis, 2026-11-09, pointing at the Contact page in that
app: "no man the framework website it is not the code-review, it is just a demo"; then "yes it is the frameworks
website and it must be documented like that", "built on the framework, and must answer questions of the framework",
and "the framework website is visitor area of a business, the framework website only has that atm. Then we will have
an portal." Of three places he chose `src/site/` (over a demo of its own and a project beside code review); of three
hosts, the site with its guide and its own small host there (over the demo's API serving it, and the guide left in
the demo, which `src/` may not reference); and the guide renamed `louis-agent-guide`.

## Decision

- **The framework's own website lives in `src/site/`:** `louis-agent.site` (the visitor area: pages, components,
  graphs, films, styles, static files), `louis-agent.identity.guide` (the guide in its corner) and
  `louis-agent.site-api` (its host), with its `config/`, `docker/` and `deploy/azure/`. Its tests are in
  `src/tests/site/`.
- **It is built on the framework like any customer's site.** It uses the framework's web, an identity and packs, the
  way a demo does, and the framework's site rules hold it as they hold every site.
- **It is the one place in `src/` that may reference a pack or an identity, and nothing else in `src/` references
  it.** So the engine, the hosts and the web can still leave for their own repository without it, and it ships no
  package. `SiteBoundaryTests` holds both directions.
- **Its host names exactly what the public site needs:** the guide on its three packs (docs, leads, site), the site's
  pages as HTML and the API the guide's chat runs on; no accounts, eval studio or coding agent. louis-agent.dev runs
  that host's image.
- **The code-review app stays a demo:** the coding agent's chat and admin, on the PC.

## Consequences

- ADR 0074's "`src/` holds only the framework" gains the exception: the framework's own site sits in `src/site/`.
- The public site no longer runs a coding agent with shell access; what a visitor can reach is what the site's
  catalogue names, and the container asks Azure for half of what the coding agent's did.
- The site's review copy is its own container on 127.0.0.1:5080; the coding agent's API moved to 5082.
- The film export (`src/tools/`) films the site from its own host, and the films live in `src/docs/films/`.
- A site's areas (the visitor area, a showroom, the chat portal, the admin portal) are F127; the framework's site has
  only the visitor area today.

## Links

- [ADR 0074: the folders say what ships](0074-the-folders-say-what-ships.md)
- [ADR 0084: every site is built on the framework's web](0084-every-site-is-built-on-the-frameworks-web.md)
- [ADR 0100: the site tells the whole architecture](0100-the-site-tells-the-whole-architecture.md)
- [louis-agent.site](../projects/louis-agent.site.md), [louis-agent.identity.guide](../projects/louis-agent.identity.guide.md)
- F124 in the work log
