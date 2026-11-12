# louis-agent.pack.shop-prices — design

> **Kind:** class library (`net10.0`) · **Path:** `packs/tools/louis-agent.pack.shop-prices/` · **References:**
> louis-agent.composition, MongoDB.Driver, ExcelDataReader, Microsoft.Extensions.FileProviders.Embedded · **Loaded by:** `PACKS=shop-prices`, or
> an identity that names it (Puls) · **Uses:** MongoDB when `SHOP_PRICES_MONGO_URL` is set

![The shop prices pack: EstimatePrices, the price tables, and the store and the upload](diagrams/louis-agent.pack.shop-prices.svg)

## Purpose

What an agent can do to say roughly what something costs: estimate a shopping list from Stats SA's average prices,
in the user's province, with the total and the price per person worked out in code. No retailer gave us its prices
(F50.2, [ADR 0069](../adr/0069-shop-prices-are-stats-sa-estimates.md)), so the prices are public averages, credited
and always shown as estimates. It is a pack ([ADR 0027](../../../src/docs/adr/0027-engine-identities-and-packs.md)) and could be
served as an MCP server unchanged ([ADR 0028](../../../src/docs/adr/0028-packs-stay-convertible-to-mcp.md)). It isn't tied to food:
another kind of price (rent, fuel) is another price file of the same shape. Where to buy is the `where-to-buy` pack's
job ([louis-agent.pack.where-to-buy](louis-agent.pack.where-to-buy.md)).

## How it works

- **One toolset, `shop-prices`.** `ShopPricesPack` defines it with its guide (`prompts/toolsets/shop-prices.md`) and
  one tool. It needs no setting, so it loads wherever it is named.
- **The tables.** Two price files ship embedded in `Prices/Data/`: Stats SA's average prices for August 2026 (P0141,
  220 products in 463 packs, nationally and per province) and 36 rows of our own for what Stats SA doesn't price
  (tinned tomatoes, coconut milk), marked as ours. A file is a source, a date and its products, each with the names a
  list may call it by and its packs (`PriceFile`, `PriceProduct`, `PricePack`).
- **A line to a product.** `BasketItem` reads a line (`beef mince 500 g`, `eggs 18`, `tinned tomatoes 410 g x 2`): its
  words, its size and how many packs. `PriceBook` finds the product: the name with the most words that all appear in
  the line wins, so *tomato sauce* beats *tomato*, and Stats SA's row wins a tie with ours.
- **A product to a price.** `PriceEstimator` takes the province's price, else the national one; the same pack when
  the line names it, else the nearest pack by size, scaled to the line's size; the table's pack when the line has no
  size. A line it can't match, or whose unit doesn't fit (a bunch of a product sold by weight), is `?`, never R0.
- **The answer.** `BasketEstimate` adds the rounded rows into the total and shares it per person;
  `EstimatePrinter` writes a table (each line, what it was priced from, `± R62`), the total, the price per person, the
  lines it couldn't price, and the credit line Stats SA asks for, which the agent copies under its list.
- **The store.** With `SHOP_PRICES_MONGO_URL`, `MongoPriceStore` keeps the tables in MongoDB, one document per source
  and month (`PriceFileDocument`, id `{date} {source}`), so each month stays as history and the newest is read. Its
  first read adds each shipped table the store doesn't have; one it has is never overwritten. Without the setting,
  `ShippedPrices` hands over the shipped tables in memory. `PriceBookCache` reads the store at most once an hour and
  falls back to the shipped tables when the store fails or is empty, with a log line (`ShopPriceLogMessage`).
- **The feed.** The pack publishes the same tables as one feed, `shop-prices` (`ShopPricesFeed`,
  [ADR 0070](../../../src/docs/adr/0070-packs-publish-data-feeds.md)), which the API serves open at `GET /v1/feeds/shop-prices`:
  `{"notice": …, "tables": [ … ]}`, each table in the shipped JSON's shape, read through the tool's cache. The notice
  (`ShopPriceFeedMessage`) says the prices are averages and whose they are.
