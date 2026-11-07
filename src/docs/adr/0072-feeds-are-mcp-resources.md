# 0072. The MCP server publishes the packs' feeds as resources

**Status:** Accepted · **Date:** 2026-11-04 (Louis picked item 1, "the packs' feeds as MCP resources", then *one
resource per feed*, *`feed://{name}`*, *a real client in memory* and *behind the key, like all*) · **Recorded:** 2026-11-04

## Context

A pack publishes read-only data feeds, and the API serves each one open at `/v1/feeds/{name}`
([ADR 0070](0070-packs-publish-data-feeds.md)). ADR 0070 said a feed maps one to one to an MCP resource, but the MCP
server published only tools: a client using louis-agent over MCP couldn't read the price tables the
`shop-prices` tool estimates from. MCP has three server primitives: **tools** (the model chooses to call them, and they
may act), **resources** (data the application or the user chooses to put in front of the model, read by a URI) and
**prompts**. A feed is plainly a resource. Four choices were open, each with three options:

- **How:** one resource per feed through the SDK's own `McpServerResource`; one template `feed://{name}` (a client
  would have to know the names); or hand-written `resources/list` and `resources/read` handlers (redoing the SDK).
- **The URI:** `feed://{name}`; `louis-agent://feeds/{name}` (the product's name in the engine); or the API's `https`
  URL (the server would have to know the API, and a client might fetch it directly).
- **The test:** a real client against a real server over in-memory pipes; the mapping alone; or a check by hand.
- **Who over HTTP:** behind the MCP key like everything else; `resources/*` open (a second auth rule reading JSON-RPC
  bodies); or a second, open MCP endpoint.

## Decision

*The MCP server publishes every feed in the catalogue as a resource, `feed://{name}`, `application/json`, read whole
by `ReadJsonAsync` when a client asks; over HTTP it sits behind the server's key.*

- **`FeedResources.From(feeds)`** (`src/hosts/louis-agent.mcp/Resources/`, an Adapter) makes one SDK resource per
  feed, with the feed's name and description; the SDK answers `resources/list` and `resources/read` and refuses a URI
  nobody publishes. The scheme is `ResourceScheme.Feed` ([ADR 0043](0043-repeated-names-are-enums.md)).
- **One setup for both transports:** `McpPrimitives.Add(options, engine, catalog)` (a Facade) puts the engine's tools
  and the feeds into the server's options; stdio and HTTP each call it once.
- **Built once at start-up:** feeds don't change while the server runs, unlike the agent's tools (`EngineToolSync`).
- **Tested over the protocol:** `tests/louis-agent.mcp.tests` runs the SDK's client against its server through
  two in-memory pipes, so a test sees what a client on stdio or HTTP sees.

## Consequences

- A new feed in a pack is a resource with no change here, as it is a route with no change to the API.
- The same data reaches a model two ways: the tool for the model, the resource for the application.
- Over HTTP the feeds need the MCP key, though the API gives them to anyone; the data stays open there. Opening them
  over MCP too would be a new decision.
- A feed is read whole, as on the API; paging or a filter would be a resource template (`feed://{name}/{province}`)
  when a feed needs it. Feeds don't send `notifications/resources/updated` when their data changes.
