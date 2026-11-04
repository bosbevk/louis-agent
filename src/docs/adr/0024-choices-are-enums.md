# 0024. Choices are enums, not strings

**Status:** Accepted · **Date:** 2026-10-17 (decided in F32 step 1b) · **Recorded:** 2026-10-17

## Context

Settings and tool arguments often name one of a fixed set: a search provider (`WEB_SEARCH_PROVIDER`), a page renderer,
a search's freshness, a thinking level. The code compared strings in each place it needed the choice
(`"tavily" =>`, `recency[..1]`), lower-casing them again each time, with the list of supported values written out by
hand beside it. A new value meant finding every string, a typo compiled, and the "supported" message could drift from
what the code accepted (it listed three providers after F32 step 1 added a fourth, and a test caught it). Louis, seeing
`WebSearchProviderFactory`'s string switch: "can we make this enums … This must also be a rule".

Considered: constants for the strings (one place for each name, but still a string everywhere it travels, and nothing
makes a switch complete); a smart-enum class per choice (more code than the choices need).

## Decision

A value that names one of a fixed set is an **enum**.

- It's **parsed once**, where the value enters: the factory that reads a setting, or the tool that receives the model's
  argument. Everything past that point holds and switches on the enum, never the text.
- **`Config/SettingChoice`** is the one parser: the member's name with the case ignored, or an alias (`ddg`); never a
  number; unset is no choice, so the caller keeps its default; anything else is unknown.
- The names a "supported" message lists come from the enum (`SettingChoice.Names<T>()`), in its order.
- An unknown value never stops start-up when the setting was optional before: the first use says what's supported, as
  the `Unavailable…` stand-ins do.
- A provider's own words for a choice (Tavily's `time_range=month`, Google's `dateRestrict=m1`) are written by that
  provider from the enum.

## Consequences

- A new choice is a new enum member; the compiler points at each switch that must handle it.
- The text a person or the model writes is still text up to the point it's parsed: environment variables, tool
  arguments and JSON keep their strings, so the golden files don't change.
- Choices still made by string are swept as they're touched (a TODO line lists them): the LLM provider, the rates
  format, a skill's script language, the MCP server's transport, the .NET package-list kind.

## Links

[F32 plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/F32-self-hosted-web.md) (*Approved during the build*) · [ADR 0009](0009-message-text-as-enum-templates.md)
(message text as enum members, the same idea for texts) · [ADR 0043](0043-repeated-names-are-enums.md) (extends it: a name that repeats is an enum too, even in an open set)
