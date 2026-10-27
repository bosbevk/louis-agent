using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Core.Sessions.Replay;

namespace LouisAgent.Api.Tests.Benchmarks.Runs;

// F18 step 5.5: the portal passes the web on. A run asks `{"repeat": 1, "web": "record"}` or `{"repeat": 1, "replay": "{runId}"}`, parsed once
// (ADR 0024); an eval's runner gets `--web record` or `--replay {runId}` (every check is an eval since F18 step 7.4). Written first: the options threw
// NotImplementedException on the red run.
public class BenchmarkRunOptionsTests
{
    private static readonly BenchmarkCheck Eval = BenchmarkCheck.ForEval("puls-cook", "Cook");

    private static BenchmarkRunOptions Ok(string? web, string? replay)
    {
        var parsed = BenchmarkRunOptions.Parse(web, replay);
        Assert.That(parsed.Refusal, Is.Null);
        return parsed.Options!;
    }

    [Test]
    public void NeitherWebNorReplay_IsNoOptions()
    {
        Assert.That(Ok(null, null), Is.EqualTo(BenchmarkRunOptions.None));
        Assert.That(Ok("", " "), Is.EqualTo(BenchmarkRunOptions.None));
    }

    [TestCase("record", WebReplayMode.Record)]
    [TestCase("Record", WebReplayMode.Record)]
    [TestCase("off", WebReplayMode.Off)]
    public void AWebIsParsedOnceByItsName_CaseIgnored(string web, WebReplayMode expected) =>
        Assert.That(Ok(web, null), Is.EqualTo(new BenchmarkRunOptions(expected, null)));

    [Test]
    public void AReplayNamesTheRunToReplay() =>
        Assert.That(Ok(null, "run_ab-12"), Is.EqualTo(new BenchmarkRunOptions(null, "run_ab-12")));

    [Test]
    public void BothWebAndReplay_IsRefused() =>
        Assert.That(BenchmarkRunOptions.Parse("record", "run_1").Refusal, Does.Contain("not both"));

    [TestCase("tuning")]
    [TestCase("1")]
    public void AnUnknownWeb_IsRefused_NamingWhatIsSupported(string web)
    {
        var refusal = BenchmarkRunOptions.Parse(web, null).Refusal;

        Assert.That(refusal, Does.Contain($"'{web}'").And.Contain("off, record"));
    }

    [Test]
    public void AWebOfReplay_IsRefused_BecauseAReplayNamesItsRun() =>
        Assert.That(BenchmarkRunOptions.Parse("replay", null).Refusal, Does.Contain("replay").And.Contain("run"));

    [TestCase("run 1")]
    [TestCase("../etc")]
    [TestCase("run:1/x")]
    public void AReplayThatIsNotARunId_IsRefused(string replay) =>
        Assert.That(BenchmarkRunOptions.Parse(null, replay).Refusal, Does.Contain($"'{replay}'"));

    // --- what the runner is started with

    private static BenchmarkRunner RunnerWith(BenchmarkCheck check) => new(new BenchmarkCatalog([check]), Path.GetTempPath(), "http://127.0.0.1:1/", "/app/evals/x.dll");

    [Test]
    public void ARecordingEvalStartsWithWebRecord()
    {
        var start = RunnerWith(Eval).StartInfoFor(Eval, new BenchmarkRun("run_1", "puls-cook", 1, new BenchmarkRunOptions(WebReplayMode.Record, null)));

        Assert.That(start.ArgumentList, Is.EqualTo(new[] { "/app/evals/x.dll", "run", "puls-cook", "--repeat", "1", "--web", "record" }));
    }

    [Test]
    public void AReplayingEvalStartsWithReplayAndTheRunsId()
    {
        var start = RunnerWith(Eval).StartInfoFor(Eval, new BenchmarkRun("run_2", "puls-cook", 1, new BenchmarkRunOptions(null, "run_1")));

        Assert.That(start.ArgumentList, Is.EqualTo(new[] { "/app/evals/x.dll", "run", "puls-cook", "--repeat", "1", "--replay", "run_1" }));
    }

    [Test]
    public void AnEvalWithNoOptions_StartsAsBefore()
    {
        var start = RunnerWith(Eval).StartInfoFor(Eval, new BenchmarkRun("run_3", "puls-cook", 2));

        Assert.That(start.ArgumentList, Is.EqualTo(new[] { "/app/evals/x.dll", "run", "puls-cook", "--repeat", "2" }));
    }

    // --- what a stored run keeps

    [Test]
    public void ARunsSettings_SayHowItsWebWasHandled()
    {
        var plain = new Dictionary<string, string> { ["provider"] = "anthropic" };

        Assert.That(BenchmarkSettings.WithOptions(plain, BenchmarkRunOptions.None), Is.EqualTo(plain));
        Assert.That(BenchmarkSettings.WithOptions(plain, new BenchmarkRunOptions(WebReplayMode.Record, null))["web"], Is.EqualTo("record"));
        Assert.That(BenchmarkSettings.WithOptions(plain, new BenchmarkRunOptions(null, "run_1"))["replay_of"], Is.EqualTo("run_1"));
    }
}
