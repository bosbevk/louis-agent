# LouisAgent.Web.Docs — design

> **Kind:** Razor library (`net10.0`, runs in the browser) · **Path:** `src/web/LouisAgent.Web.Docs/` ·
> **References:** LouisAgent.Web, Microsoft.AspNetCore.Components.Web · **Used by:** LouisAgent.Web.Framework, and
> the framework's own site, which names where its docs live

![The docs' parts: where the docs live, a link to a doc by its path, and the pages in reading order](diagrams/LouisAgent.Web.Docs.svg)

## Purpose

A page that explains the framework is written from its docs: an ADR, a project's design doc, a guide. This library
lets that page hand the reader on to the doc it came from, without the page knowing where the docs are kept. The
page says which doc, by its path in the repo, such as `src/docs/adr/0126-a-name-says-what-it-is-by-its-kind.md`.
The site says once where those paths lead. Today that is the repo on GitHub. Sending them anywhere else is a change
to that one setting, and no page changes with it.

It also holds the two parts a docs site steps through its pages with: the menu down the left, and the page before
and after at a page's foot, both read from one list of the pages so they can't disagree.

It sits apart from the framework's web so that a site with no docs ships none of it.

## How it works

The story is a reader on a page that explains something, who wants the doc behind it. Part 1 is how the site says
where its docs live, part 2 the link itself, and part 3 the menu and the page turns.

### 1. A site says where its docs live

A site names its docs among its areas, the way it names any part of the framework:

```csharp
areas.Flat().Framework().Docs(DocsAddress.GitHub("bosbevk/louis-agent", "main"))
```

`DocsAddress` is a small record holding one thing, the start of every doc's web address. `GitHub(repository, branch)`
makes the one for a repo on GitHub, `https://github.com/{repository}/blob/{branch}`, and `new DocsAddress(site)` makes
any other. `Docs(address)` is an extension on the framework's `SiteAreasBuilder`, kept in this library because the
framework's web can't name the docs' parts. The accounts' and the themes' areas are added the same way. The area
registers the address once, for every link on the site to ask.

*Decided in:* F164.1, [ADR 0120](../adr/0120-nothing-is-hand-wired-from-the-framework.md).

### 2. A link to a doc

A section of a page ends with the docs it was written from. `DocLinks` is that line, a `nav` a screen reader names
"The docs", and each `DocLink` inside it is one doc:

```razor
<DocLinks>
    <DocLink Path="src/docs/adr/0027-engine-identities-and-packs.md">Engine, identities and packs</DocLink>
</DocLinks>
```

The link asks the site's `DocsAddress` for the doc's whole web address, with `For(path)`, which joins the start and
the path with exactly one slash between them. It opens beside the page, in a new tab, so the reader keeps their place.

Beside them a `RepositoryLink` can sit, a link to another repository, such as a product built on the framework
elsewhere. It looks the same and opens the same way, but it is given its whole address, `Href`, since a doc link knows
its doc by its path here and this knows its address itself.

A site that puts a doc link on a page but names no docs' address is set up wrong. The link says so the first time it
is drawn, with an exception that tells the developer to add `.Docs(...)` to the site's areas, so the mistake is seen
at once and never ships as a link to nowhere.

![A section's doc links: the page hands each a path and its words, and each asks the address the site named](diagrams/LouisAgent.Web.Docs-components-doc-links.svg)

The page holds only the path, and a test of the framework's pages holds every path to a file that is in the repo, so
a doc that is moved or deleted fails the page that links to it.

### 3. The menu and the page turns

A docs site reads in an order: first this page, then that. `DocsContents` is that order, the site's pages under their
groups, such as Start, Understand and Decide. Each group is a `DocsGroup` with a label and its pages, and each page a
`DocsEntry` with its title in the menu and its address.

Two parts read it. `DocsMenu` is the left column: every group's label with its pages under it, the one being read
marked with `aria-current="page"`, for the eye and for a screen reader. `PageTurn` is a page's foot: the page before
and the page after in reading order, each named, so someone reading straight through never needs the menu.

Both ask the contents which page an address is on, and the contents answer with `FindEntry`. A page holds its own
address and every address under it, so a section's own address, such as `/how-it-works/the-web`, still marks How it
works in the menu. The root page holds only itself, since every address sits under it. The longest match wins.
`FindBefore` and `FindAfter` step one page back or on from there, and give nothing at either end.

The contents are a value the site's pages' package brings with its area. The framework's pages bring theirs with
`.Framework()`. The menu and the page turns redraw whenever the visitor goes to another page, so the mark and the
turns follow them.

These are the types involved:

![The types: Docs() registers the one address every link asks, and the menu and the page turns read one list of pages](diagrams/LouisAgent.Web.Docs-interface.svg)

## Structure

The place to look a file up.

```
src/web/LouisAgent.Web.Docs/
├── DocsAddress.cs        # where a doc's path leads; GitHub(repository, branch), For(path)
├── DocsArea.cs           # .Docs(address), the area a site names
├── Links/                # DocLink, DocLinks, each with its styles beside it
├── Contents/             # DocsContents, DocsGroup, DocsEntry; DocsMenu and PageTurn, each with its styles
├── Messages/             # DocsErrorMessage: what the link says when a site names no docs' address
└── _Imports.razor
src/tests/web/LouisAgent.Web.Docs.Tests/   # the same folders
```

## Configuration

None of its own. The address is the site's, in its areas, and the contents come with the package of pages that uses
them.

## Extending it

To send the links somewhere other than GitHub, give the site's area another address:
`.Docs(new DocsAddress("https://docs.example.com"))`. To lay out another set of pages, give them a `DocsContents` of
their own, through their own area, and put `DocsMenu` and `PageTurn` in the layout they wear.

## Tests

The tests are in `src/tests/web/LouisAgent.Web.Docs.Tests`. `DocsAddressTests` hold a path to its file on the branch
on GitHub, with or without a leading slash, a path under a docs site's own address, and a site naming its docs once.
`DocLinkTests` hold a link to its doc, the links standing together, and a site with no address being told how to name
one. `DocsContentsTests` hold the page an address is on, a section's and the root's among them, an address of no page
finding none, and the page before and after following the reading order. `DocsMenuTests` hold the groups and their
pages, the page being read marked, also at one of its sections, and the page turns, with none after the last.

`DocPathsTests`, in the framework's pages' tests, holds every path a page names to a file in the repo.

Of the framework's rules in `LouisAgent.Web.Tests/Rules`, `SurfacesAreDrawnByTheToolsTests` reads this library's
styles, so the menu and the page turns draw their surfaces only through the framework's surface tool.

## Limits

The docs have no site of their own, so every link leads to GitHub.

## Related docs

[LouisAgent.Web.Framework](LouisAgent.Web.Framework.md) · [LouisAgent.Web](LouisAgent.Web.md) ·
[LouisAgent.SiteFramework](LouisAgent.SiteFramework.md) ·
[ADR 0120](../adr/0120-nothing-is-hand-wired-from-the-framework.md) · [The F148 plan](../plans/F148-one-docs-endpoint.md)

---
[Docs index](../README.md) · Related: [Projects](README.md) · [Plans](../plans/README.md)
