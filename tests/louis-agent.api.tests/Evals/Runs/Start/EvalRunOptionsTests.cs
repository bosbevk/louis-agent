using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Evals.Runs;
using LouisAgent.Api.Evals.Runs.Start;
using LouisAgent.Core.Sessions.Replay;

namespace LouisAgent.Api.Tests.Evals.Runs.Start;

/// <summary>
/// The portal passes on how a run treats the web: record it, or replay an earlier run. The choice is parsed once
/// where it comes in, and the eval runner is started with <c>--web record</c> or <c>--replay {runId}</c>.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public class EvalRunOptionsTests
{
    /// <summary>The eval every runner test starts.</summary>
    private static readonly EvalEntry Eval = new EvalEntry(SampleEval.Beta, "Cook");

    /// <summary>Parses the options, checks nothing was refused, and hands them back.</summary>
    private static EvalRunOptions Ok(string? web, string? replay)
    {
        var parsed = EvalRunOptions.Parse(web, replay);
        Assert.That(parsed.Refusal, Is.Null);
        return parsed.Options!;
    }

    /// <summary>Asking for neither, or leaving both blank, means no options.</summary>
    [Test]
    public void NeitherWebNorReplay_IsNoOptions()
    {
        Assert.That(Ok(null, null), Is.EqualTo(EvalRunOptions.None));
        Assert.That(Ok("", " "), Is.EqualTo(EvalRunOptions.None));
    }

    /// <summary>The web mode is read by its name, whatever its case.</summary>
    [TestCase("record", WebReplayMode.Record)]
    [TestCase("Record", WebReplayMode.Record)]
    [TestCase("off", WebReplayMode.Off)]
    public void AWebIsParsedOnceByItsName_CaseIgnored(string web, WebReplayMode expected) =>
        Assert.That(Ok(web, null), Is.EqualTo(new EvalRunOptions(expected, null)));

    /// <summary>A replay names the run it replays.</summary>
    [Test]
    public void AReplayNamesTheRunToReplay() =>
        Assert.That(Ok(null, "run_ab-12"), Is.EqualTo(new EvalRunOptions(null, "run_ab-12")));

    /// <summary>Asking for a web mode and a replay at once is refused.</summary>
    [Test]
    public void BothWebAndReplay_IsRefused() =>
        Assert.That(EvalRunOptions.Parse("record", "run_1").Refusal, Does.Contain("not both"));

    /// <summary>A web mode we don't know is refused, and the refusal lists the ones we do.</summary>
    [TestCase("tuning")]
    [TestCase("1")]
    public void AnUnknownWeb_IsRefused_NamingWhatIsSupported(string web)
    {
        var refusal = EvalRunOptions.Parse(web, null).Refusal;

        Assert.That(refusal, Does.Contain($"'{web}'").And.Contain("off, record"));
    }

    /// <summary>"replay" isn't a web mode, since a replay has to name its run.</summary>
    [Test]
    public void AWebOfReplay_IsRefused_BecauseAReplayNamesItsRun() =>
        Assert.That(EvalRunOptions.Parse("replay", null).Refusal, Does.Contain("replay").And.Contain("run"));

    /// <summary>A replay of something that isn't a run id is refused.</summary>
    [TestCase("run 1")]
    [TestCase("../etc")]
    [TestCase("run:1/x")]
    public void AReplayThatIsNotARunId_IsRefused(string replay) =>
        Assert.That(EvalRunOptions.Parse(null, replay).Refusal, Does.Contain($"'{replay}'"));

    // --- what the runner is started with

    /// <summary>A runner that knows only this eval, so its start can be read without running anything.</summary>
    private static EvalProcess RunnerWith(EvalEntry check) => new(new EvalList([check]),
        Path.GetTempPath(), "http://127.0.0.1:1/", "/app/evals/x.dll");

    /// <summary>A recording run starts the runner with <c>--web record</c>.</summary>
    [Test]
    public void ARecordingEvalStartsWithWebRecord()
    {
        var start = RunnerWith(Eval).StartInfoFor(Eval,
            new RunningEval("run_1", SampleEval.Beta, 1, new EvalRunOptions(WebReplayMode.Record, null)));

        Assert.That(start.ArgumentList,
            Is.EqualTo(new[] { "/app/evals/x.dll", "run", SampleEval.Beta, "--repeat", "1", "--web", "record" }));
    }

    /// <summary>A replaying run starts the runner with <c>--replay</c> and the run it replays.</summary>
    [Test]
    public void AReplayingEvalStartsWithReplayAndTheRunsId()
    {
        var start = RunnerWith(Eval).StartInfoFor(Eval,
            new RunningEval("run_2", SampleEval.Beta, 1, new EvalRunOptions(null, "run_1")));

        Assert.That(start.ArgumentList,
            Is.EqualTo(new[] { "/app/evals/x.dll", "run", SampleEval.Beta, "--repeat", "1", "--replay", "run_1" }));
    }

    /// <summary>A run with no options starts the runner with nothing extra.</summary>
    [Test]
    public void AnEvalWithNoOptions_StartsAsBefore()
    {
        var start = RunnerWith(Eval).StartInfoFor(Eval, new RunningEval("run_3", SampleEval.Beta, 2));

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

        Assert.That(EvalSettings.WithOptions(plain, EvalRunOptions.None), Is.EqualTo(plain));
        Assert.That(EvalSettings.WithOptions(plain, new EvalRunOptions(WebReplayMode.Record, null))["web"],
            Is.EqualTo("record"));
        Assert.That(EvalSettings.WithOptions(plain, new EvalRunOptions(null, "run_1"))["replay_of"],
            Is.EqualTo("run_1"));
    }

    /// <summary>
    /// What a replay's settings say it replayed is read back by the same class that wrote it; a recording, or a run
    /// with no settings, replayed nothing (CL26).
    /// </summary>
    [Test]
    public void TheReplayedRun_IsReadBackFromTheSettings()
    {
        var plain = new Dictionary<string, string> { ["provider"] = "anthropic" };
        var replay = EvalSettings.WithOptions(plain, new EvalRunOptions(null, "run_1"));
        var recording = EvalSettings.WithOptions(plain, new EvalRunOptions(WebReplayMode.Record, null));

        Assert.That(EvalSettings.ReplayOf(replay), Is.EqualTo("run_1"));
        Assert.That(EvalSettings.ReplayOf(recording), Is.Null);
        Assert.That(EvalSettings.ReplayOf(null), Is.Null);
    }
}
