using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Runner;
using LouisAgent.Evals.Runner.Arguments;

namespace LouisAgent.Evals.Runner.Tests.Arguments;

/// <summary>
/// The runner's command line: what each way of asking turns into, and every way of asking wrong refused with a
/// message that says why.
/// </summary>
public class CommandLineTests
{
    /// <summary>Asking for the list is one word.</summary>
    [Test]
    public void ListIsParsed() =>
        Assert.That(CommandLine.Parse(["--list"]), Is.EqualTo(new ListRequest()));

    /// <summary>A run needs only its eval's name, and then runs each mode once.</summary>
    [Test]
    public void RunTakesItsNameAndOneRepeatByDefault() =>
        Assert.That(CommandLine.Parse(["run", "stub"]), Is.EqualTo(new RunRequest("stub", 1)));

    /// <summary>A run can ask for more repeats.</summary>
    [Test]
    public void RunTakesItsRepeats() =>
        Assert.That(CommandLine.Parse(["run", "stub", "--repeat", "3"]), Is.EqualTo(new RunRequest("stub", 3)));

    /// <summary>Repeats are kept between one and five, as the API keeps a run's.</summary>
    [TestCase("9", 5)]
    [TestCase("5", 5)]
    [TestCase("0", 1)]
    [TestCase("-2", 1)]
    public void RepeatIsClampedToOneThroughFive(string value, int expected) =>
        Assert.That(CommandLine.Parse(["run", "stub", "--repeat", value]),
            Is.EqualTo(new RunRequest("stub", expected)));

    /// <summary>A repeat that isn't a whole number is refused, and named.</summary>
    [Test]
    public void RefusesARepeatThatIsNotAWholeNumber() =>
        Assert.That(() => CommandLine.Parse(["run", "stub", "--repeat", "two"]),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("'two'"));

    /// <summary>Nothing at all gets the usage.</summary>
    [Test]
    public void RefusesNoArgumentsWithTheUsage() =>
        Assert.That(() => CommandLine.Parse([]), Throws.InstanceOf<RunnerException>().With.Message.Contains("Usage:"));

    /// <summary>A run without its eval's name gets the usage too.</summary>
    [Test]
    public void RefusesARunWithoutAName() =>
        Assert.That(() => CommandLine.Parse(["run"]),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("Usage:"));

    /// <summary>A word the runner doesn't know is refused, and named.</summary>
    [Test]
    public void RefusesAnUnknownArgument() =>
        Assert.That(() => CommandLine.Parse(["frobnicate"]),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("'frobnicate'"));

    /// <summary>
    /// A run with no web flags records nothing and replays nothing: its web is the instance's default.
    /// </summary>
    [Test]
    public void ARunWithNoWebFlags_HasNoWebAndNoRecording() =>
        Assert.That(CommandLine.Parse(["run", "stub"]), Is.EqualTo(new RunRequest("stub", 1, null, null)));

    /// <summary>The web choice is read by its name, whatever the case.</summary>
    [TestCase("record", WebReplayMode.Record)]
    [TestCase("RECORD", WebReplayMode.Record)]
    [TestCase("off", WebReplayMode.Off)]
    public void WebIsParsedWithTheCaseIgnored(string value, WebReplayMode expected) =>
        Assert.That(CommandLine.Parse(["run", "stub", "--web", value]),
            Is.EqualTo(new RunRequest("stub", 1, expected, null)));

    /// <summary>A web replay names the run it replays.</summary>
    [Test]
    public void AReplayNamesItsRecordedRun() =>
        Assert.That(CommandLine.Parse(["run", "stub", "--web", "replay", "--of", "local-20261025-064900"]),
            Is.EqualTo(new RunRequest("stub", 1, WebReplayMode.Replay, "local-20261025-064900")));

    /// <summary>The flags come in any order.</summary>
    [Test]
    public void FlagsTakeAnyOrder_MixedWithRepeat() =>
        Assert.That(CommandLine.Parse(["run", "stub", "--of", "run_A", "--repeat", "3", "--web", "replay"]),
            Is.EqualTo(new RunRequest("stub", 3, WebReplayMode.Replay, "run_A")));

    /// <summary>A flag given twice keeps what it was given last.</summary>
    [Test]
    public void AFlagGivenTwice_KeepsTheLastValue() =>
        Assert.That(CommandLine.Parse(["run", "stub", "--web", "record", "--web", "off"]),
            Is.EqualTo(new RunRequest("stub", 1, WebReplayMode.Off, null)));

