using LouisAgent.Choices;

namespace LouisAgent.Evals.Protocol.Kinds;

/// <summary>
/// Which part of a run a model worked for: the agent, a specialist, a minion, or a judge grading answers.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public enum ModelLevel
{
    [WireName("agent")] Agent,
    [WireName("specialist")] Specialist,
    [WireName("minion")] Minion,
    [WireName("grader")] Grader,
}
