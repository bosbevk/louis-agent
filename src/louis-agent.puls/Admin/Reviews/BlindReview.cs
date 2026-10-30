using LouisAgent.Choices;
using LouisAgent.Puls.Admin.Grades;
using CheckKind = LouisAgent.Evals.Protocol.Kinds.CheckKind;

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
/// Keeps a review blind (F18 step 6a.3, Louis's pick: "hide the judge's chip too"). If you see the judge's verdict
/// before giving yours, you lean on it, and the calibration then measures the judge against itself. So a judge that has
/// a human review stays hidden in the explorer until you've graded that conversation, or always when no reviewer is
/// named. A judge nobody reviews, the fixed rules and the review's own chip are never hidden; a name is the same name
/// whatever its case.
/// </summary>
public static class BlindReview
{
    /// <summary>
    /// What a human review's name ends with (<c>meal-for-people.human</c>): the one place the portal spells it; the
    /// API has its own constant.
    /// </summary>
    private const string HumanSuffix = ".human";

    /// <summary>Whether a grader is a human review: a grader a person answers, named for its judge.</summary>
    public static bool IsReview(GraderRow grader) =>
        SettingChoice.TryParse(grader.Kind, out CheckKind? kind) && kind == CheckKind.Human &&
        grader.Name.EndsWith(HumanSuffix, StringComparison.Ordinal);

    /// <summary>
    /// The judge a review is of: <c>cook-judge.human</c> gives <c>cook-judge</c>; a name without the suffix is its own
    /// judge.
    /// </summary>
    public static string JudgeOf(string review) => review.EndsWith(HumanSuffix, StringComparison.Ordinal)
        ? review[..^HumanSuffix.Length]
        : review;

    /// <summary>
    /// The grades this reviewer gave the judge on this conversation, kept under their name whatever its case; none
    /// without a name. The one place that knows whose a grade is.
    /// </summary>
    public static IEnumerable<HumanGradeRow> OwnGrades(ConversationGrades grades, string judge, string? reviewer) =>
        ReviewerName.Clean(reviewer) is { } name
            ? grades.Human.Where(h =>
                h.ModelGrader == judge && string.Equals(h.Reviewer, name, StringComparison.OrdinalIgnoreCase))
            : [];

    /// <summary>
    /// Whether this reviewer has answered the judge on this conversation: a grade kept under their name, or one given
    /// this session. Nobody has without a name.
    /// </summary>
    public static bool Answered(ConversationGrades grades, string judge, string? reviewer,
        Func<string, bool> gradedThisSession) =>
        ReviewerName.Clean(reviewer) is not null &&
        (OwnGrades(grades, judge, reviewer).Any() || gradedThisSession(judge));

    /// <summary>
    /// The run's review that waits for a grade on this conversation, for the page to show its panel; none where the
    /// question doesn't apply (a control).
    /// </summary>
    public static GraderRow? ReviewOf(IReadOnlyList<GraderRow>? graders, ConversationGrades grades) =>
        (graders ?? []).FirstOrDefault(g =>
            IsReview(g) && grades.Grades?.Any(x => x.Grader == g.Name && x.Outcome == GradeOutcome.Pending) == true);

    /// <summary>The judges whose grades this reviewer may not see yet, by name.</summary>
    public static IReadOnlySet<string> HiddenJudges(ConversationGrades grades, IReadOnlyList<GraderRow>? order,
        string? reviewer, Func<string, bool> gradedThisSession) =>
        (order ?? []).Where(IsReview).Select(g => JudgeOf(g.Name))
        .Where(judge => !Answered(grades, judge, reviewer, gradedThisSession)).ToHashSet();

    /// <summary>
    /// Whether the person's verdict (pass, fail or unsure) agrees with the judge's; a judge that said neither pass
    /// nor fail has nothing to agree with.
    /// </summary>
    /// <remarks>A switch expression on the pair, one arm per case, read like a table.</remarks>
    public static ReviewVerdictKind Compare(string person, GradeOutcome judge) => (person, judge) switch
    {
        ("unsure", _) => ReviewVerdictKind.YouWereUnsure,
        ("pass", GradeOutcome.Pass) or ("fail", GradeOutcome.Fail) => ReviewVerdictKind.Agreed,
        (_, GradeOutcome.Pass or GradeOutcome.Fail) => ReviewVerdictKind.Disagreed,
        _ => ReviewVerdictKind.JudgeCouldNotTell,
    };
}
