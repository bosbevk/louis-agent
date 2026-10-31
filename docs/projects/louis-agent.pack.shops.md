# louis-agent.pack.shops — design

> **Kind:** class library (`net10.0`) · **Path:** `packs/louis-agent.pack.shops/` · **References:** louis-agent.core,
> Microsoft.Extensions.FileProviders.Embedded · **Loaded by:** `PACKS=shops`, or an identity that names it (Puls) ·
> **Uses:** core's guarded browser (`BROWSER_URL`), OpenStreetMap's Nominatim

## Purpose

What an agent can do with shops: find a shop's products, specials and promotions in the shop's own online catalogue,
with the shop's exact prices and a link to each product. A model reading a shop's page guesses; this pack reads the
catalogue in code and hands the model a short table. It is a pack ([ADR 0027](../adr/0027-engine-identities-and-packs.md)),
so any identity can use it, and it could be served as an MCP server unchanged
([ADR 0028](../adr/0028-packs-stay-convertible-to-mcp.md)). One shop today, Checkers.

## How it works

- **One toolset, `shops`.** `ShopsPack` defines it with its guide (`Prompts/toolsets/shops.md`) and its three tools. It
  loads only where `SHOP_CATALOGUES` names a shop and `BROWSER_URL` gives a browser; otherwise `GET /v1/toolsets` lists it
  with the two settings it lacks.
- **A search.** `ShopTools.FindProducts(shop, query, maxResults, area)` asks the shop's `IShopCatalogue` and returns at
  most 20 products as a table, framed `--- untrusted shop data … ---`: name, size, pack price (a range for what's sold
  by weight), price per kg, photo and link. `SearchSpecials` and `SearchPromotions` run the same search and keep one
  kind of deal (`ShopDealKind`), with the shop's own wording in a *Deal* column.
- **Checkers.** `CheckersCatalogue` opens Checkers' search page through core's guarded browser, as a shopper's browser
  would, keeps the catalogue answer the page fetches, and parses it; no internal API is called directly.
