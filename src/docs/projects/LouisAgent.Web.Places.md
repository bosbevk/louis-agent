# LouisAgent.Web.Places — design

> **Kind:** Razor library (`net10.0`, runs in the browser) · **Path:** `src/web/LouisAgent.Web.Places/` ·
> **References:** LouisAgent.Web, Microsoft.AspNetCore.Components.Web · **Used by:** LouisAgent.Site
> (louis-agent.dev) and LouisAgent.SiteFramework, each naming `.Places()` among its areas

![Every place's own address: the area a site names, the router's catch-all, and the page's own frame](diagrams/LouisAgent.Web.Places.svg)

## Purpose

On a site built on the framework, every place on a page has an address of its own: a section, such as
`/how-it-works/the-web`, a card, or a page of a section that pages sideways. Someone can open it, share it or be
sent to it, and the guide can take a visitor there. The rule that writes and reads those addresses is the framework
web's `SiteAddresses`. This library is what makes an address work when it is opened afresh, typed in or followed from
a link, and what a site names to have it.

It is a package of its own so that a site that wants its places takes only them, and not the admin's pages that
used to come with the framework's web.

*Decided in:* F118, F164.1, [ADR 0107](../adr/0107-every-section-has-an-address.md).

## How it works

The story follows one address, `/how-it-works/the-web`, opened in a new tab. Part 1 is how the site has its places
at all, part 2 how the address finds its page, and part 3 the frame the page is drawn in.

### 1. A site names its places

A site names its places once, among its areas, with the word its landing's places sit under and its own assembly:

```csharp
areas.Flat().Framework().Docs(DocsAddress.GitHub("bosbevk/louis-agent", "main"))
    .Places(FrameworkAreas.Home, typeof(App).Assembly);
```

`Places(home, site)` is an extension on the framework's `SiteAreasBuilder`, kept here as the docs' and the accounts'
areas are. It brings the catch-all's assembly to the site's router, and registers the site's `SiteAddresses`. Those
are built from the routes the pages already declare, in the site's assembly and in every assembly its other areas
bring, such as the framework's pages. They are built the first time they are asked for, once every area has been
named, so an area named after `.Places()` counts too. The landing's places sit under the home word, as in
`/overview/the-engine`, so they never clash with a page of the same name.

Only the pages anyone may open have places. A page behind a sign-in, which says so with Blazor's `[Authorize]`, and
a page under `/admin`, behind the admin's key, have none. So a place's address can never draw such a page around the
router's own check. It is the same rule the host draws pages for a search engine by.

*Decided in:* F164.1, [ADR 0120](../adr/0120-nothing-is-hand-wired-from-the-framework.md).

### 2. The address finds its page

No page declares a route for `/how-it-works/the-web`; the How it works page declares only `/how-it-works`. So the
router finds no page for the address, and tries its last resort: `SectionPage`, whose route is `/{*Path}`, which
Blazor tries only after every other.

`SectionPage` asks the site's addresses to read the address back into a page and a place. When it names a page of the
site, it draws that page with Blazor's `DynamicComponent`, just as the page would be drawn at its own address. The
site's frame then takes the visitor to the place. When the address names no page, or the site has no addresses, it
hands the visitor to the not-found page.

![A place's address: the router hands the catch-all the path, it reads which page that names and draws it](diagrams/LouisAgent.Web.Places-components-section-page.svg)

### 3. The page's own frame

The router asks the page it draws for its layout. `SectionPage` has none of its own, since it draws whichever page
the address names. So a place on a page with its own layout, such as a business's demo in the showroom, would be
drawn in the site's frame, and lose the demo's container on a refresh.

`PlaceLayout.Of(route, fallback, addresses)` answers that. A site's router hands it to its `RouteView` as the default
layout: when the routed page is the catch-all, and its address names a page that has a layout of its own, it gives
that layout. Otherwise it gives the site's default. It uses only the router's own public parts, so nothing of how
Blazor draws a page is copied here:

```razor
<RouteView RouteData="@routeData" DefaultLayout="@PlaceLayout.Of(routeData, typeof(FrameworkLayout), Addresses)"/>
```

*Decided in:* F127.1.

These are the types involved:

![The types: Places() registers the site's addresses, read from its areas' pages; the catch-all and PlaceLayout read them](diagrams/LouisAgent.Web.Places-interface.svg)

## Structure

The place to look a file up.

```
src/web/LouisAgent.Web.Places/
├── PlacesArea.cs         # .Places(home, site), the area a site names
├── SectionPage.razor     # the catch-all route, /{*Path}, and its code-behind
├── PlaceLayout.cs        # the named page's own layout, for the site's router
└── _Imports.razor
src/tests/web/LouisAgent.Web.Places.Tests/   # PlaceLayoutTests
```

The rule itself, `SiteAddresses`, stays in the framework's web (`Components/Site/Addresses/`), since the guide, the
section index and the address bar read it too.

## Configuration

None of its own. The home word and the assembly are the site's, in its areas.

## Extending it

A site that wants its places names `.Places(home, typeof(App).Assembly)` among its areas, and hands
`PlaceLayout.Of` to its router's `RouteView`. A package of pages a site turns on gets its places with no more work,
since the area reads the pages of every area the site names.

## Tests

The tests are in `src/tests/web/LouisAgent.Web.Places.Tests`. `PlaceLayoutTests` hold that a place on a page with
its own frame is drawn in it, and that everything else gets the site's default.

Each site that names its places holds them too: the framework's site's `PlacesTests` hold that a family's address
names the Overview at its section and that the router has the catch-all, and louis-agent.dev's `SiteLinksTests` hold
every link on its pages to a place on it.

## Limits and plans

An address is read only from a page's fixed route: a page whose route has a parameter has no places.

## Related docs

[LouisAgent.Web](LouisAgent.Web.md) · [LouisAgent.SiteFramework](LouisAgent.SiteFramework.md) ·
[LouisAgent.Site](LouisAgent.Site.md) · [ADR 0107](../adr/0107-every-section-has-an-address.md)

---
[Docs index](../README.md) · Related: [Projects](README.md) · [Plans](../plans/README.md)
