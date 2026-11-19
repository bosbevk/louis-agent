# Change the engine

> **How-to.** Change `louis-agent.core`, the engine every agent runs on: add a tool or a toolset, change a built-in
> prompt, add a setting or a message, handle a provider difference, add a model, and test it. **Elsewhere:** how it
> works in [louis-agent.core](../projects/louis-agent.core.md); the rules these steps follow in
> [core's development guide](../../engine/louis-agent.core/CLAUDE.md); a capability of its own in [Build a pack](build-a-pack.md).

![The engine at a glance: louis-agent.core holds the tool loop, prompts, settings, providers and guards, and the golden files pin what the model reads](diagrams/change-the-engine.svg)

Core is the engine. It holds the tool loop (`Agent/`), asking other agents (`Delegation/`), skills and prompts
(`Skills/`, `prompts/`), the model providers' seam (`Llm/`), usage and prices (`Usage/`, `Pricing/`), the guards
(`Guardrails/`) and the start-up every host shares (`Hosting/`). It names no product, shop or country. Who an agent is
lives in an identity, and what it can do lives in a pack. `ArchitectureTests` holds that, in the code and in the
comments.

That gives you the first question to ask: core or a pack? Every tool the model asks for is a pack's, and core ships
none of its own. Core only keeps the two toolsets that need the engine's insides, `agents` and `skills`. So a new
capability, whether a service, a shop or a whole domain, is a pack.

*Decided in:* [ADR 0027](../adr/0027-engine-identities-and-packs.md), [ADR 0076](../adr/0076-core-ships-no-agent.md).

## Add a tool

A new tool goes in a pack, and the steps are in [Build a pack](build-a-pack.md). A few things hold for any tool class,
wherever it lives.

Every public method on a tool class becomes a tool, so helpers stay `private` or `internal`. A tool's name must be
unique across every toolset, because a duplicate stops the start-up. It returns text the model can act on: what went
wrong, and what to do instead. Every one of those texts is a member of its area's `{Area}ToolMessage`, and a text two
areas return stays in the shared `ToolMessage` in `louis-agent.composition`. Keep the output bounded, too. A result over
50,000 characters gets summarised, and most toolsets cap their own at 12,000.

If the tool touches the outside world, it goes through the guards. A path goes through `WorkspaceBoundary` and
`SensitiveFiles`, a URL through `SsrfGuard`, and a process through `ProcessRunner` with a list of arguments, never a
command line built from the model's text. Then name the tool in its toolset's guide, so the model knows when to use
it, and regenerate the golden files and read their diff (below).

*Decided in:* [ADR 0004](../adr/0004-arguments-as-data.md), [ADR 0009](../adr/0009-message-text-as-enum-templates.md).

## Add a toolset

Every toolset follows one pattern: guide, then tools, then guide. A mode's routing sends a task to the toolset, the
toolset's guide names its tools and when to use them, and each tool's description stays short. A pack's toolset is a
`ToolsetDefinition` the pack declares (its name, description, the settings it needs, its tool classes and its guide
file), with the guide in the pack's `prompts/toolsets/` and a routing row in each mode that uses it.
[Build a pack](build-a-pack.md) has the steps, and [Adding a tool](../../engine/louis-agent.core/CLAUDE.md#adding-a-tool)
has the rules.

Core's own two are in `Agent/Toolsets/ToolsetCatalog.cs`. `agents` lives in `Delegation/`, and `skills` in
`Agent/EngineTools/`, registered by `AgentEngine` because it needs the engine's registry. Only something that needs
the engine's own insides belongs there. A session picks its toolsets when it opens and keeps them, so its prompt stays
cacheable.

*Decided in:* [ADR 0006](../adr/0006-toolsets-per-session.md).

## Change a built-in prompt

Core's own prompts are markdown in `prompts/`: `shared/principles.md`, which opens every prompt, and the guides of
core's two toolsets in `toolsets/` (`agents.md` and `self-extension.md`). A product's modes and roles live in its
identity, and a pack's guides in its pack. All of them are embedded, so changing a word means rebuilding every image
that uses them. A running chat keeps the prompt it opened with, so start a new chat to see the change.

*Decided in:* [ADR 0017](../adr/0017-skills-by-level.md), [ADR 0018](../adr/0018-built-in-prompts-embedded.md).

## Add a setting

![Adding a setting: a test first, a property filled in FromEnvironment, an enum when it is a choice, then the demo's .env.example](diagrams/change-the-engine-flow.svg)

A setting is read once, where it enters, and everything after that asks the options class for it.

1. Read it once where it enters: a property in `Config/AgentOptions.cs` (or `LlmOptions` for an `LLM_*` setting),
   filled in `FromEnvironment`, with a test in `src/tests/engine/louis-agent.core.tests/Config/`. `AgentOptions.cs` is
   already long, so a group of new settings is the moment to give them a class of their own, as a pack has
   (`RecipesOptions`). A pack's settings are always in the pack's own options class.
2. A choice from a fixed set is an enum, parsed with `SettingChoice` (case ignored, aliases allowed, never a number;
   [ADR 0024](../adr/0024-choices-are-enums.md)).
3. Document it in the demo's `config/.env.example` (for the coding agent, `demos/louis-agent/config/.env.example`). If a container needs it, add it to the compose file's service and to the
   host's `Properties/launchSettings.json` profile, so a debug run sees what the container sees.

## Add a message

Pick the enum by what the text is and which area says it: `{Area}ErrorMessage`, `{Area}LogMessage` or
`{Area}ToolMessage`, in that area's own `Messages/` folder (for settings, `Config/Messages/`). A text two areas use goes
in a shared one: core's `Messages/` holds `ErrorMessage` and `UserText`, and `louis-agent.composition` holds
`ToolMessage`. Name the member for what happened, not for its wording. Its `[Description]` is the text, with named
holes. The holes are a record named after the member, `{Member}Holes`, in a `Holes/` folder beside the enum, and the
member names it with `[Holes(typeof(…))]`. Build the record as a named local, then hand it over:
`var owedTool = new EndWithToolHoles(owed);` and `MessageText.Format(AgentToolMessage.EndWithTool, owedTool)`. A host's
own texts live in that host's `Messages/` folder.

Log through `ILogger` (`AgentLogging.For<T>()`), which writes to stderr only, because stdout carries the ACP and MCP
protocols. The rules are in [Messages and logging](../../engine/louis-agent.core/CLAUDE.md#messages-and-logging).

## Handle a provider difference

Sometimes Anthropic, OpenAI-compatible servers and Ollama differ on the same job: usage, caching, thinking, limits.
Then write a strategy: an interface in core for that job, one implementation for each provider that differs, and a
default. The provider `Llm/LlmClientFactory.cs` finds is where the choice is made, once. A provider's own classes live
in its own library on core, as `AnthropicUsageMapper` and `AnthropicPromptCaching` do in `louis-agent.core.anthropic`.
Never check the provider's name anywhere else. For usage, the steps are in
[Change the usage probe](change-the-usage-probe.md#fix-a-usage-mapper).

*Decided in:* [ADR 0003](../adr/0003-provider-strategies.md), [ADR 0092](../adr/0092-a-project-holds-what-is-reused-together.md).

## Add a model or a price

A model is configuration. `LLM_PROFILE` picks a profile file in the demo's `config/` folder, `.env.{LLM_PROFILE}`, such
as `.env.anthropic` or `.env.ollama`, and [MODELS.md](../MODELS.md) lists the models. A new model needs its five
prices in the demo's `config/prices.json`, copied from the
provider's page, with the table's `as_of` set to that day. Without them its cost is recorded as unknown, never 0. Run
the [usage probe](change-the-usage-probe.md) before you trust a new provider's numbers.

*Decided in:* [ADR 0007](../adr/0007-measure-every-request.md).

## Test it

```bash
dotnet test src/tests/engine/louis-agent.core.tests
LOUIS_UPDATE_GOLDEN=1 dotnet test demos/louis-agent/tests/louis-agent.coding-catalog.tests --filter "ModelRequestSnapshotTests|ToolMessageSnapshotTests"
```

Two golden files pin what the model reads, and both live with the coding agent's catalogue, in
`demos/louis-agent/tests/louis-agent.coding-catalog.tests/Golden/`.
`ModelRequestSnapshot.json` holds the tools, their descriptions, the system prompt and the engine's fixed texts, and
`ToolMessageSnapshot.json` holds every text a tool returns. A new tool, text or prompt changes them on purpose:
regenerate them and read the diff before you commit. The PowerShell form, and the rest of the rules for tests, are in
[Write and run tests](write-tests.md).

## Before you ask for a review

- [ ] Every new public method on a tool class is meant to be a tool, with its test and its line in the guide.
- [ ] Paths, URLs and processes go through the guards; arguments are data.
- [ ] Every text is a message enum member; the golden diffs are exactly the change you meant.
- [ ] Nothing in core names a product, a shop or a country.

---
[Guides](README.md) · [Docs index](../README.md)
