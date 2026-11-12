# louis-agent.pack.where-to-buy — design

> **Kind:** class library (`net10.0`) · **Path:** `packs/tools/louis-agent.pack.where-to-buy/` · **References:**
> louis-agent.composition, Microsoft.Extensions.FileProviders.Embedded · **Loaded by:**
> `PACKS=where-to-buy`, or an identity that names it (Puls) · **Uses:** OpenStreetMap's Nominatim

![The where-to-buy pack: FindBranches, FindShopsNear and one polite map client](diagrams/louis-agent.pack.where-to-buy.svg)

## Purpose

What an agent can do to say where to buy something: find a shop's branches near the user, with their streets and
places, and say which one is the town's. A model reading a map's search page guesses at what's near; this pack asks
the map in code and hands the model a short table. It is a pack
([ADR 0027](../../../src/docs/adr/0027-engine-identities-and-packs.md)), so any identity can use it, and it could be served as an MCP
server unchanged ([ADR 0028](../../../src/docs/adr/0028-packs-stay-convertible-to-mcp.md)). It isn't tied to food: any shop with a
name on the map is the same call. It replaced the shops pack, which read Checkers' own catalogue, in F50.2
([ADR 0069](../adr/0069-shop-prices-are-stats-sa-estimates.md)); prices are the `shop-prices` pack's job
([louis-agent.pack.shop-prices](louis-agent.pack.shop-prices.md)).

## How it works

- **One toolset, `where-to-buy`.** `WhereToBuyPack` defines it with its guide (`prompts/toolsets/where-to-buy.md`)
  and two tools. It needs no setting, so it loads wherever it is named.
- **The town.** `FindBranches(shop, town)` asks Nominatim for the town (South Africa only, the first answer), which
  gives its centre, its municipality and its province.
- **The branches.** `BranchFinder` asks for the shop by name in a box around the town, about 16 km each way, then half
  a degree, then a degree and a half, widening only when a box finds none. The smallest comes first because the map
  answers at most 20 hits in no distance order, so a box that reaches a city loses the town's own branch to the city's
  (FX39). Only places Nominatim files as a shop count, and `ShopNames` reads
  a short name as the full one (`PnP` is `Pick n Pay`, `Woolies` is `Woolworths`). It keeps the three nearest, by the
  distance from the town's centre (`Distance`, the haversine formula).
- **Shops near the user.** `FindShopsNear(town)` is the same search for someone who named no shop: `q=supermarket` in a
  box of about 3 km, then 16, then 55, keeping what the map files as a supermarket and has a name for, the ten nearest.
  The first box is small because a city's 16 km box holds 20 hits in no order: Sea Point's real answer held a fabric shop
  and lost the nearest (FX43).
- **The place.** A branch's place is the smallest name the map gives, but never an election ward: the map files
  *Swartland Ward 10* where a suburb goes, so `MapAddress` skips it for the town or city above it.
- **The answer.** `BranchPrinter` writes the town's municipality and province (the province is what `EstimatePrices`
  needs), then a table: each branch's name, street, place, distance and whether it is in the town's municipality,
  framed as untrusted map data. With none in the town, the note says so and names the nearest.
- **Being a good guest.** `MapSearch` keeps Nominatim's usage policy: one request a second for the whole process, a
  User-Agent that names the app, a ten-second limit. A map that doesn't answer is logged and said, never retried in a
  loop (`MapUnavailableException`, `WhereToBuyLogMessage`).
