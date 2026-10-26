using System.Text.Json;

namespace LouisAgent.Puls.Admin.Reviews;

/// <summary>
/// One class of the people's grades of a judge (F18 step 6a.3): those they passed, or those they failed, and what the judge said to each. The judge agreed where it said the same; a "can't tell" is
/// not agreement. The text is the share the page shows: "13 of 14 (93%)", "few labels" under ten (a share of four is four answers), "no labels" for a class nobody has graded.
/// </summary>
public sealed record ClassAgreement(int JudgePass, int JudgeFail, int JudgeUnknown, bool PersonPassed)
{
    /// <summary>A class with fewer labels than this is said to be thin.</summary>
    public const int FewLabels = 10;

    public int Total => JudgePass + JudgeFail + JudgeUnknown;

    public int Agreed => PersonPassed ? JudgePass : JudgeFail;

    public bool Few => Total < FewLabels;

    public string Text => Total == 0
        ? "no labels"
        : $"{Agreed} of {Total} ({(int)Math.Round(100.0 * Agreed / Total, MidpointRounding.AwayFromZero)}%)" + (Few ? ", few labels" : "");
}

/// <summary>A judge and how often it agreed with the people, class by class.</summary>
public sealed record JudgeAgreement(string Grader, ClassAgreement PersonPass, ClassAgreement PersonFail)
{
    public bool AnyLabels => PersonPass.Total + PersonFail.Total > 0;
}

/// <summary>Reads a run's <c>agreement</c> (the API's counts: it reads, it doesn't rate) into what the page shows; none for a run or an API from before the reviews.</summary>
public static class AgreementView
{
    public static IReadOnlyList<JudgeAgreement> Of(JsonElement run)
    {
        if (!run.TryGetProperty("agreement", out var list) || list.ValueKind != JsonValueKind.Array) return [];
        return [.. list.EnumerateArray().Select(judge => new JudgeAgreement(judge.GetProperty("grader").GetString() ?? "",
            Class(judge.GetProperty("person_pass"), personPassed: true), Class(judge.GetProperty("person_fail"), personPassed: false)))];
    }

    private static ClassAgreement Class(JsonElement counts, bool personPassed) => ClassOf(counts, personPassed);

    /// <summary>One class's counts (<c>judge_pass</c>, <c>judge_fail</c>, <c>judge_unknown</c>) as the agreement and the judge checks (6a.5c) both send them.</summary>
    public static ClassAgreement ClassOf(JsonElement counts, bool personPassed) =>
        new(Count(counts, "judge_pass"), Count(counts, "judge_fail"), Count(counts, "judge_unknown"), personPassed);

    private static int Count(JsonElement counts, string name) => counts.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;
}
