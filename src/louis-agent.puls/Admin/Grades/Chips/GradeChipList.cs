namespace LouisAgent.Puls.Admin.Grades.Chips;

/// <summary>
/// A conversation's grades as chips (F18 step 4.5c), one template for every eval: one chip per grade in the run's grader order, a grade
/// whose grader isn't listed last; the grader's name as the text, a partial score beside it, the reason as the title, the outcome as the
/// kind. With no grades, one chip, "no grades", with no kind and never a 0.
/// </summary>
public static class GradeChipList
{
    public static IReadOnlyList<GradeChip> Of(ConversationGrades grades, IReadOnlyList<GraderRow>? order)
    {
        if (grades.Grades is not { Count: > 0 } list) return [new GradeChip(GradesText.NoGrades)];
        return list
            .Select((grade, stored) => (grade, rank: Rank(order, grade.Grader), stored))
            .OrderBy(g => g.rank).ThenBy(g => g.stored)
            .Select(g => new GradeChip(g.grade.Outcome == GradeOutcome.Pending ? GradesText.PendingChip(g.grade.Grader) : GradesText.Chip(g.grade.Grader, g.grade.Score),
                ChipKind.Of(g.grade.Outcome), g.grade.Reason))
            .ToList();
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
