using System.Text.Json;
using LouisAgent.Api.Benchmarks.Export;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.Tests.Benchmarks.Runs;

// F18 step 5b.2: a stored run is re-scored by the eval runner, which gets the run's export on stdin and `--rescore {id}` as an argument list (ADR 0004). The API
// reads, stores, starts and shows; it grades nothing. The end-to-end test runs the real runner on the C# stub, which opens no session and costs nothing.
// Written first: BenchmarkRunner had no StartRescore and the protocol's first line no rescore_of.
public class BenchmarkRescoreTests
{
    private static readonly string Python = OperatingSystem.IsWindows() ? "python" : "python3";
    private static readonly BenchmarkCheck Stub = BenchmarkCheck.ForEval("stub", "Stub");

    private static BenchmarkRunner RealRunner() =>
        new(new BenchmarkCatalog([Stub]), TestPaths.RepoRoot, "http://127.0.0.1:1/", Python, RunnerBuild.Dll);

    [Test]
    public void ARescoreStartsTheRunnerWithAnArgumentList_AndPipesStdin()
    {
        var run = new BenchmarkRun("new123456789", "stub", 1, rescoreOf: "old123456789", input: "@{\"event\":\"protocol\"}\n");

        var start = RealRunner().StartInfoFor(Stub, run);

        Assert.That(start.ArgumentList, Is.EqualTo(new[] { RunnerBuild.Dll, "run", "stub", "--rescore", "old123456789" }), "no --repeat: the stored run's is used");
        Assert.That(start.RedirectStandardInput, Is.True);
        Assert.That(start.Environment["BENCHMARK_RUN"], Is.EqualTo("new123456789"), "the new run's id, so the ledger would find any row it wrote (it writes none)");
    }

    [Test]
    public void AnOrdinaryRunPipesNothing()
    {
        var start = RealRunner().StartInfoFor(Stub, new BenchmarkRun("new123456789", "stub", 2));

        Assert.That(start.RedirectStandardInput, Is.False);
        Assert.That(start.ArgumentList, Does.Not.Contain("--rescore"));
    }

    [Test]
    public void TheBeginLineNamesTheRunItRescored_AndAnOldRunNamesNone()
    {
        var rescored = (ProtocolStarted)Parse("""@{"event":"protocol","version":1,"eval":"stub","eval_version":1,"rescore_of":"old123456789","graders":[],"models":[]}""");
        var ordinary = (ProtocolStarted)Parse("""@{"event":"protocol","version":1,"eval":"stub","eval_version":1,"graders":[],"models":[]}""");

        Assert.That(rescored.RescoreOf, Is.EqualTo("old123456789"));
        Assert.That(ordinary.RescoreOf, Is.Null);
    }

    [Test]
    public async Task TheExportWritesRescoreOfBack_AndAnOrdinaryRunWritesNone()
    {
        var store = new InMemoryBenchmarkStore();
        await store.SaveRunAsync(new BenchmarkDocuments.Run("a", "stub", 1, BenchmarkRunStatus.Done, DateTimeOffset.UnixEpoch, Eval: "stub", RescoreOf: "old123456789"));
        await store.SaveRunAsync(new BenchmarkDocuments.Run("b", "stub", 1, BenchmarkRunStatus.Done, DateTimeOffset.UnixEpoch, Eval: "stub"));

        string rescored = await BenchmarkProtocolExport.BuildAsync(store, (await store.GetRunAsync("a"))!, CancellationToken.None);
        string ordinary = await BenchmarkProtocolExport.BuildAsync(store, (await store.GetRunAsync("b"))!, CancellationToken.None);

        Assert.That(((ProtocolStarted)Parse(rescored.Split('\n')[0])).RescoreOf, Is.EqualTo("old123456789"));
        Assert.That(ordinary, Does.Not.Contain("rescore_of"));
    }

