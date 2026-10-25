using System.Text.Json;
using System.Text.Json.Nodes;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.Tests.Benchmarks.Runs;

// F31 step 6c: the stub check is the free run every hand check uses (steps 6c, 9, 10): benchmarks/stub/run_check.py
// prints a canned run in the protocol and opens no session, so it costs nothing. Run here through the real runner and
// recorder: every '@' line must be a known event (one that fell back to a log line would be a protocol drift between
// the script and the parser), and the store must hold the whole run.
public class StubCheckTests
{
    private static readonly string Python = OperatingSystem.IsWindows() ? "python" : "python3";

    private static async Task<(BenchmarkRun Run, List<string> Lines)> RunStub(InMemoryBenchmarkStore store, int repeat = 1)
    {
        var catalog = new BenchmarkCatalog([BenchmarkCatalog.Stub]);
        var runner = new BenchmarkRunner(catalog, TestPaths.RepoRoot, "http://127.0.0.1:1/", Python);
        var run = runner.Start("stub", repeat)!;
        var recording = new BenchmarkRecorder(store).RecordAsync(run);
        var lines = new List<string>();
        await foreach (string line in runner.EventsAsync(run.Id)) lines.Add(line);
        await recording;
        Assume.That(run.ExitCode, Is.Not.EqualTo(-1), $"{Python} isn't here to run the stub check");
        return (run, lines);
    }

    [Test]
    public async Task EveryEventLine_IsAKnownEvent()
    {
        var (run, lines) = await RunStub(new InMemoryBenchmarkStore());

        Assert.That(run.Status, Is.EqualTo(BenchmarkRunStatus.Done), string.Join("\n", lines));
        var unread = lines.Where(l => l.StartsWith('@') && Parse(l) is LogLine).ToList();
        Assert.That(unread, Is.Empty, "a line the parser doesn't know");
        Assert.That(Parse(lines.First(l => l.StartsWith('@'))) is ProtocolStarted { Version: BenchmarkProtocol.Version }, "the protocol first");
    }

    [Test]
    public async Task TheStore_HoldsTwoModesOfTwoConversations_WithTheirTurnsAndEvents()
    {
        var store = new InMemoryBenchmarkStore();
        var (run, _) = await RunStub(store, repeat: 2);

        var stored = await store.GetRunAsync(run.Id);
        Assert.That(stored!.Modes!.Select(m => (m.Name, m.Repeat)), Is.EqualTo(new[] { ("puls-only", 1), ("puls-cook", 1), ("puls-only", 2), ("puls-cook", 2) }));
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

    /// <summary>The C# stub through the runner's own listing, the real runner, and the recorder: an eval check, not the Python script.</summary>
    private static async Task<(BenchmarkRun Run, List<string> Lines)> RunEvalStub(InMemoryBenchmarkStore store, int repeat = 1)
    {
        var catalog = new BenchmarkCatalog([BenchmarkCheck.ForEval("stub", "Stub")]);
        var runner = new BenchmarkRunner(catalog, TestPaths.RepoRoot, "http://127.0.0.1:1/", Python, RunnerBuild.Dll);
        var run = runner.Start("stub", repeat)!;
        var recording = new BenchmarkRecorder(store).RecordAsync(run);
        var lines = new List<string>();
        await foreach (string line in runner.EventsAsync(run.Id)) lines.Add(line);
        await recording;
        Assume.That(run.ExitCode, Is.Not.EqualTo(-1), "dotnet isn't here to run the eval runner");
        return (run, lines);
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

    [Test]
    public async Task TheTwoStubs_StoreTheSameModesConversationsAndTurns()
    {
        // The parity plan (F18 step 4.3): the Python stub's documents and the C# stub's are the same, key for key, but the times and the
        // settings, which name a script or an eval by design, and the conversations' grades and outcome (step 4.5a), which the Python
        // stub never writes.
        var python = new InMemoryBenchmarkStore();
        var (pythonRun, _) = await RunStub(python);
        var csharp = new InMemoryBenchmarkStore();
        var (csharpRun, _) = await RunEvalStub(csharp);

        Assume.That(pythonRun.ExitCode, Is.Not.EqualTo(-1), $"{Python} isn't here to run the Python stub");
        Assert.That(await Shape(csharp, csharpRun.Id), Is.EqualTo(await Shape(python, pythonRun.Id)));
    }

    /// <summary>
    /// The run's modes, conversations, turns and their events as JSON, without the times ("seconds" and "at") and without the
    /// conversations' grades and outcome. The store writes its documents in PascalCase (<c>Seconds</c>, <c>At</c>), so the names match
    /// whatever their case.
    /// </summary>
    private static async Task<string> Shape(InMemoryBenchmarkStore store, string runId)
    {
        // The harness adds the pass rates to each mode (F18 step 5a); the Python stub has none, so they are left out of the comparison, like the grades.
        var modes = (await store.GetRunAsync(runId))!.Modes!.Select(mode => mode with { Rates = null }).ToList();
        var conversations = await store.ConversationsAsync(runId);
        var turns = new List<object>();
        foreach (var conversation in conversations)
            foreach (var turn in await store.TurnsAsync(runId, conversation.Key))
                turns.Add(new { turn, events = await store.TurnEventsAsync(runId, turn.Key) });
        var document = JsonSerializer.SerializeToNode(new { modes, conversations, turns });
        return Strip(document)!.ToJsonString();
    }

    private static readonly string[] Stripped = ["seconds", "at", "grades", "outcome"];

    private static bool IsStripped(string key) => Stripped.Any(s => key.Equals(s, StringComparison.OrdinalIgnoreCase));

    private static JsonNode? Strip(JsonNode? node) => node switch
    {
        JsonObject obj => new JsonObject(obj.Where(p => !IsStripped(p.Key)).Select(p => KeyValuePair.Create(p.Key, Strip(p.Value)?.DeepClone())).ToList()),
        JsonArray array => new JsonArray(array.Select(item => Strip(item)?.DeepClone()).ToArray()),
        _ => node?.DeepClone(),
    };
}
