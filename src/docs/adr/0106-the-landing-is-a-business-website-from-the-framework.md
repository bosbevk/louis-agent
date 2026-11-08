# ADR 0106: The landing page is a business's public website, built from the framework

**Status:** Accepted (2026-11-08, F117)

## Context

The louis-agent site began as the framework's own landing page: what the framework is, how a turn works, the packs,
the proof. Along the way it grew the parts a business's website has: a hero, bands of content with a kicker and a
heading, tiles, cards that open, a contact strip, a diagram on every page, a section index, and the guide in the
corner that knows what it sells, takes a lead, knows the page the visitor is reading (F115) and takes them to a page
or an area of it (F117). What we sell today is exactly that: a website for a small business, hosted, with an agent in
its corner that knows the business. Louis, 2026-11-08: "we must now already split out the framework parts, this is a
framework function, even this landing page", "the landing page will become a business public website", "so some
people will have a public facing AI on a page and not the rest".

## Decision

- **The landing page is the template of the product.** The louis-agent site's landing is what a customer's public
  website looks like: a site shell, bands of named areas, a hero, tiles, cards, a contact strip, a diagram where one
  explains, and the agent in the corner. The framework's explaining pages (how it works, packs, identities, proof)
  are this demo's own content, not the product's: a customer never gets them.
- **Every generic part is the framework's.** A part that would be the same on every business's site lives in
  `louis-agent.web` (ADR 0084), and a site brings only its words, brand, colours, addresses and which parts it uses:
  the shell and the menu (F110), the card deck (F113), the diagram (F111, ADR 0103), the chat widget with its page
  knowledge (F115, ADR 0104) and its ability to show a page (F117, the `site` pack), and, from F117 on, the **band**:
  a section with a required id, a kicker and a heading, so every area of every page is named because the framework
  insists, and the guide can send a visitor to it. The section index is built from the bands (F118).
- **Two shapes of the product.** A customer has either a whole public website built from these parts with the agent
  in its corner, or the agent alone, the widget dropped into a page of their own; nothing in the framework assumes
  the first. The widget keeps working without a site shell, as it does on the chat page today.
- **The agent is the site's, not the framework's.** The guide's words, what it sells, its leads and its door are the
  site's identity and settings; the framework gives it the means (the widget, the page block, the packs).

## Consequences

- Each generic piece found on the landing moves into the framework as it is touched, with the site's words coming in
  through parameters, and a site test holds that the framework names no site (`FrameworkNamesNoSiteTests`).
- F118 moved the landing's shape in (2026-11-08, Louis reviewing each step on the Docker copy): the section index
  built from the bands on every page, the scroll progress line and a way back up; **slides or a flowing page** as a
  layout option the site sets and a page may override, slides being a screen per section, centred, the page settling
  on one at a time, a section with more than a screen paged sideways with a pager rather than fitted by hand, a
  diagram scaled to the slide; a page-change entrance; `SiteHero`, `TileRow` and `Tile`, `SiteStrip` with the site's
  words and the site's strip colour; every place its own address (ADR 0107). The guide reads the section on the
  visitor's screen and a section from a page, opens a card, turns a paged section's page, lights a place with a
  spotlight around its content, and walks a page from a tour button (`GuideButton`, through `GuideDoor`) on the start
  slide and above its own button on every page; its four jobs are to explain as the framework's expert, walk a page,
  sell and take the lead. A framework class shared with a site takes a prefix (`.site-strip`: the films had `.strip`).
- The starter site (`demos/starter`) grows to show the product's shape, so `dotnet new louis-site` gives a business
  website with the agent in its corner and the owner's words to fill in.
- A new part is asked "would every business's site have this?" before it is written into the louis-agent site.
- The framework's explaining pages stay in the demo and can be dropped from a customer's site without a trace.

## Links

- [ADR 0084: every site is built on the framework's web](0084-every-site-is-built-on-the-frameworks-web.md)
- [ADR 0104: a message may carry the page](0104-a-message-may-carry-the-page.md)
- [ADR 0103: a diagram is data on a row grid](0103-a-diagram-is-data-on-a-row-grid.md)
- F110, F111, F113, F115, F117 in the work log; F118 in the TODO
