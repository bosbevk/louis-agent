# louis-agent.pack.shop-prices — design

> **Kind:** class library (`net10.0`) · **Path:** `packs/louis-agent.pack.shop-prices/` · **References:**
> louis-agent.core, MongoDB.Driver, Microsoft.Extensions.FileProviders.Embedded · **Loaded by:** `PACKS=shop-prices`, or
> an identity that names it (Puls) · **Uses:** MongoDB when `SHOP_PRICES_MONGO_URL` is set

## Purpose

What an agent can do to say roughly what something costs: estimate a shopping list from Stats SA's average prices,
in the user's province, with the total and the price per person worked out in code. No retailer gave us its prices
(F50.2, [ADR 0069](../adr/0069-shop-prices-are-stats-sa-estimates.md)), so the prices are public averages, credited
and always shown as estimates. It is a pack ([ADR 0027](../adr/0027-engine-identities-and-packs.md)) and could be
served as an MCP server unchanged ([ADR 0028](../adr/0028-packs-stay-convertible-to-mcp.md)). It isn't tied to food:
another kind of price (rent, fuel) is another price file of the same shape. Where to buy is the `where-to-buy` pack's
job ([louis-agent.pack.where-to-buy](louis-agent.pack.where-to-buy.md)).

## How it works

- **One toolset, `shop-prices`.** `ShopPricesPack` defines it with its guide (`Prompts/toolsets/shop-prices.md`) and
  one tool. It needs no setting, so it loads wherever it is named.
- **The tables.** Two price files ship embedded in `Prices/Data/`: Stats SA's average prices for August 2026 (P0141,
  220 products in 463 packs, nationally and per province) and 18 rows of our own for what Stats SA doesn't price
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

## Design notes

- **The sums are code, never the model's.** The model copies each estimate, the total and the price per person; the
  same list gives the same numbers every run, and a total always equals its rows.
- **An estimate says it is one.** `± R65`, never linked, never a shop's name beside it, and the credit names the
  province and the month.
- **Unknown is `?`.** A line with no price stays out of the total and is listed, so the total never looks complete
  when it isn't ([ADR 0007](../adr/0007-measure-every-request.md)'s rule).
- **The store is the record, the files only seed it.** A month corrected in the store stays corrected after a
  restart; a newly shipped month joins the history.
- **The unit is kept by its member's name in MongoDB** (`Gram`), the amounts as decimals; the shipped JSON uses the
  wire names (`g`).

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
| `Messages/` | `ShopPriceToolMessage` (what the tool returns), `ShopPriceLogMessage` (its log line) |

## Configuration

| Setting | What it does |
|---|---|
| `SHOP_PRICES_MONGO_URL` | The MongoDB the tables are kept in (a secret, `config/.env.secrets`). Unset: the shipped tables in memory |
| `SHOP_PRICES_MONGO_DATABASE` | Its database. Default `shop_prices`; the collection is `price_files` |

The Puls stack uses the eval store's server and user, on its own database.

## Extending it

- **A new Stats SA month:** the new release's two spreadsheets turned into `statssa-average-prices.json` (the same
  shape, the new `as_of`), shipped; the store picks it up as a new month on its next start. A C# refresh tool is on
  the TODO.
- **Another kind of price** (rent, fuel): a price file of the same shape, with its source and date, seeded or saved
  into the same store. A unit it needs that `PackUnit` lacks is a new member.
- **Our own rows** are `own-prices.json`; each needs a person's check.

## Tests

`tests/louis-agent.pack.shop-prices.tests`, mirroring the folders: the line reader, the matcher, the estimator (the
scaling, the province fallback, `?`), the tool's table and texts with `ShopPriceToolMessageSnapshotTests`' golden, the
cache (once an hour, the fallback, on a fake clock) and the options. `tests/louis-agent.pack.shop-prices.integration-tests`
runs `MongoPriceStore` against a real MongoDB (`BENCHMARKS_MONGO_URL`, CI's throwaway server): the seed, a newer month
read with the older kept, and a stored table never overwritten.

## Limits and plans

- The averages are over many shops and a whole province: a shop's real price can differ a lot, and the answer says
  so.
- Stats SA prices 220 products in set packs; the rest is our own rows or `?`.
- The monthly refresh is a hand step until the refresh tool exists.

## Related docs

[louis-agent.pack.where-to-buy](louis-agent.pack.where-to-buy.md) · [Build a pack](../guides/build-a-pack.md) ·
[louis-agent.identity.puls](louis-agent.identity.puls.md) · [ADR 0069](../adr/0069-shop-prices-are-stats-sa-estimates.md)

---
[Projects](README.md) · Previous: [louis-agent.pack.where-to-buy](louis-agent.pack.where-to-buy.md) · Next: [louis-agent.pack.recipes](louis-agent.pack.recipes.md)
