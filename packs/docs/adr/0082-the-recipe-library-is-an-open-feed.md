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
  by field: the name, the yield, the times, each ingredient, each step, the photo, the author and the page. A page from anywhere
  else is read and never kept, because a stored page reaches other people and could carry instructions.
- **A page with no recipe data is kept only if it plainly lays one out.** `PageTextRecipe` looks for an ingredients heading, the
  lines under it, a method heading and the steps, in code, and anything doubtful gives nothing, because what's kept is served for
  good. The cook's first read of such a page stays its text, so what it sees doesn't depend on what we could keep.
- **A page we opened and found no recipe on is excluded, in Mongo.** `ReadRecipe` refuses it and the cook reads another hit, and
  from then on a read refuses it without opening it and a search leaves it out. A search hit can't show whether a page has a
  recipe, so each page costs one look and only one. Only a page we truly read counts: a site that's down, a refusal or an empty
  page says nothing about the page, and never goes on the list. It's private, and clearing it gives those pages another chance.
- **A refused page doesn't count as a read.** The cook keeps going until it has a recipe: every hit that fits, another search,
  then a web search. Three refusals in a row once left it answering with no recipe, with hits still to try.
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
- **A search gives 10 pages, and 3 places stay for pages from outside the library,** when there are that many, so a full library
  can't crowd out a new recipe.
- **The cook isn't pushed towards the library.** Our links keep their random places, and the cook reads one when its title fits
  best. Louis's call, 2026-11-05, over marking them or giving them the first places: "it will make sure we get more recipes".
  Finding new recipes matters more than the saving.
- **The site's own address still goes to the site.** A search that names one site, or a web search, gives a site's link, and
  `ReadRecipe` follows it. What it finds is ignored if we hold that page, so the first copy stays.
- **An eval's trial leaves the library alone** unless `RECIPE_LIBRARY_IN_EVALS=true`, so one run doesn't change the next.

## Consequences

- A hit from the library opens no browser. A search takes as long as before: that's the price of always finding new recipes.
- **Some pages with no recipe data can't enter:** those that are plainly no list of ingredients and steps, such as a recipe told
  in an article. They're excluded, which costs the cook those pages, though answers from a page's text did no worse on the judge than answers from
  recipe data. Louis's call: every answer should be a proper recipe that gets saved. A check of 123 pages across our sites, 3 a
  site, kept 80 after the reader's fixes; Recipes from South Africa gave no text on any page and left the table.
- **A page read this way has no times and no author, and a reading mistake lasts.** Real pages showed a cookie notice kept as a
  step, an oven step stored as a part's name, and an instruction stored as an ingredient. So a line after a list ends it, a short
  line before the list is the first step, a line in the middle or an instruction among the ingredients keeps the page out, and
  the two records already wrong were deleted by hand. A copy that never goes stale also never corrects itself.
- **Measured, library off against on, 5 repeats, 50 answers each** (`e2b47743f792`, `24635786d616`): judge 43 against 40, time 56 s
  against 55 s, cost $0.00633 against $0.00649, biggest site 20% against 14%. That missed the win test on accuracy and cost, because
  the cook read only 10 of the 69 recipes it was offered. The 9 answers built from a library copy did better than the same dishes
  with the library off: 45 s against 56 s, the cook's cost $0.00556 against $0.00617, judge 8 of 9. Pages new to the run scored 29
  of 36 in both.
- **So the library saves little for now, and we accepted that.** Our links in places 1 and 2 were read 24% of the time and lower
  down 8%, so first places would raise its use, at the cost of new recipes.
- **Measured again on a full library** (`5188a891aa20`, 44 recipes at the start, with the 10 results, the 3 places, Best Recipes, the
  other names and the exclusion list): judge 42 of 50, 52 s, $0.00628, against the library off's 43, 56 s and $0.00633. So it met
  the win test on time and cost and missed accuracy by one answer, with no push towards the library: the cook built 31 of 50
  answers from a copy, at 47.5 s against 60.3 s for answers that read none. It kept 21 new recipes, and new pages fell by repeat
  from 10 to 3, since only 10 dishes are searched. Pap en sous went from about 68 s and outside sites to 39 s on Best Recipes.
- **Excluding cost real recipes, so the list can be corrected.** Of the 22 pages on the list by then, 17 were real recipes, most in
  shapes the reader didn't handle, such as a "What you need" heading or plain lines with no headings at all. A second reader
  (FX50) reads 16 of them, and those came off the list by hand. FinGlobal's 2026 curry, told as an article, and the 5 that aren't
  recipes stay on. A reader that improves doesn't clear the list by itself: someone reads what's on it.
- `IDataFeed` gained `ReadItemJsonAsync`, with a default that gives nothing, and the API `GET /v1/feeds/{name}/{id}`, open like the
  feed. A feed's items have no MCP resource of their own.
- A copy never changes. If a site corrects a recipe we keep the old one, and a photo that later disappears shows as broken.
- Search is `QueryWords` over the feed in memory. That's fine for thousands of recipes, and the whole feed comes over on each
  refresh, so it will want paging when it's several MB. Search by meaning is F35.6.
- **Publishing whole copies goes further on the sites' terms than reading them.** Some name commercial copying, Food & Home's
  clause 2.9.1 for one. Louis made this call knowing that, on 2026-11-05, for the 45 sites then in the table, and for Best Recipes, added after, whose terms claim ownership and are silent on reuse. Afrifood Network stays out of the table: its terms bar republishing substantial parts without written permission, with each site credited and linked
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
