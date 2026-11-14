# ADR 0117: One address for every agent call

**Status:** Accepted (2026-11-10, Louis's picks during F127.2); `AskAgent` on one instance built in F127.3.1
(2026-11-11); an agent on another server (F127.3.2), the guide mentoring (F127.4) and the minions (F127.5) to come.
Amends [ADR 0079](0079-show-a-specialists-report.md) and [ADR 0021](0021-minions-one-task-one-tool.md) (F127.5).

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
- **A minion is on the same address and the same tool** (F127.5, below): `AskAgent` with one of the asker's tools,
  since a minion runs a local model on one task with one tool (ADR 0021, ADR 0081). The plan first kept
  `StartMinion` for it; Louis's correction put it on `AskAgent`.
- **The plumbing stays Puls's:** `PeerSession` opens, asks, keeps and deletes; the limits per turn, the timeout, the
  depth, the usage rolled up to the asker's turn, kept sessions, and the `sub_session` events that carry the callee's
  every event, live, to the browser.
- **The session wire is shared records,** in `louis-agent.session-stream` beside the one stream reader (ADR 0047):
  the requests and replies core, the API and the web send and read, never an anonymous object or a reply read by
  property name (ADR 0041, ADR 0045).
- **Mentoring uses the same call.** The site's guide lists the three showroom businesses in its `Calls`; asking one
  for a visitor is `AskAgent`, and what differs is what the visitor sees (the business's own chat on the demo) and that
  the open door counts it against that visitor (F127.4).

## What F127.3.1 settled (2026-11-11)

Louis's picks during the build, each a rule now:

- **A role's level comes from its file's folder,** not from who asks: a business asked at depth 1 opens in its own
  voice from `agent/`, with its own packs and no `agents` toolset, so it can't ask further.
- **The prompt lists the identity's `Calls`** after the agents guide, from the same list the code checks. The first
  live run showed why: told of no address, the guide said it had no line to the business.
- **One kept conversation per address per chat** (`AgentLine`): asking again carries on; a busy one (`409`) says
  wait and keeps it; a gone one (`404`) starts over in the same call, and the answer says so.
- **The line under each answer is built in code** from the asked agent's own tool calls (`DidLine`,
  `[Answered by acme, did: SendLead]`), so the asker says what was done from facts, never from the agent's words.
- **`ReadAgentWork(address)`** reads an asked agent's steps on demand (Lazy Load), without its thinking, the latest
  12,000 characters under a line that says how much was cut.
- **An identity says whether it relays answers whole** (`IAgentIdentity.RelaysAnswersWhole`), in place of
  `SHOW_SPECIALIST_REPORTS`: Puls does, the guide doesn't (this amends ADR 0079).
- **The triage team and Puls ask by address** (`TriageCall`, `PulsCall`); the evals read a turn's route as the
  address it asked first, and a follow-up as an address an earlier turn asked. The cook's file names `web`, which
  Puls's `StartSubSession` used to send.
- **Puls is paused** (2026-11-11), so its 109 recorded eval transcripts were rewritten onto `AskAgent` by one checked
  script instead of being recorded again, and the recipes eval before and after waits for Puls's return (F135).
- **`StartMinion` stayed as it was** until F127.5 put the minions on the address and on `AskAgent`.

## Settled with F127.3.2 (2026-11-12)

Asking an agent on another server (F127.3 pick 2, C), each a rule now, agreed by Louis
the same day:

- **An address may name its server before a colon** (`partner:shop.cook`); without one it is this instance, which
  is `self` when a host names more than one server. A server is part of the address, so `Calls` allow it exactly.
- **A key per server:** `AGENT_PEER_KEYS` (a secret, `name=key`), the server's own key on every request to it, this
  instance's `AGENT_API_KEY` when it has none; never one key for all.
- **A server's identities are read before an address on it is trusted**, with its key, and kept ten minutes; an
  unknown server, a refused key, a server that doesn't answer, and an identity it doesn't list are each a reason.
- **The person is told first:** the agents guide has the model say, before it asks, that the question goes to
  another company's server. The privacy terms gain their line when a site's agent first asks another server.

## What F127.5 settled (2026-11-12)

The minions on the address, and who may ask whom. Louis: "it should not startminion it should use askagent also".
Each a rule now:

- **One tool for every call:** `AskAgent(address, message, attachments, tool?, stronger?)`. A minion's address is its
  identity and its function (`puls.find-recipe`, the file `minions/puls-minion-find-recipe.md`); `tool` (one of the
  asker's own, never a delegation or skills tool) and `stronger` count only for it, and a tool sent anywhere else is
  refused (Louis's pick, of three: not a tool named in the minion's role file, nor a list of tools there).
  `StartMinion` and the `minions` toolset are gone; the agents guide says how to ask a minion.
- **A minion keeps its own limits:** its model from `MINION_LLMS`, at most 16 a user turn, 2 minutes, never kept,
  and it never takes one of the turn's three asks. It runs on this instance: an address with a server and a tool is
  refused (ADR 0081, a local model).
- **An identity's `Calls` list its minions** beside its agents; a mode's prompt names only the agents among them.
- **Everyone may call everyone their `Calls` allow; only a minion can't** (Louis: "eceryone should be able to call
  everyone, if it is setup that way only minions can't", and his pick "any address, depth decides"). Every session
  may load the `agents` toolset but a minion's. At the top a session is a voice; below it, its level is the folder
  its role is in, at any depth, so a voice may hand a minion one of its tools and a specialist may ask another.
- **No agent twice in a chain of asks** (Louis: "a cook can't call a cook. but it could call a recipe specialist").
  Each session keeps the chain it was asked through (a voice named by its identity, any other role by its file); an
  ask to an agent already in it, the asker included, is refused by `AskAgent` before anything is sent, and by the
  API. Another server keeps its own chain.
- **A depth cap, a setting:** `AGENT_MAX_DEPTH` (default 4), checked by `AskAgent` and the API (Louis, on loops: "1
  and 2 but make 2 a setting").

## What F127.4.2 settled (2026-11-12)

The guided live demo (F127.3 picks 7 and 8, F127.4 picks 1, 6 to 8), each a rule now:

- **The line under an answer tells each call's result**, its first line cut at 80 characters
  (`[Answered by acme, did: ShowPage: Shown: /rooms/garden; SendLead: Sent]`), not only the tools' names.
- **A session's work is served** (`GET /v1/sessions/{id}/work`): what it was asked, said, called and got back, never
  its thinking. `ReadAgentWork` reads the whole session from its server, so turns someone else had with it count;
  what streamed through the asker is the fallback.
- **An asked session belongs to the visitor whose session asked:** the open door counts it in that visitor's day and
  admits it for them, never against the server's own address.
- **A page may hand the talk to the asked agent's own window** (`AgentWindows` in the web): the window shows the ask
  in the visitor's words and the answer as it streams, then carries on in the same session; the asker's chat says
  so in one line.

## Consequences

- A new agent-to-agent call is a line in an identity's `Calls`, not a new tool. Any identity can ask another
  identity, or another identity's specialist, with the limits, the ledger and the live view it already has.
- The tool is narrower than `StartSubSession`: on a public site the model can't open a session with toolsets of its
  choosing.
- Puls moves onto `AskAgent`: its voice, the specialists guide, the web's delegation card and the evals' tool checks
  change, and the recipes eval runs before and after to prove nothing was lost. Puls's stored replay answers name the
  old tools and are recorded again (both wait for F135, since Puls is paused).
- A role file not named for its identity can't be addressed; a test holds the naming.
- We keep one address grammar, parsed in one place and tested once.

## Links

[ADR 0031](0031-orchestration-is-an-engine-function.md) (delegation is one function in the engine; this ADR amends
its tools) · [ADR 0006](0006-toolsets-per-session.md) · [ADR 0027](0027-engine-identities-and-packs.md) ·
[ADR 0021](0021-minions-one-task-one-tool.md) · [ADR 0047](0047-protocol-v2-one-event-contract.md) ·
[ADR 0079](0079-show-a-specialists-report.md) (amended: who relays an answer whole) ·
[ADR 0049](0049-nothing-is-compatible-before-a-sale.md) · the plan page for F127.3 to F127.5
(https://claude.ai/artifact/TdTWru63EMWhCC7EiJjQk9) · F127.2 in the [work log](../work-log.md), whose plan held the
first draft

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
