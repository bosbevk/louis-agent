namespace LouisAgent.Api.Evals.Calibrations;

/// <summary>
/// What the judge said to one class of the people's labels, as the agreement card counts it: how many it passed,
/// failed and couldn't tell.
/// </summary>
public sealed record CalibrationClassCounts(int JudgePass, int JudgeFail, int JudgeUnknown);

/// <summary>
/// One conversation where the judge and the person differed: which one, which way each went (<c>pass</c>,
/// <c>fail</c>, <c>unknown</c>) and the judge's reason.
/// </summary>
public sealed record CalibrationDisagreement(
    string Scenario,
    string Mode,
    int Repeat,
    string Label,
    string Judge,
    string Reason);

/// <summary>
/// One check of a judge against the people's grades of a stored run: which run's labels, which judge and model, when it
/// ended, how often it agreed, the bar and whether it cleared it, and every disagreement.
/// </summary>
/// <remarks>
/// It keeps what the runner's <c>calibration</c> line said, as it was: the API reads, it doesn't rate. A report is
/// added and never changed; the latest is what the card shows, the rest is its history. The judge's model is missing
/// when nobody told the runner which one it was.
/// </remarks>
public sealed record CalibrationRecord(
    string Run,
    string Grader,
    string? JudgeModel,
    DateTimeOffset At,
    int Agreed,
    int Total,
    int Bar,
    bool Passes,
    CalibrationClassCounts PersonPass,
    CalibrationClassCounts PersonFail,
    IReadOnlyList<CalibrationDisagreement> Disagreements);