    [Test]
    public async Task TheStoredRunsExportReachesTheRunnersStdin_SoTheRescoreGradesTheSameTranscripts()
    {
        var store = new InMemoryBenchmarkStore();
        var runner = RealRunner();
        var first = runner.Start("stub", 2)!;
        var firstRecording = new BenchmarkRecorder(store).RecordAsync(first);
        await foreach (string _ in runner.EventsAsync(first.Id)) { }
        await firstRecording;
        Assume.That(first.ExitCode, Is.EqualTo(0), "dotnet isn't here to run the eval runner");
        string export = await BenchmarkProtocolExport.BuildAsync(store, (await store.GetRunAsync(first.Id))!, CancellationToken.None);

        var second = runner.StartRescore("stub", first.Id, export, repeat: 2)!;
        var secondRecording = new BenchmarkRecorder(store).RecordAsync(second);
        var lines = new List<string>();
        await foreach (string line in runner.EventsAsync(second.Id)) lines.Add(line);
        await secondRecording;

        Assert.That(second.ExitCode, Is.EqualTo(0), string.Join("\n", lines));
        var original = (await store.ConversationsAsync(first.Id)).Select(c => (c.Key, GradesOf(c))).ToList();
        var rescored = (await store.ConversationsAsync(second.Id)).Select(c => (c.Key, GradesOf(c))).ToList();
        Assert.That(rescored, Is.Not.Empty);
        Assert.That(rescored, Is.EqualTo(original), "the stub's graders are today's graders, so the grades are the same");
        Assert.That((await store.GetRunAsync(second.Id))!.RescoreOf, Is.EqualTo(first.Id));
        // 6a.4c (Louis, on a re-score of a --repeat 3 run that said "1 repeat" and hid repeats 2 and 3 in the explorer): a re-score keeps the stored run's repeats.
        Assert.That((await store.GetRunAsync(second.Id))!.Repeat, Is.EqualTo(2), "the re-score has the repeats of the run it grades again");
    }

    private static string GradesOf(BenchmarkDocuments.Conversation conversation) =>
        JsonSerializer.Serialize(conversation.Grades?.Select(g => (g.Grader, g.Version, g.Outcome))) + conversation.Outcome;

    // Review of 5b.2: the export was written to the runner's stdin inside the runner's lock, so a runner that is slow to read it (or never does) held every other start
    // behind it. It is written on its own task now. The stand-in "runner" is a Python script that sleeps and never reads its stdin; with a big export, a write inside
    // StartRescore would not return until the process ended.
    [Test]
    public async Task StartRescore_ReturnsAtOnce_EvenWhenTheRunnerNeverReadsItsStdin()
    {
        string script = Path.Combine(Path.GetTempPath(), "louis-sleeper-" + Guid.NewGuid().ToString("N") + ".py");
        File.WriteAllText(script, "import time\ntime.sleep(4)\n");
        try
        {
            var runner = new BenchmarkRunner(new BenchmarkCatalog([Stub]), TestPaths.RepoRoot, "http://127.0.0.1:1/", Python, script, evalHost: Python);
            string export = new('x', 20_000_000);
            var started = System.Diagnostics.Stopwatch.StartNew();

            var run = runner.StartRescore("stub", "old123456789", export, repeat: 1)!;

            Assert.That(started.Elapsed, Is.LessThan(TimeSpan.FromSeconds(2)), "the write must not hold the start");
            await foreach (string _ in runner.EventsAsync(run.Id)) { }
        }
        finally
        {
            File.Delete(script);
        }
    }

    [Test]
    public async Task ThePump_WritesEverythingAndCloses_AndToleratesARunnerThatHasGone()
    {
        var writer = new RecordingWriter();
        await BenchmarkRunner.PumpAsync(writer, "@{}\n");
        Assert.That((writer.Text, writer.Closed), Is.EqualTo(("@{}\n", true)));

        Assert.That(async () => await BenchmarkRunner.PumpAsync(new RecordingWriter(throwOnWrite: true), "@{}\n"), Throws.Nothing, "a runner that ended without reading is not an error here");
    }

    private sealed class RecordingWriter(bool throwOnWrite = false) : StringWriter
    {
        public bool Closed { get; private set; }

        public string Text => ToString();

        public override Task WriteAsync(string? value) => throwOnWrite ? throw new IOException("Broken pipe") : base.WriteAsync(value);

        public override void Close()
        {
            Closed = true;
            base.Close();
        }
    }
}
