namespace LouisAgent.Puls.Admin.Grades;

/// <summary>A conversation's grades in the stored order and its outcome; each null for a conversation from before F18 step 2a.</summary>
public sealed record ConversationGrades(IReadOnlyList<GradeRow>? Grades, GradeOutcome? Outcome)
{
    public static readonly ConversationGrades None = new(null, null);
}
