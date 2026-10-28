using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Evals.Runs;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Evals.Runs.Protocol;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Recording;
using static LouisAgent.Api.Evals.Runs.Protocol.EvalProtocol;

namespace LouisAgent.Api.IntegrationTests.Evals.Runs;

/// <summary>
/// Runs the C# stub eval through the real runner and recorder. The stub is the free run every hand check uses: it
/// writes a canned run and opens no session, so it costs nothing. Every <c>@</c> line must be an event the API knows
/// (one read as a plain log line means the runner and the parser have drifted apart), and the store must hold the
/// whole run.
/// </summary>
[Category("Integration")]
public class StubCheckTests
{
    /// <summary>Runs the stub through the real runner, records it into the store, and hands back every line.</summary>
    private static async Task<(RunningEval Run, List<string> Lines)> RunEvalStub(InMemoryEvalStore store,
        int repeat = 1)
    {
        // Start the stub and record it while it runs.
        var catalog = new EvalList([new EvalEntry("stub", "Stub")]);
        var runner = new EvalProcess(catalog, TestPaths.RepoRoot, "http://127.0.0.1:1/", RunnerBuild.Dll);
        var run = runner.Start("stub", repeat)!;
        var recording = new EvalRecorder(store).RecordAsync(run);

        // Collect its lines until it ends, then wait for the recorder to finish too.
        var lines = new List<string>();
        await foreach (string line in runner.EventsAsync(run.Id)) lines.Add(line);
        await recording;

        // An exit code of -1 means the process never started: skip rather than fail.
        Outside.Tool(run.ExitCode != -1, "dotnet isn't here to run the eval runner");
        return (run, lines);
    }

    /// <summary>Every event line the stub writes is one the parser knows, and the protocol line comes first.</summary>
    [Test]
    public async Task EveryEventLine_IsAKnownEvent()
    {
        var (run, lines) = await RunEvalStub(new InMemoryEvalStore());

        Assert.That(run.Status, Is.EqualTo(EvalRunStatus.Done), string.Join("\n", lines));
        var unread = lines.Where(l => l.StartsWith('@') && Parse(l) is LogLine).ToList();
        Assert.That(unread, Is.Empty, "a line the parser doesn't know");
        Assert.That(
            Parse(lines.First(l => l.StartsWith('@'))) is ProtocolStarted { Version: EvalProtocol.Version },
            "the protocol first");
    }

    /// <summary>
    /// A run with two repeats stores both modes, each repeat of a conversation, their turns, and the specialist's
    /// events.
    /// </summary>
    [Test]
    public async Task TheStore_HoldsTwoModesOfTwoConversations_WithTheirTurnsAndEvents()
    {
        var store = new InMemoryEvalStore();
        var (run, _) = await RunEvalStub(store, repeat: 2);

        // The stub's own names are held by the runner's tests; here the stored shape: two modes, each with its
        // repeats together, the delegating one second.
        var stored = await store.GetRunAsync(run.Id);
        var names = stored!.Modes!.Select(m => m.Name).Distinct().ToList();
        Assert.That(names, Has.Count.EqualTo(2));
        Assert.That(stored.Modes!.Select(m => (m.Name, m.Repeat)),
            Is.EqualTo(new[] { (names[0], 1), (names[0], 2), (names[1], 1), (names[1], 2) }),
            "the runner takes a mode's repeats together");

        var conversations = await store.ConversationsAsync(run.Id);
        Assert.That(conversations.Count, Is.EqualTo(8), "two modes × two conversations × two repeats");
        Assert.That(conversations.All(c => c.Scores.TryGetProperty("routed_right", out _)), Is.True,
            "every conversation scored");

        // One conversation of the delegating mode, down to its turns and the events of its first.
        var bobotie = new ConversationKey(names[1], 1, "bobotie");
        var turns = await store.TurnsAsync(run.Id, bobotie);
        Assert.That(turns.Select(t => t.Key.Turn), Is.EqualTo(new[] { 1, 2 }), "a follow-up, as the cook check has");
        Assert.That(turns[0].Role, Is.Not.Empty, "the specialist the delegating mode handed the question to");
        var events = await store.TurnEventsAsync(run.Id, turns[0].Key);
        Assert.That(events.Select(e => e.Type), Does.Contain("sub_session"), "the specialist's events, for the replay");
        Assert.That(stored.Log, Is.Not.Empty, "its plain lines");
    }

    /// <summary>The stub runs to the end, and every conversation it stores carries the stub's two grades.</summary>
    [Test]
    public async Task TheEvalStubRunsThroughTheRunnerAndTheRecorder()
    {
        var store = new InMemoryEvalStore();
        var (run, _) = await RunEvalStub(store);

        Assert.That(run.Status, Is.EqualTo(EvalRunStatus.Done));
        var conversations = await store.ConversationsAsync(run.Id);
        Assert.That(conversations.Count, Is.EqualTo(4), "two modes × two conversations");
        // The runner grades each trial with the eval's graders, so every stored conversation carries the stub's two,
        // in order.
        foreach (var conversation in conversations)
        {
            Assert.That(conversation.Grades?.Select(g => g.Grader), Is.EqualTo(new[] { "no-error", "within-time" }),
                conversation.Key.ToString());
            Assert.That(conversation.Outcome, Is.EqualTo(EvalOutcome.Pass), conversation.Key.ToString());
        }
    }
}
