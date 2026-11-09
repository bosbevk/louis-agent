# ADR 0114: An identity's skills load always or on demand

**Status:** Accepted (2026-11-10, F129)

## Context

The guide's voice (`agent/louis-agent-guide/voice.md`) had grown to 296 lines: who Louis is and how he answers, and
beside it the map of the docs and of the site's pages, sent with every message whether a visitor asked about them or
not. Every identity had the same shape: one voice file, all of it in every prompt. Louis picked, of three ways to
split it (all always, all on demand, or both), **both**, with the skills beside the voice in
`prompts/agent/{identity}/skills/`, and asked whether loading on demand is the usual practice. It is:

- **Progressive disclosure** is how Anthropic's Agent Skills work: a skill's name and description are always in the
  prompt, short enough to say when it applies, and its body is read only when a task needs it
  ([Equipping agents for the real world with Agent Skills](https://www.anthropic.com/engineering/equipping-agents-for-the-real-world-with-agent-skills)).
- **Context is a budget**, and the guide on context engineering recommends a hybrid: what every turn needs up front,
  the rest fetched just in time through a tool, as Claude Code keeps `CLAUDE.md` up front and reads other files when
  it needs them ([Effective context engineering for AI agents](https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents)).
- In code it is Fowler's **Lazy Load** (PoEAA), which `PromptCatalog` already uses for the files themselves.

## Decision

- **An identity's skills are files in `prompts/agent/{identity}/skills/`**, each with the front matter every prompt
  has (ADR 0063) and `metadata.load`: `always` or `on-demand`, parsed once into the `SkillLoad` enum.
- **An always skill goes in the prompt after the voice**, before the toolset guides: what the agent needs on every
  turn (who it is, how it answers).
- **An on-demand skill is listed by name and description**, and the model reads its body with `ReadSkill`, an engine
  tool registered for every session like `GetToday`: reference material a question needs now and then (a docs map, a
  page map, a price list).
- **The description says when to read it.** It is the only part the model sees until it asks, so a vague one means the
  skill is never read.

## Consequences

- The prompt every message carries shrinks by what the on-demand skills hold; a question that needs one costs one more
  tool call and its round trip.
- Whether the model reads a skill when it should is measured, not assumed: F129's live check asks the same questions
  before and after the split, and an eval can hold it later.
- A new identity starts with its voice alone and splits off a skill when the voice grows; the voice stays the mode
  (`RoleCatalog` reads only `agent/{identity}/*.md` as modes, never `skills/`).
- The skills stay plain markdown with the Agent Skills spec's header, so an identity's skills can be handed to another
  agent that reads that format (ADR 0028).

## Links

- [ADR 0017: skill files follow the levels](0017-skills-by-level.md)
- [ADR 0018: the built-in prompts are embedded](0018-built-in-prompts-embedded.md)
- [ADR 0028: packs stay convertible to MCP](0028-packs-stay-convertible-to-mcp.md)
- [ADR 0063: a prompt says what it is](0063-a-prompt-says-what-it-is.md)
- [ADR 0080: a tool when it improves the prompt](0080-a-tool-when-it-improves-the-prompt.md)
- F129 in the work log
