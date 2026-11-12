# louis-agent.pack.site — design

> **Explanation.** How an agent takes a visitor to a place on the site (F117), shows them a card it draws, offers
> them choices as cards (F127.4.1) and asks them with a form (F127.4.3): tools checked here, acted on and drawn by the
> web's widget. **Elsewhere:** the guide that calls it in
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

And what the visitor must write, a name, a way to reach them, a job told in their own words, was asked one thing at a
time, and answers typed into the wrong chat got lost. So the agent asks with a form: `AskFor` draws a box per field,
and the filled form comes back as the visitor's message (Louis, 2026-11-12: "can the agent offer a card where I can
put my name in?", "and then only that card", "see this should also be an input card").

## How it works

- `Tools/SiteTools.ShowPage(path)`: accepts a path of this site (one leading slash, no scheme, no host, no climb out,
  no space; each word lower-case letters, digits and dashes, a digit first allowed for a piece such as
  `/home/100-adrs`; a last word that is a number is a paged section's page) and answers that the page is shown and
  that the model should say one line about it; refuses anything else with the reason. The tool moves nothing itself.
  Its texts are `Messages/SiteToolMessage`.
- `SiteTools.FindPlace(words)` (F127.7, [ADR 0121](../../../src/docs/adr/0121-every-part-of-the-web-is-ready-for-an-agent.md)):
  searches every place on every page of the site (its sections, cards and pieces) through `IPackContext.Places` and
  lists the best five as `address  heading (kind)`, so the guide can show one from any page. The places come from the
  host that serves the site, which builds them from its own rendered pages; a host that serves none hands a Null
  Object, and the tool says there is no site to search.
- `Tools/CardTools.ShowCard(kicker, title, rows, subtitle?, status?, note?)`: a read-only ticket, one to six rows of
  an icon by name, a label and a value, and a status in a tone. `Offer(question, choices)`: two to four choices, a
  label of up to 40 characters each, a why of up to 80, one suggested at most. Both check what the agent wrote
  (`Tools/Cards/CardCheck`, `OfferCheck`) and answer *shown* or *offered*; a refusal starts with "Error", so the
  stream marks the call failed, the widget draws nothing, and the model fixes what the reason names.
- `AskFor(question, fields)`: one to four fields, a label of up to 30 characters each and no label twice, and a kind
  (`Tools/Forms/FieldKind`: Name, Phone, Email, Text for one line, LongText for a box that grows), checked by
  `FormCheck`. `Offer` and `AskFor` are marked `[EndsTurn]`: once they work the engine ends the turn, so the agent
  waits for the pick or the form, and an answer that ended so owes the guide's closing choices nothing (FX138, FX139,
  F127.4.3). Each message's holes are a record in `Messages/Holes/`, named by `[Holes]` (ADR 0119).
- **The checks are a tested duplicate.** The diagrams library under `src/web/` reads and draws the ticket, and a pack
  references no web (ADR 0086), so `CardCheck` holds the library's rules (the icons, the tones, the rows) and
  `CardCheckTests` holds them equal to the library's: a card this calls shown is a card the widget draws.
- `SitePack`: the one toolset, `site`, always on, both tool classes in it, with its guide `prompts/toolsets/site.md`:
  when to move the page, when to show a card (before anything goes on the visitor's behalf), when to offer (a
  decision, and every walkthrough stop: the next stop, another section, or stop here), when to ask with a form
  (whatever the visitor must write, never a choice that only says to type), and never to act on an answer before it
  comes back.
- **The widget acts** (`louis-agent.web`): the runner raises `ToolCalled` for every tool call as it arrives in the turn's
  stream; the widget, on `ShowPage`, navigates to the path and, once the page has rendered, scrolls the area into view
  and gives it the class `area-shown` for a few seconds, which the web's styles draw as a glow that fades. A site may
  restyle it.
- **The widget draws** (`louis-agent.web`): `CardCall` reads a ShowCard call into the library's ticket (through
  `TicketCard.FromCall`) and `DrawnCard` puts its SVG in the chat; `OfferCall` reads an Offer call and `OfferCards`
  draws the buttons. Both draw with the work hidden too, under the answer's words, and a click sends the label as
  the visitor's message; once answered, the cards fold to the one chosen. `AskForCall` reads an AskFor call into the
  web's own `AskedKind` (a tested duplicate of `FieldKind`, both sides' tests holding the same names) and `FormCard`
  draws a box per field, suited to its kind, with Send ready once each is filled; `FormAnswer` sends the form as one
  message, each field by its label (`Name: John · Phone: 066 391 2470`), and reads it back, so the form folds to what
  was sent. A form is the only choice its answer draws (`FormAlone`). Its whole look, the card, the labels, the boxes
  and Send, reads `--web-form-*` values (`web/tools/_themed.scss`), so a site makes its own by setting them: the
  framework's site gives its guide a modern, sleek form (`site/chat/_guide-form.scss`), and the demo businesses'
  windows keep the plain one.
- **The guide knows the areas**: the page block a message carries (F115, ADR 0104) ends with the page's areas as
  `#id heading`, read by the widget's module from every section with an id; the site's test holds that every section
  of every page has one. Since F127.7 the pieces are listed too, marked `(piece, selects)`, and the guide's guide says
  that showing one presses it for the visitor, that a message naming a selected piece is about that piece, and to find
  a place on another page with `FindPlace` before showing it.

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
twice refused (`OfferTests`); a good form asked, ending the turn, a kind bound by name, and no field, five, a long or
repeated label and an unknown kind refused (`AskForTests`); every message's holes record equal to its template
(`SitePackTests`). The widget's side is in `louis-agent.web.tests` (`ChatRunnerTests`, `ChatWidgetTests`,
`DrawnCardTests`, `OfferCardsTests`, `FormCardTests`, `FormAnswerTests`).

---
[Packs' docs](../README.md) · Related: [louis-agent.pack.leads](louis-agent.pack.leads.md) · [louis-agent.pack.docs](louis-agent.pack.docs.md)
