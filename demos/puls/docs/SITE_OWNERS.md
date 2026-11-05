# For site owners: what Puls reads, and how to reach us

> **Reference.** If `Puls/1.0 (+this page)` appears in your server's logs, this says what it is, what it reads, how
> often, and how to ask us to stop or to talk. **Elsewhere:** the decision behind it in
> [ADR 0038](../../../src/docs/adr/0038-the-browser-says-what-it-is.md); the project in the [README](../../../README.md).

## What Puls is

Puls is an assistant for people in South Africa, built in this repository. When a person asks it something that
needs a web page, such as a recipe, it fetches that page on the person's behalf and reads it, the way the person's
own browser would if they opened the page themselves. It does not crawl a site. **It does keep what it reads:** a recipe
page from one of the sites in its table is kept and published, as below.

## What it sends

Every request from its browser carries the user agent
`Puls/1.0 (+https://github.com/bosbevk/louis-agent/blob/main/demos/puls/docs/SITE_OWNERS.md; reads a page on a user's behalf)`,
so you can tell it apart from a person's browser, allow it, or block it. It does not pretend to be a desktop browser
and does not hide that it is automated ([ADR 0038](../../../src/docs/adr/0038-the-browser-says-what-it-is.md)). It never sends the
person's name, address or anything else about them.

## What it reads, and how much

- One page at a time, for one person's question: a recipe page, a map service's answer (where a shop's branch is). It
  is told to use the fewest requests that answer the question and not to walk a site's categories. It reads no
  shop's search, category or product pages: until a retailer gives permission or a feed, Puls shows estimated prices
  and names the shop's branch (asked of PnP, Woolworths and Checkers on 2026-10-25).
- Pages a site serves to everyone. It does not log in, does not accept terms on a person's behalf, and does not make
  WebSocket connections.
- Images are shown to the person from your own image host, as your page serves them.
- It does not read `robots.txt` yet (that comes with *Sources as data* in the TODO). If your site disallows automated
  readers there, tell us (below): your domain goes on our list of sites the browser won't read.

## What it keeps and publishes

When Puls reads a recipe page from a site in its table for the first time, it keeps the recipe, for good: the name, the yield, the
times, the ingredients, the method, the photo's address, the author and the page's link. It takes these from the recipe data the
page publishes (schema.org), or, with no data, from the page's own *Ingredients* and *Method* headings. A page without either isn't
kept, and neither are readers' comments; we note its address, privately, so we don't read it again. Kept recipes are published
openly at `/v1/feeds/recipes`, each with your site's name and a link to your page, and its image still loads from your own host.
Answers that use a kept recipe link to your page. Nothing about who asked is kept.

## Where it comes from

During development it runs from one home connection in South Africa and from one Azure Container Apps instance in
South Africa North; the browser that reads pages runs on the home connection. There is no fleet.

## To ask us to stop, or to talk

Open an issue at <https://github.com/bosbevk/louis-agent/issues> with your domain and what you'd like: to be excluded,
rate-limited, or to offer a feed or an API instead, which we would far rather use than your pages. A request to stop is
honoured without argument, and it covers what we keep: your site's recipes stop being published and their copies are
deleted. Today that list is a change to the code, shipped in the next release; a list the browser
reads at run time is planned (the TODO's *A domain stop-list for the browser*).
