# 0121. Every part of the framework's web is ready for an agent

**Status:** Accepted · **Date:** 2026-11-12 (Louis, F127.7: "all our framework must work like this, even our account
pack this must be a framework rule", "that way we can attach an agent to it easily, even a helper which can explain
admin functions then") · **Recorded:** 2026-11-12 · Builds on [0107](0107-every-section-has-an-address.md) and
[0120](0120-nothing-is-hand-wired-from-the-framework.md)

## Context

Before F127.7, the guide in louis-agent.dev's corner saw the page in front of the visitor and its sections, but not
the parts inside them, nor where on the site it was, nor any page but the one open. It sold on a framework page and
went deep into code on a selling page. To show a tile on another page it would have needed a list of every tile kept
by hand in its prompt, which Louis turned down: "is there not a better way to do this?".

Site assistants (Algolia DocSearch and Ask AI, Mintlify, Kapa) search an index built from the real pages, section by
section, each with its address. In-app copilots (CopilotKit's `useCopilotReadable`) have the page say what it shows.
WebMCP (Google and Microsoft, Chrome's early preview, February 2026) lets a page register tools for an agent. A hand
list for language models (llms.txt) stalled.

## Decision

Every part of the framework's web is built so an agent can attach to it with no change to the pages:

- **Every place has an address** (ADR 0107): a page, its sections, its cards, and its **pieces**, the tiles and cards
  a visitor or an agent can select (`data-piece`, its id from its title or number by `PieceId`, an all-number id
  taking the word `number`).
- **A site names the part each page is** through its areas (ADR 0120): `Focuses(f => f.Default(…).Under(address,
  focus))`. Every message the guide sends carries the part (`PageRequest.Focus`) and the API's page block says it, so
  the agent reads that part's skill (ADR 0114) and leads the way that part needs.
- **A piece can be selected**, by the visitor's click, by the guide showing it, or by the visitor opening a card. It
  is drawn pressed, a chip in the chat names it, and every message carries it (`PageRequest.Selected`) until the chip
  is closed.
- **Every place is in an index built from the pages themselves**: the host that serves a site renders each page with
  its prerenderer (F121), reads its sections, cards and pieces into `SitePlace` records, and hands them to the agent's
  tools through `IPackContext.Places` (`ISitePlaces` in composition, a Null Object where a host serves no site). The
  site pack's `FindPlace` searches it. Nobody keeps a list of places by hand, in a prompt or anywhere else.
- **A new page, area or library keeps this**: its places have ids, its pieces carry `data-piece`, and its part is
  named, so the account pages, the admin and the eval studio (F141) take an agent or a helper the same way.

## Consequences

- A guide or a helper attaches to any site on the framework, finds anything on it and points at it, and a site's
  owner adds a tile without touching a prompt.
- The guide's prompt stays small: a part's rules load only when the visitor is in that part, and the places come from
  a tool, never a map.
- The index renders every page once, the first time it is asked, and keeps them for the host's life: a page changed
  while the host runs is seen after a restart. The search is plain word matching, a Strategy behind `ISitePlaces`, so a
  smarter one can replace it.
- WebMCP is the browser-side version of the same idea. When browsers carry it, a page can register its places and
  actions there too (F141).

## Links

[0107](0107-every-section-has-an-address.md) · [0114](0114-an-identitys-skills-load-always-or-on-demand.md) ·
[0120](0120-nothing-is-hand-wired-from-the-framework.md) · [0055](0055-keep-it-simple.md) · Algolia,
[DocSearch records](https://docsearch.algolia.com/docs/required-configuration) · CopilotKit,
[useCopilotReadable](https://docs.copilotkit.ai/reference/useCopilotReadable) · [WebMCP](https://zuplo.com/blog/what-is-webmcp)

---
[Docs index](../README.md) · [ADRs](README.md)