- **The upload** (F55, [ADR 0071](../../../src/docs/adr/0071-packs-take-data-uploads.md)). The pack declares one upload,
  `shop-prices` (`StatsSaImport`), which the admin portal shows at `/admin/imports`: Stats SA's two P0141
  spreadsheets, all urban areas and provinces. `StatsSaSheets` reads them by column name (`H03` the code, `H04` the
  product, `H08` the size, `H09` the province, `M201701`… a column per month); `PackSize` and `StatsSaProduct` tidy
  sizes and names; `StatsSaNames` adds the names a list may use, kept in `statssa-names.json` by Stats SA's code;
  `StatsSaPrices` makes one price file per month. The preview compares each month with the stored one
  (`StatsSaUpload`, `MonthChanges`) and lists the new, the corrected (with the prices that changed) and the
  unchanged months and any product with no names; Save writes the new and the corrected and makes the cache read
  again (`PriceBookCache.Forget`). Only the MongoDB store can save; without it the preview works and Save says why.
- **An older month fills a gap.** The cache reads every stored month of each source; `PriceBook` looks in the newest
  months first, then the older ones, newest first, so a product the newest month lacks is priced from the newest
  month that has it, and the line names that month (`from July 2026, the newest month that prices it`).

## Design notes

- **The sums are code, never the model's.** The model copies each estimate, the total and the price per person; the
  same list gives the same numbers every run, and a total always equals its rows.
- **An estimate says it is one.** `± R65`, never linked, never a shop's name beside it, and the credit names the
  province and the month.
- **Unknown is `?`.** A line with no price stays out of the total and is listed, and the total and the price per
  person say so in the cell the agent copies (`± R312 + 2 items unpriced`), so the total never looks complete when it
  isn't ([ADR 0007](../../../src/docs/adr/0007-measure-every-request.md)'s rule).
- **The store is the record, the files only seed it.** A month corrected in the store stays corrected after a
  restart; a newly shipped month joins the history.
- **The unit is kept by its member's name in MongoDB** (`Gram`), the amounts as decimals; the shipped JSON uses the
  wire names (`g`).

## Diagrams

![The shop-prices pack: its tools, the prices and the estimate](diagrams/louis-agent.pack.shop-prices-diagrams.svg)

![Pricing a shopping list: each line is matched to a product in the price book and priced for the province, then printed with the total and the credit](diagrams/louis-agent.pack.shop-prices-diagrams-2.svg)

