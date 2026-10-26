using LouisAgent.Puls.Admin.Grades;

namespace LouisAgent.Puls.Admin.Reviews;

/// <summary>What a conversation's review needs from this reviewer.</summary>
public enum ReviewState
{
    NoReview,
    ToReview,
    Reviewed,
}

/// <summary>
/// A conversation's review state for this reviewer (6a.4d, Louis: "I need to know which ones to check and which ones not and which ones do not need it"). No review grade on the
/// conversation (the question doesn't apply there, 6a.4a) is <see cref="ReviewState.NoReview"/>; a Pending review this reviewer hasn't answered is <see cref="ReviewState.ToReview"/>;
/// one they have (stored under their name, whatever its case, or given this session) is <see cref="ReviewState.Reviewed"/>. The same rule <see cref="BlindReview"/> uses to show the judge.
/// </summary>
public static class ReviewStatus
{
    private const string HumanSuffix = ".human";

    public static ReviewState Of(ConversationGrades grades, IReadOnlyList<GraderRow>? order, string? reviewer, Func<string, bool> gradedThisSession)
    {
        var reviews = (order ?? []).Where(g => g.Kind == "Human" && g.Name.EndsWith(HumanSuffix, StringComparison.Ordinal)).Select(g => g.Name).ToList();
        var pending = grades.Grades?.Where(g => g.Outcome == GradeOutcome.Pending && reviews.Contains(g.Grader)).Select(g => g.Grader[..^HumanSuffix.Length]).ToList() ?? [];
        if (pending.Count == 0) return ReviewState.NoReview;
        string? name = ReviewerName.Clean(reviewer);
        bool answered = name is not null && pending.All(judge =>
            grades.Human.Any(h => h.ModelGrader == judge && string.Equals(h.Reviewer, name, StringComparison.OrdinalIgnoreCase)) || gradedThisSession(judge));
        return answered ? ReviewState.Reviewed : ReviewState.ToReview;
    }

    public static string Text(ReviewState state) => state switch
    {
        ReviewState.ToReview => "to review",
        ReviewState.Reviewed => "reviewed ✓",
        _ => "no review",
    };

    /// <summary>The state as a CSS-safe word, for a card's data attribute and the pill's class.</summary>
    public static string Slug(ReviewState state) => state switch
    {
        ReviewState.ToReview => "to-review",
        ReviewState.Reviewed => "reviewed",
        _ => "no-review",
    };
}
