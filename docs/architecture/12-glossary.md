# 12. Glossary

> **Reference.** The words these docs use in a sense of their own. **Elsewhere:** where each lives in
> [building blocks](05-building-block-view.md).

| Term | Meaning |
|---|---|
| **Engine** | `AgentEngine`: one conversation loop with its tools and system prompt; built per toolset list and shared by the sessions that use it |
| **Guide** | A toolset's markdown in `louis-agent.core/Prompts/toolsets/{toolset}.md`, in the system prompt only when the toolset is loaded |
| **Host** | A program that puts the engine in front of someone: the CLI, the ACP server (Rider), the HTTP API, the MCP server |
| **Ledger** | The usage ledger: one JSON line per model request, with its tokens and cost |
| **Peer** | Another louis-agent API this one may open sub-sessions on (`AGENT_PEERS`) |
| **Personality** | Who the agent is and how it talks, in its level's folder under `louis-agent.core/Prompts/`: Puls's voice (`agent/`), or a specialist's role (`specialists/`) |
| **Prefix** | The start of every request (tools, system prompt) that stays the same and is cached |
| **Puls** | The general assistant in Afrikaans, which delegates every task to a specialist |
| **Router** | The routing part of a mode (CL9): after its voice, in the same file, it sends a task to the toolset it needs; level 0 only |
| **Runbook** | The orchestrator's instructions for one API method of the service it watches |
| **Session** | One conversation with its own history, toolsets and personality, fixed when it opens |
| **Skill** | A markdown file of guidance, or a named procedure in one (`## Skill:`) that `ExecuteSkill` runs |
| **Specialist** | A sub-session with a role (researcher, reader, historian, analyst) that does one task and reports back |
| **Sub-session** | A session opened by another agent on a peer, one level deep (`StartSubSession`) |
| **Toolset** | A named group of tools with its guide (`files`, `git`, `web`, …), listed in the catalogue |
| **Turn** | One user message and everything the agent does to answer it, across its tool rounds |
