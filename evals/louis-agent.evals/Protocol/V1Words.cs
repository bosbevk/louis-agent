using LouisAgent.Evals.Protocol.Kinds;

namespace LouisAgent.Evals.Protocol;

/// <summary>
/// The version 1 words the harness still needs while the rest of the stack catches up: a grader's kind as the API's
/// listing reader expects it, and a turn's failure as a stored v1 line spells it. The API reads the contract's words
/// in step 7c.3, and the harness's readers read v2 in step 7c.2e; this class goes then.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public static class V1Words
{
    /// <summary>The old spellings of a turn's failure, so a v1 line still reads.</summary>
    public static readonly IReadOnlyDictionary<string, TurnFailure> FailureAliases =
        new Dictionary<string, TurnFailure>
        {
            ["api_error"] = TurnFailure.ApiError,
            ["replay_miss"] = TurnFailure.ReplayMiss,
        };

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
