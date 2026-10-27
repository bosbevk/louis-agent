using LouisAgent.Choices;

namespace LouisAgent.Evals.Grading.Outcomes;

/// <summary>
/// A trial's outcome (F18 step 2a): a pass only when every required grader passes; Unknown when a required grader couldn't
/// tell, which is shown and never counted as a pass.
/// </summary>
public enum TrialOutcome
{
    // PascalCase on purpose (Louis, 2026-10-27): stored runs and the protocol carry the text as written today; one lower-case style waits for protocol v2 (7c), which starts stored runs fresh.
    [WireName("Pass")] Pass,
    [WireName("Fail")] Fail,
    [WireName("Unknown")] Unknown,
    [WireName("Infrastructure")] Infrastructure,
}
