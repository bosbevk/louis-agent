namespace LouisAgent.Puls.Admin.Grades;

/// <summary>
/// Where an outcome enters the portal (ADR 0024: parsed once, case ignored, never a number): a word it doesn't know reads as
/// <see cref="GradeOutcome.Unknown"/>, never a pass. Its chip kind is the existing <c>.chip</c> class (<c>ok</c>, <c>bad</c>, <c>warn</c>).
/// </summary>
public static class GradeOutcomes
{
    public static GradeOutcome Parse(string? text) => text?.Trim().ToLowerInvariant() switch
    {
        "pass" => GradeOutcome.Pass,
        "fail" => GradeOutcome.Fail,
        _ => GradeOutcome.Unknown,
    };

    public static string KindOf(GradeOutcome outcome) => outcome switch
    {
        GradeOutcome.Pass => "ok",
        GradeOutcome.Fail => "bad",
        _ => "warn",
    };
}
