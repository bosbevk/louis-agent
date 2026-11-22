using System.ComponentModel;

namespace LouisAgent.Evals.Files;

/// <summary>
/// One turn: what the person asks, the facts code checks about it, and the criteria the judge marks. Either list may
/// be left out, since a follow-up such as "Next." often has only one of them.
/// </summary>
/// <param name="Ask">What the person asks.</param>
public sealed record Turn(
    [property: Description("What the person asks in this turn.")]
    string Ask)
{
    /// <summary>What code checks about the turn, each as its words: <c>used ReadDoc</c>, <c>under 30s</c>.</summary>
    [Description("What code checks about the turn: no-error, used {tool}, not-used {tool}, under {n}s, " +
                 "never-says {text}, routed-to {address} or continued {address}.")]
    public IReadOnlyList<Fact> Facts { get; init; } = [];

    /// <summary>What the judge marks about the answer, each a plain sentence.</summary>
    [Description("What the judge marks about the answer, each a plain sentence.")]
    public IReadOnlyList<string> Criteria { get; init; } = [];
}
