# louis-agent.pack.site — design

> **Explanation.** How the guide takes a visitor to a page of the site, and to an area of it (F117): one tool, checked
> here, acted on by the web's widget. **Elsewhere:** the guide that calls it in
> [louis-agent.identity.expert](../../../demos/louis-agent/docs/projects/louis-agent.identity.expert.md); the widget
> that hears it in [louis-agent.web](../../../src/docs/projects/louis-agent.web.md); how a pack is shaped in
> [Build a pack](../../../src/docs/guides/build-a-pack.md).

> **Kind:** Class library (`net10.0`) · **Path:** `packs/tools/louis-agent.pack.site/` · **References:**
> `louis-agent.composition` only · **Used by:** `louis-agent.coding-catalog`, for the guide

## Purpose

The guide in a site's corner could point at a page ("the Identities page shows the levels") but not take anyone there.
A tool when it improves the prompt (ADR 0080): the guide gets `ShowPage`, names the page, with an area after `#` when
there is one, and the visitor's browser goes there, scrolled to the area and with the area lit up for a moment. Louis,
2026-11-08: "I want this agent to take you to a page", "he should be able to direct you to a certain part of the page
also", "can we also give it the capability to highlight areas on the page?"

## How it works

- `Tools/SiteTools.ShowPage(path)`: accepts a path of this site (one leading slash, no scheme, no host, no climb out,
  no space; an area after `#` is an id in words) and answers that the page is shown and that the model should say one
  line about it; refuses anything else with the reason. The tool moves nothing itself. Its texts are
  `Messages/SiteToolMessage`.
- `SitePack`: the one toolset, `site`, always on, with its guide `Prompts/toolsets/site.md`, which says when to call
  it (the right page or area exists for what the visitor asks), once, and what to say after.
- **The widget acts** (`louis-agent.web`): the runner raises `ToolCalled` for every tool call as it arrives in the turn's
  stream; the widget, on `ShowPage`, navigates to the path and, once the page has rendered, scrolls the area into view
  and gives it the class `area-shown` for a few seconds, which the web's styles draw as a glow that fades. A site may
  restyle it.
- **The guide knows the areas**: the page block a message carries (F115, ADR 0104) ends with the page's areas as
  `#id heading`, read by the widget's module from every section with an id; the site's test holds that every section
  of every page has one.

## Rules it keeps

- References the composition only, names no identity and no other pack (ADR 0027); stays convertible to MCP: one tool,
  text in, text out (ADR 0028). An MCP client that calls it gets the text and moves nothing, which is right: the tool
  says what should be shown, the client decides whether it can.
- Arguments as data (ADR 0004): the path is checked, never run; the browser only ever receives a path of its own site.

## Tests

`packs/tests/louis-agent.pack.site.tests`: the pack's shape and guide, a site path shown, a path with an area shown,
another host, a scheme, a climb out, a space, an empty path and a bad area refused with the reason. The widget's side
is in `louis-agent.web.tests` (`ChatRunnerTests`, `ChatWidgetTests`).

---
[Packs' docs](../README.md) · Related: [louis-agent.pack.leads](louis-agent.pack.leads.md) · [louis-agent.pack.docs](louis-agent.pack.docs.md)
