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
    private const string HumanSuffix = ".human";

    /// <summary>The judges whose grades this reviewer may not see yet, by name.</summary>
    public static IReadOnlySet<string> HiddenJudges(ConversationGrades grades, IReadOnlyList<GraderRow>? order, string? reviewer, Func<string, bool> gradedThisSession)
    {
        var judges = (order ?? []).Where(g => g.Kind == "Human" && g.Name.EndsWith(HumanSuffix, StringComparison.Ordinal)).Select(g => g.Name[..^HumanSuffix.Length]);
        string? name = ReviewerName.Clean(reviewer);
        return judges.Where(judge => name is null
            || !(grades.Human.Any(h => h.ModelGrader == judge && string.Equals(h.Reviewer, name, StringComparison.OrdinalIgnoreCase)) || gradedThisSession(judge))).ToHashSet();
    }

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