    /// <summary><c>--replay</c> replays a run's web and its model's answers too, so it costs nothing.</summary>
    [Test]
    public void Replay_IsTheWebReplayOfARun_WithTheModelsAnswersToo() =>
        Assert.That(CommandLine.Parse(["run", "stub", "--replay", "run_A"]),
            Is.EqualTo(new RunRequest("stub", 1, WebReplayMode.Replay, "run_A", Model: true)));

    /// <summary><c>--replay</c> comes in any order with the repeats.</summary>
    [Test]
    public void Replay_TakesAnyOrder_MixedWithRepeat() =>
        Assert.That(CommandLine.Parse(["run", "stub", "--replay", "local-20261025-064900", "--repeat", "3"]),
            Is.EqualTo(new RunRequest("stub", 3, WebReplayMode.Replay, "local-20261025-064900", Model: true)));

    /// <summary>A web replay without <c>--replay</c> asks the model again.</summary>
    [Test]
    public void TheOtherWebFlags_NeverAskForTheModel() =>
        Assert.That(((RunRequest)CommandLine.Parse(["run", "stub", "--web", "replay", "--of", "run_A"])).Model,
            Is.False);

    /// <summary><c>--replay</c> already names the run and its web, so a web flag beside it is refused.</summary>
    [TestCase("--web", "replay")]
    [TestCase("--web", "record")]
    [TestCase("--of", "run_B")]
    public void RefusesReplayGivenWithWebOrOf_InEitherOrder(string flag, string value)
    {
        Assert.That(() => CommandLine.Parse(["run", "stub", "--replay", "run_A", flag, value]),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("--replay").And.Message
                .Contains("--web or --of"));
        Assert.That(() => CommandLine.Parse(["run", "stub", flag, value, "--replay", "run_A"]),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("--web or --of"));
    }

    /// <summary>A bad run id after <c>--replay</c> is refused in the same words as after <c>--of</c>.</summary>
    [TestCase("")]
    [TestCase("a/b")]
    [TestCase("run A")]
    [TestCase("..")]
    public void RefusesABadRunIdAsReplay_AsOfRefusesOne(string value)
    {
        var error = Assert.Throws<RunnerException>(() => CommandLine.Parse(["run", "stub", "--replay", value]))!;

        Assert.That(error.Message,
            Is.EqualTo(Assert.Throws<RunnerException>(() => CommandLine.Parse(["run", "stub", "--of", value]))!
                .Message));
    }

    /// <summary>The usage says how to replay.</summary>
    [Test]
    public void TheUsageText_NamesReplay()
    {
        Assert.That(Assert.Throws<RunnerException>(() => CommandLine.Parse(["run"]))!.Message,
            Does.Contain("--replay <runId>"));
        Assert.That(Assert.Throws<RunnerException>(() => CommandLine.Parse(["frobnicate"]))!.Message,
            Does.Contain("--replay <runId>"));
    }

    /// <summary>An unknown web choice is refused, named, with the choices there are.</summary>
    [TestCase("sometimes", "'sometimes'")]
    [TestCase("1", "'1'")]
    [TestCase("", "''")]
    public void RefusesAnUnknownWeb_NamingItAndTheSupportedNames(string value, string shown)
    {
        var error = Assert.Throws<RunnerException>(() => CommandLine.Parse(["run", "stub", "--web", value]))!;

        Assert.That(error.Message, Does.Contain(shown).And.Contain("off, record, replay"));
    }

    /// <summary>A web replay needs to know which run it replays.</summary>
    [Test]
    public void RefusesReplayWithoutAnOf() =>
        Assert.That(() => CommandLine.Parse(["run", "stub", "--web", "replay"]),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("needs --of"));

    /// <summary>A run to replay means nothing unless the web is replayed.</summary>
    [TestCase("record")]
    [TestCase("off")]
    public void RefusesAnOfThatIsNotAReplay(string web) =>
        Assert.That(() => CommandLine.Parse(["run", "stub", "--web", web, "--of", "run_A"]),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("--of needs --web replay"));

