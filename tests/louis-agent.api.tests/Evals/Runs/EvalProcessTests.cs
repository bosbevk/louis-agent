using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Evals.Runs;
using LouisAgent.Api.Evals.Runs.Kinds;

namespace LouisAgent.Api.Tests.Evals.Runs;

/// <summary>
/// An eval runs as a child process, from an argument list and never through a shell, one at a time. The API starts
/// only evals now, through the eval runner, so there's no script and no interpreter to hand it.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0004-arguments-as-data.md">ADR 0004: arguments as data</seealso>
public class EvalProcessTests
{
    /// <summary>The test's own working folder.</summary>
    private string _workspace = "";

    /// <summary>Gives each test an empty folder of its own.</summary>
    [SetUp]
    public void SetUp()
    {
        _workspace = Path.Combine(Path.GetTempPath(), "louis-bench-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspace);
    }

    /// <summary>Removes the folder. These tests only build a start, so no child is ever left running.</summary>
    [TearDown]
    public void TearDown() => Directory.Delete(_workspace, recursive: true);

    /// <summary>An eval starts the runner through dotnet, with its run id and repeat count.</summary>
    [Test]
    public void AnEvalCheckStartsTheRunnerWithItsRunAndRepeat()
    {
        // The argument list and the environment come from the start info, so nothing has to run.
        var eval = new EvalEntry("stub", "Stub");
        var runner = new EvalProcess(new EvalList([eval]), _workspace, "http://127.0.0.1:1/",
            "/app/evals/x.dll");

        var start = runner.StartInfoFor(eval, new RunningEval("run_1", "stub", 2));

        Assert.That(start.FileName, Is.EqualTo("dotnet"));
        Assert.That(start.ArgumentList, Is.EqualTo(new[] { "/app/evals/x.dll", "run", "stub", "--repeat", "2" }));
        Assert.That((start.Environment["BENCHMARK_RUN"], start.Environment["AGENT_API"]),
            Is.EqualTo(("run_1", "http://127.0.0.1:1/")));
        Assert.That(start.Environment.ContainsKey("PULS_API"), Is.False, "the API names no product (ADR 0037)");
    }

    /// <summary>No run starts Python or sets its flags: only the eval host runs.</summary>
    [Test]
    public void NoRunStartsAnInterpreter_OnlyTheEvalHost()
    {
        var eval = new EvalEntry("stub", "Stub");
        var runner = new EvalProcess(new EvalList([eval]), _workspace, "http://127.0.0.1:1/",
            "/app/evals/x.dll");

        var start = runner.StartInfoFor(eval, new RunningEval("run_1", "stub", 1));

        Assert.That(start.FileName, Is.EqualTo("dotnet"));
        Assert.That(start.Environment.Keys, Has.None.StartsWith("PYTHON"), "no Python flags");
        Assert.That(start.ArgumentList, Has.None.EndsWith(".py"));
    }
}
