namespace LouisAgent.Puls.Admin.Grades.Chips;

/// <summary>An outcome's chip class, the view's choice: the existing <c>.chip</c> kinds (<c>ok</c>, <c>bad</c>, <c>warn</c>).</summary>
public static class ChipKind
{
    public static string Of(GradeOutcome outcome) => outcome switch
    {
        GradeOutcome.Pass => "ok",
        GradeOutcome.Fail => "bad",
        _ => "warn",
    };
}
