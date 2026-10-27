namespace LouisAgent.Api.Contracts.Admin;

/// <summary>A run's judge checks, newest first, as the agreement card reads them.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed record CalibrationsReply(IReadOnlyList<CalibrationReportRow> Calibrations);

/// <summary>
/// One judge check (F18 step 6a.5b): which run and judge, how often the judge agreed with the people and the bar it had
/// to reach, each class of the people's grades by what the judge said, and every answer where they differed.
/// </summary>
public sealed record CalibrationReportRow(
    string Run,
    string Grader,
    string? JudgeModel,
    DateTimeOffset At,
    int Agreed,
    int Total,
    int Bar,
    bool Passes,
    CalibrationClassRow PersonPass,
    CalibrationClassRow PersonFail,
    IReadOnlyList<DisagreementRow> Disagreements);

/// <summary>One class of the people's grades, by what the judge said of each: pass, fail or don't know.</summary>
public sealed record CalibrationClassRow(int JudgePass, int JudgeFail, int JudgeUnknown);

/// <summary>One answer where the judge and the person differed, and the judge's reason.</summary>
public sealed record DisagreementRow(
    string Scenario,
    string Mode,
    int Repeat,
    string Label,
    string Judge,
    string Reason);