![The types: Stats SA's prices imported into a store, read into a cached book, and a basket estimated from it](diagrams/louis-agent.pack.shop-prices-interface.svg)

## Structure

| Path | What's in it |
|---|---|
| `ShopPricesPack.cs`, `ShopPricesToolset.cs`, `ShopPricesOptions.cs` | The pack, its toolset enum (ADR 0043) and its settings |
| `Tools/` | `ShopPriceTools.EstimatePrices` and `EstimatePrinter`, which writes its table |
| `Estimates/` | `BasketItem` (a line read), `PriceEstimator` (a line priced), `PriceEstimate` and `EstimateKind` (how it was priced), `BasketEstimate` (the list, its total and per person) |
| `Prices/` | `PriceBook` (the tables and the word match), `PriceMatch`, `ProductWords` (lowercase, letters and digits, a rough singular) |
| `Prices/Files/` | The price file's records and `PackUnit` |
| `Prices/Regions/` | `Region` (the country and the nine provinces), `RegionNames` (English and Afrikaans names), `RegionPrice` |
| `Prices/Store/` | `IPriceStore` with `ShippedPrices` and `MongoPriceStore`, `PriceFileDocument`, `PriceBookCache` |
| `Prices/Data/` | The shipped tables (embedded) |
| `Prices/StatsSa/`, `Prices/StatsSa/Sheets/` | Stats SA's release read: the sheets (`StatsSaSheets`, `StatsSaRow`), sizes and names tidied (`PackSize`, `StatsSaProduct`), the names file (`StatsSaNames`) and the months as price files (`StatsSaPrices`) |
| `Imports/` | `StatsSaImport` (the upload), `StatsSaUpload` (what the files change), `StatsSaReport`, `MonthChanges` |
| `Feeds/` | `ShopPricesFeed` (the tables as a feed) and `PriceFeed` (what it sends) |
| `Messages/` | `ShopPriceToolMessage` (what the tool returns), `ShopPriceLogMessage` (its log line), `ShopPriceFeedMessage` (the feed's description and notice), `ShopPriceImportMessage` (the upload's texts) |

## Configuration

| Setting | What it does |
|---|---|
| `SHOP_PRICES_MONGO_URL` | The MongoDB the tables are kept in (a secret, `config/.env.secrets`). Unset: the shipped tables in memory |
| `SHOP_PRICES_MONGO_DATABASE` | Its database. Default `shop_prices`; the collection is `price_files` |

The Puls stack uses the eval store's server and user, on its own database.

## Extending it

- **A new Stats SA release:** upload its two spreadsheets on the admin portal's Uploads page, check the preview,
  Save. A product Stats SA adds shows in the preview as having no names; add its row to `statssa-names.json` by its
  code. The shipped `statssa-average-prices.json` only seeds an empty store.
- **Another upload** (a retailer's feed): another `IDataImport` in `Imports/`, listed in `ShopPricesPack.Imports`.
- **Another kind of price** (rent, fuel): a price file of the same shape, with its source and date, seeded or saved
  into the same store. A unit it needs that `PackUnit` lacks is a new member.
- **Our own rows** are `own-prices.json`, our own estimates, each checked by a person when it is added; the print marks
  them as ours.
- **Another feed** (one table on its own, a province's): another `IDataFeed` in `Feeds/`, listed in `ShopPricesPack.Feeds`.

## Tests

`packs/tests/louis-agent.pack.shop-prices.tests`, mirroring the folders: the line reader, the matcher, the estimator (the
scaling, the province fallback, `?`), the tool's table and texts with `ShopPriceToolMessageSnapshotTests`' golden, the
cache (once an hour, the fallback, on a fake clock), the feed (its name, its shape, what the store holds) and the
options, and the upload: the sheets read from a trimmed fixture pair, every size in the August release, the August
release giving back the shipped table, the preview and save over an in-memory store, and an older month filling a
gap. `packs/tests/louis-agent.pack.shop-prices.integration-tests` runs `MongoPriceStore` against a real MongoDB
(`BENCHMARKS_MONGO_URL`, CI's throwaway server): the seed, a newer month read with the older kept, and a stored table
never overwritten, and an upload saving a month and replacing a corrected one. The API's `FeedRouteTests` read the
feed over HTTP, and `ImportRouteTests` list, preview and save the upload.

## Limits and plans

- The averages are over many shops and a whole province: a shop's real price can differ a lot, and the answer says
  so.
- Stats SA prices 220 products in set packs; the rest is our own rows or `?`.
- An upload travels twice (preview, then save); fine at a few megabytes ([ADR 0071](../../../src/docs/adr/0071-packs-take-data-uploads.md)).

## Related docs

[louis-agent.pack.where-to-buy](louis-agent.pack.where-to-buy.md) · [Build a pack](../../../src/docs/guides/build-a-pack.md) ·
[louis-agent.identity.puls](../../../demos/puls/docs/projects/louis-agent.identity.puls.md) · [ADR 0069](../adr/0069-shop-prices-are-stats-sa-estimates.md) ·
[ADR 0070](../../../src/docs/adr/0070-packs-publish-data-feeds.md) · [ADR 0071](../../../src/docs/adr/0071-packs-take-data-uploads.md)

---
[Projects](../../../src/docs/projects/README.md) · Previous: [louis-agent.pack.where-to-buy](louis-agent.pack.where-to-buy.md) · Next: [louis-agent.pack.recipes](louis-agent.pack.recipes.md)
