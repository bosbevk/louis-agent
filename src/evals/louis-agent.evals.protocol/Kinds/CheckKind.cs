using LouisAgent.Choices;

namespace LouisAgent.Evals.Protocol.Kinds;

/// <summary>How a grader decides: a rule in code, a model, or a person.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public enum CheckKind
{
    [WireName("fixed-rule")] FixedRule,
    [WireName("model")] Model,
    [WireName("human")] Human,
}
