using LouisAgent.Evals.Protocol.Kinds;

namespace LouisAgent.Evals.Protocol;

/// <summary>
/// The one version 1 word the harness still writes: a grader's kind, as the API's listing reader expects it. The API
/// reads the contract's words in step 7c.3, and this class goes then.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public static class V1Words
{
    /// <summary>A grader's kind as version 1 writes it: the member's name.</summary>
    /// <remarks>A switch expression on the kind, one arm per case, read like a table.</remarks>
    public static string Kind(GraderKind kind) => kind switch
    {
        GraderKind.FixedRule => "FixedRule",
        GraderKind.Model => "Model",
        GraderKind.Human => "Human",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}
