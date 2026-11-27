# 12. Glossary

> **Reference.** The words these docs use in a sense of their own. **Elsewhere:** where each lives in
> [building blocks](05-building-block-view.md).

![The glossary: who an agent is, what it can do, how a conversation runs, and how agents ask each other](diagrams/12-glossary.svg)

The words come in alphabetical order, so you can look one up. Each says what the word means here, and where to find
it in the code when that helps.

| Term | Meaning |
|---|---|
| **Address** | Who an agent call goes to (ADR 0117): `{identity}` for an identity in its own voice (`seemeeu-guesthouse`), `{identity}.{role}` for one of its roles (`puls.cook`); `AskAgent` takes it |
| **Calls** | The addresses an identity may ask, in its code (`IAgentIdentity.Calls`); any other is refused, and its prompt lists the same ones |
| **Engine** | `AgentEngine`: one conversation loop with its tools and system prompt; built per toolset list and shared by the sessions that use it |
| **Guide** | A toolset's markdown, `toolsets/{toolset}.md` in the pack that brings it (or in core, for core's own two), in the system prompt only when the toolset is loaded |
| **Host** | A program that puts the engine in front of someone: the CLI, the ACP server (Rider), the HTTP API, the MCP server |
| **Identity** | Who an agent is (ADR 0027): its name, its modes and roles, its own skills and whom it may ask (`IAgentIdentity`); in a program built on host-packs, `IDENTITIES` names the ones it loads |
| **Language** | A mode in one language (`puls/voice-af`, `puls/voice-en`): only the voice; its product's shared `voice.md` (the routing) loads after it (CL9) |
| **Ledger** | The usage ledger: one JSON line per model request, with its tokens and cost |
| **Lookup agent** | A level-2 session a subagent hands one task to, with one of the asker's tools, inside its agent's `lookups/`; asked by its address (`puls.find-recipe`), at most 16 a user turn, and never kept (ADR 0021) |
| **Mode** | What a level-0 session names (CL9, ADR 0019): one file, its voice then its routing, `agents/{product}/{mode}.md`; the default is `louis-agent/voice` |
| **Peer** | Another louis-agent API this one may ask agents on (`AGENT_PEERS`), itself included (`self`); an address names one before a colon (`partner:shop`) and each is called with its own key (`AGENT_PEER_KEYS`) |
| **Pack** | What an agent can do (ADR 0027): one capability's toolsets, their guides, its settings and state (`IAgentPack`); in a program built on host-packs, `PACKS` names the ones it loads; and a pack stays convertible to an MCP server |
| **Personality** | A role's old name; `PERSONALITIES_ENABLED` is still read, with a warning, when `ROLES_ENABLED` is unset |
| **Principles** | `shared/principles.md`: never lie, cheat, steal or murder; first in every agent's prompt (ADR 0020) |
| **Prefix** | The start of every request (tools, system prompt) that stays the same and is cached |
| **Puls** | The general assistant in Afrikaans or English, which answers the web itself and asks its subagents, such as its cook (`puls.cook`) for cooking |
| **Role** | What a session is, from the prompt folders at the root of an identity's project: a mode at level 0 (`agents/{identity}/`), a subagent at level 1 (`agents/{identity}/subagents/{name}/voice.md`, named by its address), a lookup agent at level 2 (`agents/{identity}/lookups/{name}/voice.md`, also named by its address); `RoleCatalog` in the code, `role` on the API |
| **Router** | The routing part of a mode (CL9): after its voice, in the same file, it sends a task to the toolset it needs; level 0 only |
| **Runbook** | The orchestrator's instructions for one API method of the service it watches |
| **Session** | One conversation with its own history, toolsets and role, fixed when it opens |
| **Sequence** | A fixed order of agents an identity declares from its own `Calls` (`IAgentIdentity.Sequences`, F176, ADR 0117): asking its first address with `AskAgent` runs every step in order, in code; it has no address of its own |
| **Skill** | A folder with a `SKILL.md`, in the Agent Skills shape: a built-in one is guidance a toolset or an identity brings; one the agent made also holds its parameters and one script, which `UseSkill` reads or runs |
| **Draft** | A skill the agent made with `CreateSkill`, waiting in `skills/drafts/`: only the chat that made it can use it, until `/approve` gives it to every chat or `/reject` deletes it |
| **Subagent** | A sub-session with its own voice inside its agent's folder, that does one task and reports back; one name everywhere, its address `{identity}.{name}` (`triage.sorter`), ADR 0133 |
| **Sub-session** | A session opened by another agent on a peer, one level below its asker (`AskAgent`, a lookup agent with one of the asker's tools), at most `AGENT_MAX_DEPTH` deep |
| **Toolset** | A named group of tools with its guide (`files`, `git`, `web`, …), listed in the catalogue |
| **Turn** | One user message and everything the agent does to answer it, across its tool rounds |
