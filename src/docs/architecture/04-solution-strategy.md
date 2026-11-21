# 4. Solution strategy

> **Explanation.** The few approaches every part of louis-agent follows, each in a paragraph, with the decision record
> that holds its reasons. **Elsewhere:** how they shape the parts in [building blocks](05-building-block-view.md).

![Solution strategy: our own loop, one core with thin hosts, the framework apart from the agents, prompts as content, measuring everything and safety by construction](diagrams/04-solution-strategy.svg)

**Our own loop, any provider.** louis-agent calls models at the Messages API tier through `IChatClient`, with its own
tool loop, so the provider is configuration and the tools run in our containers
([0002](../adr/0002-messages-api-tier-with-our-own-loop.md)). Where providers differ (usage, caching, thinking,
limits), the difference is a strategy behind one interface, `ILlmProvider`. Each provider's is in its own small
library, which a program hands in, and `LlmClientFactory` picks the one `LLM_PROVIDER` names
([0003](../adr/0003-provider-strategies.md)).

**One core, thin hosts.** All behaviour lives in `LouisAgent.Core`. Each host (CLI, Rider, web and API, MCP) only maps
the agent's stream to its protocol. Core's folders split by concern, and a test holds the layout
([0010](../adr/0010-folders-by-concern.md)).

**The framework apart from its agents.** Core ships no agent and no tool. An identity says who an agent is and a pack
what it can do, and only the project that picks a program's packs knows them all. The folders say what ships: `src/` is
the framework, and each demo holds its own agents, config and deploy
([0076](../adr/0076-core-ships-no-agent.md), [0074](../adr/0074-the-folders-say-what-ships.md)). Every site is built on
the framework's web, from its controls
([0084](../adr/0084-every-site-is-built-on-the-frameworks-web.md), [0125](../adr/0125-a-site-is-configured-framework-components.md)).

**A session loads only what it needs.** A session names its toolsets when it opens, and only their tools and guides
are sent, so the prefix stays small and cacheable ([0006](../adr/0006-toolsets-per-session.md)). The history only grows
([0008](../adr/0008-history-is-append-only.md)), and the guards keep any one result from flooding it
([0005](../adr/0005-guard-the-context.md)).

**Prompts are content.** The system prompt and procedures are markdown in `prompts/`, embedded by level and read only
from the assembly. The texts tools return are enum templates. Both are pinned by golden files
([0011](../adr/0011-prompts-are-content.md), [0009](../adr/0009-message-text-as-enum-templates.md)).

**Measure everything.** Every model request lands in a priced ledger, and every host shows what an answer cost. A
change to cost is proven on an eval run ([0007](../adr/0007-measure-every-request.md)).

**Safe by construction.** Model text is data, never a command ([0004](../adr/0004-arguments-as-data.md)). File tools
stay inside the workspace and away from secrets. On a PC everything listens on `127.0.0.1`, and a host on the network
refuses to start without its key, unless `AGENT_AUTH=off` says it's open on purpose.

**Agents that delegate.** A general agent (Puls) hands a task to a specialist sub-session on a peer, with only the
toolsets it needs. An orchestrator per service asks the coding agent for fixes and verifies them
([0012](../../../demos/louis-agent/docs/adr/0012-one-orchestrator-per-microservice.md)).

---
[Architecture](README.md) · Previous: [Context and scope](03-context-and-scope.md) · Next: [Building block view](05-building-block-view.md)
