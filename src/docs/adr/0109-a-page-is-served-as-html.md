# ADR 0109: A site's page is served as HTML, rendered once per deploy

**Status:** Accepted (2026-11-09, F121)

## Context

Every site on the framework is a Blazor WebAssembly app: the host answered every page's address with the same
`index.html`, holding the landing's title and description and an empty body until the app had loaded in the browser.
Google runs that script, slowly. Most other crawlers, link previews and AI crawlers don't, so for them every page of
every site was the landing, with no words. The sitemap and `robots.txt` (F119, ADR 0107) told them where the pages
were, and each one answered the same nothing. Louis, 2026-11-09: "we also need to make the website SEO friendly",
then "but again this should be built into the webframework". Of three options each, he chose to **prerender each page**
(over moving the sites to server prerendering, which changes how every site is hosted, and meta tags alone, which
leave the body empty) and to do it **in the API, on request** (over a publish step writing files, and a test writing
them into git).

## Decision

- **A page says what it is about.** The framework's `PageDescription` sits beside Blazor's own `PageTitle`: it writes
  the page's `<meta name="description">` through `HeadContent`, so the app shows it in the browser too. A site's page
  meant for search declares both; each site holds its pages to it with a test (every page, 50 to 170 characters, none
  repeated, no semicolons).
- **The API renders a site's page to HTML.** `MapLouisAgentSite(app, typeof(App).Assembly, services => …)` replaces
  `MapFallbackToFile("index.html")` in a host's program. It finds the page an address names from the site's own
  routes (a section's address renders its page, the landing's under the home word), renders it with ASP.NET Core's
  `HtmlRenderer` beside a `HeadOutlet`, and puts the page's title, description, link card, canonical link and body
  into the site's `index.html`. The app replaces that body when it starts, as before.
- **Rendered once per deploy.** A page is rendered the first time any address under it is asked for and kept in
  memory until the process restarts, which is a deploy. Nothing is written to disk or to git.
- **What a crawler sees is what the page says before the app starts.** No script runs and no `OnAfterRender`: the
  page's words, its sections and its links. The services a page injects come from the site (one registration callback
  in the program); the framework adds a navigation that stays put and a script runtime that does nothing.
- **Only a page that declares a description is served as HTML.** A chat, an admin screen or an account page has
  nothing a search engine wants, and some can't render without their live services, so they answer the plain shell.
  A page that throws while rendering answers the plain shell too, logged once. An address that names no page answers
  the shell with a 404.
- **The public address is the request's.** The canonical link and `og:url` use the scheme and host the request came to
  (behind Azure's proxy, the forwarded ones), so there is no setting to keep right; a host may pass one.

## Consequences

- `curl` of any page's address returns its own title, description, canonical link and words; search engines, link
  previews and AI crawlers that run no script see each page as a person does.
- The first request after a deploy for each page pays for its render (tens of milliseconds); the rest are a lookup.
- Once the app starts, the head holds the description twice, the shell's and the page's own, with the same text.
  Blazor's head outlet adds rather than replaces, and a crawler that runs no script sees one. Left as it is.
- A page that injects a service its site didn't register renders nothing and falls back to the shell, with a warning
  in the log: the log is where a missing service shows.
- A program maps `MapLouisAgentSite`, not `MapFallbackToFile`; the route table's test allows it and the API guide's
  whole program uses it.

## Links

- [ADR 0107: every place on a site's page has its own address](0107-every-section-has-an-address.md)
- [ADR 0106: the landing is a business website from the framework](0106-the-landing-is-a-business-website-from-the-framework.md)
- [ADR 0084: every site is built on the framework's web](0084-every-site-is-built-on-the-frameworks-web.md)
- [ADR 0051: flat conditionals and named endpoints](0051-flat-conditionals-and-named-endpoints.md)
- F121 in the work log
