namespace LouisAgent.Puls.Admin.Grades.Chips;

/// <summary>
/// Turns a conversation's grades into chips, the same way for every eval: one chip per grade, in the run's grader
/// order, with the grader's name, a partial score beside it, the reason on hover and the outcome as its colour. With
/// no grades you get one plain "no grades" chip, never a 0.
/// </summary>
public static class GradeChipList
{
    /// <summary>
    /// Builds the chips for one conversation. A grader the run doesn't list goes last, a hidden one shows only its
    /// name, and a grade still waiting for a person shows as a review chip.
    /// </summary>
    public static IReadOnlyList<GradeChip> Of(ConversationGrades grades, IReadOnlyList<GraderRow>? order,
        IReadOnlySet<string>? hidden = null, string? reviewer = null)
    {
        if (grades.Grades is not { Count: > 0 } list) return [new GradeChip(GradesText.NoGrades)];
        return list
            .Select((grade, stored) => (grade, rank: Rank(order, grade.Grader), stored))
            .OrderBy(g => g.rank).ThenBy(g => g.stored)
            .Select(g => Chip(g.grade, grades, hidden, reviewer))
            .ToList();
    }

    /// <summary>
    /// One grade's chip: just the grader's name when it's hidden, a review chip while a person still has to grade,
    /// else the grade with its score, its outcome's colour and its reason.
    /// </summary>
    /// <remarks>
    /// Guard clauses, one per case from the top (Fowler's *Replace Nested Conditional with Guard Clauses*).
    /// </remarks>
    private static GradeChip Chip(GradeRow grade, ConversationGrades grades, IReadOnlySet<string>? hidden,
        string? reviewer)
    {
        if (hidden?.Contains(grade.Grader) == true) return new GradeChip(grade.Grader);
        if (grade.Outcome == GradeOutcome.Pending) return Review(grade, grades, reviewer);
        return new GradeChip(GradesText.Chip(grade.Grader, grade.Score), ChipKind.Of(grade.Outcome), grade.Reason);
    }

    /// <summary>
    /// A review's chip: once you've graded it shows your own latest verdict ("meal-for-people.human · pass (Louis)"),
    /// before that "waiting for review". Someone else's grade stays out of sight, so nobody is swayed before they
    /// grade (blind review); the run's own line only ever says waiting, since the grade lives in the API's store.
    /// </summary>
    private static GradeChip Review(GradeRow grade, ConversationGrades grades, string? reviewer)
    {
        var pending = new GradeChip(GradesText.PendingChip(grade.Grader), ChipKind.Of(grade.Outcome), grade.Reason);
        if (reviewer is null) return pending;

        // This reviewer's latest grade of this judge, if they gave one.
        var mine = grades.Human
            .Where(h => h.Grader == grade.Grader &&
                        string.Equals(h.Reviewer, reviewer, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(h => h.At)
            .FirstOrDefault();
        if (mine is null) return pending;

        return new GradeChip(GradesText.ReviewedChip(grade.Grader, mine.Verdict, reviewer),
            ChipKind.OfVerdict(mine.Verdict), mine.Note.Length > 0 ? mine.Note : null);
    }

    /// <summary>Where the grader sits in the run's order. One the run doesn't list goes after all the others.</summary>
    private static int Rank(IReadOnlyList<GraderRow>? order, string grader)
    {
        if (order is null) return int.MaxValue;
        for (int i = 0; i < order.Count; i++)
            if (order[i].Name == grader)
                return i;
        return int.MaxValue;
    }
}
