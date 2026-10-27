using System.Text.Json;
using LouisAgent.Api.Admin;
using LouisAgent.Api.Benchmarks.Runs;

namespace LouisAgent.Api.IntegrationTests.Benchmarks.Runs;

// F18 step 6a.5b: a judge check is started like a re-score: the eval runner as a child process with an argument list (ADR 0004), `--calibrate {eval} {grader} --labels -`, and the label
// lines on its stdin. It is the runner's one run at a time (the judge is shared), and its row says what it is, so the Benchmarks page can leave it alone. Written first: StartCalibration
// did not exist.
[Category("Integration")]
public class BenchmarkCalibrationStartIntegrationTests
{
    private static readonly BenchmarkCheck Stub = new BenchmarkCheck("stub", "Stub");

    private static BenchmarkRunner Runner() =>
        new(new BenchmarkCatalog([Stub]), TestPaths.RepoRoot, "http://127.0.0.1:1/", RunnerBuild.Dll);

    [Test]
    public void ACalibrationStartsTheRunnerWithAnArgumentList_AndPipesTheLabels()
    {
        var run = new BenchmarkRun("cal123456789", "stub", 1, calibration: new BenchmarkCalibration("76667d39dcdb", "meal-for-people"), input: "{\"grader\":\"meal-for-people\"}\n");

        var start = Runner().StartInfoFor(Stub, run);

        Assert.That(start.ArgumentList, Is.EqualTo(new[] { RunnerBuild.Dll, "--calibrate", "stub", "meal-for-people", "--labels", "-" }), "no run, no --repeat: a check of the judge, not a run");
        Assert.That(start.RedirectStandardInput, Is.True);
        Assert.That(start.Environment["BENCHMARK_RUN"], Is.EqualTo("cal123456789"));
    }

    [Test]
    public void ACalibrationOfACheckTheRunnerDoesNotList_IsRefused()
    {
        Assert.That(Runner().StartCalibration("unknown", "76667d39dcdb", "meal-for-people", "{}\n"), Is.Null, "no eval of that name, so no judge to check");
    }
}
