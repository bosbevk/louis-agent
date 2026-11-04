# louis-agent.pack.where-to-buy — design

> **Kind:** class library (`net10.0`) · **Path:** `packs/louis-agent.pack.where-to-buy/` · **References:**
> louis-agent.core, louis-agent.evals.checks, Microsoft.Extensions.FileProviders.Embedded · **Loaded by:**
> `PACKS=where-to-buy`, or an identity that names it (Puls) · **Uses:** OpenStreetMap's Nominatim

## Purpose

What an agent can do to say where to buy something: find a shop's branches near the user, with their streets and
places, and say which one is the town's. A model reading a map's search page guesses at what's near; this pack asks
the map in code and hands the model a short table. It is a pack
([ADR 0027](../adr/0027-engine-identities-and-packs.md)), so any identity can use it, and it could be served as an MCP
server unchanged ([ADR 0028](../adr/0028-packs-stay-convertible-to-mcp.md)). It isn't tied to food: any shop with a
name on the map is the same call. It replaced the shops pack, which read Checkers' own catalogue, in F50.2
([ADR 0069](../adr/0069-shop-prices-are-stats-sa-estimates.md)); prices are the `shop-prices` pack's job
([louis-agent.pack.shop-prices](louis-agent.pack.shop-prices.md)).

## How it works

- **One toolset, `where-to-buy`.** `WhereToBuyPack` defines it with its guide (`Prompts/toolsets/where-to-buy.md`)
  and one tool. It needs no setting, so it loads wherever it is named.
- **The town.** `FindBranches(shop, town)` asks Nominatim for the town (South Africa only, the first answer), which
  gives its centre, its municipality and its province.
- **The branches.** `BranchFinder` asks for the shop by name in a box around the town, half a degree each way, then a
  degree and a half when the first box finds none. Only places Nominatim files as a shop count, and `ShopNames` reads
  a short name as the full one (`PnP` is `Pick n Pay`, `Woolies` is `Woolworths`). It keeps the three nearest, by the
  distance from the town's centre (`Distance`, the haversine formula).
- **The answer.** `BranchPrinter` writes the town's municipality and province (the province is what `EstimatePrices`
  needs), then a table: each branch's name, street, place, distance and whether it is in the town's municipality,
  framed as untrusted map data. With none in the town, the note says so and names the nearest.
- **Being a good guest.** `MapSearch` keeps Nominatim's usage policy: one request a second for the whole process, a
  User-Agent that names the app, a ten-second limit. A map that doesn't answer is logged and said, never retried in a
  loop (`MapUnavailableException`, `WhereToBuyLogMessage`).
- **The shop rules.** The checks an eval file names to prove an answer about shops live here (F49.3,
  [ADR 0060](../adr/0060-a-pack-ships-its-checks.md)): `estimates-at-shop` (no shop page read, no price given as fact,
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

## Structure

| Path | What's in it |
|---|---|
| `WhereToBuyPack.cs`, `WhereToBuyToolset.cs` | The pack and its toolset enum (ADR 0043) |
| `Tools/` | `WhereToBuyTools.FindBranches` and `BranchPrinter`, which writes its table |
| `Branches/` | `BranchFinder` (the town, the boxes, the nearest three), `BranchSearch` (its result), `Branch`, `Distance`, `ShopNames` |
| `Branches/Map/` | Nominatim: `MapSearch` (the requests, the pace), `MapPlace` and `MapAddress` (its answer, as records), `MapUnavailableException` |
| `Checks/` | The shop rules (`WhereToBuyChecks.All`, which the composition's `EvalChecks` gathers): `EstimatesAtShop`, `UntooledShops`; `Reading/` holds what they read an answer with (`ShopSites`, `ShopLines`, `BranchReading`, `BranchFinding`, `ShopExpectation`, `ScenarioShop`) |
| `Messages/` | `WhereToBuyToolMessage` (what the tool returns), `WhereToBuyLogMessage` (its log line; the town shows as a hash through core's `LogContent`), `ShopCheckMessage` (the rules' reasons) |
| `Prompts/toolsets/where-to-buy.md` | The guide the model reads with the toolset: its routing line, the tool, how to name a branch |

## Configuration

None. The User-Agent and the pace are code, so a change to either is a reviewed commit.

## Extending it

- **Another kind of shop** needs nothing: a hardware store or a pharmacy is the same `FindBranches` call. A short name
  people use for it is a line in `ShopNames`.
- **Another map** (our own Nominatim, a paid geocoder) is another `MapSearch`, behind an interface when a second one
  exists.
- A new pack like this one is [Build a pack](../guides/build-a-pack.md).

## Tests

`tests/louis-agent.pack.where-to-buy.tests`, mirroring the folders: `MapSearchTests` (the requests' addresses and the
answer read through records, on fixtures in Nominatim's `jsonv2` shape written by hand, since the container has no
route to Nominatim), `BranchFinderTests` (the nearest three, the second box, the municipality), `ShopNamesTests`,
`DistanceTests`, `WhereToBuyToolsTests` (the table and every failure text), `WhereToBuyToolMessageSnapshotTests` with
its golden of the model-facing texts, and the checks' tests under `Checks/`. `PackConvertibilityTests` (composition
tests) checks the MCP shapes.

## Limits and plans

- Nominatim's public service allows one request a second; Puls in public needs its own instance.
- The map holds what its mappers added: a branch it lacks can't be named, and the note says so.
- The fixtures are hand-written in Nominatim's shape; a live check against the real service is a hand check.

## Related docs

[louis-agent.pack.shop-prices](louis-agent.pack.shop-prices.md) · [Build a pack](../guides/build-a-pack.md) ·
[louis-agent.identity.puls](louis-agent.identity.puls.md) · [ADR 0069](../adr/0069-shop-prices-are-stats-sa-estimates.md)

---
[Projects](README.md) · Previous: [louis-agent.identity.puls.judges](louis-agent.identity.puls.judges.md) · Next: [louis-agent.pack.shop-prices](louis-agent.pack.shop-prices.md)
