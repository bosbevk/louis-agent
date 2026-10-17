# F33 · Shop catalogues: product prices read from the shop, in code

> **Status:** done (2026-10-17), on `feature/F33-shop-catalogues`: its run priced 22 of 23 ingredients with sources · **Milestone:** — · **Depends on:** F32 (the
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
- **Step 1 (2026-10-17): the guarded browser, a seam of its own.** `Tools/Web/Browsing` holds `IWebBrowser` and
  `ChromeWebBrowser` (F32's renderer's guards, moved: the address rule on every request, no WebSockets, cancellation,
  4 pages at once), `RenderPolicy` and `RenderVerdict`; `ChromePageRenderer` is a few lines over it, its answers
  unchanged. A visit keeps the response bodies a caller asks for (the live test: of two JSON documents a page fetched,
  exactly the one asked for), each read by a task the visit awaits, so no handler is `async void` and none is lost to
  the page closing. `ToolsetContext.Browser` gives a toolset the browser `BROWSER_URL` names.
- **Step 2 (2026-10-17): Checkers' catalogue.** `Tools/Shops` holds the contract (`IShopCatalogue`, `ShopProduct`,
  `ShopKind`, `ShopCatalogueException`, `ShopCatalogueFactory`); `Providers/CheckersCatalogue` visits Checkers' search
  page through the guarded browser, keeps its `get-products-filter` answer and reads each product: in stock only, at
  what it costs now (a special's price when lower), per kg (with its pack's weight, "1.1–1.3 kg") or each, linked to
  its card on the page (`/product/{name}-{article}{unit}`) or to the search. Tested against a real answer and page,
  recorded and trimmed (`Fixtures/`); the live search of "chakalaka" returned Checkers' products with prices and links.
- **Step 3 (2026-10-17): the `shops` toolset.** `ShopTools.FindProducts(shop, query, maxResults)` answers with a table
  (product, size, price now, price per kg, link) framed as untrusted shop data; a shop that isn't enabled says which
  are; a catalogue's error comes back with its reason; the turn's cancellation reaches the browser. `SHOP_CATALOGUES`
  (comma-separated, parsed with `SettingChoice`, unknown names enabling nothing) and `BROWSER_URL` both have to be set,
  or the toolset is unavailable. Its guide (`toolsets/shops.md`) says to ask the shop before the web and to link each
  price to its product; the coding agent's routing names it. The golden files gained the tool, its guide, the routing
  row and six messages; the snapshot sets a placeholder shop and browser, as it does Paymo's key.
- **Step 4 (2026-10-17): the cook has the shops, always, where they are.** Its file's header declares `toolsets:
  shops?`, which the loader strips; `?` makes a toolset optional (loaded where the instance has it, skipped and logged
  where it doesn't), so the sold endpoint, without `SHOP_CATALOGUES`, still cooks with the web. Its prompt prices a shop
  `FindProducts` covers through it first (one call per ingredient, the price linked to the product the table gives), and
  makes that shop one of the two when it's in the town. On this machine `puls-agent` lists `shops` as available.
- **The run (2026-10-17, Louis's go): cook check `fc7f8ecfdc87`, 1 repeat, $0.068, 61 requests, 1.13M tokens.** The
  cook priced **22 of 23 ingredients with a source** (cook-af 9/9, cook-en 6/7, cook-shops 7/7), where F32's run priced
  0 of 32; no guessed price; every conversation routed right. It called `FindProducts` 24 times, once per ingredient,
  the tool loaded from its own role (`shops?`), Puls passing `web` alone. 119 of 128 tool calls worked. Puls alone, with
  no catalogue, still priced 0 of 3 with a source (2 guessed): the gain is the cook's tool. The run cost a little more
  than F32's ($0.057) for priced answers instead of none. One wording slip, "Checkers's products", became "Products at
  Checkers for …".
