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
            await foreach (string _ in _runner.EventsAsync(run.Id)) { }
        Directory.Delete(_workspace, recursive: true);
    }

    /// <summary>A runner whose checks are the stand-in's behaviours, each an eval of that name.</summary>
    private BenchmarkRunner Runner(string pulsApi = "http://x/")
    {
        string[] behaviours = ["args", "echo", "run-id", "slow", "late", "fail"];
        var catalog = new BenchmarkCatalog([.. behaviours.Select(id => new BenchmarkCheck(id, id))]);
        return _runner = new BenchmarkRunner(catalog, _workspace, pulsApi, StandInBuild.Dll);
    }

    private static async Task<List<string>> LinesOf(BenchmarkRunner runner, BenchmarkRun run)
    {
        var lines = new List<string>();
        await foreach (string line in runner.EventsAsync(run.Id)) lines.Add(line);
        return lines;
    }

    [Test]
    public async Task TheRunner_GetsItsRunAndRepeat_AsAnArgumentList_NoOutputFolder()
    {
        // CL10: a run writes no folder under samples/; where its results go (puls_benchmarks) is the later feature's.
        var runner = Runner();

        BenchmarkRun run = runner.Start("args", repeat: 2)!;

        Assert.That(await LinesOf(runner, run), Is.EqualTo(new[] { "run", "args", "--repeat", "2" }), "an argument list, never a shell string");
    }

    [Test]
    public async Task ARun_StreamsTheChildsLines_ThenEndsDone()
    {
        var runner = Runner("http://127.0.0.1:8080/");

        BenchmarkRun run = runner.Start("echo", repeat: 2)!;
        var lines = await LinesOf(runner, run);

        Assert.That(lines, Is.EqualTo(new[] { "repeat 2", "api http://127.0.0.1:8080/" }));
        Assert.That((run.Status, run.ExitCode), Is.EqualTo((BenchmarkRunStatus.Done, (int?)0)));
        Assert.That(runner.Current, Is.SameAs(run), "the last run stays readable");
    }

    [Test]
    public async Task OnlyOneRun_AtATime()
    {
        // The containers are shared: a second run would skew the first's ledger and its timings.
        var runner = Runner();

        BenchmarkRun first = runner.Start("slow", 1)!;
        Assert.That(runner.Start("slow", 1), Is.Null, "refused while one runs");

        await foreach (string _ in runner.EventsAsync(first.Id)) { }
        Assert.That(runner.Start("args", 1), Is.Not.Null, "free again when it ends");
    }

    [Test]
    public void AnUnknownCheck_IsRefused_WithoutAProcess()
    {
        var runner = Runner();

        Assert.That(runner.Start("nope", 1), Is.Null);
        Assert.That(runner.Current, Is.Null);
    }

    [Test]
    public async Task AChildThatFails_EndsFailed_WithItsExitCode_AndItsStderr()
    {
        var runner = Runner();

        BenchmarkRun run = runner.Start("fail", 1)!;
        var lines = await LinesOf(runner, run);

        Assert.That(lines, Does.Contain("boom"));
        Assert.That((run.Status, run.ExitCode), Is.EqualTo((BenchmarkRunStatus.Failed, (int?)3)));
    }

    [Test]
    public void Repeats_AreClampedToTheCap()
    {
        var runner = Runner();

        Assert.That(runner.Start("args", 50)!.Repeat, Is.EqualTo(BenchmarkRunner.MaxRepeat));
    }

    [Test]
    public async Task ALateListener_GetsTheLinesSoFar_ThenTheRest()
    {
        // The page may open after the run started (a reload): it replays what was printed, then follows live.
        var runner = Runner();

        BenchmarkRun run = runner.Start("late", 1)!;
        await Task.Delay(400);

        Assert.That(await LinesOf(runner, run), Is.EqualTo(new[] { "one", "two" }));
    }

    [Test]
    public async Task TheRunner_GetsItsRunId_ToTagItsSessions()
    {
        // F31 step 6a: the runner tags its sessions {BENCHMARK_RUN}:{mode}:{repeat}, so the ledger's rows find the run.
        var runner = Runner();

        BenchmarkRun run = runner.Start("run-id", repeat: 1)!;

        Assert.That(await LinesOf(runner, run), Is.EqualTo(new[] { run.Id }));
    }

    [Test]
    public void AnEvalCheckStartsTheRunnerWithItsRunAndRepeat()
    {
        // The argument list and the environment, from the start info, so nothing has to run: dotnet, the runner, its run and repeat.
        var eval = new BenchmarkCheck("stub", "Stub");
        var runner = new BenchmarkRunner(new BenchmarkCatalog([eval]), _workspace, "http://127.0.0.1:1/", "/app/evals/x.dll");

        var start = runner.StartInfoFor(eval, new BenchmarkRun("run_1", "stub", 2));

        Assert.That(start.FileName, Is.EqualTo("dotnet"));
        Assert.That(start.ArgumentList, Is.EqualTo(new[] { "/app/evals/x.dll", "run", "stub", "--repeat", "2" }));
        Assert.That((start.Environment["BENCHMARK_RUN"], start.Environment["PULS_API"]), Is.EqualTo(("run_1", "http://127.0.0.1:1/")));
    }

    [Test]
    public void NoRunStartsAnInterpreter_OnlyTheEvalHost()
    {
        // F18 step 7.4: the API runs evals and nothing else, so no start names Python or sets its flags.
        var eval = new BenchmarkCheck("stub", "Stub");
        var runner = new BenchmarkRunner(new BenchmarkCatalog([eval]), _workspace, "http://127.0.0.1:1/", "/app/evals/x.dll");

        var start = runner.StartInfoFor(eval, new BenchmarkRun("run_1", "stub", 1));

        Assert.That(start.FileName, Is.EqualTo("dotnet"));
        Assert.That(start.Environment.Keys, Has.None.StartsWith("PYTHON"), "no Python flags");
        Assert.That(start.ArgumentList, Has.None.EndsWith(".py"));
    }
}
