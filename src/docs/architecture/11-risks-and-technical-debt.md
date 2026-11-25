# 11. Risks and technical debt

> **Explanation.** What could go wrong, and what is knowingly unfinished. **Elsewhere:** the one list of known
> limitations, each with what removes it, in the [TODO](../../../TODO.md#known-limitations).

![Risks and technical debt: the known limitations, the open fix, the open decisions and Rider's discovery](diagrams/11-risks-and-technical-debt.svg)

## Known limitations

The TODO keeps the one list of what is true today but shouldn't stay that way, each with the feature or fix that
removes it. It holds things like these: sessions live in memory, so a restart ends them. A message gets at most 10
tool rounds. A local model gets a truncated prompt. And changing the tools mid-chat breaks a chat on Haiku 5.5. This
page doesn't repeat the list, so the two can't drift apart.

## The open fix

A Haiku 5.5 chat rejects a request once its system prompt or tool list changed after thinking blocks were written,
so history must stay append-only ([ADR 0008](../adr/0008-history-is-append-only.md)). Since FX4 that holds: a chat
keeps the tools and the prompt it started with, and a change reaches the next chat. What remains is a cost, not a
break: a chat resumed after a release moved its tools loses the thinking that no longer matches, stripped by us or
dropped by Anthropic's API.

## Open decisions

The orchestrator still has open questions, recorded in its
[ADR 0012](../../../demos/louis-agent/docs/adr/0012-one-orchestrator-per-microservice.md). Should one instance serve
every service, or one each? Should a webhook trigger it? Should it open pull requests instead of local branches? And
should the real one keep core, or move to Anthropic's Tool Runner?

## Rider's tools, found but not callable

When the ACP server starts, it can ask Rider's MCP server (`RIDER_MCP_ENDPOINT`, by default `http://127.0.0.1:64482`)
for its tools, in the background. Each tool it finds goes to the engine as `rider_{name}`. **Today the engine only logs
them; they are not added to the model's tool list**, because calling them needs a full MCP client. A failure only
logs, and it never holds up start-up.

The discovery is the ACP server's own, handed to core through its `IToolDiscovery` seam, so no other host tries to
reach Rider. It is on unless `RIDER_MCP_AUTO_DISCOVER` says `false`, and the example `config/.env` turns it off.
