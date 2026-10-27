using LouisAgent.Choices;
namespace LouisAgent.Evals.Grading;

/// <summary>
/// A grader's outcome. Unknown is shown and never counted as a pass: a grader that can't tell says so. Pending (F18 step 6a) is a grade nobody has given yet, a human review's before a
/// person has graded: never a pass, and not Unknown either, since nobody has failed to tell.
/// </summary>
public enum GradeOutcome
{
    // PascalCase on purpose (Louis, 2026-10-27): stored runs and the protocol carry the text as written today; one lower-case style waits for protocol v2 (7c), which starts stored runs fresh.
    [WireName("Pass")] Pass,
    [WireName("Fail")] Fail,
    [WireName("Unknown")] Unknown,
    [WireName("Pending")] Pending,
}
