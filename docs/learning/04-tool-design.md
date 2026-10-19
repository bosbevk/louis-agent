# 4 · Tool design: what the cut taught about packs

> **Skill:** #4 in the [goal](../GOAL.md)'s map, with #11 (MCP) for the convertibility rule · **Stage:** the platform
> plan's Phase 2, CL15 *Engine, identities and packs* (2026-10-19) · **Written by** Claude, for Louis to read
> (2026-10-19) · **Next:** results as tool calls between agents (F39, Phase 6); each tool's declared effect and errors
> that list every problem at once (F41, Phase 7); MCP servers as packs (F36, Phase 8)

What this answers: what changed in how a tool reaches the model when the shops left core for a pack, why a pack has a
contract and a test instead of a convention, and what "convertible to MCP" buys before anything is converted.

## Before and after

| | Before CL15 | After CL15 |
|---|---|---|
| Where a toolset lives | One list in core, `ToolsetCatalog.All`, the shops among the coding tools | Core's toolsets in `ToolsetCatalog`; a pack's in its own project, listed through one `AgentCatalog` |
| Where a tool's settings live | `AgentOptions`, one class for everything (`SHOP_CATALOGUES` beside `PAYMO_API_KEY`) | The pack's own options class, read at the composition root; core's options know no shop |
| Who may name a tool | Any class in core; a duplicate failed when an engine was built | One function, `AgentCatalog.NamesOf`; a duplicate, or an engine tool's name, stops the host at start-up |
| What a tool's text may say | The `area` parameter said "when Puls gave no place" | No description names an identity; the brief is "the brief" |
| What holds the rule | A sentence in an ADR | `PackConvertibilityTests`, over every `PackKind`, through the MCP SDK's own `McpServerTool.Create` |
| The model's view | Unchanged on purpose: the same 3 shop tools, the same guide, one routing sentence moved | Proved by the Puls golden: 3 `area` texts and 1 routing line changed, nothing else |

## What a pack is, and why it has a contract

A toolset was already a seam (F5): a session names its toolsets and gets only those tools and their guides. The cut
made the seam a contract, `IAgentPack`: a name, its `ToolsetDefinition`s, its prompts, and the packs it requires by
name. Nothing in it says "shops". The engine never references a pack; the composition root does, once, and hands the
engine a catalogue. That is an extension point in the classic sense: open to a new pack, closed to a change in core.
The shops moved without touching the engine's loop, its guards or its prompt assembly, and the test that proves it is
the coding agent's golden, which lost exactly the shop tools and nothing else.

The contract has a shape beyond C#. A pack's tools take plain data and return text, a failure as text with its reason;
whatever a tool reads from the session also arrives as a parameter; no description names an identity; the guides are
prose. Each of those is what an MCP server can and can't do: a server can't throw into our loop, can't read our
session, can't know who Puls is, and can't run a `## Skill:` fence. Keeping every pack in that shape means a pack can
be served to another agent, or replaced by someone else's server, with little new code, when a real need comes. Until
then it costs a test.

## What the test can and can't see

`PackConvertibilityTests` builds each tool's function the way the engine builds it, from the tool class and never an
instance, so it needs no browser, setting or network, then wraps it in the MCP SDK's `McpServerTool` and checks the
name, the input schema and the description survive. It also checks the shapes: a return type of `string`,
`Task<string>` or `ValueTask<string>`; parameters that are plain data (strings, numbers, booleans, enums, dates,
nullables and sequences of those, our own records of those) plus a `CancellationToken`, which an async tool must take;
a name that matches `^[A-Za-z0-9_-]{1,64}\z`; no identity's name as a whole word in any description; a guide in the
pack's own prompts with no `## Skill:` line. A fake pack that breaks each rule once must produce exactly eight
problems, so the test is tested too.

It can't see behaviour: a tool that throws instead of returning text, one that reads the user's place from the session
instead of a parameter, outside data returned without its frame, output without a bound. Those are the review
checklist in [core's guide](../../louis-agent.core/CLAUDE.md#building-an-identity-or-a-pack). The split matters: a
rule a test can hold is held by a test, and what it can't hold is written where the reviewer looks, not left implied.

## What was learned

- **A tool's text is part of its interface.** The one thing the cut changed in what the model reads was an argument's
  description that named Puls. A pack's description is read by whatever agent loads it; naming one identity there
  would have made the shops Puls's forever. The test now refuses it as a whole word, case ignored, so "demonstrate"
  isn't "Demo".
- **Settings belong to the thing they configure.** `SHOP_CATALOGUES` in `AgentOptions` meant every host knew about
  shops. In `ShopsOptions`, read where the pack is built, the coding agent's host has no shop setting unless it loads
  the pack. The same move waits for `PAYMO_API_KEY` and the DevOps keys when CL17 makes those packs.
- **A pack can't route itself.** The shops' routing row left the coding agent's mode for the pack's guide, which loads
  only where the toolset does, so the coding agent never learns the shops exist. Nothing is lost today, and it is on
  the TODO: when F36 brings MCP packs, the routing rows come from each toolset's description.
- **Shared state is the pack's, one per host.** The store cache that remembered each town's store sat on the toolset
  context; it is now a field of `ShopsPack`, one instance however many engines a host builds, which is what F34 wanted
  and what an MCP server would have to do for itself.
- **Reuse before building held.** The SDK's own `McpServerTool.Create` checks what a server would publish, instead of a
  hand-written mirror of its rules; it is a test-only package, kept in step with the MCP server's.

---
[Learning write-ups](README.md) · [Goal](../GOAL.md) · [Plan](../plans/CL15-engine-identities-packs.md) ·
[ADR 0027](../adr/0027-engine-identities-and-packs.md) · [ADR 0028](../adr/0028-packs-stay-convertible-to-mcp.md)
