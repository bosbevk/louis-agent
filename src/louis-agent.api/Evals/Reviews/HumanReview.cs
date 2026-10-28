namespace LouisAgent.Api.Evals.Reviews;

/// <summary>
/// What a human review is called: the model grader's name with this ending, <c>meal-for-people.human</c>. The review
/// and calibration routes all spell it from here; the portal has its own copy in <c>BlindReview</c>.
/// </summary>
public static class HumanReview
{
    /// <summary>The ending that turns a model grader's name into its human review's.</summary>
    public const string Suffix = ".human";
}
