namespace LouisAgent.Puls.Admin.Reviews;

/// <summary>
/// How a judge check reads (F18 step 6a.5c; Louis, 6a.4c: "I do not understand what I'm reading"): when and with which judge, how often it agreed with you, whether it clears the bar
/// and, if not, which class fell short and by how much; a disagreement says which conversation and who said what. The classes themselves read with <see cref="AgreementText"/>.
/// <paramref name="when"/> is the time as the page shows it: the page knows the viewer's clock, this class doesn't.
/// </summary>
public static class CalibrationText
{
    public static string Headline(CalibrationReportView r, string when) =>
        $"Checked {when} with {Judge(r)}: the judge agreed with you on {r.Agreed} of {r.Total} {(r.Total == 1 ? "answer" : "answers")}.";

    public static string Verdict(CalibrationReportView r)
    {
        if (r.Passes) return $"It sees what you see: at least {r.Bar}% in each class.";
        var shortClasses = new[] { r.PersonPass, r.PersonFail }.Where(c => !Clears(c, r.Bar))
            .Select(c => Short(c, r.Bar));
        return $"Not yet: {string.Join("; ", shortClasses)}.";
    }

    public static string Where(CalibrationDisagreementView d) =>
        $"{d.Scenario} · {d.Mode} · repeat {d.Repeat}: you said {d.Label}, {(d.Judge == "unknown" ? "the judge couldn't tell" : $"the judge said {d.Judge}")}.";

    public static string Earlier(CalibrationReportView r, string when) =>
        $"{when} · {Judge(r)} · {r.Agreed} of {r.Total} · {(r.Passes ? "pass" : "not yet")}";

    /// <summary>A check whose run failed: the runner's last line is the reason (6a.5e: the card's words live here).</summary>
    public static string Failed(string? lastLine) =>
        $"The check ended without a report. The runner's last line: {lastLine}";

    /// <summary>A check whose run ended well but left no report in the store.</summary>
    public const string NoReportKept = "The check ended, but no report was kept: the API's log says why.";

    /// <summary>The judge model the runner was told, or that it was told none: then every answer was "can't tell".</summary>
    public static string Judge(CalibrationReportView r) => r.JudgeModel ?? "no judge model set";

    private static bool Clears(ClassAgreement c, int bar) => c.Total > 0 && c.Agreed * 100 >= c.Total * bar;

    private static string Short(ClassAgreement c, int bar)
    {
        string you = c.PersonPassed ? "passed" : "failed";
        return c.Total == 0
            ? $"you have {you} no answers yet, so that class is untested"
            : $"of the {c.Total} {(c.Total == 1 ? "answer" : "answers")} you {you}, it agreed on {c.Agreed}, under {bar}%";
    }
}
