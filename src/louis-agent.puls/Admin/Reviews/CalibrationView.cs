using LouisAgent.Api.Contracts.Admin.Reviews;

namespace LouisAgent.Puls.Admin.Reviews;

/// <summary>
/// One answer where the judge and you differed, as the API kept it: which conversation, who said what (<c>pass</c>,
/// <c>fail</c>, <c>unknown</c>) and the judge's reason.
/// </summary>
public sealed record CalibrationDisagreementView(
    string Scenario,
    string Mode,
    int Repeat,
    string Label,
    string Judge,
    string Reason);

/// <summary>
/// One check of a judge against the answers you graded (F18 step 6a.5c): with which model, when, how often it agreed,
/// the bar and whether it clears it, each class of your grades as the agreement card reads it
/// (<see cref="ClassAgreement"/>), and the disagreements. The counts are the API's; the words are
/// <see cref="CalibrationText"/>'s.
/// </summary>
public sealed record CalibrationReportView(
    string Grader,
    string? JudgeModel,
    DateTimeOffset At,
    int Agreed,
    int Total,
    int Bar,
    bool Passes,
    ClassAgreement PersonPass,
    ClassAgreement PersonFail,
    IReadOnlyList<CalibrationDisagreementView> Disagreements);

/// <summary>
/// Reads the contract's judge checks into the card's reports, as the API orders them (newest first); none for a run
/// with no check yet.
/// </summary>
public static class CalibrationView
{
    /// <summary>The reports, newest first.</summary>
    public static IReadOnlyList<CalibrationReportView> Of(CalibrationsReply reply) =>
    [
        .. reply.Calibrations.Select(c => new CalibrationReportView(c.Grader, c.JudgeModel, c.At, c.Agreed, c.Total,
            c.Bar, c.Passes, Class(c.PersonPass, personPassed: true), Class(c.PersonFail, personPassed: false),
            [
                .. c.Disagreements.Select(d => new CalibrationDisagreementView(d.Scenario, d.Mode, d.Repeat, d.Label,
                    d.Judge, d.Reason))
            ]))
    ];

    /// <summary>One class of your grades as the agreement card reads it.</summary>
    private static ClassAgreement Class(CalibrationClassRow counts, bool personPassed) =>
        new(counts.JudgePass, counts.JudgeFail, counts.JudgeUnknown, personPassed);
}
