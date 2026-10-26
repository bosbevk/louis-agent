using LouisAgent.Evals.Runner;
using LouisAgent.Evals.Runner.Arguments;

namespace LouisAgent.Evals.Runner.Tests.Arguments;

// F18 step 6.3: the two commands that check a model grader against a person, `--label-sheet` and `--calibrate`. Written first: the parser did not know them.
public class CalibrationCommandLineTests
{
    [Test]
    public void Calibrate_NamesTheEvalAndTheGrader() =>
        Assert.That(CommandLine.Parse(["--calibrate", "puls-cook", "meal-for-people"]), Is.EqualTo(new CalibrateRequest("puls-cook", "meal-for-people")));

    [Test]
    public void LabelSheet_NamesTheEvalTheGraderTheRunAndTheFolder_AndTenLabelsByDefault() =>
        Assert.That(CommandLine.Parse(["--label-sheet", "puls-cook", "meal-for-people", "8f7802b9a72e", "labels"]),
            Is.EqualTo(new LabelSheetRequest("puls-cook", "meal-for-people", "8f7802b9a72e", "labels", 10)));

    [Test]
    public void LabelSheet_TakesACount() =>
        Assert.That(CommandLine.Parse(["--label-sheet", "puls-cook", "meal-for-people", "8f7802b9a72e", "labels", "6"]),
            Is.EqualTo(new LabelSheetRequest("puls-cook", "meal-for-people", "8f7802b9a72e", "labels", 6)));

    [TestCase("0")]
    [TestCase("many")]
    public void LabelSheet_RefusesACountThatIsNotAWholeNumberOfAtLeastOne(string count) =>
        Assert.That(() => CommandLine.Parse(["--label-sheet", "puls-cook", "meal-for-people", "8f7802b9a72e", "labels", count]),
            Throws.InstanceOf<RunnerException>().With.Message.Contains($"'{count}'"));

    [Test]
    public void LabelSheet_RefusesARunIdThatIsNotAnId() =>
        Assert.That(() => CommandLine.Parse(["--label-sheet", "puls-cook", "meal-for-people", "../x", "labels"]),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("'../x'"));

    [TestCase("--calibrate")]
    [TestCase("--calibrate", "puls-cook")]
    [TestCase("--label-sheet", "puls-cook", "meal-for-people")]
    public void BothRefuseMissingArgumentsWithTheUsage(params string[] args) =>
        Assert.That(() => CommandLine.Parse(args), Throws.InstanceOf<RunnerException>().With.Message.Contains("Usage:"));
}
