using LouisAgent.Choices;

namespace LouisAgent.Evals.Trials;

/// <summary>
/// Why a trial failed for the infrastructure, not the agent: reported apart, and left out of pass@k and pass^k, so a flaky
/// network never counts as the agent failing.
/// </summary>
public enum TrialFailure
{
    Timeout,
    Transport,
    [WireName("api_error")] ApiError,
    [WireName("replay_miss")] ReplayMiss,
}
