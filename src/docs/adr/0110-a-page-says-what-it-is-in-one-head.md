# ADR 0110: A page says what it is in one head, and the framework adds where it sits

**Status:** Accepted (2026-11-09, F123)

## Context

F121 (ADR 0109) served every page as HTML with its own title, description and canonical link. A search engine still
had to guess the rest: who runs a site, what the software is, and where a page sits in its site. Search engines read
that from *structured data*, facts written in schema.org's words as JSON-LD (a `<script type="application/ld+json">`
in the head). Louis's goal (2026-11-09): page one of Google for "c# agent framework" and "c# ai framework". Of three
options each he chose **technical work and one page written for the search** (over technical only, and content first),
structured data from **a component in the page's head** (over one site record at start-up, and static tags in
`index.html`), and the page at **`/csharp-ai-agent-framework`**.

The first build placed a `PageSchema` component beside `PageDescription`. On the landing, three of them pushed the
description out of the head: Blazor keeps only the last `HeadContent` a page renders, so the prerender found no
description and served the plain shell. Of three fixes Louis chose **one `PageHead`** (over a description component
that also takes schemas, whose name would say less than it does, and two components joined by a named section, which
couples them and drops a schema on a page with no description).

## Decision

- **One component writes a page's head.** `PageHead` (`Components/Site/Meta/`) takes the page's `Description` and an
  optional `Schemas` list, and writes the description meta tag and one JSON-LD script per schema in a single
  `HeadContent`. A page never writes its own `HeadContent` beside it.
- **The facts are records.** `SchemaOrganization`, `SchemaWebSite`, `SchemaSoftwareApplication` and
  `SchemaBreadcrumbList` (`Meta/Schema/`) are plain records with `@context` and `@type` as their names, written by one
  options object (`SchemaJson`) whose encoder keeps a `</script>` in the words from closing the script (ADR 0041).
- **The prerender carries them, and adds a breadcrumb.** `PrerenderedHtml` reads every JSON-LD script from the
  rendered head into `RenderedPage.Schemas`, and `PageShell` writes them before the served page's head closes. For
  every page under the landing it adds a `BreadcrumbList` of its own: the site's name (from `og:site_name`), then the
  page's title without the site's, at the page's route. No page declares its breadcrumb.
- **A site's facts are its own.** The framework writes them; the site says them. The louis-agent site keeps its three
  in `Content/SiteFacts.cs`, on its landing, with a 180 by 180 `apple-touch-icon.png` as the logo.
- **A page about other frameworks says of them only what their own docs say, and links each.** The comparison page's
  claims about Microsoft Agent Framework and Semantic Kernel come from Microsoft's pages, read on a date the page
  shows; its claims about louis-agent are held to the repo by a test. It never says a rival needs what its docs don't
  say it needs.

## Consequences

- `curl` of the landing shows the organisation, the website and the software; of any other page, its breadcrumb.
  Google's Rich Results Test reads them from the served HTML, with no script run.
- `PageDescription` is gone: every page of the three sites says `PageHead`, renamed by Rider. A page that wants a fact
  in its head passes it to `PageHead`, and a test (`TwoHeads_OnlyTheLastReachesTheHead`) keeps Blazor's rule in view.
- The API keeps its own breadcrumb record, the same shape as the web library's: the API serves any site and references
  no site's library (ADR 0027), so `PageShellTests` hold the JSON.
- Ranking is not promised. Google weighs links from other sites most, which no code here makes; the
  [how-to](../guides/get-a-site-found.md) lists what the site's owner does.

## Links

- [ADR 0109: a site's page is served as HTML](0109-a-page-is-served-as-html.md)
- [ADR 0107: every place on a site's page has its own address](0107-every-section-has-an-address.md)
- [ADR 0084: every site is built on the framework's web](0084-every-site-is-built-on-the-frameworks-web.md)
- [ADR 0041: JSON through records](0041-json-through-records-by-hand-only-for-bytes.md)
- F123 in the work log
- The comparison page's sources, read 2026-11-09: the
  [Agent Framework overview](https://learn.microsoft.com/en-us/agent-framework/overview/agent-framework-overview)
  (the direct successor to Semantic Kernel and AutoGen; .NET, Python, Go in preview; MCP clients), its
  [README](https://github.com/microsoft/agent-framework) (graph workflows, checkpointing, middleware, OpenTelemetry,
  Foundry hosted agents and A2A in hosting, MIT), the
  [Semantic Kernel overview](https://learn.microsoft.com/en-us/semantic-kernel/overview/) (a development kit for C#,
  Python and Java) and its [README](https://github.com/microsoft/semantic-kernel) ("Semantic Kernel is now Microsoft
  Agent Framework", Agent Framework at version 1.0, MIT). None of them says either builds on Microsoft.Extensions.AI or
  tracks a request's cost, so the page claims neither.
