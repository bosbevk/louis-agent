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
- **The list's photo (2026-10-17, Louis on a screenshot):** a cell with a product's photo keeps a lane for it, so a long
  name wraps beside the photo, never under it (Puls's styles).
- **Step 2 (2026-10-17): each product's deal.** `ShopProduct.Deal`: a `ShopDeal` of `ShopDealKind.Special` when the
  answer links the product to a bonus buy (`bonusBuyIds`), else `Promotion` when it flags one (`isOnPromotion`), and the
  wording from the badge on the product's card (a `promotion-badge_save` button or span: `BUY 2 FOR R199`, `SAVE`).
  The price fields never change for a deal, so this is where it shows. Tested on a real answer and page (NESCAFÉ
  Classic's multi-buy, Farmer's Choice's promotion, a product with none, and a multi-buy whose card the page left out:
  its kind, no wording). Live: chicken's deals with their wording, three runs in four; the one that failed read the page
  before its cards were drawn (3 s), so a deal can come without its wording when the page is slow: its kind still
  says there is one.
- **Step 3 (2026-10-17): `SearchSpecials` and `SearchPromotions`.** The same search as `FindProducts` (one private
  `SearchAsync` for the three), asking the catalogue for the most it gives (20) and keeping one kind of deal, up to
  `maxResults`, with a *Deal* column: the card's wording, or "a multi-buy (its link shows it)" when the page didn't word
  it. `FindProducts`' table is unchanged. The `shops` guide says when: a budget meal, a broad word per main ingredient;
  a multi-buy's pack price stays one pack's, the deal named beside it and used only when the recipe needs that many;
  never a saving the shop doesn't state. Checkers' own `dealsOnly` filter isn't used: it would mean changing the
  request the page makes, nearer to calling its API than visiting it as a shopper (F33's line).
- **Step 4 (2026-10-17): the cook prices only shops it has a tool for.** `puls-cook.md` drops the two shops picked at
  random, the column completed at other shops, `WebSearch` for prices, the notes on which shops' pages give a price and
  the search-index line. A shop is priced when a shop tool covers it and it has a branch in the town (looked up by name);
  without one, the list keeps its rows with `-` and says *No shop near you has a catalogue here yet* (Afrikaans: *Geen
  winkel naby jou het nog 'n katalogus hier nie*). `SearchSpecials` and `SearchPromotions` when cost matters, a deal
  beside the pack's price (`[R24,99](…) · BUY 2 FOR R45`). The list's example is one shop's; a *Best price* column only
  with more than one. `WebSearch` stays for a recipe and a branch's own page; the shops guide no longer sends a shop
  without a catalogue to the web.
- **Step 5 (2026-10-17): the cook check measures F34.** `cook-malmesbury` and `cook-no-shop` (Darling) join the
  conversations; the scorer counts the cook's shop tool calls and its deal tool calls, the deals named in the list, the
  shops priced with no tool for them (0 is right) and the no-catalogue line; `priced_at_shops` counts a shop tool as
  pricing at a shop. The manual and the portal's chips show them. Louis runs it (paid).
  **Proof of the store:** each search's header names the store that answered by Checkers' own id, and names the town
  only when that id is one of the town's stores (else it says the town's store wasn't used, and logs it). The check
  counts both from the recorded tool results (`store_for_area`, `store_default`). Live: a search from Malmesbury was
  answered by one of Malmesbury's stores, not the default.
- **A shop without a provider (2026-10-17, Louis).** Under the list, once, a polite note for each shop the cook can't
  price (in town but no tool, or one the user names): Puls shows prices from shops that make them available online in a
  form it can read, such as Checkers; Pick n Pay doesn't yet, so it isn't listed; the user can ask it to, and the more
  customers ask, the sooner. In both languages; never blame. Worded as what's true: Checkers' own site's catalogue is
  what's read, not a public API it publishes, and Shoprite is named only once it has a provider.
- **Cook check `dd992620f6cc` (2026-10-18, Louis's go; 1 repeat, $0.082, 80 requests).** With the cook: cook-af 8 of
  10 priced, cook-en 5/7, cook-shops 7/7, cook-malmesbury 7/11, Darling 10/11; no guessed price, no price per kg, no
  shop priced without a tool, every conversation routed right, every follow-up to the same cook. Malmesbury's,
  Stellenbosch's and Darling's searches all answered by the town's store; cook-en's by the default store (Pretoria was
  only in its message). The unpriced rows were all pantry staples (fixed since: a line under the list). The cook looked
  for specials in Malmesbury four times and found multi-buys on chicken, mince and rice, and used none: the recipe needed
  one pack of each. Searches took 3-4 s.
- **After the run:** the shop tools take `area` (the task's town when no place was shared); the no-shop town is Prince
  Albert (Darling is Malmesbury's municipality and Checkers serves it); and a place Checkers answers with its fallback
  stores, its default first, has no store of its own, so its header never claims one.

---
[Features](README.md) · [Plan](../plans/F34-shop-deals.md) · Previous: [F33 Shop catalogues](F33-shop-catalogues.md)
