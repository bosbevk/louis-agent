# ADR 0117: One address for every agent call

**Status:** Accepted (2026-11-10, Louis's picks during F127.2); built in F127.3 to F127.5

## Context

An agent reaches another agent two ways today, both on one path in core (`PeerSession`, ADR 0031): Puls asks its cook
with `StartSubSession`, which opens a session on a peer in a role (`puls-cook`) with toolsets the model picks, and a
minion starts with `StartMinion`. An identity can't ask another identity at all.

F127.2 asked for the site's guide to help a visitor use a demo's business agent, and to send it a question when the
visitor wants it. Louis then set the shape: "the agent must call the other agent via the identity api", "this is not
delegation this is just our guide mentoring the visitor", "we already do it with puls so what is different", "the guide
should be able to start it for the visitor or add to it later if needed", "puls could call louis-agent.cook?", and "you
should be able to call an identity --> identity, identity --> identity.specialist, identity --> specialist, identify
--> minion".

So the guide's question to a business is the same call as Puls's to its cook, and the four calls want one way to be
named. An identity is who answers: its voice, its facts, its packs (ADR 0027). A role is a prompt that identity takes
on with the tools its header names: a specialist's toolsets (ADR 0025), or a minion's one task and one tool on a local
model (ADR 0021). The open request already takes both (`identity`, F109.3; `role`, F21), so an address never needs
toolsets of its own: the role brings them.

## Decision

- **Every agent call has an address:** `{identity}` or `{identity}.{role}`, read once into a typed value
  (`AgentAddress`, a Value Object), its kind from the identity's own files: the identity in its default voice, one of
  its specialists, or one of its minion roles. A role's file is named for its identity (`puls-cook`), so `puls.cook`
  finds it. The four calls are `seemeeu-guesthouse`, `louis-agent.cook`, `puls.cook` (its own) and
  `puls.find-recipe` (its own minion).
- **An identity says whom it may call,** in code (`IAgentIdentity.Calls`, none unless it says so): the allow-list ADR
  0031 asks for. An address it doesn't list is refused with the reason, whatever the model asks.
- **One tool asks: `AskAgent(address, message)`,** in core's `agents` toolset. It starts a session with that address,
  or adds to the one this chat already has with it, or to the user's own session with it (a Facade over the "which
  session" rule). The model names the address and the message, nothing else: no peer, toolsets, role or model to pick.
  `StartSubSession`, `ContinueSubSession` and the `specialists` toolset go (ADR 0049: nothing is kept for backward
  compatibility).
- **A minion keeps its own tool on the same address,** `StartMinion(address, …)`, since a minion is a different thing
  to start: a local model, one task, one tool (ADR 0021, ADR 0081).
- **The plumbing stays Puls's:** `PeerSession` opens, asks, keeps and deletes; the limits per turn, the timeout, the
  depth, the usage rolled up to the asker's turn, kept sessions, and the `sub_session` events that carry the callee's
  every event, live, to the browser.
- **The session wire is shared records,** in `louis-agent.session-stream` beside the one stream reader (ADR 0047):
  the requests and replies core, the API and the web send and read, never an anonymous object or a reply read by
  property name (ADR 0041, ADR 0045).
- **Mentoring uses the same call.** The site's guide lists the three showroom businesses in its `Calls`; asking one
  for a visitor is `AskAgent`, and what differs is what the visitor sees (the business's own chat on the demo) and that
  the open door counts it against that visitor (F127.4).

## Consequences

- A new agent-to-agent call is a line in an identity's `Calls`, not a new tool. Any identity can ask another
  identity, or another identity's specialist, with the limits, the ledger and the live view it already has.
- The tool is narrower than `StartSubSession`: on a public site the model can't open a session with toolsets of its
  choosing.
- Puls moves onto `AskAgent`: its voice, the specialists guide, the web's delegation card and the evals' tool checks
  change, and the recipes eval runs before and after to prove nothing was lost. Puls's stored replay answers name the
  old tools and are recorded again.
- A role file not named for its identity can't be addressed; a test holds the naming.
- We keep one address grammar, parsed in one place and tested once.

## Links

[ADR 0031](0031-orchestration-is-an-engine-function.md) (delegation is one function in the engine; this ADR amends
its tools) · [ADR 0006](0006-toolsets-per-session.md) · [ADR 0027](0027-engine-identities-and-packs.md) ·
[ADR 0021](0021-minions-one-task-one-tool.md) · [ADR 0047](0047-protocol-v2-one-event-contract.md) ·
[ADR 0049](0049-nothing-is-compatible-before-a-sale.md) · the plan page for F127.3 to F127.5
(https://claude.ai/artifact/TdTWru63EMWhCC7EiJjQk9) · F127.2 in the [work log](../work-log.md), whose plan held the
first draft

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
