using LouisAgent.Core.Sessions.Replay;
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

    // F18 step 4a.3: --web and --of. A run with neither records nothing and replays nothing: its web is the instance's default.
    [Test]
    public void ARunWithNoWebFlags_HasNoWebAndNoRecording() =>
        Assert.That(CommandLine.Parse(["run", "stub"]), Is.EqualTo(new RunRequest("stub", 1, null, null)));

    [TestCase("record", WebReplayMode.Record)]
    [TestCase("RECORD", WebReplayMode.Record)]
    [TestCase("off", WebReplayMode.Off)]
    public void WebIsParsedWithTheCaseIgnored(string value, WebReplayMode expected) =>
        Assert.That(CommandLine.Parse(["run", "stub", "--web", value]), Is.EqualTo(new RunRequest("stub", 1, expected, null)));

    [Test]
    public void AReplayNamesItsRecordedRun() =>
        Assert.That(CommandLine.Parse(["run", "stub", "--web", "replay", "--of", "local-20261025-064900"]),
            Is.EqualTo(new RunRequest("stub", 1, WebReplayMode.Replay, "local-20261025-064900")));

    [Test]
    public void FlagsTakeAnyOrder_MixedWithRepeat() =>
        Assert.That(CommandLine.Parse(["run", "stub", "--of", "run_A", "--repeat", "3", "--web", "replay"]),
            Is.EqualTo(new RunRequest("stub", 3, WebReplayMode.Replay, "run_A")));

    [Test]
    public void AFlagGivenTwice_KeepsTheLastValue() =>
        Assert.That(CommandLine.Parse(["run", "stub", "--web", "record", "--web", "off"]), Is.EqualTo(new RunRequest("stub", 1, WebReplayMode.Off, null)));

    [TestCase("sometimes", "'sometimes'")]
    [TestCase("1", "'1'")]
    [TestCase("", "''")]
    public void RefusesAnUnknownWeb_NamingItAndTheSupportedNames(string value, string shown)
    {
        var error = Assert.Throws<RunnerException>(() => CommandLine.Parse(["run", "stub", "--web", value]))!;

        Assert.That(error.Message, Does.Contain(shown).And.Contain("off, record, replay"));
    }

    [Test]
    public void RefusesReplayWithoutAnOf() =>
        Assert.That(() => CommandLine.Parse(["run", "stub", "--web", "replay"]), Throws.InstanceOf<RunnerException>().With.Message.Contains("needs --of"));

    [TestCase("record")]
    [TestCase("off")]
    public void RefusesAnOfThatIsNotAReplay(string web) =>
        Assert.That(() => CommandLine.Parse(["run", "stub", "--web", web, "--of", "run_A"]),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("--of needs --web replay"));

    [Test]
    public void RefusesAnOfWithNoWebAtAll() =>
        Assert.That(() => CommandLine.Parse(["run", "stub", "--of", "run_A"]),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("--of needs --web replay"));

    [TestCase("../x", "'../x'")]
    [TestCase("a:b", "'a:b'")]
    [TestCase("a/b", "'a/b'")]
    [TestCase("", "''")]
    public void RefusesAnOfThatIsNotARunIdsCharacters(string value, string shown)
    {
        var error = Assert.Throws<RunnerException>(() => CommandLine.Parse(["run", "stub", "--web", "replay", "--of", value]))!;

        Assert.That(error.Message, Does.Contain(shown));
    }

    [TestCase("--web")]
    [TestCase("--of")]
    [TestCase("--repeat")]
    public void AFlagWithNothingAfterIt_IsRefused_AsTheFlag(string flag) =>
        Assert.That(() => CommandLine.Parse(["run", "stub", flag]), Throws.InstanceOf<RunnerException>().With.Message.Contains($"'{flag}'"));
}
