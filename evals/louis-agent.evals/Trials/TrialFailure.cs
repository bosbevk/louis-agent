using System.Text.Json.Serialization;

namespace LouisAgent.Evals.Trials;

/// <summary>
/// Why a trial failed for the infrastructure, not the agent: reported apart, and left out of pass@k and pass^k, so a flaky
/// network never counts as the agent failing.
/// </summary>
public enum TrialFailure
{
    Timeout,
    Transport,
    [JsonStringEnumMemberName("api_error")] ApiError,
    [JsonStringEnumMemberName("replay_miss")] ReplayMiss,
}
