using LouisAgent.Evals.Runner;
using LouisAgent.Evals.Runner.Arguments;

namespace LouisAgent.Evals.Runner.Tests.Arguments;

// The command line (F18 step 4.1): --list, and run {name} --repeat N, with the repeats clamped as the API's runs are.
// Written first: the parser threw NotImplementedException on the red run.
public class CommandLineTests
{
    [Test]
    public void ListIsParsed() =>
        Assert.That(CommandLine.Parse(["--list"]), Is.EqualTo(new ListRequest()));

    [Test]
    public void RunTakesItsNameAndOneRepeatByDefault() =>
        Assert.That(CommandLine.Parse(["run", "stub"]), Is.EqualTo(new RunRequest("stub", 1)));

    [Test]
    public void RunTakesItsRepeats() =>
        Assert.That(CommandLine.Parse(["run", "stub", "--repeat", "3"]), Is.EqualTo(new RunRequest("stub", 3)));

    [TestCase("9", 5)]
    [TestCase("5", 5)]
    [TestCase("0", 1)]
    [TestCase("-2", 1)]
    public void RepeatIsClampedToOneThroughFive(string value, int expected) =>
        Assert.That(CommandLine.Parse(["run", "stub", "--repeat", value]), Is.EqualTo(new RunRequest("stub", expected)));

    [Test]
    public void RefusesARepeatThatIsNotAWholeNumber() =>
        Assert.That(() => CommandLine.Parse(["run", "stub", "--repeat", "two"]),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("'two'"));

    [Test]
    public void RefusesNoArgumentsWithTheUsage() =>
        Assert.That(() => CommandLine.Parse([]), Throws.InstanceOf<RunnerException>().With.Message.Contains("Usage:"));

    [Test]
    public void RefusesARunWithoutAName() =>
        Assert.That(() => CommandLine.Parse(["run"]), Throws.InstanceOf<RunnerException>().With.Message.Contains("Usage:"));

    [Test]
    public void RefusesAnUnknownArgument() =>
        Assert.That(() => CommandLine.Parse(["frobnicate"]),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("'frobnicate'"));
}
