using LouisAgent.Choices;

namespace LouisAgent.Evals.Runner.Promptfoo;

/// <summary>
/// The promptfoo assertion types our checks have a counterpart for (F49.5, finding T): the mapping table's rows, by
/// promptfoo's own names. Any other type has none, and is named as left out rather than dropped.
/// </summary>
/// <remarks>
/// An enum, as ADR 0043 says of a name that repeats: promptfoo's names are wire names, parsed once with
/// <see cref="SettingChoice"/>; the import and the export switch on it, so the table is in one place each way.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public enum PromptfooAssertion
{
    [WireName("contains")] Contains,
    [WireName("icontains")] IContains,
    [WireName("not-contains")] NotContains,
    [WireName("not-icontains")] NotIContains,
    [WireName("starts-with")] StartsWith,
    [WireName("is-json")] IsJson,
    [WireName("levenshtein")] Levenshtein,
    [WireName("latency")] Latency,
    [WireName("cost")] Cost,
    [WireName("llm-rubric")] LlmRubric,
}

/// <summary>One line <c>--import-promptfoo</c> prints: an assert it left out, and why.</summary>
public sealed record LeftOutLine(string LeftOut);
