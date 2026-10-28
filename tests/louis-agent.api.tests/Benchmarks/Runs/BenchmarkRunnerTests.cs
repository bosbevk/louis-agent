using LouisAgent.Api.Benchmarks.Runs;

namespace LouisAgent.Api.Tests.Benchmarks.Runs;

/// <summary>
/// An eval runs as a child process, from an argument list and never through a shell, one at a time. The API starts
/// only evals now, through the eval runner, so there's no script and no interpreter to hand it.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0004-arguments-as-data.md">ADR 0004: arguments as data</seealso>
public class BenchmarkRunnerTests
{
    /// <summary>The test's own working folder.</summary>
    private string _workspace = "";

    /// <summary>The runner a test started, if any, so tear-down can wait for its child.</summary>
    private BenchmarkRunner? _runner;

    /// <summary>Gives each test an empty folder of its own.</summary>
    [SetUp]
    public void SetUp()
    {
        _workspace = Path.Combine(Path.GetTempPath(), "louis-bench-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspace);
    }

    /// <summary>Waits for any child still running, then removes the folder.</summary>
    [TearDown]
    public async Task TearDown()
    {
        // A test may leave its child running; the folder can't go while it does, so read its lines to the end.
        if (_runner?.Current is { Status: BenchmarkRunStatus.Running } run)
            await foreach (string _ in _runner.EventsAsync(run.Id))
            {
            }

        Directory.Delete(_workspace, recursive: true);
    }

    /// <summary>An eval starts the runner through dotnet, with its run id and repeat count.</summary>
    [Test]
    public void AnEvalCheckStartsTheRunnerWithItsRunAndRepeat()
    {
        // The argument list and the environment come from the start info, so nothing has to run.
        var eval = new BenchmarkCheck("stub", "Stub");
        var runner = new BenchmarkRunner(new BenchmarkCatalog([eval]), _workspace, "http://127.0.0.1:1/",
            "/app/evals/x.dll");

        var start = runner.StartInfoFor(eval, new BenchmarkRun("run_1", "stub", 2));

        Assert.That(start.FileName, Is.EqualTo("dotnet"));
        Assert.That(start.ArgumentList, Is.EqualTo(new[] { "/app/evals/x.dll", "run", "stub", "--repeat", "2" }));
        Assert.That((start.Environment["BENCHMARK_RUN"], start.Environment["PULS_API"]),
            Is.EqualTo(("run_1", "http://127.0.0.1:1/")));
    }

    /// <summary>No run starts Python or sets its flags: only the eval host runs.</summary>
    [Test]
    public void NoRunStartsAnInterpreter_OnlyTheEvalHost()
    {
        var eval = new BenchmarkCheck("stub", "Stub");
        var runner = new BenchmarkRunner(new BenchmarkCatalog([eval]), _workspace, "http://127.0.0.1:1/",
            "/app/evals/x.dll");

        var start = runner.StartInfoFor(eval, new BenchmarkRun("run_1", "stub", 1));

        Assert.That(start.FileName, Is.EqualTo("dotnet"));
        Assert.That(start.Environment.Keys, Has.None.StartsWith("PYTHON"), "no Python flags");
        Assert.That(start.ArgumentList, Has.None.EndsWith(".py"));
    }
}
