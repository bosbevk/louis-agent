using LouisAgent.Choices;

namespace LouisAgent.Evals.Protocol.Kinds;

/// <summary>
/// What on our side ended a turn early: it took too long, the connection broke, the API answered with an error, or a
/// replay had no recording for what was asked.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public enum TurnFailure
{
    [WireName("timeout")] Timeout,
    [WireName("transport")] Transport,
    [WireName("api-error")] ApiError,
    [WireName("replay-miss")] ReplayMiss,
}
