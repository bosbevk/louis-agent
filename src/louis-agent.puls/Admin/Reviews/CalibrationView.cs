using System.Text.Json;

namespace LouisAgent.Puls.Admin.Reviews;

/// <summary>One answer where the judge and you differed, as the API kept it: which conversation, who said what (<c>pass</c>, <c>fail</c>, <c>unknown</c>) and the judge's reason.</summary>
public sealed record CalibrationDisagreementView(string Scenario, string Mode, int Repeat, string Label, string Judge, string Reason);

/// <summary>
/// One check of a judge against the answers you graded (F18 step 6a.5c): with which model, when, how often it agreed, the bar and whether it clears it, each class of your grades as the
/// agreement card reads it (<see cref="ClassAgreement"/>), and the disagreements. The counts are the API's; the words are <see cref="CalibrationText"/>'s.
/// </summary>
public sealed record CalibrationReportView(string Grader, string? JudgeModel, DateTimeOffset At, int Agreed, int Total, int Bar, bool Passes,
    ClassAgreement PersonPass, ClassAgreement PersonFail, IReadOnlyList<CalibrationDisagreementView> Disagreements);

/// <summary>Reads <c>GET …/runs/{id}/calibrations</c> into the card's reports, as the API orders them (newest first); none for a run with no check yet.</summary>
public static class CalibrationView
{
    public static IReadOnlyList<CalibrationReportView> Of(JsonElement response)
    {
        if (!response.TryGetProperty("calibrations", out var list) || list.ValueKind != JsonValueKind.Array) return [];
        return [.. list.EnumerateArray().Select(c => new CalibrationReportView(Text(c, "grader") ?? "", Text(c, "judge_model"),
            c.TryGetProperty("at", out var at) && at.ValueKind == JsonValueKind.String ? at.GetDateTimeOffset() : DateTimeOffset.MinValue,
            Int(c, "agreed"), Int(c, "total"), Int(c, "bar"), c.TryGetProperty("passes", out var p) && p.ValueKind == JsonValueKind.True,
            AgreementView.ClassOf(c.GetProperty("person_pass"), personPassed: true), AgreementView.ClassOf(c.GetProperty("person_fail"), personPassed: false),
            c.TryGetProperty("disagreements", out var list2) && list2.ValueKind == JsonValueKind.Array
                ? [.. list2.EnumerateArray().Select(d => new CalibrationDisagreementView(Text(d, "scenario") ?? "", Text(d, "mode") ?? "", Int(d, "repeat"), Text(d, "label") ?? "", Text(d, "judge") ?? "", Text(d, "reason") ?? ""))]
                : []))];
    }

    private static string? Text(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static int Int(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;
}
