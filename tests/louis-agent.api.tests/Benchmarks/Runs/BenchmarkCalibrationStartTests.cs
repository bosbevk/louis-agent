using System.Text.Json;
using LouisAgent.Api.Admin;
using LouisAgent.Api.Benchmarks.Runs;

namespace LouisAgent.Api.Tests.Benchmarks.Runs;

// F18 step 6a.5b: a judge check is started like a re-score: the eval runner as a child process with an argument list (ADR 0004), `--calibrate {eval} {grader} --labels -`, and the label
// lines on its stdin. It is the runner's one run at a time (the judge is shared), and its row says what it is, so the Benchmarks page can leave it alone. Written first: StartCalibration
// did not exist.
public class BenchmarkCalibrationStartTests
{
    private static readonly BenchmarkCheck Stub = BenchmarkCheck.ForEval("stub", "Stub");

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

    [Test]
    public void TheRow_SaysWhatRunItChecksAndWhichJudge_AndItsLastLine()
    {
        var run = new BenchmarkRun("cal123456789", "stub", 1, calibration: new BenchmarkCalibration("76667d39dcdb", "meal-for-people"), input: "{}\n");
        run.Add("Agreement: 36 of 36");

        var row = JsonDocument.Parse(JsonSerializer.Serialize(AdminRows.Row(run))).RootElement;
        var ordinary = JsonDocument.Parse(JsonSerializer.Serialize(AdminRows.Row(new BenchmarkRun("run123456789", "stub", 1)))).RootElement;

        Assert.That((row.GetProperty("calibration_of").GetString(), row.GetProperty("grader").GetString(), row.GetProperty("last_line").GetString()),
            Is.EqualTo(("76667d39dcdb", "meal-for-people", "Agreement: 36 of 36")));
        Assert.That(ordinary.GetProperty("calibration_of").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(ordinary.GetProperty("last_line").ValueKind, Is.EqualTo(JsonValueKind.Null), "an ordinary run's lines are streamed, not summarised");
    }
}
