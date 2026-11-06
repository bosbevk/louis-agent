# 0086. A pack's screens are described

**Status:** Accepted, replaces how [ADR 0083](0083-packs-keep-reports-behind-the-admin-key.md) reaches the page · **Date:**
2026-11-06 (Louis: "why don't packs have their own project, where the web controls live?", "should be overrideable and
customised", "all our pack controls will work like that") · **Recorded:** 2026-11-07

## Context

A pack could show whoever runs it one thing: a report, which was a table of numbers on one page. The next things wanted, managing the
recipe library and the git and file controls, don't fit a report, and a page per pack would mean every site writing pages that
name packs. Three shapes were weighed:

- **The plugin ships real pages** ([Microsoft](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/class-libraries?view=aspnetcore-10.0),
  [Backstage](https://backstage.io/docs/frontend-system/architecture/extensions)): full freedom, but every pack needs web code for
  an ordinary table, and a pack that ships web code can't be taken without it.
- **The plugin describes the screen** ([Django admin](https://docs.djangoproject.com/en/5.2/ref/contrib/admin/),
  [Filament](https://filamentphp.com/docs/4.x/resources/overview)): one generic page draws it, with no web code in the pack, but
  a description can only say what its vocabulary has.
- **Described first, a real component when needed:** the second, with the first as the way out.

We picked the third. [MCP Apps](https://modelcontextprotocol.io/docs/extensions/apps) is the one formal standard here, built for
screens inside a chat, so it doesn't fit an admin; the idea kept from it is that a screen can do only what the server named.

**Where this goes against them:** Django and Filament draw on the server, and our description crosses the wire as JSON. We found no
strong source on doing that for an admin. The risk is a vocabulary that grows without end, so the rule below limits it.

## Decision

*A pack describes its screens as plain data and one generic page draws them. When a description can't say what's wanted, a real
component replaces it by name, and the last to register wins.*

- **`IDataScreen`** in the composition (`Packs/Screens/`): a `ScreenSpec` (name, title, sentence, columns, whether it searches) and
  `ReadAsync`, answering a page of rows and the total. `IAgentPack.Screens` is empty unless a pack lists some, and `AgentCatalog`
  gathers them and refuses a name twice. A store that's down gives an empty page and never throws.
- **Two routes on the admin group,** `GET /v1/admin/screens` and `GET /v1/admin/screens/{name}/rows`, which name no pack.
- **One page, `/admin/screens/{name}`, in the framework.** A site writes no page and no link: `PackMenu` in `AdminShell` lists the
  screens, and each site's router finds the framework's pages through `FrameworkPages.Assemblies`.
- **`PackViews` is the override list** (Registry, Fowler): a name to a component, the plain `ScreenTable` by default. A site fills it
  at start, and order is precedence: the framework's default, a pack's view, a demo's.
- **A pack's real components live in `packs-web/`,** never in the pack. A pack references no web and its web project references no
  pack, so someone can take the tools alone and a browser never carries a pack's database driver. A test project under
  `packs-tests/` sees both and holds the names they share. `PackWebBoundaryTests` holds the rule.
- **A new kind of column, field or control enters only when a second screen needs it;** until then it's a custom component.
- **Only a web address becomes a link.** A value can come from a page we read, and a `javascript:` address would run in the admin's
  tab, with its key.

Everything sits behind the admin key, and none of it is a tool, so no model can call it.

## Consequences

- A pack gets an ordinary table with no web code, and a new screen shows on every site with no change to any of them.
- Reports went: `IDataReport`, its route, `ReportCard` and the Reports page. The recipes counter is `RecipesUsageScreen`, with the same
  numbers. What ADR 0083 decided about counting (one record per day, a small write beside the work, days in UTC) stands.
- The name and column headings a pack's view reads are written twice, once on each side of the wire. Only the test project stops them
  drifting.
- A view is built into the site, so swapping one takes a rebuild, though a description doesn't.
- A screen can't be served as an MCP App today. A description is plain data, so it could be drawn that way later; no plan names it
  ([ADR 0055](0055-keep-it-simple.md)).

## Links

[ADR 0027](0027-engine-identities-and-packs.md) · [ADR 0028](0028-packs-stay-convertible-to-mcp.md) ·
[ADR 0071](0071-packs-take-data-uploads.md) · [ADR 0083](0083-packs-keep-reports-behind-the-admin-key.md) ·
[ADR 0084](0084-every-site-is-built-on-the-frameworks-web.md) ·
[Plugin (Fowler)](https://martinfowler.com/eaaCatalog/plugin.html) · [Registry (Fowler)](https://martinfowler.com/eaaCatalog/registry.html) ·
[Write-up 20](../learning/20-extensibility.md#described-screens-f595-2026-11-07)

---
[ADR index](README.md)
