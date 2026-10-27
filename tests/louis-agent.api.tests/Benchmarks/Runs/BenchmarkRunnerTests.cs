using LouisAgent.Api.Benchmarks.Runs;

namespace LouisAgent.Api.Tests.Benchmarks.Runs;

// F27 step 3 (F27-S2), on a stand-in since F18 step 7.4: a check runs as a child process (an argument list, never a shell), one at a time, its lines streamed to whoever listens (CL10: no run
// folder any more). The child is the stand-in for the eval runner (tests/louis-agent.api.standin: a real process on every platform, no Python): the check's id picks what it does, so each test names
// the behaviour it wants by the check it starts. The API starts only evals now, so there is no script and no interpreter to give.
public class BenchmarkRunnerTests
{
    private string _workspace = "";

    private BenchmarkRunner? _runner;

    [SetUp]
    public void SetUp()
    {
        _workspace = Path.Combine(Path.GetTempPath(), "louis-bench-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspace);
    }

    [TearDown]
    public async Task TearDown()
    {
        // A test may leave its child running; the folder can't go while it does.
        if (_runner?.Current is { Status: BenchmarkRunStatus.Running } run)
            await foreach (string _ in _runner.EventsAsync(run.Id))
            {
            }

        Directory.Delete(_workspace, recursive: true);
    }

    [Test]
    public void AnEvalCheckStartsTheRunnerWithItsRunAndRepeat()
    {
        // The argument list and the environment, from the start info, so nothing has to run: dotnet, the runner, its run and repeat.
        var eval = new BenchmarkCheck("stub", "Stub");
        var runner = new BenchmarkRunner(new BenchmarkCatalog([eval]), _workspace, "http://127.0.0.1:1/",
            "/app/evals/x.dll");

        var start = runner.StartInfoFor(eval, new BenchmarkRun("run_1", "stub", 2));

        Assert.That(start.FileName, Is.EqualTo("dotnet"));
        Assert.That(start.ArgumentList, Is.EqualTo(new[] { "/app/evals/x.dll", "run", "stub", "--repeat", "2" }));
        Assert.That((start.Environment["BENCHMARK_RUN"], start.Environment["PULS_API"]),
            Is.EqualTo(("run_1", "http://127.0.0.1:1/")));
    }

    [Test]
    public void NoRunStartsAnInterpreter_OnlyTheEvalHost()
    {
        // F18 step 7.4: the API runs evals and nothing else, so no start names Python or sets its flags.
        var eval = new BenchmarkCheck("stub", "Stub");
        var runner = new BenchmarkRunner(new BenchmarkCatalog([eval]), _workspace, "http://127.0.0.1:1/",
            "/app/evals/x.dll");

        var start = runner.StartInfoFor(eval, new BenchmarkRun("run_1", "stub", 1));

        Assert.That(start.FileName, Is.EqualTo("dotnet"));
        Assert.That(start.Environment.Keys, Has.None.StartsWith("PYTHON"), "no Python flags");
        Assert.That(start.ArgumentList, Has.None.EndsWith(".py"));
    }
}
