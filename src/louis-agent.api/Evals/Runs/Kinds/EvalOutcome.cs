using LouisAgent.Choices;

namespace LouisAgent.Api.Evals.Runs.Kinds;

/// <summary>
/// How a grade or a trial came out. Unknown is shown but never counts as a pass. Infrastructure means our side broke,
/// not the agent, so it's neither a pass nor a fail and stays out of the rates.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum EvalOutcome
{
    // PascalCase on purpose (Louis, 2026-10-27): stored runs and the protocol carry the text as written today.
    // One lower-case style waits for protocol v2 (7c), which starts stored runs fresh.
    [WireName("Pass")] Pass,
    [WireName("Fail")] Fail,
    [WireName("Unknown")] Unknown,
    [WireName("Infrastructure")] Infrastructure,
    [WireName("Pending")] Pending,
}
