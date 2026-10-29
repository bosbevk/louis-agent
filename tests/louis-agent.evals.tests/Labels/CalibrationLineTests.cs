using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Labels;
using LouisAgent.Evals.Protocol.Kinds;

namespace LouisAgent.Evals.Tests.Labels;

// F18 step 6a.5a: a calibration report as one line of data, for the API that started the runner. The classes are in the agreement card's shape (person_pass and person_fail, each the
// judge's pass, fail and unknown counts), the verdict words are the enum's in lower case, and the bar rides along so a reader need not know it. Written first: the line did not exist.
public class CalibrationLineTests
{
    private static LabeledAnswer Label(string scenario, bool pass, int repeat = 1) =>
        new($"{scenario}.{repeat}.json", "meal-for-people", scenario, SampleMode.Beta, "76667d39dcdb", repeat,
            [new LabelTurn("q", "a")], pass, "");

    private static readonly CalibrationReport Report = new(Agreed: 3, Total: 4,
        [
            new Disagreement(Label(SampleScenario.Alpha, pass: false, repeat: 2), GradeOutcome.Unknown,
                "too little to decide")
        ],
        [
            new JudgedAnswer(Label(SampleScenario.Beta, true), GradeOutcome.Pass),
            new JudgedAnswer(Label(SampleScenario.Alpha, true), GradeOutcome.Pass),
            new JudgedAnswer(Label("cook-shops", false), GradeOutcome.Fail),
            new JudgedAnswer(Label(SampleScenario.Alpha, false, repeat: 2), GradeOutcome.Unknown)
        ]);

    [Test]
    public void TheLine_CarriesTheReport_InTheAgreementCardsShape()
    {
        string text = CalibrationLine.Of(SampleEval.Beta, "meal-for-people", "qwen3:4b", Report).ToLine();

        Assert.That(text, Does.StartWith("@{").And.Not.Contain("\n"));
        var line = JsonDocument.Parse(text[1..]).RootElement;
        Assert.That(line.GetProperty("event").GetString(), Is.EqualTo("calibration"));
        Assert.That(
            (line.GetProperty("eval").GetString(), line.GetProperty("grader").GetString(),
                line.GetProperty("judge_model").GetString()),
            Is.EqualTo((SampleEval.Beta, "meal-for-people", "qwen3:4b")));
        Assert.That(
            (line.GetProperty("agreed").GetInt32(), line.GetProperty("total").GetInt32(),
                line.GetProperty("bar").GetInt32(), line.GetProperty("passes").GetBoolean()),
            Is.EqualTo((3, 4, 90, false)));
        Assert.That(line.GetProperty("person_pass").GetProperty("judge_pass").GetInt32(), Is.EqualTo(2));
        Assert.That(line.GetProperty("person_fail").GetProperty("judge_fail").GetInt32(), Is.EqualTo(1));
        Assert.That(line.GetProperty("person_fail").GetProperty("judge_unknown").GetInt32(), Is.EqualTo(1));
        var d = line.GetProperty("disagreements").EnumerateArray().Single();
        Assert.That(
            (d.GetProperty("scenario").GetString(), d.GetProperty("repeat").GetInt32(),
                d.GetProperty("label").GetString(), d.GetProperty("judge").GetString(),
                d.GetProperty("reason").GetString()),
            Is.EqualTo((SampleScenario.Alpha, 2, "fail", "unknown", "too little to decide")));
    }

    [Test]
    public void TheClassesTheReportPrints_AreTheOnesTheLineCarries()
    {
        var pass = Report.ClassOf(pass: true);
        var fail = Report.ClassOf(pass: false);

        Assert.That((pass.Total, pass.Agreed, pass.Clears, pass.Few), Is.EqualTo((2, 2, true, true)));
        Assert.That((fail.Total, fail.Agreed, fail.Clears, fail.Few), Is.EqualTo((2, 1, false, true)));
    }

    // 6a.5e (the review's nit): the bar is at least 90% of a class, in whole numbers: 9 of 10 clears, 8 of 10 does not, 18 of 20 clears, and no labels clears nothing.
    [Test]
    public void AClassAtExactlyTheBar_Clears()
    {
        Assert.That(new ClassReport(true, 10, 9, 9, 1, 0).Clears, Is.True);
        Assert.That(new ClassReport(true, 10, 8, 8, 2, 0).Clears, Is.False);
        Assert.That(new ClassReport(false, 20, 18, 2, 18, 0).Clears, Is.True);
        Assert.That(new ClassReport(false, 0, 0, 0, 0, 0).Clears, Is.False);
    }
}
