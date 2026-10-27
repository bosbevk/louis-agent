namespace LouisAgent.Api.Contracts.Info;

/// <summary>The probe's answer: the instance is up.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed record HealthReply(string Status);

/// <summary>The model this instance answers with, and its provider (Puls's model picker shows it).</summary>
public sealed record ModelReply(string Provider, string Model);

/// <summary>
/// The roles a session can choose, per level; <c>default</c> is the voice a level-0 session gets when it names none.
/// </summary>
public sealed record RolesReply(
    string Default,
    IReadOnlyList<string> Agent,
    IReadOnlyList<string> Specialists,
    IReadOnlyList<string> Minions);

/// <summary>The LLM profiles a session can name besides this instance's model; no default means its own.</summary>
public sealed record LlmProfilesReply(string? Default, IReadOnlyList<string> Profiles);

/// <summary>The toolsets a session gets when it names none, and every toolset it could load.</summary>
public sealed record ToolsetsReply(IReadOnlyList<string> Default, IReadOnlyList<ToolsetRow> Toolsets);

/// <summary>
/// One toolset: its name, what it's for, and whether this instance can load it, with the settings it lacks if not.
/// Never the tool definitions, so a general agent can choose without loading them.
/// </summary>
public sealed record ToolsetRow(string Name, string Description, bool Available, IReadOnlyList<string> MissingSettings);

/// <summary>
/// What the web app needs to show costs in another currency: the display currency and its symbol, the base it converts
/// from, the rate's date, and a symbol for every currency it knows a rate for.
/// </summary>
public sealed record DisplayReply(
    string DisplayCurrency,
    string DisplaySymbol,
    string Base,
    string? RateAsOf,
    IReadOnlyDictionary<string, string> Symbols);
