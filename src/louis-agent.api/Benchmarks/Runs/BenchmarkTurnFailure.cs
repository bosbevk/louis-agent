using LouisAgent.Choices;

namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>
/// Why a turn failed on our side rather than the agent's: it timed out, the connection broke, the API errored, or a
/// replay had no recording for it. It stays with the turn, so a stored run never blames the agent for it.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum BenchmarkTurnFailure
{
    Timeout,
    Transport,
    [WireName("api_error")] ApiError,
    [WireName("replay_miss")] ReplayMiss,
}
