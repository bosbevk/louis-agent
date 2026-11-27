namespace LouisAgent.Core.Llm.Failures;

/// <summary>What a model's failure means for the request that met it (F8).</summary>
public enum ModelFailureKind
{
    /// <summary>A busy moment: asking again after a short wait may well work.</summary>
    Transient,

    /// <summary>The account's limit is reached: asking again fails until it lifts, so the work stops.</summary>
    AccountLimit,

    /// <summary>Anything else: not asked again, and passed on as it is.</summary>
    Other,
}
