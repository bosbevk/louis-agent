namespace LouisAgent.Evals.Grading.Common;

/// <summary>A common grader's verdict, with its score in step with its outcome: a pass scores 1, a fail and an unknown score 0.</summary>
internal static class Verdict
{
    public static Grade Pass(string grader, int version, string reason) => new(grader, version, 1, GradeOutcome.Pass, reason);

    public static Grade Fail(string grader, int version, string reason) => new(grader, version, 0, GradeOutcome.Fail, reason);

    public static Grade Unknown(string grader, int version, string reason) => new(grader, version, 0, GradeOutcome.Unknown, reason);
}
