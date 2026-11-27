namespace LouisAgent.Evals.Runs.Stops;

/// <summary>
/// The agent's model reached its account's limit (F8): asking anything more fails until it lifts, so it stops the run
/// as a replay miss does, instead of failing every conversation after it.
/// </summary>
/// <param name="reason">What the API said, the time it lifts among it when it knows.</param>
public sealed class AccountLimitReachedException(string reason) : Exception(reason);
