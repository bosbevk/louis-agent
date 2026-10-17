# F34 · Shop deals: the user's store, its specials and promotions, and a cook that prices only where it has a tool

> **Status:** planned (2026-10-17), on `feature/F34-shop-deals` · **Milestone:** — · **Depends on:** F33 (the shops
> toolset and Checkers' catalogue), F22 (the user's place), F25 (the cook)
>
> **In the TODO:** **F34 Shop deals** under *Tooling and repositories* ([TODO](../../TODO.md)) · **Plan:**
> [plans/F34](../plans/F34-shop-deals.md)

F33 reads Checkers' products and prices for the cook, but from Checkers' default store, and without the deals that
make a cheap meal cheap. A free look at the live site (2026-10-17) found both: the catalogue call names no store
(`"storeContexts": []`), and the site's address picker turns an address into the area's store contexts; and a deal is
either a multi-buy ("BUY 2 FOR R199", linked by `bonusBuyIds`) or a promotion (`isOnPromotion`, a "SAVE" badge), its
wording on the product's card, never in the price fields. Louis: "let's just start by adding the 2 new Tools
SearchSpecials and SearchPromotions", then "the cook may only use shops for who has tools for", and "make sure we set
the address in checkersprovider, to make sure we use the correct catalogue".

## User stories

**F34-S1 — The user's store.** As the *cook*, I want Checkers' prices from the store that serves the user's town, so a
price is what the user would pay there.
- *Given* the user shared their town *then* the search runs in the store Checkers serves it from, looked up once per
  town.
- *Given* no town, or none Checkers serves *then* the search is the default store's, and the answer says so.

**F34-S2 — Specials.** As the *cook*, I want `SearchSpecials(shop, query)` to give the products with a multi-buy and
its wording, so a budget meal can use "BUY 2 FOR R199" when the recipe needs two.

**F34-S3 — Promotions.** As the *cook*, I want `SearchPromotions(shop, query)` to give the products on promotion and
their badge's wording.

**F34-S4 — Only where there's a tool.** As *Louis*, I want the cook to price only shops it has a tool for; with none,
the recipe and its list with `-`, and a line saying no shop near the user has a catalogue here yet. No web-search
prices.

## Design notes

- **One visit, more of it read:** the deal's kind is in the answer, its wording on the page; no new request.
- **The store from the town's centre**, never the user's address; development and benchmarks first (ADR 0023), as F33.
- **The sold endpoint prices nothing** until it has a shop tool: Louis's choice (F34-S4).

## Done when

- A search for a town runs in that town's store (its store id differs from the default's); without a town, the default
  store, said in the header.
- `SearchSpecials("checkers", "coffee")` returns NESCAFÉ's "BUY 2 FOR R199"; `SearchPromotions` returns promoted
  products with their badges; both tested against recorded pages, with live tests.
- The cook prices only shops with a tool, and without one gives the list with `-`; a cook check (paid, Louis's go)
  shows no guessed price.

## What the build changed

- **Step 1a (2026-10-17): a visit with a session, or a script, of its own.** The recorded visit found that Checkers
  keeps an area's store in cookies its own address call sets, and that every visit shared the browser's default
  context, so one town's store would have answered every later search. A visit can now start with cookies
  (`BrowserSession`), run one script of ours once the page has loaded (its requests under the same address rule), and
  return the cookies the site set; such a visit has a browser context of its own, dropped after. Plain visits, the
  renderer's and today's catalogue searches, are unchanged. Live tests: a session's cookie reaches the site, stays in
  its visit (a plain visit and another session don't see it), a site's cookie comes back, a script's result and its
  failure, and a script's request to an internal address refused.
- **Step 1b (2026-10-17): the user's store.** Where the user shared their town, `CheckersStore` asks Checkers for the
  stores serving its centre (OpenStreetMap's Nominatim gives it), in a visit of its own, the site's own call, and keeps
  the three cookies Checkers sets; each search then runs in them. Once per town; no town, an unknown one, none served or
  a failed lookup: Checkers' default store, as before. The table's header names the store: `Products at Checkers (the
  store serving Malmesbury)`, `(its default store: no area shared)` or `(… none found for {town})`. Live: Malmesbury's
  stores aren't the default's, and a search from Malmesbury is answered there; only the town's centre is sent.

---
[Features](README.md) · [Plan](../plans/F34-shop-deals.md) · Previous: [F33 Shop catalogues](F33-shop-catalogues.md)
