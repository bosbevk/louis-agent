# 0128. A site names one address per page, for search engines

**Status:** Accepted · **Date:** 2026-11-20 · **Recorded:** 2026-11-20 · **Amends:**
[0107](0107-every-section-has-an-address.md), [0109](0109-a-page-is-served-as-html.md)

![One address per page: a section names its page, the sitemap lists pages only, and a page on another host is sent to the site's address](diagrams/0128-a-site-names-one-address-per-page.svg)

## Context

Every place on a page has its own address ([ADR 0107](0107-every-section-has-an-address.md)), and each one answers
with its whole page. The served page's canonical link named the address that was asked for, and the sitemap listed
every place. And since [ADR 0109](0109-a-page-is-served-as-html.md) the canonical's host was the request's own, so
`www.louis-agent.dev` served a second copy of the site whose canonicals named www.

Google treats pages with the same words as copies, and picks one as the original. Each copy here said it was the
original, so Google ignored them all and chose for itself. On 2026-11-20 Search Console showed 95 of louis-agent.dev's
pages as "Duplicate, Google chose different canonical than user", and indexed 37 of 197. Google's own guidance is
one canonical address per piece of content, only canonical addresses in a sitemap, and a permanent redirect as the
strongest sign of which address is the original (Google Search Central: *Consolidate duplicate URLs*, *Build and
submit a sitemap*, *Redirects and Google Search*).

Louis weighed three options on each point. For a section he picked "canonical to its page" over making each
section a page of its own or marking sections `noindex`. For other hosts he picked "the framework redirects to the
site's address" over a Cloudflare rule or fixing only the canonical.

## Decision

**Every page has one original address, on the site's own host, and a site says so in three ways.** This binds the
framework's web host, `MapLouisAgentSite`, and so every site on it.

- A page's canonical link and `og:url` name the page's own route, whichever of its places was asked for, and however
  it was spelled.
- A site's sitemap lists its pages, one address each, and no section or card.
- A site names its address in `SITE_ADDRESS`, a whole https address, or its program passes one. A page asked for on
  any other host answers a permanent redirect (301) to the same path and question on that address. Files and API
  routes are never redirected, since health probes and a chat on another site reach them on other hosts. With no
  address named, a site answers on the address it was asked at, as a developer's localhost needs.

ADR 0107's rule that the sitemap lists every place, and ADR 0109's rule that the public address is the request's, are
replaced by these. The rest of both stands: every place keeps its address for people, and every page is served as
HTML.

## Consequences

- Google sees one original per page, so the copies stop competing. A section can no longer rank on its own, which it
  couldn't before either, since Google folded it into its page.
- A deployed site has one more setting to keep right. A wrong one is taken as none, so it never sends visitors away.
- A site that serves several host names on purpose, such as a client's own domain, must name its address per host
  when that comes. Today every site has one.
- The old addresses a site no longer has answer 404, and are not redirected.

## Links

- [ADR 0107](0107-every-section-has-an-address.md), [ADR 0109](0109-a-page-is-served-as-html.md)
- [Get a site found](../guides/get-a-site-found.md)
- `SiteAddressOptions` and `SitePageHandler` in `src/hosts/louis-agent.api/`
