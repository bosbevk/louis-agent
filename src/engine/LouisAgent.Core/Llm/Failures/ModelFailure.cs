namespace LouisAgent.Core.Llm.Failures;

/// <summary>
/// A model's failure, as its provider names it (F8): its kind, and for an account limit the provider's own reason and,
/// when it says, when access comes back.
/// </summary>
/// <param name="Kind">What the failure means for the request.</param>
/// <param name="Reason">The provider's words for an account limit; none for any other kind.</param>
/// <param name="ResumesAt">When an account limit lifts, when the provider says; none otherwise.</param>
public sealed record ModelFailure(ModelFailureKind Kind, string? Reason = null, DateTimeOffset? ResumesAt = null)
{
    /// <summary>A failure that isn't worth asking again and isn't an account limit.</summary>
    public static ModelFailure Other { get; } = new(ModelFailureKind.Other);

    /// <summary>A busy moment, worth asking again.</summary>
    public static ModelFailure Transient { get; } = new(ModelFailureKind.Transient);

    /// <summary>An account limit, with the provider's reason and when it lifts.</summary>
    public static ModelFailure AccountLimit(string reason, DateTimeOffset? resumesAt) =>
        new(ModelFailureKind.AccountLimit, reason, resumesAt);
}
