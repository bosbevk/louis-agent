# F33 · Shop catalogues: product prices read from the shop, in code

> **Status:** planned (2026-10-17), on `feature/F33-shop-catalogues` · **Milestone:** — · **Depends on:** F32 (the
> headless browser and its guards), F25 (the cook), F31 (the cook check that measures it)
>
> **In the TODO:** **F33 Shop catalogues** under *Tooling and repositories* ([TODO](../../TODO.md)) · **Plan:**
> [plans/F33](../plans/F33-shop-catalogues.md)

F32 gave Puls search and page rendering with no key, and its cook check still priced nothing with a source. The cook
was told not to open the shops' search pages, and it guessed their addresses wrong: Checkers ignores `/search?q=`, while
its own search box sends `/search?Search=…`, which shows 61 results with prices. The page's own call to Checkers'
catalogue answers with structured products and exact prices. Louis (2026-10-17): "next up Shops … then we add Checkers
… a provider for only checkers product lookup".

## User stories

**F33-S1 — A shop's prices, exact.** As the *cook*, I want to ask a shop for a product and get its products with their
prices, sizes and links, so a price on the shopping list is the shop's own, with its source.
- *Given* `FindProducts("checkers", "chicken pieces")` *then* a short table: name, size, price, unit price and a link to
  the product on checkers.co.za, read from Checkers' catalogue, not from a model reading a page.
- *Given* a shop that isn't enabled, or one with no catalogue *then* the answer says which shops are.
- *Given* Checkers' catalogue changes or refuses the browser *then* the error says what's missing or refused.

**F33-S2 — Guarded like every page.** As an *operator*, I want the catalogue to use the same browser and guards as
`ReadRenderedPage`, so a shop's page can't reach anything internal.
- *Given* any request the shop's page makes *then* the address rule decides it, and no WebSocket opens.

**F33-S3 — Off unless switched on.** As *Louis*, I want the shops toolset off unless a deployment lists the shops it
may read (`SHOP_CATALOGUES=checkers`), so the sold endpoint doesn't read shops' sites until their terms are checked.

**F33-S4 — Measured.** As *Louis*, I want the cook check run with the cook pricing Checkers through its catalogue, so
the run shows priced ingredients with sources.

## Design notes

- **One browser, three users.** The guarded Chrome moves out of `ChromePageRenderer` into `Tools/Web/Browsing`; the
  renderer and the catalogues both use it.
- **The catalogue's JSON, through the page.** The provider loads Checkers' search page like any visitor and keeps the
  catalogue answer the page itself fetches: exact prices, no internal API called directly, the site's bot check passed
  as any browser does. No evasion.
- **A package of its own, later** (Louis: "I can split it out into a separate package later"): `Tools/Shops` depends
  only on the browser's contract, `Config` and `Messages`, so it can move to its own project without untangling.
- **Terms** (ADR 0023): reading a shop's site by automation is governed by its terms; development and benchmarks
  first.
- **Later:** a delivery area per shop (Louis's Malmesbury idea; Checkers keeps it in cookies), and Shoprite, Pick n Pay
  and Woolworths as providers.

## Done when

- `FindProducts("checkers", …)` returns Checkers' products with exact prices and links, from the running Puls stack.
- The browser seam, the Checkers parser (against a recorded answer) and the tool are tested; the live tests run against
  `puls-browser` and skip without it.
- The cook check, run from the portal, prices ingredients at Checkers with sources, its numbers here.

---
[Features](README.md) · [Plan](../plans/F33-shop-catalogues.md) · Previous: [F32 Self-hosted web](F32-self-hosted-web.md)

## What the build changed
