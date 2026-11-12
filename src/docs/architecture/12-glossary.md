# 12. Glossary

> **Reference.** The words these docs use in a sense of their own. **Elsewhere:** where each lives in
> [building blocks](05-building-block-view.md).

| Term | Meaning |
|---|---|
| **Address** | Who an agent call goes to (ADR 0117): `{identity}` for an identity in its own voice (`seemeeu-guesthouse`), `{identity}.{role}` for one of its roles (`puls.cook`); `AskAgent` takes it |
| **Calls** | The addresses an identity may ask, in its code (`IAgentIdentity.Calls`); any other is refused, and its prompt lists the same ones |
| **Engine** | `AgentEngine`: one conversation loop with its tools and system prompt; built per toolset list and shared by the sessions that use it |
| **Guide** | A toolset's markdown in `src/engine/louis-agent.core/prompts/toolsets/{toolset}.md`, in the system prompt only when the toolset is loaded |
| **Host** | A program that puts the engine in front of someone: the CLI, the ACP server (Rider), the HTTP API, the MCP server |
| **Language** | A mode in one language (`puls/voice-af`, `puls/voice-en`): only the voice; its product's shared `voice.md` (the routing) loads after it (CL9) |
| **Ledger** | The usage ledger: one JSON line per model request, with its tokens and cost |
| **Mode** | What a level-0 session names (CL9, ADR 0019): one file, its voice then its routing, `agent/{product}/{mode}.md`; the default is `louis-agent/voice` |
| **Peer** | Another louis-agent API this one may ask agents on (`AGENT_PEERS`), itself included (`self`); an address names one before a colon (`partner:shop`) and each is called with its own key (`AGENT_PEER_KEYS`) |
| **Personality** | A role's name before CL13 (the code) and CL14 (the API); still read in recorded runs and from `PERSONALITIES_ENABLED`; CL20 took it out of the API |
| **Principles** | `shared/principles.md`: never lie, cheat, steal or murder; first in every agent's prompt (ADR 0020) |
| **Prefix** | The start of every request (tools, system prompt) that stays the same and is cached |
| **Puls** | The general assistant in Afrikaans or English, which answers the web itself and asks its cook (`puls.cook`) for cooking |
| **Role** | What a session is, in its level's folder under `src/engine/louis-agent.core/prompts/`: a mode at level 0 (`agent/`), a specialist's role at level 1 (`specialists/`), a minion's function at level 2 (`minions/`); `RoleCatalog` in the code, `role` on the API |
| **Router** | The routing part of a mode (CL9): after its voice, in the same file, it sends a task to the toolset it needs; level 0 only |
| **Runbook** | The orchestrator's instructions for one API method of the service it watches |
| **Session** | One conversation with its own history, toolsets and role, fixed when it opens |
| **Skill** | A markdown file of guidance, or a named procedure in one (`## Skill:`) that `ExecuteSkill` runs |
| **Specialist** | A sub-session with a role (the cook; others off until they pass a check) that does one task and reports back; asked by its address, `{identity}.{role}` (`puls.cook`) |
| **Sub-session** | A session opened by another agent on a peer, one level below its asker (`AskAgent`, a minion with one of the asker's tools), at most `AGENT_MAX_DEPTH` deep |
| **Toolset** | A named group of tools with its guide (`files`, `git`, `web`, …), listed in the catalogue |
| **Turn** | One user message and everything the agent does to answer it, across its tool rounds |
