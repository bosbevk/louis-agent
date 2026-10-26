using System.Text.Json;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Labels;

namespace LouisAgent.Evals.Tests.Labels;

// F18 step 6a.5a: a calibration report as one line of data, for the API that started the runner. The classes are in the agreement card's shape (person_pass and person_fail, each the
// judge's pass, fail and unknown counts), the verdict words are the enum's in lower case, and the bar rides along so a reader need not know it. Written first: the line did not exist.
public class CalibrationLineTests
{
    private static LabeledAnswer Label(string scenario, bool pass, int repeat = 1) =>
        new($"{scenario}.{repeat}.json", "meal-for-people", scenario, "puls-cook", "76667d39dcdb", repeat, [new LabelTurn("q", "a")], pass, "");

    private static readonly CalibrationReport Report = new(Agreed: 3, Total: 4,
        [new Disagreement(Label("cook-af", pass: false, repeat: 2), GradeOutcome.Unknown, "too little to decide")],
        [new JudgedAnswer(Label("cook-en", true), GradeOutcome.Pass), new JudgedAnswer(Label("cook-af", true), GradeOutcome.Pass),
         new JudgedAnswer(Label("cook-shops", false), GradeOutcome.Fail), new JudgedAnswer(Label("cook-af", false, repeat: 2), GradeOutcome.Unknown)]);

    [Test]
    public void TheLine_CarriesTheReport_InTheAgreementCardsShape()
    {
        string text = CalibrationLine.Of("puls-cook", "meal-for-people", "qwen3:4b", Report).ToLine();

        Assert.That(text, Does.StartWith("@{").And.Not.Contain("\n"));
        var line = JsonDocument.Parse(text[1..]).RootElement;
        Assert.That(line.GetProperty("event").GetString(), Is.EqualTo("calibration"));
        Assert.That((line.GetProperty("eval").GetString(), line.GetProperty("grader").GetString(), line.GetProperty("judge_model").GetString()), Is.EqualTo(("puls-cook", "meal-for-people", "qwen3:4b")));
        Assert.That((line.GetProperty("agreed").GetInt32(), line.GetProperty("total").GetInt32(), line.GetProperty("bar").GetInt32(), line.GetProperty("passes").GetBoolean()), Is.EqualTo((3, 4, 90, false)));
        Assert.That(line.GetProperty("person_pass").GetProperty("judge_pass").GetInt32(), Is.EqualTo(2));
        Assert.That(line.GetProperty("person_fail").GetProperty("judge_fail").GetInt32(), Is.EqualTo(1));
        Assert.That(line.GetProperty("person_fail").GetProperty("judge_unknown").GetInt32(), Is.EqualTo(1));
        var d = line.GetProperty("disagreements").EnumerateArray().Single();
        Assert.That((d.GetProperty("scenario").GetString(), d.GetProperty("repeat").GetInt32(), d.GetProperty("label").GetString(), d.GetProperty("judge").GetString(), d.GetProperty("reason").GetString()),
            Is.EqualTo(("cook-af", 2, "fail", "unknown", "too little to decide")));
    }

    [Test]
    public void TheClassesTheReportPrints_AreTheOnesTheLineCarries()
    {
        var pass = Report.ClassOf(pass: true);
        var fail = Report.ClassOf(pass: false);

        Assert.That((pass.Total, pass.Agreed, pass.Clears, pass.Few), Is.EqualTo((2, 2, true, true)));
        Assert.That((fail.Total, fail.Agreed, fail.Clears, fail.Few), Is.EqualTo((2, 1, false, true)));
    }
}
