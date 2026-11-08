# ADR 0107: Every place on a site's page has its own address

**Status:** Accepted (2026-11-08, F118)

## Context

A site built on the framework's web is a page of sections: bands with a kicker and a heading, cards that open, and,
on a page of slides, sections that page sideways when they hold more than a screen. The guide in the corner reads the
page the visitor is on and takes them to a place on it (F115, F117, ADR 0105). Until now a place was a fragment,
`/proof#rand`, which the address bar never showed as the visitor scrolled, a copied link didn't carry, and the guide
named in a form of its own. Louis, 2026-11-08: "can we implement urls then instead of #", "make it part of the
framework", "this must be a rule for the framework", "the agent sections must have an url in the framework,
otherwise the agents won't work optimally", and for the cards and the pages of a paged section, "these also".

## Decision

- **Every place has its own address, in one form.** A page is its route, `/proof`. A section is the page and its id,
  `/proof/rand`. A card is the page and its id, `/identities/puls`, and opening it at that address opens the card. A
  page of a section paged sideways is the section's address and its number, `/how-it-works/parts/2`, and its first
  page is the section itself, never `/parts/1`. A page's first section, the one every page names `top`, is the page
  itself: `/architecture`, its second page `/architecture/2`, so the guide, the index and the bar never give one
  place two names (Louis: "see this will confuse the agent"). The landing's
  places sit under the site's home word, `/home/rule`, so a landing section never clashes with a page of the same
  name (the landing has sections called "proof" and "contact", and those are pages too). Louis chose this of three
  options; the others were no prefix with the page winning a clash, and a query (`/proof?section=rand`).
- **One rule, both ways, in the framework.** `SiteAddresses` (`Components/Site/Addresses/`) writes a place's address
  and reads an address back into the page, the place and the page of it. It is built once from the routes the
  site's pages already declare, so no page is edited to give its places addresses and a new page has them by
  construction. A site registers it with its home word: `SiteAddresses.From("home", typeof(App).Assembly)`.
- **The router opens any place.** `SectionPage`, the framework's catch-all route, draws the page an address names
  when no page's own route matches it, and the frame then goes to the place: scrolls to it, opens a card, turns a
  paged section. An address that names no page of the site is the not-found page's.
- **Everything uses the addresses.** The section index's links are the sections' addresses; the address bar follows
  the section in view, a card opened and a page turned, replacing the entry so the back button still goes to the
  page before; the page the guide reads names every place by its address, and the section on the visitor's screen
  by its address; the guide's `ShowPage` takes an address, and the widget goes to that place without reloading
  when it is on the page already. The `#area` form is gone (ADR 0049: one form, no second reader).

## Consequences

- A link someone copies opens on the place they copied it from, and the guide names a place as it read it, so the
  agent and the visitor share one name for every place.
- A page's places are known only once it draws, so the sitemap (F119) is a file a site test writes from the rendered
  pages and holds to them, `/sitemap.xml` named by `robots.txt`: every page and place, a paged section's later pages
  left out since they depend on the screen. Search engines then list every place by its own address.
- A site that registers no `SiteAddresses` keeps plain pages: the index links to `#id` on the page, the catch-all
  answers nothing, the guide's page tool is ignored.

## Links

- [ADR 0106: the landing is a business website from the framework](0106-the-landing-is-a-business-website-from-the-framework.md)
- [ADR 0105: the guide acts on the page through a tool the widget hears](0105-the-guide-acts-on-the-page-through-a-tool-the-widget-hears.md)
- [ADR 0049: nothing is backward compatible until something is sold](0049-nothing-is-compatible-before-a-sale.md)
- F118 in the work log
