# louis-agent.web.themes — design

> **Kind:** contracts library (`net10.0`, built as a Razor class library so it serves files) · **Path:** `src/web/louis-agent.web.themes/` · **References:**
> louis-agent.web only · **Used by:** louis-agent.starter, and so what `dotnet new louis-site` makes

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

### 3. A theme's values

A theme's look is a sheet of values for each style, in its folder under `Styles/`, such as
`Styles/theme-glass/_dark.scss`. The sheet is one block, under `:root[data-theme="glass"][data-style="dark"]`,
that sets every name the framework sets: about sixty colours, each written through the framework's `colours.both`
or `colours.numbers` so its two forms agree, the faces and the easing, and the surfaces, such as a pane's fill, rim
and blur. When the page's `<html>` says that theme and that style, the block's values win over the framework's.

Glass in the dark is black with white words and Ultraviolet for the purple. A pane is almost clear, has no border
of its own, takes its edge from a rim that is brightest at the bottom left and the top right, has two lights inside
those corners, blurs what is behind it and throws no shadow. A piece set into the page, an inset such as the chat's
card or a field, is as see-through as a pane, its edge drawn by its drawing. A bar is solid with a hairline, and a
press is solid violet with white words. The mark is a ball of violet glass with a white letter. Flat sets an inset
to its page's colour with its hairline.

A pane that answers the pointer differs between the two as well. In glass it lifts three pixels and wears no edge
and no new shadow, since its light is in the hover drawing. In flat it stays where it is and wears a two pixel ring
of the violet. What floats over the page throws a deep shadow in glass and none in flat, where its line sets it
apart.

A veil, the thin sheet the showroom lays over its stage, is a pane's own glass in the glass theme: the pane's fill
and blur, with no edge. In flat it is the pane's fill with the pane's edge and no blur.

Glass in the light turns white and black over and keeps the purple. The page is a pale wash of the violet, mixed
from its second step a third of the way into white, so it follows the purple. The words are black, and a
visitor's mark is black. A pane is white glass, brighter than the page, with a white rim, a fine violet-dark line
outside it and a soft shadow under it. A bar is near-white with a hairline.

Flat draws every surface as one solid colour with a hairline round it, with no gradient, shadow or blur anywhere,
so a pane's rim, light, shadow and blur are all `none`. In the dark the page is black and a pane a near-black
grey. In the light the page is white and a pane a solid grey clearly darker than it, with a firmer grey line, since
a card on a white page must be darker than the page. The press is the same solid violet in both, the second button
an outline in the words' colour, and the mark a flat violet disc with a white letter.

In both themes your own message sits under the same ink as a press, since the framework writes both with one name,
`--web-on-primary`. Their ink is white, so your message is a dark grey in the dark and black in the light.

### 4. Drawings, handed over as values

A glass piece is a drawing laid over a see-through blur. The framework names one drawing for each part and state,
32 of them, `--web-art-pane-rest`, `--web-art-press-hover`, `--web-art-toggle-on` and the rest, each `none` as its
own value, since the framework draws no glass. Glass sets each to the address of its drawing, and flat to `none`.
So a theme still hands over only values, and no component names a theme.

The drawings are SVGs with no size of their own: their shapes are written against their own edges, so the browser
draws one fresh at whatever size the piece is. They are in `wwwroot/glass/dark/` and `wwwroot/glass/light/`, one
file each, named for the part and the state. The project is built as a Razor class library, though it holds no
component, so a site that references it serves them at `_content/louis-agent.web.themes/`. The list of drawings
is written once, in `Styles/_theme-art.scss`, and each theme's sheet sets every one of them from it. A site's
compiled sheet sits in its `css/` folder, so a drawing's address in it starts `../_content/`.

### 5. Held to the framework's names

The project's own build compiles `Styles/app.scss` to `obj/css/app.css`, through the same Sass compiler the sites
use, outside what the project serves. That sheet forwards the framework's colours and surfaces, then every theme's sheets, so the
compiled file holds the framework's names and each theme's blocks. No site loads it. It is there so the tests can
ask the framework's `ThemeSheet` which names a theme's block for one style leaves unset, against the names the
framework really sets.

## Structure

| Path | What's in it |
|---|---|
| `Glass/GlassTheme.cs` | The glass theme, its area's name, and `Glass()`, the extension a site names |
| `Flat/FlatTheme.cs` | The flat theme, its area's name, and `Flat()` |
| `Styles/theme-glass/_dark.scss`, `_light.scss` | Glass's values in the dark style and in the light |
| `Styles/theme-flat/_dark.scss`, `_light.scss` | Flat's values in the dark style and in the light |
| `Styles/_theme-art.scss` | The list of drawings, written once, and the two ways a theme names them: each to its drawing, or each to none |
| `wwwroot/glass/dark/`, `wwwroot/glass/light/` | Glass's drawings, one SVG for each part and state, served at `_content/louis-agent.web.themes/` |
| `Styles/app.scss`, `sasscompiler.json` | The sheet the build compiles for the tests, the framework's names and every theme's sheets, and the compiler's settings (`obj/css/`, not served) |

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
its name, once, even when it is named twice, and that glass named before flat comes first. `GlassSheetTests` and
`FlatSheetTests` read the compiled `app.css`, through `CompiledThemes`, and ask `ThemeSheet` which of the
framework's names their theme leaves unset, in each style; the answer must be none. `ThemesArtTests` reads the
values: each of glass's drawing names is its drawing's address, and the file is there, and each of flat's is none.

## Limits and plans

A picture of a screen, such as the product window, stays dark in a light style only once the part that draws it
says so; a sheet of values can't. The starter is the one site that names the themes and lists their sheets.
Today's components read the colours and not yet the surface names or the drawings, so a pane is not glass anywhere
yet, and glass and flat in the dark look much alike until they do. The marks, the gauge and the dial have no
drawing here: a mark is round and is drawn as it is, and a gauge and a dial show a number, which a drawing held as
a value can't be given.

## Related docs

[louis-agent.web](louis-agent.web.md) · [ADR 0124](../adr/0124-a-sites-look-is-a-theme-and-a-style.md)

---
[Projects](README.md) · [Docs index](../README.md)
