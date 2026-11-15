# louis-agent.web.themes — design

> **Kind:** contracts library (`net10.0`) · **Path:** `src/web/louis-agent.web.themes/` · **References:**
> louis-agent.web only · **Used by:** a site that offers a theme; none yet

![The themes: one project a site names its looks from, glass and flat, each a folder holding its theme's id and name, and nothing else](diagrams/louis-agent.web.themes.svg)

## Purpose

A site on the framework can wear a theme, a look a visitor picks. This project holds the themes: glass and flat,
each in a folder of its own. A theme is only values, so nothing here holds a component or a rule for one. Each
theme tells a site it is there, and holds the values it sets.

A site offers a theme by naming it among its areas, `.Glass()` or `.Flat()`, the way it names its admin or its
showroom. The first theme a site names is its default.

*Decided in:* F151.2, [ADR 0124](../adr/0124-a-sites-look-is-a-theme-and-a-style.md).

## How it works

### 1. A site names a theme

Each theme is an extension on the framework's `SiteAreasBuilder`: `GlassTheme.Glass()` and `FlatTheme.Flat()`.
Each adds one area, `theme-glass` or `theme-flat`, that carries its theme and nothing else: it registers no service
and brings no page. The site's areas then list the themes in the order the site named them, so a site that names
glass and then flat opens in glass. A theme named twice still counts once, as any area does.

The extensions live here and not in the framework, because the framework's web can't name a theme. Dependencies
point one way, from this project to the framework, as they do for the accounts library.

### 2. What a theme is

Each theme holds a `SiteTheme`: its id, which goes into the page's `data-theme` and into the selector of its
stylesheet's blocks, and the name a switch shows a person. Glass is `glass` and Glass, flat is `flat` and Flat. The
framework checks the id when the theme is made.

![The types: a class per theme adds its area through the framework's builder, the area carries the theme, and the site's areas list it](diagrams/louis-agent.web.themes-interface.svg)

## Structure

| Path | What's in it |
|---|---|
| `Glass/GlassTheme.cs` | The glass theme, its area's name, and `Glass()`, the extension a site names |
| `Flat/FlatTheme.cs` | The flat theme, its area's name, and `Flat()` |

## Configuration

There's nothing to set. A site references the project and names the themes it offers among its areas.

## Extending it

A new theme is a new folder: a class like `GlassTheme`, with its own id, name and area. The project holds values
only. A change to how a component looks is made in the component, reading the framework's names, never here. A
name a theme should set is added in the framework first, in its settings, and then given a value in every theme,
for each style.

*Decided in:* [ADR 0124](../adr/0124-a-sites-look-is-a-theme-and-a-style.md).

## Tests

The tests are in `src/tests/web/louis-agent.web.themes.tests`, on NUnit, a folder per theme as the project has.
`GlassThemeTests` and `FlatThemeTests` name each theme on a site's areas and check that they list it by its id and
its name, once, even when it is named twice, and that glass named before flat comes first.

## Limits and plans

The themes hold no sheet of values yet, so a site that names one lists it among its themes and still looks as it
did. No site names one yet.

## Related docs

[louis-agent.web](louis-agent.web.md) · [ADR 0124](../adr/0124-a-sites-look-is-a-theme-and-a-style.md)

---
[Projects](README.md) · [Docs index](../README.md)
