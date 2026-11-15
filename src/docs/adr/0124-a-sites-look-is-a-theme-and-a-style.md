# 0124. A site's look is a theme and a style, and a theme is values

**Status:** Accepted · **Date:** 2026-11-15 (Louis: "then i will keep both, you will be able to choose your
theme, it defaults to glass, light", "2 themes, 2 styles light and dark. each theme in it's own package which then
gets wired into core.web", "we should just have the user pick", "the user picks and the browser rememebrs"; and the same day, of where the
themes live: "we should only have one project called themes and add them there not seperately") ·
**Recorded:** 2026-11-15

**It binds** the framework's web, the libraries above it, and every site built on it.

## Context

The framework's web had one look. Its colours were named once, in `Styles/web/settings/_colours.scss`, and a site
that wanted its own set the same names after the framework's. The code-review demo went one step further and let a
person pick between two files of those values, kept in the browser. Nothing in the framework knew what a theme
was, and there was no light style at all.

Then two looks were designed for louis-agent.dev, glass and flat, each light and dark, and Louis kept both. That
forced a choice about how a second look reaches about 330 components without each of them being written twice,
and about who decides which look a visitor sees.

Three ways were weighed. A theme could ship its own rules for each component, which is quick to start but makes
the theme know every component's class names, so it breaks quietly when one changes. A site could compile a whole
stylesheet per theme and swap it, but the components' own styles are compiled once with their project, so that
means building them twice. Or a theme could be only values, which is how the repo already said a site makes a
look of its own: `tools/_themed.scss` reads a look "by setting values, never by copying a rule".

## Decision

1. **A site's look is two things: a theme and a style.** The theme is which look, and the style is light or
   dark. The page's `<html>` element says both, in `data-theme` and `data-style`.
2. **A theme is a package of values and nothing else.** For each of its two styles it sets every name the
   framework lists: the colours, and the surface names that say what a pane, a bar, a press and the mark are
   drawn with. It holds no rule for any component.
3. **A component reads names and never names a theme.** Where a look needs structure, such as the lit edge of a
   glass card, the framework's shared tools draw it once from the names, and a theme that does not want it sets
   those names to nothing.
4. **A value no name says gets a name first.** The name is added in the framework with today's look as its
   value, and every theme then sets it. A theme is held to the full list by a test.
5. **A theme is an area a site opts into.** The themes live together in one project above the framework's web,
   `louis-agent.web.themes`. Each is its own class and its own sheets, in a folder of its own, and a site names
   the themes it offers where it names its other areas. The first one named is its default.
6. **The visitor picks, and the browser remembers.** The theme and the style are chosen with a switch on the
   page and kept in the visitor's own browser. Nothing about the choice is sent to the host. A first visit gets
   the site's default theme in the light style, whatever the device's own setting is.

## Consequences

A third theme is one class and two sheets in the themes project, and no change to any component. In return, a look that cannot be said in the
names waits for a name, so the list of names is something to keep short and to guard.

Every site built on the framework gets the same switch and the same themes it names. A site keeps a brand of its
own by setting names after the theme's, as it always did, and then has to set each new name too.

Because the choice lives in the browser, a host cannot know it when it renders a page for a search engine, so
that page is rendered in the site's default. The script that applies what the browser remembers runs before the
first paint, so a returning visitor does not see the default first.

Remembering the choice needs no consent notice by itself: it is the visitor's own setting, it is kept only in
their browser, and it is about no one.

## Links

[ADR 0084: every site is built on the framework's web](0084-every-site-is-built-on-the-frameworks-web.md) ·
[ADR 0094: a component's styles sit beside it](0094-a-components-styles-sit-beside-it.md) ·
[ADR 0120: nothing is hand-wired from the framework](0120-nothing-is-hand-wired-from-the-framework.md) ·
[Plugin](https://martinfowler.com/eaaCatalog/plugin.html) ·
[Theming, web.dev](https://web.dev/learn/design/theming)

---
[Docs index](../README.md) · [ADRs](README.md)
