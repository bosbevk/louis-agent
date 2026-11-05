# ADR 0082: The recipe library keeps each new read for good, and is an open feed we read ourselves

> **Explanation.** Why the cook keeps what it reads, why anyone can read it, and why we read it the way they do.

## Context

Every search asked the sites and every read opened the page, so the same bobotie was read again and again. Louis wants a
repository of recipes that grows as people search: "we must just store each read from a site we do not have yet, and then we
serve those to ourselves. but we must still make sure we get new recipes, like having different types of milktart". Then:
"recipes can never become stale", and "it will be served open via the api, we will read it from there as a source".

The danger in any cache like this is freezing: answer from what you hold and you stop finding anything new. A library-first
search would stop at the few kinds of melktert it already had.

## Decision

- **Each new read from a site in our table is kept for good.** What's kept is the print as the cook read it, text pages too. A
  page from anywhere else is read and never kept, because a stored page reaches other people and could carry instructions.
- **The library is the recipes pack's feed**, open with no key at `/v1/feeds/recipes` (ADR 0070), and the MCP server offers it
  as a resource (ADR 0072). Each recipe carries its site's name and a link to its own page, and nothing about who asked.
- **Our cook reads that feed over HTTP**, as anyone would, and holds it for five minutes. The store and the reader meet only at
  the feed, so the feed's first customer is ourselves.
- **The sites are still asked on every search.** The library is one more voice among them, in a random place, with its pages in
  any order. A page we hold costs no browser read whoever listed it, so savings grow as it fills, and new recipes keep coming.
- **A text page's print ends at the recipe,** because real pages run on into readers' comments with their names, which an open
  feed must not publish.
- **An eval's trial leaves the library alone** unless `RECIPE_LIBRARY_IN_EVALS=true`, so one run doesn't change the next.

## Consequences

- A second read of a page opens no browser. A search takes as long as before: that's the price of always finding new recipes.
- A copy never changes. If a site corrects a recipe we keep the old one, and a photo that later disappears shows as broken.
- Search is `QueryWords` over the feed in memory. That's fine for thousands of recipes, and the whole feed comes over on each
  refresh, so it will want paging when it's several MB. Search by meaning is F35.6.
- **Publishing whole copies goes further on the sites' terms than reading them.** Some name commercial copying, Food & Home's
  clause 2.9.1 for one. Louis made this call knowing that, on 2026-11-05, for all 45 sites, with each site credited and linked
  and removed on request. It isn't legal advice, and a qualified check is worth having before customers depend on it.
- A site leaves by coming out of the table, which stops its recipes at once, and by `PurgeSiteAsync`, which deletes its copies.
  No page is removed for any other reason.
- `IPackContext` gained `InATrial`, the kind of growth the TODO's *Each pack declares what it needs* is meant to end.

## Links

- [ADR 0070: packs publish data feeds](../../../src/docs/adr/0070-packs-publish-data-feeds.md)
- [ADR 0067: a page's own data before its text](../../../src/docs/adr/0067-a-pages-own-data-before-its-text.md)
- [ADR 0078: a recipe search asks several sites](0078-a-recipe-search-asks-several-sites.md)
- [For site owners](../../../demos/puls/docs/SITE_OWNERS.md)
- [Cache-Aside, Microsoft](https://learn.microsoft.com/azure/architecture/patterns/cache-aside)

---
[Docs index](../../../src/docs/README.md) · Related: [ADR index](../../../src/docs/adr/README.md)
