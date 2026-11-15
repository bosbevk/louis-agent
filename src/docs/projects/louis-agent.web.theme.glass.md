# louis-agent.web.theme.glass — design

> **Kind:** contracts library (`net10.0`) · **Path:** `src/web/louis-agent.web.theme.glass/` · **References:**
> louis-agent.web only · **Used by:** a site that wears the glass theme; none yet

![The glass theme: a package a site names among its areas, holding the theme's id and name, and nothing else](diagrams/louis-agent.web.theme.glass.svg)

## Purpose

A site on the framework can wear a theme, a look a visitor picks. This package is the glass one. A theme is only
values, so a package like this holds no component and no rule for one: it tells a site the theme is there, and it
holds the values the theme sets.

A site wears glass by naming it among its areas, `.Glass()`, the way it names its admin or its showroom. The first
theme a site names is its default.

*Decided in:* F151.2, [ADR 0124](../adr/0124-a-sites-look-is-a-theme-and-a-style.md).

## How it works

### 1. A site names it

`GlassTheme.Glass()` is an extension on the framework's `SiteAreasBuilder`. It adds one area, `theme-glass`, that
carries the theme and nothing else: it registers no service and brings no page. The site's areas then list glass
among their themes, in the order the site named them. Named twice, it still counts once, as any area does.

The extension lives here and not in the framework, because the framework's web can't name a theme. Dependencies
point one way, from this package to the framework, as they do for the accounts library.

### 2. What the theme is

`GlassTheme.Theme` is a `SiteTheme`: the id `glass`, which goes into the page's `data-theme` and into the selector
of the theme's stylesheet blocks, and the name `Glass`, which a switch shows a person. The framework checks the id
when the theme is made.

![The types: GlassTheme adds an area through the framework's builder, the area carries the theme, and the site's areas list it](diagrams/louis-agent.web.theme.glass-interface.svg)

## Structure

| Path | What's in it |
|---|---|
| `GlassTheme.cs` | The theme, the area's name, and `Glass()`, the extension a site names |

## Configuration

There's nothing to set. A site references the project and names `.Glass()` among its areas.

## Extending it

The package holds values only. A change to how a component looks is made in the component, reading the
framework's names, never here. A name the theme should set is added in the framework first, in its settings,
and then given a value here for each style.

*Decided in:* [ADR 0124](../adr/0124-a-sites-look-is-a-theme-and-a-style.md).

## Tests

The tests are in `src/tests/web/louis-agent.web.theme.glass.tests`, on NUnit. `GlassThemeTests` names the area
on a site's areas and checks that they list the glass theme by its id and its name, once, even when it is named
twice.

## Limits and plans

The package holds no sheet of values yet, so a site that names it lists glass among its themes and still looks as
it did. No site names it yet.

## Related docs

[louis-agent.web](louis-agent.web.md) · [ADR 0124](../adr/0124-a-sites-look-is-a-theme-and-a-style.md)

---
[Projects](README.md) · [Docs index](../README.md)
