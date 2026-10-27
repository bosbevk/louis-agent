using LouisAgent.Choices;
using System.Text.Json;
using LouisAgent.Evals.Grading;

namespace LouisAgent.Evals.Labels;

/// <summary>The judge's answers to one class of a person's labels, as the API's agreement card counts them: how many it passed, failed and could not tell.</summary>
public sealed record CalibrationClassLine(int JudgePass, int JudgeFail, int JudgeUnknown);

/// <summary>One answer where the judge and the person differed: which conversation, which way each went (<c>pass</c>, <c>fail</c>, <c>unknown</c>), and the judge's reason.</summary>
public sealed record CalibrationDisagreementLine(string Scenario, string Mode, int Repeat, string Run, string Label, string Judge, string Reason);

/// <summary>
/// A calibration report as one line of data (F18 step 6a.5a), printed after the human lines so the API that started the runner can keep it: <c>@{"event":"calibration",…}</c>, the
/// shape the protocol's lines have. It carries the eval and the grader, the judge model (null when none is set), the agreement and the bar, whether the judge clears it, each class in
/// the agreement card's shape, and each disagreement. The words are the outcome's names in lower case, never numbers. Written with the harness's snake_case options (ADR 0041).
/// </summary>
public sealed record CalibrationLine(string Event, string Eval, string Grader, string? JudgeModel, int Agreed, int Total, int Bar, bool Passes,
    CalibrationClassLine PersonPass, CalibrationClassLine PersonFail, IReadOnlyList<CalibrationDisagreementLine> Disagreements)
{
    public const string EventName = "calibration";

    public static CalibrationLine Of(string eval, string grader, string? judgeModel, CalibrationReport report) =>
        new(EventName, eval, grader, judgeModel, report.Agreed, report.Total, CalibrationReport.Bar, report.Passes,
            Class(report.ClassOf(pass: true)), Class(report.ClassOf(pass: false)),
            [.. report.Disagreements.Select(d => new CalibrationDisagreementLine(d.Answer.Scenario, d.Answer.Mode, d.Answer.Repeat, d.Answer.Run,
                Word(d.Answer.Pass ? GradeOutcome.Pass : GradeOutcome.Fail), Word(d.Verdict), d.Reason))]);

    /// <summary>The line as the runner prints it: <c>@</c> and the JSON, on one line.</summary>
    public string ToLine() => "@" + JsonSerializer.Serialize(this, EvalJson.SnakeCase);

    private static CalibrationClassLine Class(ClassReport c) => new(c.Passed, c.Failed, c.CantTell);

    private static string Word(GradeOutcome outcome) => outcome.Wire();
}
