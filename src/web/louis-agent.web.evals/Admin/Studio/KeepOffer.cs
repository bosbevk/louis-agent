namespace LouisAgent.Web.Evals.Admin.Studio;

/// <summary>What a graded conversation can be kept as: nothing, a reference, or a known-bad.</summary>
public enum KeepOffer
{
    None,
    Reference,
    KnownBad,
}

/// <summary>
/// After a person grades a conversation, says what the panel offers to keep it as: a pass a reference, a fail a
/// known-bad, an unsure (or no grade yet) nothing. The API holds the same rule over every person's verdicts.
/// </summary>
public static class KeepOffers
{
    /// <summary>What this verdict offers.</summary>
    /// <remarks>A switch expression on the verdict's word, one arm per case (ADR 0051).</remarks>
    public static KeepOffer For(string? verdict) => verdict switch
    {
        "pass" => KeepOffer.Reference,
        "fail" => KeepOffer.KnownBad,
        _ => KeepOffer.None,
    };
}
