using LouisAgent.Choices;

namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>
/// Why a turn failed for the infrastructure, not the agent (F18 step 5.1's <c>TrialFailure</c>, as the protocol's <c>turn</c> line names it;
/// ADR 0024: parsed once where it enters). Kept with the turn, so a stored run never grades such a turn as the agent's.
/// </summary>
public enum BenchmarkTurnFailure
{
    Timeout,
    Transport,
    [WireName("api_error")] ApiError,
    [WireName("replay_miss")] ReplayMiss,
}
