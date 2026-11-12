# louis-agent.pack.site — design

> **Explanation.** How an agent takes a visitor to a place on the site (F117), shows them a card it draws, and offers
> them choices as cards (F127.4.1): tools checked here, acted on and drawn by the web's widget. **Elsewhere:** the guide that calls it in
> [louis-agent.identity.guide](../../../src/docs/projects/louis-agent.identity.guide.md); the widget
> that hears it in [louis-agent.web](../../../src/docs/projects/louis-agent.web.md); how a pack is shaped in
> [Build a pack](../../../src/docs/guides/build-a-pack.md).

> **Kind:** Class library (`net10.0`) · **Path:** `packs/tools/louis-agent.pack.site/` · **References:**
> `louis-agent.composition` only · **Used by:** `louis-agent.coding-catalog`, for the guide

![The site pack: ShowPage, ShowCard and Offer, and the widget that acts on them](diagrams/louis-agent.pack.site.svg)

## Purpose

The guide in a site's corner could point at a page ("the Identities page shows the levels") but not take anyone there.
A tool when it improves the prompt (ADR 0080): the guide gets `ShowPage`, names the page, with an area after `#` when
there is one, and the visitor's browser goes there, scrolled to the area and with the area lit up for a moment. Louis,
2026-11-08: "I want this agent to take you to a page", "he should be able to direct you to a certain part of the page
also", "can we also give it the capability to highlight areas on the page?"

Then a decision. An agent that lists the choices in words makes the visitor type one back, and a booking read back
as a sentence is easy to miss a wrong date in. So the agent draws: `ShowCard` shows what it gathered as a ticket,
and `Offer` puts two to four choices as cards the visitor clicks (Louis, 2026-11-12: "when the guide must give
options to the user, like you do for me", and "the agents must draw this themselves with our diagram tool").

## How it works

- `Tools/SiteTools.ShowPage(path)`: accepts a path of this site (one leading slash, no scheme, no host, no climb out,
  no space; an area after `#` is an id in words) and answers that the page is shown and that the model should say one
  line about it; refuses anything else with the reason. The tool moves nothing itself. Its texts are
  `Messages/SiteToolMessage`.
- `Tools/CardTools.ShowCard(kicker, title, rows, subtitle?, status?, note?)`: a read-only ticket, one to six rows of
  an icon by name, a label and a value, and a status in a tone. `Offer(question, choices)`: two to four choices, a
  label of up to 40 characters each, a why of up to 80, one suggested at most. Both check what the agent wrote
  (`Tools/Cards/CardCheck`, `OfferCheck`) and answer *shown* or *offered*; a refusal starts with "Error", so the
  stream marks the call failed, the widget draws nothing, and the model fixes what the reason names.
- **The checks are a tested duplicate.** The diagrams library under `src/web/` reads and draws the ticket, and a pack
  references no web (ADR 0086), so `CardCheck` holds the library's rules (the icons, the tones, the rows) and
  `CardCheckTests` holds them equal to the library's: a card this calls shown is a card the widget draws.
- `SitePack`: the one toolset, `site`, always on, both tool classes in it, with its guide `prompts/toolsets/site.md`:
  when to move the page, when to show a card (before anything goes on the visitor's behalf) and when to offer (a
  decision, and every walkthrough stop: the next stop, another section, or stop here), and never to act on a choice
  before it comes back.
- **The widget acts** (`louis-agent.web`): the runner raises `ToolCalled` for every tool call as it arrives in the turn's
  stream; the widget, on `ShowPage`, navigates to the path and, once the page has rendered, scrolls the area into view
  and gives it the class `area-shown` for a few seconds, which the web's styles draw as a glow that fades. A site may
  restyle it.
- **The widget draws** (`louis-agent.web`): `CardCall` reads a ShowCard call into the library's ticket (through
  `TicketCard.FromCall`) and `DrawnCard` puts its SVG in the chat; `OfferCall` reads an Offer call and `OfferCards`
  draws the buttons. Both draw with the work hidden too, under the answer's words, and a click sends the label as
  the visitor's message; once answered, the cards fold to the one chosen.
- **The guide knows the areas**: the page block a message carries (F115, ADR 0104) ends with the page's areas as
  `#id heading`, read by the widget's module from every section with an id; the site's test holds that every section
  of every page has one.

## Rules it keeps

- References the composition only, names no identity and no other pack (ADR 0027); stays convertible to MCP: text and
  records of plain data in, text out (ADR 0028). An MCP client that calls it gets the text and moves nothing, which is right: the tool
  says what should be shown, the client decides whether it can.
- Arguments as data (ADR 0004): the path is checked, never run; the browser only ever receives a path of its own site.

## Diagrams

![How it works: the tool says, the widget hears the call, and the visitor's own browser moves](diagrams/louis-agent.pack.site-flow.svg)

![A booking: only the visitor's click books, the agent shows a card and offers, and sends nothing before](diagrams/louis-agent.pack.site-sequence.svg)

![The types: one pack, tools that move and draw nothing, their checks, and the web's readers](diagrams/louis-agent.pack.site-interface.svg)

## Tests

`packs/tests/louis-agent.pack.site.tests`: the pack's shape and guide, a site path shown, a path with an area shown,
another host, a scheme, a climb out, a space, an empty path and a bad area refused with the reason; a card shown, an
unknown icon or tone and a card without rows refused (`ShowCardTests`); the card rules held equal to the library's
(`CardCheckTests`); good choices offered, and too few, too many, a long label or why, two suggested and the same label
twice refused (`OfferTests`). The widget's side is in `louis-agent.web.tests` (`ChatRunnerTests`, `ChatWidgetTests`,
`DrawnCardTests`, `OfferCardsTests`).

---
[Packs' docs](../README.md) · Related: [louis-agent.pack.leads](louis-agent.pack.leads.md) · [louis-agent.pack.docs](louis-agent.pack.docs.md)
