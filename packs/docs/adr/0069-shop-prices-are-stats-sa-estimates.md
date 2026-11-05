# 0069. Shop prices are Stats SA's averages, and where to buy is the public map

**Status:** Accepted · **Date:** 2026-11-04 (Louis: "scrap it they never came back"; two packs, one job each; "we must
save shop prices in mongo") · **Recorded:** 2026-11-04

## Context

The shops pack (F33, F34) read Checkers' own online catalogue through our browser: the products, specials and the
user's store. It gave the cook exact prices, but reading a retailer's site is governed by its terms, so it ran for
development and evals only, and the cook was told to quote no price until a retailer said yes (F18 step 5.7). On
2026-10-25 we asked Checkers, Pick n Pay and Woolworths for permission or a feed. None of them answered. The pack
was code we couldn't ship, the cook's prices were all guesses called estimates, and the branch was found by the model
reading OpenStreetMap's search page.

Two sources we are allowed to use were waiting:

- **Stats SA's average prices** (P0141, the consumer price index): every month, the average price of about 400
  products in set pack sizes, nationally and per province. Public, and free to reuse with credit.
- **OpenStreetMap**: the shops' branches, with their streets and places, through Nominatim's public search, under its
  usage policy (one request a second, a user agent that names the app), its data under the ODbL, which asks for
  credit wherever it is shown.

## Decision

*The shops pack goes, and two packs take its place, each with one job and neither tied to food.*

- **`where-to-buy`** finds a shop's branches near the user: `FindBranches(shop, town)` asks Nominatim for the town,
  then for the shop's branches in a box around it (half a degree, then a degree and a half), and keeps the three
  nearest, each with its street, its place, its distance, and whether it is in the town's municipality. It also gives
  the province, which the prices need. Wherever a branch is named, the answer credits "© OpenStreetMap
  contributors". Any kind of shop (a hardware store, a pharmacy) is the same call. The shop rules
  the eval files name (`estimates-at-shop`, `untooled-shops`) move here with it.
- **`shop-prices`** estimates what a list costs: `EstimatePrices(items, province, serves)` matches each line to a
  product in Stats SA's table, prices the nearest pack and scales it to the line's size, then adds the total and the
  price per person. All of it is code, so the numbers are the same every run and never the model's. A product Stats
  SA doesn't price may have a row of our own, marked as ours. A line with no match is `?`, never R0
  ([ADR 0007](../../../src/docs/adr/0007-measure-every-request.md)'s rule for unknowns). Every result ends on the credit line Stats SA asks
  for, and every price is shown as an estimate, `± R65`, never as a shop's price.
- **The prices are kept in MongoDB** (`SHOP_PRICES_MONGO_URL`), one document per source and month, so each month stays
  as history and the newest is read. The tables the pack ships seed the store: a table the store doesn't have yet is
  added, one it has is never overwritten. The pack reads the store at most once an hour and falls back to the shipped
  tables when the store is unset or doesn't answer, so a price is always there.
- **Other price sources fit the same shape.** A price file is a source, a date and its products with their packs and
  regional prices, so rent or fuel prices are a new file and a new source in the same store, with no code.

## Consequences

- The cook gives prices it is allowed to give, credited, at the province's average, and names a real branch. The prices
  are averages, not the user's shop's, and the prompt says so.
- No retailer's site is read by any tool. A retailer that later gives a feed is a new price source, not a scraper.
- The Stats SA file has to be refreshed each month; for now that is a data change (a new file shipped, which seeds
  the store). A refresh tool in C# is on the TODO.
- Our own rows need a person's check, since they are estimates of our own.
- Nominatim is a shared public service: one request a second, and a failure is said, never retried in a loop. A busy
  Puls needs its own instance or a paid geocoder.
- `SHOP_CATALOGUES` and `SHOP_STORE_CACHE` are gone ([ADR 0049](../../../src/docs/adr/0049-nothing-is-compatible-before-a-sale.md): no
  alias).
