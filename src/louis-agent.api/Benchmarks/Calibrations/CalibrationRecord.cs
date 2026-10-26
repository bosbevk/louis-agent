namespace LouisAgent.Api.Benchmarks.Calibrations;

/// <summary>The judge's answers to one class of the people's labels, as the agreement card counts them: how many it passed, failed and could not tell.</summary>
public sealed record CalibrationClassCounts(int JudgePass, int JudgeFail, int JudgeUnknown);

/// <summary>One answer where the judge and the person differed: which conversation, which way each went (<c>pass</c>, <c>fail</c>, <c>unknown</c>) and the judge's reason.</summary>
public sealed record CalibrationDisagreement(string Scenario, string Mode, int Repeat, string Label, string Judge, string Reason);

/// <summary>
/// One check of a judge against the people's grades of a stored run (F18 step 6a.5b): the run whose labels were checked, the judge, the model that judged (null when the runner was not
/// told one), when it ended, how often it agreed, the bar and whether it clears it, each class's counts, and each disagreement. It is what the runner's <c>calibration</c> line said,
/// kept as it was: the API reads, it doesn't rate. A report is added and never changed; the latest is the card's, the rest its history.
/// </summary>
public sealed record CalibrationRecord(string Run, string Grader, string? JudgeModel, DateTimeOffset At, int Agreed, int Total, int Bar, bool Passes,
    CalibrationClassCounts PersonPass, CalibrationClassCounts PersonFail, IReadOnlyList<CalibrationDisagreement> Disagreements);