- **The user's store.** Checkers answers for a default store unless an area is chosen. Where the user shared a town
  (the session's place, or the `area` parameter when a caller has no session), `CheckersStore` finds the town's centre
  with `PlaceCoordinates` (Nominatim, the town only, South Africa only), asks Checkers which store serves it, and keeps
  that store's cookies as the session its searches start from. The table says which store answered, and says so when
  the town's store was asked for but another answered.
- **Being a good guest.** `ShopThrottle` allows two searches at once per shop and pauses every search after the shop
  refuses one; `StoreSessionCache` looks each town up once, keeps it a day, and can keep it in a file across restarts.
- **Failures are text.** A shop's error, an unknown shop or an unreachable store comes back as a result with its reason
  (`ShopToolMessage`), never an exception the model can't read.

## Design notes

What F33 and F34 settled, and why (the measured story is [write-up 07](../learning/07-context-engineering.md): sourced
prices 0 of 32 when the cook read pages, 19 of 23 with the catalogue, 31 of 31 with the user's store):

- **Visit the page as a shopper does, call no API of theirs.** A search is the page Checkers' own search box opens; the
  provider keeps the catalogue answer the page fetches (`get-products-filter`). Checkers' `dealsOnly` filter isn't
  used, since it would change the page's own request. The one call of the site's own is the store lookup its address
  picker makes (`fetch-store-contexts`), with the town's centre only, once per town a day, in a visit of its own whose
  cookies never reach another visit. Reading a shop's site is governed by its terms, so this is for development and
  evals ([ADR 0023](../adr/0023-copyleft-service-beside-us.md)), and a retailer's permission or feed comes first for
  more shops.
- **Back off, never get round.** Two searches at once per shop, a 20-second pause after a refusal (Checkers allowed
  about 50 searches in seven minutes, then refused for four), and a refused or failed lookup is never kept as an answer.
  Nothing changes who we look like or where we ask from.
- **A pack's price, never its price per kg.** A pack sold by weight has `Price` and `PriceTo`, the lightest and the
  heaviest pack (per kg times the pack's weight), shown as a range, both `null` without a weight; the cook totals the
  upper end. A multi-buy's price stays one pack's, the deal named beside it.
- **A deal is on the product,** from the same visit: `Special` when the answer links it to a bonus buy, else
  `Promotion` when it's flagged, with the card's own wording (which can be missing when the page is slow; the kind
  still says there is one).
- **The table names the store that answered,** by Checkers' own id, and names the town only when that store is one of
  the town's; a town Checkers answers with its fallback stores has no store of its own, and the header never claims
  one. `area` keeps only the town: a part with a digit (a house number, a postcode) never goes to Nominatim, the log or
  the cache file.
- **Photos from the shop's image host only.** Each product's photo is the one its card shows; the web apps load an
  image a model writes only over https from a host that came back in a tool result (FX22), any other becomes a link, so
  injected text can't leak the chat through an image's address.
- **Read-only, so its calls run at once** ([ADR 0026](../adr/0026-parallel-tool-calls-for-read-only-toolsets.md)): a
  round's shop searches run in parallel, as `web`'s do.
- **The cook prices only shops it has a tool for** (Louis's choice): a shop with a branch in the town and a tool; else
  its rows say `-` and *No shop near you has a catalogue here yet*, with a polite note per shop it can't price. A branch
  is looked up by name, since OpenStreetMap maps some outside their town's boundary (Malmesbury's Checkers is on the
  N7).

## Structure

| Path | What's in it |
|---|---|
| `ShopsPack.cs` | The pack: the `shops` toolset, what it needs, how its tools are built; one store cache for every engine |
| `ShopsToolset.cs`, `Catalogues/ShopKind.cs` | The pack's own toolset enum (ADR 0043); `ShopKinds.DisplayName` is the shop's name as a person reads it, its own table and never the wire name (F18 step 7.5) |
| `ShopsOptions.cs` | The pack's own settings, read once from the environment |
| `Tools/ShopTools.cs` | The three tools and the table they return |
| `Catalogues/` | The shop contract (`IShopCatalogue`), the records (`ShopProduct`, `ShopDeal`, `ShopSearch`), the choices (`ShopKind`, `ShopDealKind`) and `ShopCatalogueFactory`, which builds a catalogue per enabled shop |
| `Providers/` | Checkers: `CheckersCatalogue` (search and parse), `CheckersStore` (the store for a town), `ShopThrottle`, `StoreSessionCache` |
| `Places/PlaceCoordinates.cs` | A town's centre from Nominatim, for a shop that serves by area |
| `Checks/` | The shop rules that prove the tools (F49.3), an eval file names them: `ShopsChecks.All` (the pack's list, which the composition's `EvalChecks` gathers), `EstimatesAtShop` (`estimates-at-shop`: no shop looked up, no price as fact, the estimates line, the branch under Where to buy; whether a price is given as fact is read by the `IPriceReader` the file names, Puls's `rand-prices`) and `UntooledShops` (`untooled-shops`: no price column for a shop the agent can't read), and `PricedAtRetailer` (`priced-at-shop`, no entry: kept, tested, for the day a retailer gives access); the pack references the check contract for them, never the harness |
| `Checks/Reading/` | What the rules read an answer with: `ShopSites` (the shops' names and sites, held equal to Puls's copy by the composition's `ShopTableTests`, and the tools that read a shop), `ShopLines` (the estimates and no-branch lines, plain scans), `BranchReading` and `BranchFinding` (the branch under Where to buy), and `ShopExpectation` with `ScenarioShop` (the scenario's `shop` and `town`, and the rule that a Checkers scenario names its town; the one reader of them since F49.3) |
| `Messages/` | `ShopCheckMessage` (the shop rules' reasons), `ShopToolMessage` (what the tools return) and `ShopLogMessage` (its log lines: the shop and what happened stay in clear; the town, its centre's coordinates, the store ids and a failure's reason show as keyed hashes and lengths through core's `LogContent`, F18 step 4f; `LOG_CONTENT=full` shows them) |
| `Prompts/toolsets/shops.md` | The guide the model reads with the toolset: its routing line, the tools, how to search and price |

## Configuration

| Setting | What it does |
|---|---|
| `SHOP_CATALOGUES` | The shops whose catalogue may be read, comma separated (`checkers`). Unset: the toolset is off |
| `SHOP_STORE_CACHE` | A file that keeps each town's store a day, across restarts. Unset: memory only |
| `BROWSER_URL` | Core's headless browser, which the catalogues read through (core's setting) |

Details in [Setup](../SETUP.md). Reading a shop's site is governed by its terms, so the shops are for development and
evals until those are checked (*Design notes*).

## Extending it

A new shop is [Add a shop](../guides/add-a-shop.md); a pack like this one from scratch is
[Build a pack](../guides/build-a-pack.md). In short: a member in `ShopKind`, a provider in `Providers/` implementing
`IShopCatalogue` (with its own throttle, created in `ShopCatalogueFactory`), tests on a recorded answer, and a line in
the guide; each only with the retailer's permission or feed.

## Tests

`tests/louis-agent.pack.shops.tests`, mirroring the folders: `ShopsPackTests` (the guide comes from the pack, routes
here and holds no procedure), `ShopToolsTests` (the toolset needs a shop and a browser; the tables, the deals, the
store line, every failure text), `CheckersCatalogueTests` and `CheckersStoreTests` (on real Checkers answers and pages
in `Providers/Fixtures/`, recorded and trimmed, through a fake browser), `ShopThrottleTests`, `StoreSessionCacheTests`, `PlaceCoordinatesTests`,
`ShopCatalogueFactoryTests`, and `ShopToolMessageSnapshotTests` with its golden of the model-facing texts.
`PackConvertibilityTests` (composition tests) checks the MCP shapes.

## Limits and plans

- One shop. Shoprite, Pick n Pay and Woolworths come after *Sources as data*, which makes `ShopKind` rows of data
  ([TODO](../../TODO.md), *Puls*).
- The geocoder lives here until the Places pack takes it behind `IGeocoder`.
- `CheckersCatalogue.cs` is over the 200-line limit, on `FileSizeTests`' list of files to split
  ([ADR 0034](../adr/0034-code-a-person-can-maintain.md)).
- A site change at Checkers breaks the parser; the recorded fixtures make that fail in the tests first, and a live run
  shows it.

## Related docs

[Build a pack](../guides/build-a-pack.md) · [louis-agent.identity.puls](louis-agent.identity.puls.md) ·
[Write-up 07](../learning/07-context-engineering.md) ·
[For site owners](../SITE_OWNERS.md)

---
[Projects](README.md) · Previous: [louis-agent.identity.puls.judges](louis-agent.identity.puls.judges.md) · Next: [louis-agent.composition](louis-agent.composition.md)
