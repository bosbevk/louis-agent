# 11. Risks and technical debt

> **Explanation.** What could go wrong, and what is knowingly unfinished. **Elsewhere:** the one list of known
> limitations, each with what removes it, in the [TODO](../../TODO.md#known-limitations).

## Known limitations

The TODO keeps the list (in-memory sessions, a 10-round limit per message, local models' truncated prompts, changing
tools mid-chat on Haiku 5.5, and more), each with the feature or fix that removes it.

## Open fixes

| Fix | Risk |
|---|---|
| [FX4](../fixes/FX04-append-only-history.md) | Changing tools or skills mid-chat breaks a Haiku 5.5 chat ([0008](../adr/0008-history-is-append-only.md) not yet fully held) |
| [FX9](../fixes/FX09-cancelled-request-usage.md) | A request cancelled without streaming is still written to the ledger |

## Open decisions

The orchestrator's production topology, webhook triggering, pull requests instead of local branches, and whether it
stays on core ([0012](../adr/0012-one-orchestrator-per-microservice.md)); how often Puls should delegate (the TODO).

## Rider MCP discovery (partial)

With `RIDER_MCP_AUTO_DISCOVER=true`, `AgentHost.Build()` starts a background task that asks Rider's MCP server
(`RIDER_MCP_ENDPOINT`) for its tools (`RiderMcpClient`) and passes each to `AgentEngine.RegisterDiscoveredTool` as
`rider_{name}` (`RiderMcpToolDiscovery`). **Today this only logs the discovered tools; they are not added to the
model's tool list**, because calling them needs a full MCP client. Discovery is off in `config/.env`. It never blocks
startup and failures only log.
