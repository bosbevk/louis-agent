# For site owners: what Puls reads, and how to reach us
> **Reference.** If `Puls/1.0 (+this page)` appears in your server's logs, this says what it is, what it reads, how
> often, and how to ask us to stop or to talk. **Elsewhere:** the decision behind it in
> [ADR 0035](adr/0035-the-browser-says-what-it-is.md); the project in the [README](../README.md).

## What Puls is

Puls is an assistant for people in South Africa, built in this repository. When a person asks it something that
needs a web page, such as a recipe or what an ingredient costs at the shop they will buy from, it fetches that page on
the person's behalf and reads it, the way the person's own browser would if they opened the page themselves. It does
not crawl, index or republish sites.

## What it sends

Every request from its browser carries the user agent
`Puls/1.0 (+https://github.com/bosbevk/louis-agent/blob/main/docs/SITE_OWNERS.md; reads a page on a user's behalf)`,
so you can tell it apart from a person's browser, allow it, or block it. It does not pretend to be a desktop browser
and does not hide that it is automated ([ADR 0035](adr/0035-the-browser-says-what-it-is.md)). It never sends the
person's name, address or anything else about them.

## What it reads, and how much

- One page at a time, for one person's question: a recipe page, a shop's search page for an ingredient, a store
  locator's answer. It is told to use the fewest requests that answer the question, to read one search page per
  ingredient at one shop, and not to walk a site's categories or product pages.
- Pages a site serves to everyone. It does not log in, does not accept terms on a person's behalf, and does not make
  WebSocket connections.
- Images are shown to the person from your own image host, as your page serves them.
- It does not read `robots.txt` yet (that comes with *Sources as data* in the TODO). If your site disallows automated
  readers there, tell us (below) and your domain is honoured by name until the fetcher reads the file itself.

## Where it comes from

During development it runs from one home connection in South Africa and from one Azure Container Apps instance in
South Africa North; the browser that reads pages runs on the home connection. There is no fleet.

## To ask us to stop, or to talk

Open an issue at <https://github.com/bosbevk/louis-agent/issues> with your domain and what you'd like: to be excluded,
rate-limited, or to offer a feed or an API instead, which we would far rather use than your pages. A request to stop is
honoured without argument.
