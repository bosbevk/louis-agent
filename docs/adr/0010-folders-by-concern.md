# 0010. Folders by concern, the namespace is the folder path, and a test holds it

**Status:** Accepted · **Date:** 2026-10-11 · **Recorded:** 2026-10-14

## Context

Core mixed its concerns: the agent loop lived in `tools/`, the LLM factory beside the skill providers, usage and pricing
in one folder. Namespaces were lowercase (`louis_agent.core.tools`), and 15 files held more than one type. A change to
one concern touched several folders, and nothing stopped a tool from depending on the agent loop.

## Decision

Core's folders split the code by concern (`Agent`, `Llm/{Name}Provider`, `Tools/{Toolset}`, `Skills`, `Guardrails`,
`Usage`, `Pricing`, `Protocols/Mcp`, `Hosting`, `Config`, `Messages`, `Logging`). The namespace is the folder path in
PascalCase, in every project; one top-level type per file. Dependencies point one way: hosts → `Hosting` → `Agent` →
`Llm`, `Tools`, `Skills`, `Usage`, `Pricing`, `Protocols` → `Guardrails` → `Config`, `Messages`, `Logging`.
`ArchitectureTests` reads core's source and fails on a namespace off its folder, two types in a file, an unknown folder
or a dependency against the arrow.

## Consequences

- A change to one concern (a provider, a tool, a guard) touches one folder.
- The rules can't drift: a new concern is a new row in the layout table and in the test's layers.
- No `Helpers/`, `Utils/` or `Interfaces/` folders: an interface lives beside its implementations.
- Moved with `git mv`, so history follows; the behaviour and the 779 tests were unchanged.

## Links

[CL1 plan](../plans/CL1-project-layout.md) · [Project layout](../../louis-agent.core/CLAUDE.md#project-layout)