    /// <summary>The same with no web choice at all.</summary>
    [Test]
    public void RefusesAnOfWithNoWebAtAll() =>
        Assert.That(() => CommandLine.Parse(["run", "stub", "--of", "run_A"]),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("--of needs --web replay"));

    /// <summary>A run id is letters, digits, <c>_</c> and <c>-</c>; anything else is refused, and named.</summary>
    [TestCase("../x", "'../x'")]
    [TestCase("a:b", "'a:b'")]
    [TestCase("a/b", "'a/b'")]
    [TestCase("", "''")]
    public void RefusesAnOfThatIsNotARunIdsCharacters(string value, string shown)
    {
        var error = Assert.Throws<RunnerException>(() =>
            CommandLine.Parse(["run", "stub", "--web", "replay", "--of", value]))!;

        Assert.That(error.Message, Does.Contain(shown));
    }

    /// <summary>A flag at the end with nothing after it is refused, and named.</summary>
    [TestCase("--web")]
    [TestCase("--of")]
    [TestCase("--repeat")]
    public void AFlagWithNothingAfterIt_IsRefused_AsTheFlag(string flag) =>
        Assert.That(() => CommandLine.Parse(["run", "stub", flag]),
            Throws.InstanceOf<RunnerException>().With.Message.Contains($"'{flag}'"));

    /// <summary>A run asks for the dev scenarios unless it says otherwise.</summary>
    [Test]
    public void ARunWithNoSplit_IsTheDevScenarios() =>
        Assert.That(((RunRequest)CommandLine.Parse(["run", "stub"])).Split, Is.EqualTo(RunSplit.Dev));

    /// <summary>A split is read by its name whatever the case, and the old <c>held_out</c> still reads.</summary>
    [TestCase("dev", RunSplit.Dev)]
    [TestCase("held-out", RunSplit.HeldOut)]
    [TestCase("held_out", RunSplit.HeldOut)]
    [TestCase("HeldOut", RunSplit.HeldOut)]
    [TestCase("all", RunSplit.All)]
    [TestCase("ALL", RunSplit.All)]
    public void ASplitIsParsedOnceByItsName(string value, RunSplit expected) =>
        Assert.That(CommandLine.Parse(["run", "stub", "--split", value]),
            Is.EqualTo(new RunRequest("stub", 1, Split: expected)));

    /// <summary>An unknown split, or a number, is refused, named, with the splits there are.</summary>
    [TestCase("tuning")]
    [TestCase("1")]
    [TestCase("2")]
    public void RefusesAnUnknownSplit_AndANumber(string value) =>
        Assert.That(() => CommandLine.Parse(["run", "stub", "--split", value]),
            Throws.InstanceOf<RunnerException>().With.Message.Contains($"'{value}'").And.Message
                .Contains("dev, held-out, all"));

    /// <summary>A split comes with any other flag.</summary>
    [Test]
    public void ASplitComesWithAnyOtherFlag() =>
        Assert.That(CommandLine.Parse(["run", "stub", "--split", "all", "--repeat", "2", "--web", "record"]),
            Is.EqualTo(new RunRequest("stub", 2, WebReplayMode.Record, Split: RunSplit.All)));

    /// <summary>A re-score names the stored run it grades again.</summary>
    [Test]
    public void ARescoreNamesTheRunItGradesAgain() =>
        Assert.That(CommandLine.Parse(["run", "stub", "--rescore", "run_A"]),
            Is.EqualTo(new RunRequest("stub", 1, RescoreOf: "run_A")));

    /// <summary>A re-score opens no session, so a flag that shapes a session or a run is refused beside it.</summary>
    [TestCase("--web", "record")]
    [TestCase("--replay", "run_B")]
    [TestCase("--repeat", "2")]
    [TestCase("--split", "all")]
    public void RefusesARescoreGivenWithAFlagThatShapesARun_InEitherOrder(string flag, string value)
    {
        Assert.That(() => CommandLine.Parse(["run", "stub", "--rescore", "run_A", flag, value]),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("opens no session"));
        Assert.That(() => CommandLine.Parse(["run", "stub", flag, value, "--rescore", "run_A"]),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("opens no session"));
    }

    /// <summary>A re-score's run id is checked like any other.</summary>
    [Test]
    public void RefusesARescoreOfARunIdWithOtherCharacters() =>
        Assert.That(() => CommandLine.Parse(["run", "stub", "--rescore", "run/A"]),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("'run/A'"));
}
