using LouisAgent.Puls.Admin.Grades;

namespace LouisAgent.Puls.Admin.Reviews;

/// <summary>The review panel's sentences (F18 step 6a.3c, out of its code-behind, beside <see cref="GradesText"/>): the question it asks, whether you agreed with the judge, and what the judge said.</summary>
public static class ReviewText
{
    /// <summary>The question the review's grader asks, or one naming the judge when it set none.</summary>
    public static string Question(string? question, string judge) => question ?? $"Is this answer good enough for {judge}?";

    /// <summary>Whether this reviewer agreed with the judge; empty until both have said something.</summary>
    public static string Result(string? person, GradeOutcome? judge) => person is null || judge is not { } outcome ? "" : BlindReview.Compare(person, outcome) switch
    {
        ReviewVerdictKind.Agreed => "You agreed with the judge.",
        ReviewVerdictKind.Disagreed => "You disagreed with the judge.",
        ReviewVerdictKind.YouWereUnsure => "You were unsure.",
        _ => "The judge could not tell.",
    };

    public static string Judge(GradeOutcome? outcome) => outcome switch
    {
        GradeOutcome.Pass => "The judge said pass.",
        GradeOutcome.Fail => "The judge said fail.",
        _ => "The judge could not tell.",
    };
}
