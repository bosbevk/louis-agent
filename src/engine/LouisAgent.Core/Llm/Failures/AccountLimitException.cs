namespace LouisAgent.Core.Llm.Failures;

/// <summary>
/// The model's account has reached its limit (F8): asking again fails until it lifts, so a chat says so and an eval run
/// stops. It carries the provider's own reason and, when the provider says, when access comes back.
/// </summary>
/// <param name="limit">The account limit, as the provider named it.</param>
/// <param name="provider">The provider's own failure, kept inside for the log; none when there wasn't one.</param>
public sealed class AccountLimitException(ModelFailure limit, Exception? provider = null)
    : Exception(limit.Reason, provider)
{
    /// <summary>When access comes back, when the provider says; none otherwise.</summary>
    public DateTimeOffset? ResumesAt { get; } = limit.ResumesAt;
}
