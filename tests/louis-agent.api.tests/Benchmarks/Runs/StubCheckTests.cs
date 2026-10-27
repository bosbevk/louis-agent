using LouisAgent.Testing;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.Tests.Benchmarks.Runs;

// F31 step 6c, on the C# stub since F18 step 7.3 (the Python stub is gone): the stub eval is the free run every hand check uses (steps 6c, 9, 10): it writes a canned run in the protocol and opens no
// session, so it costs nothing. Run here through the real runner and recorder: every '@' line must be a known event (one that fell back to a log line would be a protocol drift between the runner and
// the parser), and the store must hold the whole run.
public class StubCheckTests
{
    /// <summary>The C# stub through the runner's own listing, the real runner, and the recorder.</summary>
    private static async Task<(BenchmarkRun Run, List<string> Lines)> RunEvalStub(InMemoryBenchmarkStore store, int repeat = 1)
    {
        var catalog = new BenchmarkCatalog([new BenchmarkCheck("stub", "Stub")]);
        var runner = new BenchmarkRunner(catalog, TestPaths.RepoRoot, "http://127.0.0.1:1/", RunnerBuild.Dll);
        var run = runner.Start("stub", repeat)!;
        var recording = new BenchmarkRecorder(store).RecordAsync(run);
        var lines = new List<string>();
        await foreach (string line in runner.EventsAsync(run.Id)) lines.Add(line);
        await recording;
        Assume.That(run.ExitCode, Is.Not.EqualTo(-1), "dotnet isn't here to run the eval runner");
        return (run, lines);
    }

    [Test]
    public async Task EveryEventLine_IsAKnownEvent()
    {
        var (run, lines) = await RunEvalStub(new InMemoryBenchmarkStore());

        Assert.That(run.Status, Is.EqualTo(BenchmarkRunStatus.Done), string.Join("\n", lines));
        var unread = lines.Where(l => l.StartsWith('@') && Parse(l) is LogLine).ToList();
        Assert.That(unread, Is.Empty, "a line the parser doesn't know");
        Assert.That(Parse(lines.First(l => l.StartsWith('@'))) is ProtocolStarted { Version: BenchmarkProtocol.Version }, "the protocol first");
    }

    [Test]
    public async Task TheStore_HoldsTwoModesOfTwoConversations_WithTheirTurnsAndEvents()
    {
        var store = new InMemoryBenchmarkStore();
        var (run, _) = await RunEvalStub(store, repeat: 2);

        var stored = await store.GetRunAsync(run.Id);
        Assert.That(stored!.Modes!.Select(m => (m.Name, m.Repeat)), Is.EqualTo(new[] { ("puls-only", 1), ("puls-only", 2), ("puls-cook", 1), ("puls-cook", 2) }), "the runner takes a mode's repeats together");
        var conversations = await store.ConversationsAsync(run.Id);
        Assert.That(conversations.Count, Is.EqualTo(8), "two modes × two conversations × two repeats");
        Assert.That(conversations.All(c => c.Scores.TryGetProperty("routed_right", out _)), Is.True, "every conversation scored");

        var bobotie = new ConversationKey("puls-cook", 1, "bobotie");
        var turns = await store.TurnsAsync(run.Id, bobotie);
        Assert.That(turns.Select(t => t.Key.Turn), Is.EqualTo(new[] { 1, 2 }), "a follow-up, as the cook check has");
        Assert.That(turns[0].Role, Is.EqualTo("puls-cook"));
        var events = await store.TurnEventsAsync(run.Id, turns[0].Key);
        Assert.That(events.Select(e => e.Type), Does.Contain("sub_session"), "the specialist's events, for the replay");
        Assert.That(stored.Log, Is.Not.Empty, "its plain lines");
    }

    [Test]
    public async Task TheEvalStubRunsThroughTheRunnerAndTheRecorder()
    {
        var store = new InMemoryBenchmarkStore();
        var (run, _) = await RunEvalStub(store);

        Assert.That(run.Status, Is.EqualTo(BenchmarkRunStatus.Done));
        var conversations = await store.ConversationsAsync(run.Id);
        Assert.That(conversations.Count, Is.EqualTo(4), "two modes × two conversations");
        // F18 step 4.5a: the runner grades each trial with the eval's graders, so every stored conversation carries the stub's two, in order.
        foreach (var conversation in conversations)
        {
            Assert.That(conversation.Grades?.Select(g => g.Grader), Is.EqualTo(new[] { "no-error", "within-time" }), conversation.Key.ToString());
            Assert.That(conversation.Outcome, Is.EqualTo(BenchmarkOutcome.Pass), conversation.Key.ToString());
        }
    }
}
