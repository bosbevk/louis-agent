# Get a site found by search

> **How-to.** What makes a site on the framework show up in search, and the steps its owner takes that no code can:
> Search Console, the sitemap, checking the structured data, and links. **Elsewhere:** why the pages are served as
> HTML in [ADR 0109](../adr/0109-a-page-is-served-as-html.md), why a page writes one head in
> [ADR 0110](../adr/0110-a-page-says-what-it-is-in-one-head.md), making a site in [Spawn a site](spawn-a-site.md).

![Getting found: what the framework does, what a site does, what the owner does once, and the links that help after that](diagrams/get-a-site-found.svg)

Nobody can promise a place on page one. Google ranks a page mostly by how many good sites link to it, and that comes
from people, not code. What the framework does is make sure that when a search engine looks, it reads every page
whole, knows what each one is about, and can find them all.

## What the framework already does

Every page is served as HTML, with its own title, description and canonical link, so a crawler that runs no script
still reads it whole. That's `MapLouisAgentSite`'s job.

The facts a page hands its `PageHead` go into the served head as structured data (JSON-LD), so a search engine knows
what the page is about. Every page under the landing also gets a breadcrumb of the site and the page.

Every place on the site has its own address, so a link can open a page at one section. But a section's address
answers its whole page, word for word, and a search engine that sees the same words at two addresses picks one as the
original and drops the other. So the framework tells it which one to keep. A section's canonical link names its page,
and the site's `sitemap.xml` lists the pages only, one address each. A site test writes the sitemap from the pages,
and `robots.txt` points at it.

The same goes for host names. A site sets `SITE_ADDRESS` to its one address, such as `https://louis-agent.dev`. Then
every canonical link is on it, and a page asked for on any other host, such as `www.louis-agent.dev`, answers a
permanent redirect there. Without the setting, the www copy would be a second site in Google's eyes.

*Decided in:* [ADR 0107](../adr/0107-every-section-has-an-address.md), [ADR 0109](../adr/0109-a-page-is-served-as-html.md),
[ADR 0110](../adr/0110-a-page-says-what-it-is-in-one-head.md), [ADR 0128](../adr/0128-a-site-names-one-address-per-page.md).

## What a site does

1. **Give every page meant for search a `PageHead`** with a description of 50 to 170 characters, none repeated. The
   site's own test holds it (for louis-agent, `PageDescriptionsTests`).
2. **Say who runs the site, on the landing**: an organisation, the website and, for software, what it is. They're the
   framework's `Schema*` records in the landing's `PageHead`. louis-agent keeps them in `Content/SiteFacts.cs`.
3. **Add an `apple-touch-icon.png`**, 180 by 180 with no transparency, linked in `index.html`. It's the icon a phone
   puts on its home screen, and a good logo for the organisation.
4. **Write a page for what people type.** One page per search you want to be found for, in plain words, at an address
   that says it (the framework's site has `/comparison`). Say of anyone else only what their own docs say, and link
   them.
5. **Set `SITE_ADDRESS` in the app's settings** to the site's one https address, in its Container App's yaml.
6. **Regenerate the sitemap when pages change.** Run the site's tests with `LOUIS_UPDATE_GOLDEN=1 dotnet test` and
   `--filter SitemapTests`, then commit `wwwroot/sitemap.xml`.

## What the owner does, once

![The owner's steps: add the domain to Search Console, submit the sitemap, ask for the important pages, check the structured data, import into Bing](diagrams/get-a-site-found-flow.svg)

1. **Add the site to Google Search Console** (search.google.com/search-console) as a *Domain* property for the
   site's domain (`louis-agent.dev`). Google gives you a TXT record. Add it in Cloudflare's DNS for the domain and press
   Verify.
2. **Submit the sitemap** under *Sitemaps*: `https://louis-agent.dev/sitemap.xml`. Google reports how many addresses
   it found and how many it indexed.
3. **Ask for the important pages first** under *URL inspection*: the landing and `/demos`, then *Request indexing*. A
   new site waits days to weeks otherwise.
4. **Check the structured data** with Google's Rich Results Test (search.google.com/test/rich-results) on the landing
   and on one other page. It should find the organisation and the breadcrumb.
5. **Add the site to Bing Webmaster Tools** (bing.com/webmasters). It can import the site from Search Console in one
   step, sitemap included.

## What helps after that

What helps most is links from other sites: a LinkedIn profile and posts that link the site, an article about
building agents in C# that links the comparison page, a helpful answer where someone asks which C# agent framework to
use. Never pay for links or post the same link everywhere, because search engines count that against a site.

When the packages ship, they help too. A NuGet package's project link points at the site, and its page is one more
link people find (F74). But don't link the repository while it's private: a link to a page people can't open helps
nobody.

When Search Console's *Pages* report says why pages aren't indexed, most rows are normal for a young site.
*Page with redirect* is an address that sends you on, like http to https or www to the site's address: Google keeps
the address it lands on, so don't validate that row. *Discovered, currently not indexed* is a page Google hasn't
crawled yet. The row to worry about is *Duplicate, Google chose different canonical than user*. It means two addresses
serve the same words and both claim to be the original, which the framework's canonical links and `SITE_ADDRESS` are
there to prevent.

Then measure. After a few weeks, Search Console's *Performance* report shows which searches showed the site, where it
ranked and how often people clicked. Look up "c# agent framework" there before you decide what to write next.

---
[Docs index](../README.md) · Related: [Guides](README.md) · [ADR 0110](../adr/0110-a-page-says-what-it-is-in-one-head.md) ·
[Spawn a site](spawn-a-site.md)
