# 4. Solution strategy

> **Explanation.** The few approaches every part of louis-agent follows, each in a paragraph, with the decision record
> that holds its reasons. **Elsewhere:** how they shape the parts in [building blocks](05-building-block-view.md).

**Our own loop, any provider.** louis-agent calls models at the Messages API tier through `IChatClient`, with its own
tool loop, so the provider is configuration and the tools run in our containers
([0002](../adr/0002-messages-api-tier-with-our-own-loop.md)). Where providers differ (usage, caching, thinking, limits),
the difference is a strategy chosen once in `LlmClientFactory` ([0003](../adr/0003-provider-strategies.md)).

**One core, thin hosts.** All behaviour lives in `louis-agent.core`; each host (CLI, Rider, web and API, MCP) only maps
the agent's stream to its protocol. Core's folders split by concern, and a test holds the layout
([0010](../adr/0010-folders-by-concern.md)).

**A session loads only what it needs.** A session names its toolsets when it opens; only their tools and guides are
sent, so the prefix stays small and cacheable ([0006](../adr/0006-toolsets-per-session.md)). The history only grows
([0008](../adr/0008-history-is-append-only.md)), and the guards keep any one result from flooding it
([0005](../adr/0005-guard-the-context.md)).

**Prompts are content.** The system prompt and procedures are markdown in `Skills/`, embedded in core and overridable
on disk; the texts tools return are enum templates; both are pinned by golden files
([0011](../adr/0011-prompts-are-content.md), [0009](../adr/0009-message-text-as-enum-templates.md)).

**Measure everything.** Every model request lands in a priced ledger, and every host shows what an answer cost; a
change to cost is proven on a benchmark run ([0007](../adr/0007-measure-every-request.md)).

**Safe by construction.** Model text is data, never a command ([0004](../adr/0004-arguments-as-data.md)); file tools
stay inside the workspace and away from secrets; everything listens on `127.0.0.1`.

**Agents that delegate.** A general agent (Puls) hands a task to a specialist sub-session on a peer, with only the
toolsets it needs; an orchestrator per service asks louis-agent for fixes and verifies them
([0012](../adr/0012-one-orchestrator-per-microservice.md)).
