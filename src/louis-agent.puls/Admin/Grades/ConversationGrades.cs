namespace LouisAgent.Puls.Admin.Grades;

/// <summary>
/// A conversation's grades in the stored order and its outcome; each null for a conversation from before F18 step 2a. <see cref="HumanGrades"/> (step 6a.3) are the people's grades of it,
/// each reviewer's latest; none for a conversation nobody graded, or from an API that doesn't send them.
/// </summary>
public sealed record ConversationGrades(IReadOnlyList<GradeRow>? Grades, GradeOutcome? Outcome, IReadOnlyList<HumanGradeRow>? HumanGrades = null)
{
    public static readonly ConversationGrades None = new(null, null);

    public IReadOnlyList<HumanGradeRow> Human => HumanGrades ?? [];
}
