using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Evals.Runs;
using LouisAgent.Api.Evals.Runs.Kinds;

namespace LouisAgent.Api.IntegrationTests.Evals.Runs;

/// <summary>
/// The runner starts an eval as a child process with an argument list (never a shell), one at a time, and streams its
/// lines to whoever listens. The child here is a stand-in for the eval runner, a real process on every platform:
/// the eval's name picks what it does, so each test names the behaviour it wants by the eval it starts.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0004-arguments-as-data.md">ADR 0004: arguments as data</seealso>
[Category("Integration")]
public class EvalProcessIntegrationTests
{
    /// <summary>A fresh folder per test for the runner to work in.</summary>
    private string _workspace = "";

    /// <summary>The runner the test made, so the tear-down can wait for a child left running.</summary>
    private EvalProcess? _runner;

    /// <summary>Makes the test's own working folder.</summary>
    [SetUp]
    public void SetUp()
    {
        _workspace = Path.Combine(Path.GetTempPath(), "louis-bench-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspace);
    }

    /// <summary>Waits for any child still running, then deletes the test's folder.</summary>
    [TearDown]
    public async Task TearDown()
    {
        // A test may leave its child running; the folder can't go while it does.
        if (_runner?.Current is { Status: EvalRunStatus.Running } run)
            await foreach (string _ in _runner.EventsAsync(run.Id))
            {
            }

        Directory.Delete(_workspace, recursive: true);
    }

    /// <summary>A runner whose evals are the stand-in's behaviours, each under its own name.</summary>
    private EvalProcess Runner(string pulsApi = "http://x/")
    {
        string[] behaviours = ["args", "echo", "run-id", "slow", "late", "fail"];
        var catalog = new EvalList([.. behaviours.Select(id => new EvalEntry(id, id))]);
        return _runner = new EvalProcess(catalog, _workspace, pulsApi, StandInBuild.Dll);
    }

    /// <summary>Every line the run prints, once it has ended.</summary>
    private static async Task<List<string>> LinesOf(EvalProcess runner, RunningEval run)
    {
        var lines = new List<string>();
        await foreach (string line in runner.EventsAsync(run.Id)) lines.Add(line);
        return lines;
    }

    /// <summary>The child gets the eval and the repeats as separate arguments, and no output folder.</summary>
    [Test]
    public async Task TheRunner_GetsItsRunAndRepeat_AsAnArgumentList_NoOutputFolder()
    {
        // A run writes no folder of its own; its results go to the store.
        var runner = Runner();

        RunningEval run = runner.Start("args", repeat: 2)!;

        Assert.That(await LinesOf(runner, run), Is.EqualTo(new[] { "run", "args", "--repeat", "2" }),
            "an argument list, never a shell string");
    }

    /// <summary>A run passes on what the child prints, ends done, and stays readable afterwards.</summary>
    [Test]
    public async Task ARun_StreamsTheChildsLines_ThenEndsDone()
    {
        var runner = Runner("http://127.0.0.1:8080/");

        RunningEval run = runner.Start("echo", repeat: 2)!;
        var lines = await LinesOf(runner, run);

        Assert.That(lines, Is.EqualTo(new[] { "repeat 2", "api http://127.0.0.1:8080/" }));
        Assert.That((run.Status, run.ExitCode), Is.EqualTo((EvalRunStatus.Done, (int?)0)));
        Assert.That(runner.Current, Is.SameAs(run), "the last run stays readable");
    }

    /// <summary>A second run is refused while one runs, and allowed again once it ends.</summary>
    [Test]
    public async Task OnlyOneRun_AtATime()
    {
        // The containers are shared: a second run would skew the first's ledger and its timings.
        var runner = Runner();

        RunningEval first = runner.Start("slow", 1)!;
        Assert.That(runner.Start("slow", 1), Is.Null, "refused while one runs");

        // Let the first one finish.
        await foreach (string _ in runner.EventsAsync(first.Id))
        {
        }

        Assert.That(runner.Start("args", 1), Is.Not.Null, "free again when it ends");
    }

    /// <summary>An eval the runner doesn't know starts nothing.</summary>
    [Test]
    public void AnUnknownCheck_IsRefused_WithoutAProcess()
    {
        var runner = Runner();

        Assert.That(runner.Start("nope", 1), Is.Null);
        Assert.That(runner.Current, Is.Null);
    }

    /// <summary>A child that fails ends the run failed, with its exit code and its error output.</summary>
    [Test]
    public async Task AChildThatFails_EndsFailed_WithItsExitCode_AndItsStderr()
    {
        var runner = Runner();

        RunningEval run = runner.Start("fail", 1)!;
        var lines = await LinesOf(runner, run);

        Assert.That(lines, Does.Contain("boom"));
        Assert.That((run.Status, run.ExitCode), Is.EqualTo((EvalRunStatus.Failed, (int?)3)));
    }

    /// <summary>Asking for too many repeats gets the most the runner allows.</summary>
    [Test]
    public void Repeats_AreClampedToTheCap()
    {
        var runner = Runner();

        Assert.That(runner.Start("args", 50)!.Repeat, Is.EqualTo(EvalProcess.MaxRepeat));
    }

    /// <summary>Someone who starts listening late still gets every line, the early ones first.</summary>
    [Test]
    public async Task ALateListener_GetsTheLinesSoFar_ThenTheRest()
    {
        // The page may open after the run started (a reload): it replays what was printed, then follows live.
        var runner = Runner();

        RunningEval run = runner.Start("late", 1)!;
        await Task.Delay(400);

        Assert.That(await LinesOf(runner, run), Is.EqualTo(new[] { "one", "two" }));
    }

    /// <summary>The child is told the run's id, so it can tag its sessions with it.</summary>
    [Test]
    public async Task TheRunner_GetsItsRunId_ToTagItsSessions()
    {
        // The runner tags its sessions {BENCHMARK_RUN}:{mode}:{repeat}, so the ledger's rows find the run.
        var runner = Runner();

        RunningEval run = runner.Start("run-id", repeat: 1)!;

        Assert.That(await LinesOf(runner, run), Is.EqualTo(new[] { run.Id }));
    }
}
