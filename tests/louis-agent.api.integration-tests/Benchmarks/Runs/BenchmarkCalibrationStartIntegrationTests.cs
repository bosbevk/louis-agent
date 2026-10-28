using System.Text.Json;
using LouisAgent.Api.Admin;
using LouisAgent.Api.Benchmarks.Runs;

namespace LouisAgent.Api.IntegrationTests.Benchmarks.Runs;

/// <summary>
/// A judge check starts like a re-score: the eval runner as a child process with an argument list, told to calibrate,
/// with the labels piped in on its input. Only one runs at a time, since the judge is shared.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0004-arguments-as-data.md">ADR 0004: arguments as data</seealso>
[Category("Integration")]
public class BenchmarkCalibrationStartIntegrationTests
{
    /// <summary>The one eval the runner knows in these tests.</summary>
    private static readonly BenchmarkCheck Stub = new BenchmarkCheck("stub", "Stub");

    /// <summary>A runner on the built eval runner, pointed at an API address nobody answers.</summary>
    private static BenchmarkRunner Runner() =>
        new(new BenchmarkCatalog([Stub]), TestPaths.RepoRoot, "http://127.0.0.1:1/", RunnerBuild.Dll);

    /// <summary>A judge check gets its eval and grader as separate arguments, and the labels on its input.</summary>
    [Test]
    public void ACalibrationStartsTheRunnerWithAnArgumentList_AndPipesTheLabels()
    {
        var run = new BenchmarkRun("cal123456789", "stub", 1,
            calibration: new BenchmarkCalibration("76667d39dcdb", "meal-for-people"),
            input: "{\"grader\":\"meal-for-people\"}\n");

        var start = Runner().StartInfoFor(Stub, run);

        Assert.That(start.ArgumentList,
            Is.EqualTo(new[] { RunnerBuild.Dll, "--calibrate", "stub", "meal-for-people", "--labels", "-" }),
            "no run, no --repeat: a check of the judge, not a run");
        Assert.That(start.RedirectStandardInput, Is.True);
        Assert.That(start.Environment["BENCHMARK_RUN"], Is.EqualTo("cal123456789"));
    }

    /// <summary>You can't check the judge of an eval the runner doesn't know.</summary>
    [Test]
    public void ACalibrationOfACheckTheRunnerDoesNotList_IsRefused()
    {
        Assert.That(Runner().StartCalibration("unknown", "76667d39dcdb", "meal-for-people", "{}\n"), Is.Null,
            "no eval of that name, so no judge to check");
    }
}
