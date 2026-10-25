namespace LouisAgent.Puls.Admin.Grades.Reading;

/// <summary>
/// Where an outcome enters the portal (ADR 0024: parsed once, case ignored, never a number): a word it doesn't know reads as
/// <see cref="GradeOutcome.Unknown"/>, never a pass.
/// </summary>
public static class GradeOutcomes
{
    public static GradeOutcome Parse(string? text) => text?.Trim().ToLowerInvariant() switch
    {
        "pass" => GradeOutcome.Pass,
        "fail" => GradeOutcome.Fail,
        _ => GradeOutcome.Unknown,
    };
}
