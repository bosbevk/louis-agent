using LouisAgent.Testing;
using LouisAgent.Puls.Admin.Reviews;

namespace LouisAgent.Puls.Tests.Admin.Reviews;

// F18 step 6a.5c: a judge check reads as plain sentences (Louis, 6a.4c: "I do not understand what I'm reading"): when and with which judge, how often it agreed, whether it clears the bar
// and, if not, which class fell short and by how much; each disagreement says which conversation and who said what. Written first: the words did not exist.
public class CalibrationTextTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 26, 14, 2, 0, TimeSpan.Zero);

    private static CalibrationReportView Report(int failAgreed, int failTotal = 9, string? judge = "qwen3:4b", int passTotal = 27) =>
        new("meal-for-people", judge, At, passTotal + failAgreed, passTotal + failTotal, 90, failTotal > 0 && failAgreed * 100 >= failTotal * 90 && passTotal > 0,
            new ClassAgreement(passTotal, 0, 0, PersonPassed: true), new ClassAgreement(failTotal - failAgreed, failAgreed, 0, PersonPassed: false), []);

    [Test]
    public void TheHeadline_SaysWhenWithWhichJudge_AndHowOftenItAgreed()
    {
        Assert.That(CalibrationText.Headline(Report(9), "2026-10-26 16:02"), Is.EqualTo("Checked 2026-10-26 16:02 with qwen3:4b: the judge agreed with you on 36 of 36 answers."));
        Assert.That(CalibrationText.Headline(Report(8, judge: null), "2026-10-26 16:02"), Does.StartWith("Checked 2026-10-26 16:02 with no judge model set: the judge agreed with you on 35 of 36 answers."));
    }

    [Test]
    public void TheVerdict_IsTheBar_OrWhichClassFellShort()
    {
        Assert.That(CalibrationText.Verdict(Report(9)), Is.EqualTo("It sees what you see: at least 90% in each class."));
        Assert.That(CalibrationText.Verdict(Report(8)), Is.EqualTo("Not yet: of the 9 answers you failed, it agreed on 8, under 90%."));
        Assert.That(CalibrationText.Verdict(Report(0, failTotal: 0)), Is.EqualTo("Not yet: you have failed no answers yet, so that class is untested."));
        Assert.That(CalibrationText.Verdict(Report(0, failTotal: 0, passTotal: 0)),
            Is.EqualTo("Not yet: you have passed no answers yet, so that class is untested; you have failed no answers yet, so that class is untested."));
    }

    [Test]
    public void ADisagreement_SaysWhichConversation_AndWhoSaidWhat()
    {
        Assert.That(CalibrationText.Where(new CalibrationDisagreementView("cook-shops", SampleMode.Beta, 2, "fail", "pass", "a dish")), Is.EqualTo($"cook-shops · {SampleMode.Beta} · repeat 2: you said fail, the judge said pass."));
        Assert.That(CalibrationText.Where(new CalibrationDisagreementView(SampleScenario.Alpha, SampleMode.Gamma, 1, "pass", "unknown", "too little")), Is.EqualTo(SampleScenario.Fill($"<ScenarioAlpha> · {SampleMode.Gamma} · repeat 1: you said pass, the judge couldn't tell.")));
    }

    [Test]
    public void AnEarlierCheck_IsOneShortLine()
    {
        Assert.That(CalibrationText.Earlier(Report(8), "2026-10-26 15:00"), Is.EqualTo("2026-10-26 15:00 · qwen3:4b · 35 of 36 · not yet"));
        Assert.That(CalibrationText.Earlier(Report(9, judge: null), "2026-10-26 15:00"), Is.EqualTo("2026-10-26 15:00 · no judge model set · 36 of 36 · pass"));
    }

    // 6a.5e (the review's nit): the bar is "at least 90%", so a class at exactly 90% clears and is not listed as short.
    [Test]
    public void AClassAtExactlyTheBar_Clears_AndIsNotListedAsShort()
    {
        var atTheBar = new CalibrationReportView("meal-for-people", "qwen3:4b", At, 9, 11, 90, false,
            new ClassAgreement(9, 1, 0, PersonPassed: true), new ClassAgreement(1, 0, 0, PersonPassed: false), []);

        Assert.That(CalibrationText.Verdict(atTheBar), Is.EqualTo("Not yet: of the 1 answer you failed, it agreed on 0, under 90%."));
    }

    // 6a.5e: the card's two error sentences live here, with the other words.
    [Test]
    public void TheErrors_ReadAsSentences()
    {
        Assert.That(CalibrationText.Failed("Eval 'x' has no grader 'y'."), Is.EqualTo("The check ended without a report. The runner's last line: Eval 'x' has no grader 'y'."));
        Assert.That(CalibrationText.NoReportKept, Does.Contain("no report was kept"));
    }
}
