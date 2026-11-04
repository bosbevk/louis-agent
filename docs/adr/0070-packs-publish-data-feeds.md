# 0070. Packs publish data feeds, and the API serves them open

**Status:** Accepted · **Date:** 2026-11-04 (Louis: "we can then serve it via the api", "as open for all this is free
to use", "for now"; picked *Packs publish data feeds* over a shop-prices route in the API and a pack-mapped route)
· **Recorded:** 2026-11-04

## Context

The shop-prices pack keeps Stats SA's average prices, and our own rows, in MongoDB ([ADR 0069](0069-shop-prices-are-stats-sa-estimates.md)).
Louis wanted them served by the API, open to anyone, since they are free to use. But the API may not know a pack:
only `louis-agent.composition` references packs ([ADR 0027](0027-engine-identities-and-packs.md)), and a pack
stays convertible to an MCP server ([ADR 0028](0028-packs-stay-convertible-to-mcp.md)). Three ways were weighed:

- **A shop-prices route in the API**, reading the pack's store: the API would reference a pack, which ADR 0027
  forbids, and every new kind of data would be a new route.
- **A pack maps its own routes**: the pack would reference ASP.NET Core, and an MCP server has no routes, so the pack
  would no longer convert.
- **Packs publish data feeds**: core names a read-only feed (a name, a description, and its data as JSON), a pack lists
  its own, and the API serves every feed the catalogue holds, without knowing whose it is.

## Decision

*A pack can publish read-only data feeds, and the API serves each one, open, at `/v1/feeds/{name}`.*

- **`IDataFeed`** in core (`Agent/Catalog/`): `Name`, `Description` and `ReadJsonAsync`. `IAgentPack.Feeds` lists a
  pack's feeds and is empty unless the pack says otherwise, so no other pack changes.
- **The catalogue gathers them** (`AgentCatalog.Feeds`, `Feed(name)`) and refuses a name twice, as it does a toolset's.
- **The pack writes its own JSON** from its own records ([ADR 0041](0041-json-through-records-by-hand-only-for-bytes.md));
  the API passes the text on and knows nothing of its shape, as `GET /v1/rates` passes on our rates.json.
- **Open routes**: `GET /v1/feeds` lists each feed with its path, and `GET /v1/feeds/{name}` reads one; a name no pack
  publishes is the API's 404. They are open beside the prices and the rates ([ADR 0039](0039-api-routes-under-v1.md)'s
  rule that a route is closed until the code opens it): what a pack publishes is meant for anyone.
- **The first feed is `shop-prices`**: the tables the tool estimates from, through the tool's own cache (so the store is
  read at most once an hour, and a store that is down answers with the shipped tables), under a notice that says they
  are averages and whose they are.

## Consequences

- Another kind of data (rent, fuel) is a feed in its pack, with no route and no change to the API.
- A feed maps one to one to an MCP resource, so a pack served over MCP can publish the same data; the MCP server
  doesn't yet (on the TODO).
- Open means anyone, and as many times as they like. "For now": a feed that costs us to serve, or data we may not give
  away, needs a key or a limit first, and that is a new decision, not a flag.
- A feed is read whole. A large one would need paging or a filter (a province, a product), which no feed needs yet.
