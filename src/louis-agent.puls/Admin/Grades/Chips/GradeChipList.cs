namespace LouisAgent.Puls.Admin.Grades.Chips;

/// <summary>
/// A conversation's grades as chips (F18 step 4.5c), one template for every eval: one chip per grade in the run's grader order, a grade
/// whose grader isn't listed last; the grader's name as the text, a partial score beside it, the reason as the title, the outcome as the
/// kind. With no grades, one chip, "no grades", with no kind and never a 0.
/// </summary>
public static class GradeChipList
{
    public static IReadOnlyList<GradeChip> Of(ConversationGrades grades, IReadOnlyList<GraderRow>? order, IReadOnlySet<string>? hidden = null, string? reviewer = null)
    {
        if (grades.Grades is not { Count: > 0 } list) return [new GradeChip(GradesText.NoGrades)];
        return list
            .Select((grade, stored) => (grade, rank: Rank(order, grade.Grader), stored))
            .OrderBy(g => g.rank).ThenBy(g => g.stored)
            .Select(g => hidden?.Contains(g.grade.Grader) == true
                ? new GradeChip(g.grade.Grader)
                : g.grade.Outcome == GradeOutcome.Pending
                    ? Review(g.grade, grades, reviewer)
                    : new GradeChip(GradesText.Chip(g.grade.Grader, g.grade.Score), ChipKind.Of(g.grade.Outcome), g.grade.Reason))
            .ToList();
    }

    /// <summary>
    /// A review's chip (6a.4e): this reviewer's own latest verdict once they have graded ("meal-for-people.human · pass (Louis)"), else "waiting for review". Another reviewer's grade
    /// stays out of sight, so nobody is anchored before they grade (blind review); the run's own line only ever says waiting, since the grade lives in the API's store.
    /// </summary>
    private static GradeChip Review(GradeRow grade, ConversationGrades grades, string? reviewer)
    {
        var mine = reviewer is null ? null : grades.Human
            .Where(h => h.Grader == grade.Grader && string.Equals(h.Reviewer, reviewer, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(h => h.At).FirstOrDefault();
        return mine is null
            ? new GradeChip(GradesText.PendingChip(grade.Grader), ChipKind.Of(grade.Outcome), grade.Reason)
            : new GradeChip(GradesText.ReviewedChip(grade.Grader, mine.Verdict, reviewer), ChipKind.OfVerdict(mine.Verdict), mine.Note.Length > 0 ? mine.Note : null);
    }

    /// <summary>The grader's place in the run's order; past the end when the run doesn't list it.</summary>
    private static int Rank(IReadOnlyList<GraderRow>? order, string grader)
    {
        if (order is null) return int.MaxValue;
        for (int i = 0; i < order.Count; i++)
            if (order[i].Name == grader) return i;
        return int.MaxValue;
    }
}
