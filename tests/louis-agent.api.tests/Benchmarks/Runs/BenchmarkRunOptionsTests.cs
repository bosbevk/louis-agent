using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Tests.Endpoints.Routing;
using LouisAgent.Core.Sessions.Replay;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.Tests.Benchmarks.Runs;

// F18 step 5.5: the portal passes the web on. A run asks `{"repeat": 1, "web": "record"}` or `{"repeat": 1, "replay": "{runId}"}`, parsed once
// (ADR 0024); an eval's runner gets `--web record` or `--replay {runId}`; a Python check refuses both. Written first: the options threw
// NotImplementedException on the red run.
public class BenchmarkRunOptionsTests
{
    private static readonly BenchmarkCheck Eval = BenchmarkCheck.ForEval("puls-cook", "Cook");
    private static readonly BenchmarkCheck Script = new("cook", "Kook", "benchmarks/cook/run_check.py");

    private static BenchmarkRunOptions Ok(string? web, string? replay, BenchmarkCheck check)
    {
        var parsed = BenchmarkRunOptions.Parse(web, replay, check);
        Assert.That(parsed.Refusal, Is.Null);
        return parsed.Options!;
    }

    [Test]
    public void NeitherWebNorReplay_IsNoOptions_ForAnyCheck()
    {
        Assert.That(Ok(null, null, Eval), Is.EqualTo(BenchmarkRunOptions.None));
        Assert.That(Ok("", " ", Script), Is.EqualTo(BenchmarkRunOptions.None));
    }

    [TestCase("record", WebReplayMode.Record)]
    [TestCase("Record", WebReplayMode.Record)]
    [TestCase("off", WebReplayMode.Off)]
    public void AWebIsParsedOnceByItsName_CaseIgnored(string web, WebReplayMode expected) =>
        Assert.That(Ok(web, null, Eval), Is.EqualTo(new BenchmarkRunOptions(expected, null)));

    [Test]
    public void AReplayNamesTheRunToReplay() =>
        Assert.That(Ok(null, "run_ab-12", Eval), Is.EqualTo(new BenchmarkRunOptions(null, "run_ab-12")));

    [Test]
    public void BothWebAndReplay_IsRefused() =>
        Assert.That(BenchmarkRunOptions.Parse("record", "run_1", Eval).Refusal, Does.Contain("not both"));

    [TestCase("tuning")]
    [TestCase("1")]
    public void AnUnknownWeb_IsRefused_NamingWhatIsSupported(string web)
    {
        var refusal = BenchmarkRunOptions.Parse(web, null, Eval).Refusal;

        Assert.That(refusal, Does.Contain($"'{web}'").And.Contain("off, record"));
    }

    [Test]
    public void AWebOfReplay_IsRefused_BecauseAReplayNamesItsRun() =>
        Assert.That(BenchmarkRunOptions.Parse("replay", null, Eval).Refusal, Does.Contain("replay").And.Contain("run"));

    [TestCase("run 1")]
    [TestCase("../etc")]
    [TestCase("run:1/x")]
    public void AReplayThatIsNotARunId_IsRefused(string replay) =>
        Assert.That(BenchmarkRunOptions.Parse(null, replay, Eval).Refusal, Does.Contain($"'{replay}'"));

    [TestCase("record", null)]
    [TestCase(null, "run_1")]
    [TestCase("off", null)]
    public void APythonCheck_RefusesBoth(string? web, string? replay)
    {
        var refusal = BenchmarkRunOptions.Parse(web, replay, Script).Refusal;

        Assert.That(refusal, Does.Contain("cook").And.Contain("script"));
    }

    // --- what the runner is started with

    private static BenchmarkRunner RunnerWith(BenchmarkCheck check) => new(new BenchmarkCatalog([check]), Path.GetTempPath(), "http://127.0.0.1:1/", "python3", "/app/evals/x.dll");

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

    [Test]
    public void AScriptIsNeverStartedWithTheWeb()
    {
        // The parser refuses it before a run; the runner holds the line as well, so a script never gets a flag it would not know.
        var start = RunnerWith(Script).StartInfoFor(Script, new BenchmarkRun("run_4", "cook", 1, new BenchmarkRunOptions(WebReplayMode.Record, null)));

        Assert.That(start.ArgumentList, Does.Not.Contain("--web"));
    }

    // --- the route

    [Test]
    public async Task APythonCheckStartedWithTheWeb_Is400_BeforeAnythingElseIsAsked()
    {
        // The one script check the tests give the catalogue (the default has none since F18 step 7.3; step 7.4 removes the script path altogether).
        await using var app = ApiTestHost.Build(adminKey: "admin-secret", store: new InMemoryBenchmarkStore(), catalog: new BenchmarkCatalog([Script]));
        await app.StartAsync();
        var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/admin/benchmarks/cook/runs")
        {
            Content = JsonContent.Create(new { repeat = 1, web = "record" }),
        };
        request.Headers.Add("x-api-key", "admin-secret");

        using var response = await client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.BadRequest));
        Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain("script"));
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
