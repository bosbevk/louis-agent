using LouisAgent.Choices;

namespace LouisAgent.Evals.Protocol.Kinds;

/// <summary>
/// What one grader made of a conversation. Pending means a person hasn't graded it yet, which is never a pass.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public enum GradeOutcome
{
    [WireName("pass")] Pass,
    [WireName("fail")] Fail,
    [WireName("unknown")] Unknown,
    [WireName("pending")] Pending,
}
