namespace LouisAgent.Evals.Grading;

/// <summary>
/// A grader's outcome. Unknown is shown and never counted as a pass: a grader that can't tell says so. Pending (F18 step 6a) is a grade nobody has given yet, a human review's before a
/// person has graded: never a pass, and not Unknown either, since nobody has failed to tell.
/// </summary>
public enum GradeOutcome
{
    Pass,
    Fail,
    Unknown,
    Pending,
}
