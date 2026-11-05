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

- **Each new read from a site in our table is kept for good, as a proper recipe.** What's kept is the page's own recipe, field
  by field: the name, the yield, the times, each ingredient, each step, the photo, the author and the page. A page with no recipe
  data is read for the cook and not kept, because a guess at its structure would be served for good. A page from anywhere else is
  read and never kept, because a stored page reaches other people and could carry instructions.
- **The library is the recipes pack's feed**, open with no key at `/v1/feeds/recipes` (ADR 0070), and the MCP server offers it
  as a resource (ADR 0072). Each recipe carries its site's name and a link to its own page, and nothing about who asked.
- **Each recipe has its own address on our API,** `/v1/feeds/recipes/{id}`, and a search hands the cook that address. `ReadRecipe`
  reads it over HTTP, as anyone would, so the library is a real source and not a lookup behind the site's link. The store and the
  reader meet only at the feed, so the feed's first customer is ourselves. A search holds the list for five minutes.
- **The credit stays the site's.** A kept recipe prints exactly as the live page did, and its `Link:` line is the site's page, so
  the answer links the site. Our address is only how the cook fetches it.
- **An address counts as ours only on our own path, with a real id,** and the request is built from our setting, not from what the
  model gave. Anything else goes through the browser and its guards.
- **The sites are still asked on every search.** The library is one more voice among them, in a random place, with its pages in
  any order. A page we hold that a site also lists gets our link, so savings grow as it fills, and new recipes keep coming.
- **The site's own address still goes to the site.** A search that names one site, or a web search, gives a site's link, and
  `ReadRecipe` follows it. What it finds is ignored if we hold that page, so the first copy stays.
- **An eval's trial leaves the library alone** unless `RECIPE_LIBRARY_IN_EVALS=true`, so one run doesn't change the next.

## Consequences

- A hit from the library opens no browser. A search takes as long as before: that's the price of always finding new recipes.
- **A page with no recipe data never enters.** In one measured run 12 of 30 answers used such a page, so the library can't serve
  those however often they're read, and the live print still ends at the recipe (`PrintedRecipeText`), which spares the cook the
  readers' comments.
- `IDataFeed` gained `ReadItemJsonAsync`, with a default that gives nothing, and the API `GET /v1/feeds/{name}/{id}`, open like the
  feed. A feed's items have no MCP resource of their own.
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
