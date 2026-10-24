namespace LouisAgent.Evals.Grading;

/// <summary>
/// A grader's outcome. Unknown is shown and never counted as a pass: a grader that can't tell says so.
/// </summary>
public enum GradeOutcome
{
    Pass,
    Fail,
    Unknown,
}
