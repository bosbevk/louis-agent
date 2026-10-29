using LouisAgent.Choices;

namespace LouisAgent.Evals.Protocol.Kinds;

/// <summary>
/// How a whole conversation came out. Infrastructure means something on our side broke, so it isn't counted against
/// the agent.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public enum TrialOutcome
{
    [WireName("pass")] Pass,
    [WireName("fail")] Fail,
    [WireName("unknown")] Unknown,
    [WireName("infrastructure")] Infrastructure,
}
