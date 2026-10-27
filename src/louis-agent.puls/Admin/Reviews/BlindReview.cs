using LouisAgent.Puls.Admin.Grades;

namespace LouisAgent.Puls.Admin.Reviews;

/// <summary>What a reviewer is told once they have graded: whether they agreed with the judge.</summary>
public enum ReviewVerdictKind
{
    Agreed,
    Disagreed,
    YouWereUnsure,
    JudgeCouldNotTell,
}

/// <summary>
/// The rules of a blind review (F18 step 6a.3, Louis's pick: "hide the judge's chip too"). A person who sees the judge's verdict before giving theirs anchors on it, and the calibration then
/// measures the judge against itself. So for a judge that has a human review, its verdict is hidden everywhere in the explorer until this reviewer has graded that conversation (or with no
/// reviewer name, since then nobody has): a judge nobody reviews, the fixed rules and the review's own chip are never hidden. A name is the same name whatever its case.
/// </summary>
public static class BlindReview
{
    /// <summary>What a human review's name ends with (<c>meal-for-people.human</c>): the one place the portal spells it (6a.3b); the API has its own constant, <c>ReviewEndpoints.HumanSuffix</c>.</summary>
    private const string HumanSuffix = ".human";

    /// <summary>Whether a grader is a human review: a Human grader named for its judge.</summary>
    public static bool IsReview(GraderRow grader) =>
        grader.Kind == "Human" && grader.Name.EndsWith(HumanSuffix, StringComparison.Ordinal);

    /// <summary>The judge a review is of: <c>cook-judge.human</c> gives <c>cook-judge</c>; a name without the suffix is its own judge.</summary>
    public static string JudgeOf(string review) => review.EndsWith(HumanSuffix, StringComparison.Ordinal)
        ? review[..^HumanSuffix.Length]
        : review;

    /// <summary>The grades this reviewer gave the judge on this conversation, stored under their name (whatever its case); none without a name. The one place that knows whose a grade is.</summary>
    public static IEnumerable<HumanGradeRow> OwnGrades(ConversationGrades grades, string judge, string? reviewer) =>
        ReviewerName.Clean(reviewer) is { } name
            ? grades.Human.Where(h =>
                h.ModelGrader == judge && string.Equals(h.Reviewer, name, StringComparison.OrdinalIgnoreCase))
            : [];

    /// <summary>Whether this reviewer has answered the judge on this conversation: a grade stored under their name, or one given this session. Nobody has without a name.</summary>
    public static bool Answered(ConversationGrades grades, string judge, string? reviewer,
        Func<string, bool> gradedThisSession) =>
        ReviewerName.Clean(reviewer) is not null &&
        (OwnGrades(grades, judge, reviewer).Any() || gradedThisSession(judge));

    /// <summary>The run's review that has a Pending grade on this conversation, for the page to show its panel; none where the question doesn't apply (a control, 6a.4a).</summary>
    public static GraderRow? ReviewOf(IReadOnlyList<GraderRow>? graders, ConversationGrades grades) =>
        (graders ?? []).FirstOrDefault(g =>
            IsReview(g) && grades.Grades?.Any(x => x.Grader == g.Name && x.Outcome == GradeOutcome.Pending) == true);

    /// <summary>The judges whose grades this reviewer may not see yet, by name.</summary>
    public static IReadOnlySet<string> HiddenJudges(ConversationGrades grades, IReadOnlyList<GraderRow>? order,
        string? reviewer, Func<string, bool> gradedThisSession) =>
        (order ?? []).Where(IsReview).Select(g => JudgeOf(g.Name))
        .Where(judge => !Answered(grades, judge, reviewer, gradedThisSession)).ToHashSet();

    /// <summary>Whether the person's verdict (pass, fail or unsure) agrees with the judge's outcome; a judge that didn't say pass or fail has nothing to agree with.</summary>
    public static ReviewVerdictKind Compare(string person, GradeOutcome judge)
    {
        if (person == "unsure") return ReviewVerdictKind.YouWereUnsure;
        return judge switch
        {
            GradeOutcome.Pass => person == "pass" ? ReviewVerdictKind.Agreed : ReviewVerdictKind.Disagreed,
            GradeOutcome.Fail => person == "fail" ? ReviewVerdictKind.Agreed : ReviewVerdictKind.Disagreed,
            _ => ReviewVerdictKind.JudgeCouldNotTell,
        };
    }
}