- **The shop rules.** The checks an eval file names to prove an answer about shops live apart from the pack, in
  `packs/evals/louis-agent.pack.where-to-buy.checks/`, which only the runner references (F49.3,
  [ADR 0073](../../../src/docs/adr/0073-packs-reference-no-evals.md)): `estimates-at-shop` (no shop page read, no price given as fact,
  the estimates line, the branch under Where to buy) and `untooled-shops` (no column of a shop's prices).

## Design notes

- **In the town's municipality, not within a radius.** A branch is the town's when the map files it under the same
  municipality: Malmesbury's Checkers on the N7 is outside the town's edge but is the town's shop. A radius would
  call a branch in the next town "in town".
- **The nearest three, never more.** Enough to choose from, small enough that the model never reads a long list.
- **Credit the map.** OpenStreetMap's data is under the ODbL, which asks for credit wherever it is shown, so the guide
  and the cook say *Branches from © OpenStreetMap contributors.* under the branches.
- **The map is a shared public service.** One request a second is the policy; a busy public Puls needs its own
  Nominatim or a paid geocoder (*Limits and plans*).

## Diagrams

![The where-to-buy pack: finding branches on the map](diagrams/louis-agent.pack.where-to-buy-diagrams.svg)

![Finding a shop's branches: the town is found on the map, then ever bigger boxes round it until one holds the shop, and the three nearest are printed](diagrams/louis-agent.pack.where-to-buy-diagrams-2.svg)

![The types: two tools over the map, the town first, then the shop's branches around it](diagrams/louis-agent.pack.where-to-buy-interface.svg)

## Structure

| Path | What's in it |
|---|---|
| `WhereToBuyPack.cs`, `WhereToBuyToolset.cs` | The pack and its toolset enum (ADR 0043) |
| `Tools/` | `WhereToBuyTools.FindBranches` and `FindShopsNear`, and `BranchPrinter`, which writes their table |
| `Branches/` | `BranchFinder` (the town, the boxes, the nearest three), `BranchSearch` (its result), `Branch`, `Distance`, `ShopNames` |
| `Branches/Map/` | Nominatim: `MapSearch` (the requests, the pace), `MapPlace` and `MapAddress` (its answer, as records), `MapUnavailableException` |
| `../louis-agent.pack.where-to-buy.checks/Checks/` | The shop rules (`WhereToBuyChecks.All`, which the runner's `PulsCatalogs` gathers): `EstimatesAtShop`, `UntooledShops`; `Reading/` holds what they read an answer with (`ShopSites`, `ShopLines`, `BranchReading`, `BranchFinding`, `ShopExpectation`, `ScenarioShop`) |
| `Messages/` | `WhereToBuyToolMessage` (what the tool returns), `WhereToBuyLogMessage` (its log line; the town shows as a hash through core's `LogContent`), `ShopCheckMessage` (the rules' reasons) |
| `prompts/toolsets/where-to-buy.md` | The guide the model reads with the toolset: its routing line, the tool, how to name a branch |

## Configuration

None. The User-Agent and the pace are code, so a change to either is a reviewed commit.

## Extending it

- **Another kind of shop** needs nothing: a hardware store or a pharmacy is the same `FindBranches` call. A short name
  people use for it is a line in `ShopNames`.
- **Another map** (our own Nominatim, a paid geocoder) is another `MapSearch`, behind an interface when a second one
  exists.
- A new pack like this one is [Build a pack](../../../src/docs/guides/build-a-pack.md).

## Tests

`packs/tests/louis-agent.pack.where-to-buy.tests`, mirroring the folders: `MapSearchTests` (the requests' addresses and the
answer read through records, on fixtures in Nominatim's `jsonv2` shape written by hand, since the container has no
route to Nominatim), `BranchFinderTests` (the nearest three, the second box, the municipality), `ShopNamesTests`,
`DistanceTests`, `WhereToBuyToolsTests` (the table and every failure text), `WhereToBuyToolMessageSnapshotTests` with
its golden of the model-facing texts. The checks' tests are in `packs/tests/louis-agent.pack.where-to-buy.checks.tests`.
`PackConvertibilityTests` (composition tests) checks the MCP shapes.

## Limits and plans

- Nominatim's public service allows one request a second; Puls in public needs its own instance.
- The map holds what its mappers added: a branch it lacks can't be named, and the note says so.
- The fixtures are hand-written in Nominatim's shape; a live check against the real service is a hand check.

## Related docs

[louis-agent.pack.shop-prices](louis-agent.pack.shop-prices.md) · [Build a pack](../../../src/docs/guides/build-a-pack.md) ·
[louis-agent.identity.puls](../../../demos/puls/docs/projects/louis-agent.identity.puls.md) · [ADR 0069](../adr/0069-shop-prices-are-stats-sa-estimates.md)

---
[Projects](../../../src/docs/projects/README.md) · Previous: [louis-agent.identity.puls.judges](../../../demos/puls/docs/projects/louis-agent.identity.puls.judges.md) · Next: [louis-agent.pack.shop-prices](louis-agent.pack.shop-prices.md)
