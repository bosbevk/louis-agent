using LouisAgent.Api.Services;

namespace LouisAgent.Api.Tests;

// F27 step 3 (F27-S2): a check runs as a child process (an argument list, never a shell), one at a time, its lines
// streamed to whoever listens, into a timestamped folder under samples/. The script here is a stand-in: a few lines
// of Python that print what they got, so the test needs no containers, model or network.
public class BenchmarkRunnerTests
{
    private static readonly string Python = OperatingSystem.IsWindows() ? "python" : "python3";
    private string _workspace = "";
    private BenchmarkRunner? _runner;

    [SetUp]
    public void SetUp()
    {
        _workspace = Path.Combine(Path.GetTempPath(), "louis-bench-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_workspace, "samples"));
    }

    [TearDown]
    public async Task TearDown()
    {
        // A test may leave its script running; the folder can't go while it does.
        if (_runner?.Current is { Status: BenchmarkRunStatus.Running } run)
            await foreach (string _ in _runner.EventsAsync(run.Id)) { }
        Directory.Delete(_workspace, recursive: true);
    }

    /// <summary>A runner whose "cook" check is <paramref name="python"/> saved under samples/.</summary>
    private BenchmarkRunner Runner(string python, string pulsApi = "http://x/")
    {
        string script = Path.Combine("samples", "fake-check", "run_check.py");
        Directory.CreateDirectory(Path.Combine(_workspace, "samples", "fake-check"));
        File.WriteAllText(Path.Combine(_workspace, script), python);
        var catalog = new BenchmarkCatalog(Path.Combine(_workspace, "samples"), [new BenchmarkCheck("cook", "Die kokkontrole", script.Replace('\\', '/'), ["sample-output-cook"])]);
        return _runner = new BenchmarkRunner(catalog, _workspace, pulsApi, Python);
    }

    private const string Echo = """
        import os, sys
        print("out", sys.argv[1])
        print("repeat", sys.argv[3])
        print("api", os.environ.get("PULS_API"))
        """;

    [Test]
    public async Task ARun_StreamsTheScriptsLines_ThenEndsDone()
    {
        var runner = Runner(Echo, "http://127.0.0.1:8080/");

        BenchmarkRun run = runner.Start("cook", repeat: 2)!;
        var lines = new List<string>();
        await foreach (string line in runner.EventsAsync(run.Id)) lines.Add(line);

        Assert.That(run.Folder, Does.StartWith("sample-output-cook-"));
        Assert.That(lines, Is.EqualTo(new[] { $"out samples/{run.Folder}", "repeat 2", "api http://127.0.0.1:8080/" }));
        Assert.That((run.Status, run.ExitCode), Is.EqualTo((BenchmarkRunStatus.Done, (int?)0)));
        Assert.That(runner.Current, Is.SameAs(run), "the last run stays readable");
    }

    [Test]
    public async Task OnlyOneRun_AtATime()
    {
        // The containers are shared: a second run would skew the first's ledger and its timings.
        var runner = Runner("import time\nprint('start', flush=True)\ntime.sleep(2)\n");

        BenchmarkRun first = runner.Start("cook", 1)!;
        Assert.That(runner.Start("cook", 1), Is.Null, "refused while one runs");

        await foreach (string _ in runner.EventsAsync(first.Id)) { }
        Assert.That(runner.Start("cook", 1), Is.Not.Null, "free again when it ends");
    }

    [Test]
    public void AnUnknownCheck_IsRefused_WithoutAProcess()
    {
        var runner = Runner(Echo);

        Assert.That(runner.Start("nope", 1), Is.Null);
        Assert.That(runner.Current, Is.Null);
    }

    [Test]
    public async Task AScriptThatFails_EndsFailed_WithItsExitCode_AndItsStderr()
    {
        var runner = Runner("import sys\nprint('boom', file=sys.stderr)\nsys.exit(3)\n");

        BenchmarkRun run = runner.Start("cook", 1)!;
        var lines = new List<string>();
        await foreach (string line in runner.EventsAsync(run.Id)) lines.Add(line);

        Assert.That(lines, Does.Contain("boom"));
        Assert.That((run.Status, run.ExitCode), Is.EqualTo((BenchmarkRunStatus.Failed, (int?)3)));
    }

    [Test]
    public void Repeats_AreClampedToTheCap()
    {
        var runner = Runner(Echo);

        Assert.That(runner.Start("cook", 50)!.Repeat, Is.EqualTo(BenchmarkRunner.MaxRepeat));
    }

    [Test]
    public async Task ALateListener_GetsTheLinesSoFar_ThenTheRest()
    {
        // The page may open after the run started (a reload): it replays what was printed, then follows live.
        var runner = Runner("import time\nprint('one', flush=True)\ntime.sleep(1)\nprint('two', flush=True)\n");

        BenchmarkRun run = runner.Start("cook", 1)!;
        await Task.Delay(400);
        var lines = new List<string>();
        await foreach (string line in runner.EventsAsync(run.Id)) lines.Add(line);

        Assert.That(lines, Is.EqualTo(new[] { "one", "two" }));
    }
}
