# 0028. Packs stay convertible to MCP; MCP plugs in at the toolset

**Status:** Accepted · **Date:** 2026-10-19 (decided in the [platform plan](../plans/platform.md)) ·
**Recorded:** 2026-10-19

## Context

Louis: "the rules must state then that this layer [should be convertible] to mcp quickly", and MCP only "if needed".
A business's own systems should plug in without our code, and our packs should be publishable to other agents. An
earlier draft listed an MCP server's tools when a session opened, which can't work: engines are cached per toolset
list (`AgentEngineFactory`), one engine holds one tool list (`ToolRegistry`), and `CreateTools` is synchronous
(`ToolsetDefinition`), so a list that changes after start would change open chats.

What the ModelContextProtocol 2.2.0 source says (read, not run): it uses the 2026-07-28 protocol revision by default,
probing with `server/discover` and falling back to `initialize` within a 5 s probe timeout; `serverInfo` is optional;
`list_changed` arrives only after `subscriptions/listen`, which the 2.2.0 client never opens; a listing carries
`ttlMs` and `cacheScope` hints; a server's list may vary with the authorization presented; annotations are untrusted
unless the server is trusted; clients should prefix tool names, and servers should return tools in a fixed order.

Considered: building `McpPack` now; async start and dispose on `IAgentPack`; `CreateTools` typed to `AITool`; listing
when a session opens; doing nothing.

## Decision

- **Every pack is kept shaped so it could be served as an MCP server, or replaced by one**, with little or no new
  code. Nothing is converted until a real need comes.
- **MCP plugs in at the toolset:** a configured server becomes a pack with one `ToolsetDefinition`, connected and listed
  when the host starts, before the catalogue is built. `list_changed`, `ttlMs` and `cacheScope` are ignored on purpose.
  An admin refresh may list the servers again for chats opened after it; an open chat's tools never change (F36 step
  9a, the plan's question 12).
- **A pack's tools** take plain data and return text, a failure as text with its reason; what they read from the
  session also arrives as a parameter; no description names an identity. A pack frames the outside data it returns,
  bounds its own output, keeps its guides as prose, reads its own settings and core's shared services, and names other
  packs instead of referencing them.
- **Tool names** are unique across the catalogue, MCP-safe, and come from one function, `AgentCatalog.NamesOf`.
- **A server is pinned** by its version when it reports one, and always by a hash of the listed tools; an allowlisted
  tool missing from the list counts as drift. The `ReadOnly` hint comes from a tool's declared effect (`ToolEffect`),
  never from `ParallelSafe`.

## Consequences

- `PackConvertibilityTests` holds the shapes, through the SDK's own `McpServerTool.Create` (a test-only
  `ModelContextProtocol.Core`); a review checklist holds the behaviour the test can't see.
- A server that is down when the host starts stays off until a refresh or a restart, with its reason shown.
- An orange pack's guide is configuration read once at start-up, which refines [0018](0018-built-in-prompts-embedded.md)
  for packs that are configuration.
- Someone else's code runs as their own MCP server, never as a DLL in our process (F37 cut).

## Links

[Platform plan](../plans/platform.md) (Part 2, Phases 2 and 8) · [0027](0027-engine-identities-and-packs.md) ·
[0006](0006-toolsets-per-session.md) · [0008](0008-history-is-append-only.md)

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
