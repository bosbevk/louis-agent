# 0125. A site is configured framework components

**Status:** Accepted · **Date:** 2026-11-16 (Louis: "our site may not contain html tags only configured components
from core.web", "this can be split into 2 control all of H3 and p in a control and all of ul and kids in a control.
we must think like this and make it a rule", "then we would not had this styling issue now", "an ADR"; and of the
first wording, "no a site can add a onceoff look on it own") · **Recorded:** 2026-11-16

**It binds** every site built on the framework's web, and the framework's web, which gives them their controls.

## Context

louis-agent.dev wrote its sections in plain HTML: a heading, a paragraph, a list of entries with a name in bold
code and a sentence after it, written out again on each page. Its own stylesheets gave those tags their look. When
the site was asked to wear the theme alone and its own sheets came out (FX162), every one of those sections lost its
look at once, because nothing in them was the theme's. The same shapes were repeated across pages, each copy styled
by the site, so a look could only be changed one site and one copy at a time.

Three ways were open. A site could keep its HTML and the framework could style bare tags everywhere, which reaches
every site but makes a heading or a list look the same wherever it stands, the chat and the admin included. A site
could keep its HTML and its own sheets, which is how it was, and which is what broke. Or a site could hand its words
and data to the framework's controls, one control for each shape it repeats, so the look is the control's and the
theme's, and the site never draws it.

## Decision

1. **A site's pages hold no HTML elements.** A page is the framework's components, given the site's words and data
   as parameters and records.
2. **Each repeated shape is one control in `LouisAgent.Web`, drawn by the theme.** A heading with its lede is one
   control, and a list of named entries is another. When a page needs a shape any site could use and the framework
   has no control for it, that control is built in the framework first.
3. **A site may still add a one-off look of its own**, for something only it has: as a component of its own in the
   site, drawn from the theme's names the way the framework's controls are.
4. **Text that needs inline code or emphasis is passed as Markdown**, which the framework renders safely, so a site
   never writes a tag inside its words.
5. **A test holds it.** It lists the site's pages that still hold HTML, and the list may only shrink.

## Consequences

A theme change reaches every site's shared shapes with no edit to any site. Every new shared shape costs a framework
control first, before a page can use it. A site's own one-off look is its own to keep in line with the theme. A
site's pages read as data: what is on a page is its words, and how it looks is the framework's.

## Links

[ADR 0084](0084-every-site-is-built-on-the-frameworks-web.md) (every site is built on the framework's web) ·
[ADR 0120](0120-nothing-is-hand-wired-from-the-framework.md) (nothing is hand-wired from the framework) ·
[ADR 0124](0124-a-sites-look-is-a-theme-and-a-style.md) (a site's look is a theme and a style)

---
[ADRs](README.md) · [Docs index](../README.md)
