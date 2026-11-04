using System.Text.Json;
using LouisAgent.Api.Evals.Export;
using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Evals.Runs;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Recording;

namespace LouisAgent.Api.IntegrationTests.Evals.Runs;

/// <summary>
/// A stored run is graded again by the eval runner: it gets the run's export on its input and the old run's id as an
/// argument. The API reads, stores, starts and shows; it grades nothing. The end-to-end test runs the real runner on
/// the C# stub, which opens no session and costs nothing.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0004-arguments-as-data.md">ADR 0004: arguments as data</seealso>
[Category("Integration")]
public class EvalRescoreIntegrationTests
{
    /// <summary>The one eval the real runner knows in these tests.</summary>
    private static readonly EvalEntry Stub = new EvalEntry("stub", "Stub");

    /// <summary>A runner on the built eval runner, pointed at an API address nobody answers.</summary>
    private static EvalProcess RealRunner() =>
        new(new EvalList([Stub]), TestPaths.RepoRoot, "http://127.0.0.1:1/", RunnerBuild.Dll);

    /// <summary>A re-score names the old run as a separate argument and reads the export from its input.</summary>
    [Test]
    public void ARescoreStartsTheRunnerWithAnArgumentList_AndPipesStdin()
    {
        var run = new RunningEval("new123456789", "stub", 1, rescoreOf: "old123456789",
            input: "@{\"event\":\"protocol\"}\n");

        var start = RealRunner().StartInfoFor(Stub, run);

        Assert.That(start.ArgumentList,
            Is.EqualTo(new[] { RunnerBuild.Dll, "run", "stub", "--rescore", "old123456789" }),
            "no --repeat: the stored run's is used");
        Assert.That(start.RedirectStandardInput, Is.True);
        Assert.That(start.Environment["BENCHMARK_RUN"], Is.EqualTo("new123456789"),
            "the new run's id, so the ledger would find any row it wrote (it writes none)");
    }

    /// <summary>An ordinary run has nothing to pipe in and no re-score argument.</summary>
    [Test]
    public void AnOrdinaryRunPipesNothing()
    {
        var start = RealRunner().StartInfoFor(Stub, new RunningEval("new123456789", "stub", 2));

        Assert.That(start.RedirectStandardInput, Is.False);
        Assert.That(start.ArgumentList, Does.Not.Contain("--rescore"));
    }

    /// <summary>
    /// Re-scoring a real stub run grades the very same transcripts, so the grades match, and keeps the old run's
    /// repeats.
    /// </summary>
    [Test]
    public async Task TheStoredRunsExportReachesTheRunnersStdin_SoTheRescoreGradesTheSameTranscripts()
    {
        // Run the stub twice over and record it; skip if dotnet can't run the runner here.
        var store = new InMemoryEvalStore();
        var runner = RealRunner();
        var first = runner.Start("stub", 2)!;
        var firstRecording = new EvalRecorder(store).RecordAsync(first);
        await foreach (string _ in runner.EventsAsync(first.Id))
        {
        }

        await firstRecording;
        Outside.Tool(first.ExitCode == 0, "dotnet isn't here to run the eval runner");

        // Export that run and re-score it, recording the second run too.
        string export =
            await EvalProtocolExport.BuildAsync(store, (await store.GetRunAsync(first.Id))!,
                CancellationToken.None);
        var second = runner.StartRescore("stub", first.Id, export, repeat: 2)!;
        var secondRecording = new EvalRecorder(store).RecordAsync(second);
        var lines = new List<string>();
        await foreach (string line in runner.EventsAsync(second.Id)) lines.Add(line);
        await secondRecording;

        Assert.That(second.ExitCode, Is.EqualTo(0), string.Join("\n", lines));
        var original = (await store.ConversationsAsync(first.Id)).Select(c => (c.Key, GradesOf(c))).ToList();
        var rescored = (await store.ConversationsAsync(second.Id)).Select(c => (c.Key, GradesOf(c))).ToList();
        Assert.That(rescored, Is.Not.Empty);
        Assert.That(rescored, Is.EqualTo(original),
            "the stub's graders are today's graders, so the grades are the same");
        Assert.That((await store.GetRunAsync(second.Id))!.RescoreOf, Is.EqualTo(first.Id));
        // A re-score keeps the stored run's repeats (Louis saw one say "1 repeat" and hide repeats 2 and 3).
        Assert.That((await store.GetRunAsync(second.Id))!.Repeat, Is.EqualTo(2),
            "the re-score has the repeats of the run it grades again");
    }

    /// <summary>A conversation's grades and outcome as one string, so two runs' grades compare in one go.</summary>
    private static string GradesOf(EvalDocuments.Conversation conversation) =>
        JsonSerializer.Serialize(conversation.Grades?.Select(g => (g.Grader, g.Version, g.Outcome))) +
        conversation.Outcome;

    /// <summary>
    /// Starting a re-score never waits for the runner to read its input. The export used to be written inside the
    /// runner's lock, so a runner slow to read it held every other start behind it; now it's written on its own task.
    /// The stand-in here sleeps and never reads its input, so with a big export a write inside the start would hang.
    /// </summary>
    [Test]
    public async Task StartRescore_ReturnsAtOnce_EvenWhenTheRunnerNeverReadsItsStdin()
    {
        var runner = new EvalProcess(new EvalList([new EvalEntry("sleeper", "Sleeper")]),
            TestPaths.RepoRoot, "http://127.0.0.1:1/", StandInBuild.Dll);
        string export = new('x', 20_000_000);
        var started = System.Diagnostics.Stopwatch.StartNew();

        var run = runner.StartRescore("sleeper", "old123456789", export, repeat: 1)!;

        Assert.That(started.Elapsed, Is.LessThan(TimeSpan.FromSeconds(2)), "the write must not hold the start");
        // Drain its events so the stand-in ends before the test does.
        await foreach (string _ in runner.EventsAsync(run.Id))
        {
        }
    }
}
