using LouisAgent.Testing;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Core.Sessions.Replay;

namespace LouisAgent.Api.Tests.Benchmarks.Runs;

/// <summary>
/// The portal passes on how a run treats the web: record it, or replay an earlier run. The choice is parsed once
/// where it comes in, and the eval runner is started with <c>--web record</c> or <c>--replay {runId}</c>.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public class BenchmarkRunOptionsTests
{
    /// <summary>The eval every runner test starts.</summary>
    private static readonly BenchmarkCheck Eval = new BenchmarkCheck(SampleEval.Beta, "Cook");

    /// <summary>Parses the options, checks nothing was refused, and hands them back.</summary>
    private static BenchmarkRunOptions Ok(string? web, string? replay)
    {
        var parsed = BenchmarkRunOptions.Parse(web, replay);
        Assert.That(parsed.Refusal, Is.Null);
        return parsed.Options!;
    }

    /// <summary>Asking for neither, or leaving both blank, means no options.</summary>
    [Test]
    public void NeitherWebNorReplay_IsNoOptions()
    {
        Assert.That(Ok(null, null), Is.EqualTo(BenchmarkRunOptions.None));
        Assert.That(Ok("", " "), Is.EqualTo(BenchmarkRunOptions.None));
    }

    /// <summary>The web mode is read by its name, whatever its case.</summary>
    [TestCase("record", WebReplayMode.Record)]
    [TestCase("Record", WebReplayMode.Record)]
    [TestCase("off", WebReplayMode.Off)]
    public void AWebIsParsedOnceByItsName_CaseIgnored(string web, WebReplayMode expected) =>
        Assert.That(Ok(web, null), Is.EqualTo(new BenchmarkRunOptions(expected, null)));

    /// <summary>A replay names the run it replays.</summary>
    [Test]
    public void AReplayNamesTheRunToReplay() =>
        Assert.That(Ok(null, "run_ab-12"), Is.EqualTo(new BenchmarkRunOptions(null, "run_ab-12")));

    /// <summary>Asking for a web mode and a replay at once is refused.</summary>
    [Test]
    public void BothWebAndReplay_IsRefused() =>
        Assert.That(BenchmarkRunOptions.Parse("record", "run_1").Refusal, Does.Contain("not both"));

    /// <summary>A web mode we don't know is refused, and the refusal lists the ones we do.</summary>
    [TestCase("tuning")]
    [TestCase("1")]
    public void AnUnknownWeb_IsRefused_NamingWhatIsSupported(string web)
    {
        var refusal = BenchmarkRunOptions.Parse(web, null).Refusal;

        Assert.That(refusal, Does.Contain($"'{web}'").And.Contain("off, record"));
    }

    /// <summary>"replay" isn't a web mode, since a replay has to name its run.</summary>
    [Test]
    public void AWebOfReplay_IsRefused_BecauseAReplayNamesItsRun() =>
        Assert.That(BenchmarkRunOptions.Parse("replay", null).Refusal, Does.Contain("replay").And.Contain("run"));

    /// <summary>A replay of something that isn't a run id is refused.</summary>
    [TestCase("run 1")]
    [TestCase("../etc")]
    [TestCase("run:1/x")]
    public void AReplayThatIsNotARunId_IsRefused(string replay) =>
        Assert.That(BenchmarkRunOptions.Parse(null, replay).Refusal, Does.Contain($"'{replay}'"));

    // --- what the runner is started with

    /// <summary>A runner that knows only this eval, so its start can be read without running anything.</summary>
    private static BenchmarkRunner RunnerWith(BenchmarkCheck check) => new(new BenchmarkCatalog([check]),
        Path.GetTempPath(), "http://127.0.0.1:1/", "/app/evals/x.dll");

    /// <summary>A recording run starts the runner with <c>--web record</c>.</summary>
    [Test]
    public void ARecordingEvalStartsWithWebRecord()
    {
        var start = RunnerWith(Eval).StartInfoFor(Eval,
            new BenchmarkRun("run_1", SampleEval.Beta, 1, new BenchmarkRunOptions(WebReplayMode.Record, null)));

        Assert.That(start.ArgumentList,
            Is.EqualTo(new[] { "/app/evals/x.dll", "run", SampleEval.Beta, "--repeat", "1", "--web", "record" }));
    }

    /// <summary>A replaying run starts the runner with <c>--replay</c> and the run it replays.</summary>
    [Test]
    public void AReplayingEvalStartsWithReplayAndTheRunsId()
    {
        var start = RunnerWith(Eval).StartInfoFor(Eval,
            new BenchmarkRun("run_2", SampleEval.Beta, 1, new BenchmarkRunOptions(null, "run_1")));

        Assert.That(start.ArgumentList,
            Is.EqualTo(new[] { "/app/evals/x.dll", "run", SampleEval.Beta, "--repeat", "1", "--replay", "run_1" }));
    }

    /// <summary>A run with no options starts the runner with nothing extra.</summary>
    [Test]
    public void AnEvalWithNoOptions_StartsAsBefore()
    {
        var start = RunnerWith(Eval).StartInfoFor(Eval, new BenchmarkRun("run_3", SampleEval.Beta, 2));

        Assert.That(start.ArgumentList,
            Is.EqualTo(new[] { "/app/evals/x.dll", "run", SampleEval.Beta, "--repeat", "2" }));
    }

    // --- what a stored run keeps

    /// <summary>
    /// A stored run's settings say how its web was handled: nothing extra with no options, the web mode, or the run
    /// it replayed.
    /// </summary>
    [Test]
    public void ARunsSettings_SayHowItsWebWasHandled()
    {
        var plain = new Dictionary<string, string> { ["provider"] = "anthropic" };

        Assert.That(BenchmarkSettings.WithOptions(plain, BenchmarkRunOptions.None), Is.EqualTo(plain));
        Assert.That(BenchmarkSettings.WithOptions(plain, new BenchmarkRunOptions(WebReplayMode.Record, null))["web"],
            Is.EqualTo("record"));
        Assert.That(BenchmarkSettings.WithOptions(plain, new BenchmarkRunOptions(null, "run_1"))["replay_of"],
            Is.EqualTo("run_1"));
    }
}
