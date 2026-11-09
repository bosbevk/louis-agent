# Get a site found by search

> **How-to.** What makes a site on the framework show up in search, and the steps its owner takes that no code can:
> Search Console, the sitemap, checking the structured data, and links. **Elsewhere:** why the pages are served as
> HTML in [ADR 0109](../adr/0109-a-page-is-served-as-html.md), why a page writes one head in
> [ADR 0110](../adr/0110-a-page-says-what-it-is-in-one-head.md), making a site in [Spawn a site](spawn-a-site.md).

Nobody can promise a place on page one. Google ranks a page mostly by how many good sites link to it, and that comes
from people, not code. What the framework does is make sure that when a search engine looks, it reads every page
whole, knows what each one is about, and can find them all.

## What the framework already does

- **Every page is served as HTML** with its own title, description and canonical link, so a crawler that runs no
  script reads it (`MapLouisAgentSite`, ADR 0109).
- **Structured data:** the facts a page passes to its `PageHead` go into the served head as JSON-LD, and every page
  under the landing gets a breadcrumb of the site and the page (ADR 0110).
- **Every place has an address** listed in the site's `sitemap.xml`, which a site test writes from the pages, and
  `robots.txt` points at it (ADR 0107).

## What a site does

1. **Give every page meant for search a `PageHead`** with a description of 50 to 170 characters, none repeated. The
   site's own test (for louis-agent, `PageDescriptionsTests`) holds it.
2. **Say who runs the site on the landing:** an organisation, the website and, for software, what it is, as the
   framework's `Schema*` records in the landing's `PageHead` (louis-agent keeps them in `Content/SiteFacts.cs`).
3. **Add an `apple-touch-icon.png`**, 180 by 180 with no transparency, linked in `index.html`. It is the icon a phone
   puts on its home screen, and a good logo for the organisation.
4. **Write a page for what people type.** One page per search you want to be found for, in plain words, at an address
   that says it (`/csharp-ai-agent-framework`). Say of anyone else only what their own docs say, and link them.
5. **Regenerate the sitemap** when pages change: `LOUIS_UPDATE_GOLDEN=1 dotnet test` on the site's tests with
   `--filter SitemapTests`, then commit `wwwroot/sitemap.xml`.

## What the owner does, once

1. **Google Search Console** (search.google.com/search-console): add a *Domain* property for the site's domain
   (`louis-agent.dev`). Google gives a TXT record; add it in Cloudflare's DNS for the domain and press Verify.
2. **Submit the sitemap** under *Sitemaps*: `https://louis-agent.dev/sitemap.xml`. Google reports how many addresses
   it found and how many it indexed.
3. **Ask for the important pages first** under *URL inspection*: the landing and `/csharp-ai-agent-framework`, then
   *Request indexing*. A new site waits days to weeks otherwise.
4. **Check the structured data** with Google's Rich Results Test (search.google.com/test/rich-results) on the landing
   and on one other page: it should find the organisation and the breadcrumb.
5. **Bing Webmaster Tools** (bing.com/webmasters) can import the site from Search Console in one step, sitemap
   included.

## What helps after that

- **Links from other sites.** A LinkedIn profile and posts that link the site, an article about building agents in C#
  that links the comparison page, a helpful answer where someone asks which C# agent framework to use. Never pay for
  links or post the same link everywhere: search engines count that against a site.
- **The packages, when they ship.** A NuGet package's project link points at the site, and its page is one more link
  people find (F74).
- **No link to the repository** while it is private: a link people follow to a page they can't open helps nobody.
- **Measure.** After a few weeks Search Console's *Performance* report shows which searches showed the site, where it
  ranked and how often people clicked. Look up "c# agent framework" there before deciding what to write next.

---
[Docs index](../README.md) · Related: [Guides](README.md) · [ADR 0110](../adr/0110-a-page-says-what-it-is-in-one-head.md) ·
[Spawn a site](spawn-a-site.md)
