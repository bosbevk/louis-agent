# Change the engine

> **How-to.** Change `louis-agent.core`, the engine every agent runs on: add a tool or a toolset, change a built-in
> prompt, add a setting or a message, handle a provider difference, add a model, and test it. **Elsewhere:** how it
> works in [louis-agent.core](../projects/louis-agent.core.md); the rules these steps follow in
> [core's development guide](../../louis-agent.core/CLAUDE.md); a capability of its own in [Build a pack](build-a-pack.md).

Core is the engine: the tool loop (`Agent/`), the toolsets and their guides (`Tools/`, `Prompts/`), skills, the model
providers (`Llm/`), usage and prices (`Usage/`, `Pricing/`), the guards (`Guardrails/`) and the start-up every host
shares (`Hosting/`). It names no product, shop or country: who an agent is lives in an identity, and what it can do
beyond the basics lives in a pack ([ADR 0027](../adr/0027-engine-identities-and-packs.md)). `ArchitectureTests` holds
that in the code and the comments.

**Core or a pack?** A new capability (a service, a shop, a domain) is a pack. Core's own toolsets are moving into packs
too (CL17), so add to core only what every agent needs.

## Add a tool to a toolset

1. **Test first** in `tests/louis-agent.core.tests/Tools/{Toolset}/`, with `FakeChatClient` or `StubHandler` for
   anything it would call. No network.
2. Add a public method with a `[Description]` on it and on each parameter to the toolset's class in
   `Tools/{Toolset}/`. **Every public method on a tool class is a tool**, so helpers stay `private` or `internal`.
   Tool names are unique across every toolset; a duplicate stops start-up.
3. Return text the model can act on: what went wrong and what to do instead. Every text is a `ToolMessage` member
   ([ADR 0009](../adr/0009-message-text-as-enum-templates.md)). Keep the output bounded: results over 50,000
   characters are summarised, and most toolsets cap their own at 12,000.
4. If it touches the outside world, use the guards: a path through `WorkspaceBoundary` and `SensitiveFiles`, a URL
   through `SsrfGuard`, and a process through `ProcessRunner` with an argument list, never a command line built from
   model text ([ADR 0004](../adr/0004-arguments-as-data.md)).
5. Name it in the toolset's guide, `Prompts/toolsets/{toolset}.md`, so the model knows when to use it.
6. Regenerate the golden files and read their diff (below).

## Add a toolset

A toolset is guide, then tools, then guide: a mode's routing sends a task to the toolset, its guide names its tools,
and each tool's description stays short. You add a tool class, a `ToolsetDefinition` in
`Agent/Toolsets/ToolsetCatalog.cs` (its name, description, needed settings, tool classes and guide file), the guide
in `Prompts/toolsets/`, and a routing row in each mode that uses it. The full list, and what is reserved, is in
[Adding a toolset](../../louis-agent.core/CLAUDE.md#adding-a-toolset). A session picks its toolsets when it opens and
keeps them, so its prompt stays cacheable ([ADR 0006](../adr/0006-toolsets-per-session.md)).

## Change a built-in prompt

Built-in prompts are markdown in `Prompts/`, by the level that reads them: `agent/` (a mode: voice, then routing),
`toolsets/` (a toolset's guide) and `shared/` (`principles.md`, which opens every prompt)
([ADR 0017](../adr/0017-skills-by-level.md)). They are embedded, so a wording change is a rebuild of every image that
uses core. A running chat keeps the prompt it opened with: start a new chat to see the change. A product's own modes
and roles live in its identity, not here.

## Add a setting

1. Read it once where it enters: a property in `Config/AgentOptions.cs` (or `LlmOptions` for an `LLM_*` setting),
   filled in `FromEnvironment`, with a test in `tests/louis-agent.core.tests/Config/`. `AgentOptions.cs` is over the
   200-line limit and on `FileSizeTests`' list, so a group of new settings is the moment to give them a class of their
   own, as a pack has (`ShopsOptions`).
2. A choice from a fixed set is an enum, parsed with `SettingChoice` (case ignored, aliases allowed, never a number;
   [ADR 0024](../adr/0024-choices-are-enums.md)).
3. Document it in `config/.env.example`. If a container needs it, add it to the compose file's service and to the
   host's `Properties/launchSettings.json` profile, so a debug run sees what the container sees.

## Add a message

Pick the enum by kind (`ErrorMessage`, `LogMessage`, `ToolMessage`, `UserText`) and name the member for what happened,
not for its wording; its `[Description]` is the text, with named holes:
`MessageText.Format(ErrorMessage.UnknownCacheTtl, new { Value = other })`. A host's own texts live in that host's
`Messages/` folder. Log through `ILogger` (`AgentLogging.For<T>()`): stderr only, since stdout carries the ACP and MCP
protocols. The rules are in [Messages and logging](../../louis-agent.core/CLAUDE.md#messages-and-logging).

## Handle a provider difference

Where Anthropic, OpenAI-compatible servers and Ollama differ (usage, caching, thinking, limits), write a strategy: an
interface for the concern, one implementation per provider that differs, and a default, chosen once in
`Llm/LlmClientFactory.cs`. A provider's own classes live in `Llm/Providers/{Name}Provider/`, as
`AnthropicUsageMapper` and `AnthropicPromptCaching` do. Never check the provider's name anywhere else
([ADR 0003](../adr/0003-provider-strategies.md)). For usage, the steps are in
[Change the usage probe](change-the-usage-probe.md#fix-a-usage-mapper).

## Add a model or a price

A model is configuration: `LLM_PROFILE` picks `config/.env.anthropic` or `config/.env.ollama`, and
[MODELS.md](../MODELS.md) lists the models. A new model needs its five prices in `config/prices.json`, copied from the
provider's page with the table's `as_of` set to that day, or its cost is recorded as unknown, never 0
([ADR 0007](../adr/0007-measure-every-request.md)). Run the [usage probe](change-the-usage-probe.md) before trusting a
new provider's numbers.

## Test it

```bash
dotnet test tests/louis-agent.core.tests
LOUIS_UPDATE_GOLDEN=1 dotnet test tests/louis-agent.core.tests --filter "ModelRequestSnapshotTests|ToolMessageSnapshotTests"
```

Two golden files pin what the model reads: `Agent/ModelRequestSnapshot.json` (the tools, their descriptions, the
system prompt, the engine's fixed texts) and `Messages/ToolMessageSnapshot.json` (every text a tool returns). A new
tool, text or prompt changes them on purpose: regenerate them and read the diff before committing. The PowerShell form,
and the rest of the test rules, are in [Write and run tests](write-tests.md).

## Before you ask for a review

- [ ] Every new public method on a tool class is meant to be a tool, with its test and its line in the guide.
- [ ] Paths, URLs and processes go through the guards; arguments are data.
- [ ] Every text is a message enum member; the golden diffs are exactly the change you meant.
- [ ] Nothing in core names a product, a shop or a country.

---
[Guides](README.md) · [Docs index](../README.md)
