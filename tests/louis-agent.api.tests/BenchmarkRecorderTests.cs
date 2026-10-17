using System.Text.Json;
using LouisAgent.Api.Services;
using LouisAgent.Api.Services.Benchmarks;
using static LouisAgent.Api.Services.Benchmarks.BenchmarkDocuments;
using static LouisAgent.Api.Services.Benchmarks.BenchmarkProtocol;

namespace LouisAgent.Api.Tests;

// F31 step 3: the recorder listens to a run's lines as the page does and folds them into the store as they come, so a
// half-done run is already readable, and when the run ends it closes whatever is still open. A store that fails stops
// the recording, never the run. The run here is driven by hand (Add, Complete): no process, no model.
public class BenchmarkRecorderTests
{
    private InMemoryBenchmarkStore _store = null!;
    private BenchmarkRun _run = null!;

    [SetUp]
    public void SetUp()
    {
        _store = new InMemoryBenchmarkStore();
        _run = new BenchmarkRun("run123456789", "cook", 1);
    }

    private static string At(object e) => "@" + JsonSerializer.Serialize(e);

    private void Add(params string[] lines)
    {
        foreach (string line in lines) _run.Add(line);
    }

    private static string Protocol(int version = 1) => At(new { @event = "protocol", version });

    private static string Mode(string mode = "puls-cook", int repeat = 1) =>
        At(new { @event = "mode", mode, repeat, toolsets = new[] { "specialists", "web" }, role = "puls/voice-af", conversations = 2 });

    private static string Question(string task, int turn = 1, string mode = "puls-cook", int repeat = 1) =>
        At(new { @event = "question", mode, repeat, task, turn, session = $"sess_{task}", text = $"{task}?" });

    private static string Sse(string task, string text, int turn = 1, string mode = "puls-cook", int repeat = 1) =>
        At(new { @event = "sse", mode, repeat, task, turn, type = "content_block_delta", data = new { delta = new { text } } });

    private static string TurnDone(string task, int turn = 1, string mode = "puls-cook", int repeat = 1) =>
        At(new
        {
            @event = "turn", mode, repeat, task, turn, session = $"sess_{task}", question = $"{task}?", answer = "Hier is…",
            role = "puls-cook", puls_calls = 1, specialist_calls = 6, hosts = new[] { "www.checkers.co.za" }, seconds = 30.5,
        });

    private static string ConversationDone(string task, string mode = "puls-cook", int repeat = 1) =>
        At(new { @event = "conversation", mode, repeat, task, role = "puls-cook", scores = new { routed_right = true }, errors = 0, seconds = 30.5 });

    private static string ModeDone(string mode = "puls-cook", int repeat = 1) => At(new { @event = "mode_done", mode, repeat });

    private static async Task Eventually(Func<Task<bool>> condition)
    {
        for (int i = 0; i < 200; i++)
        {
            if (await condition()) return;
            await Task.Delay(10);
        }
        Assert.Fail("the store never got there");
    }

    [Test]
    public async Task AWholeRun_IsInTheStore_WhenItEnds()
    {
        var recording = new BenchmarkRecorder(_store).RecordAsync(_run, new Dictionary<string, string> { ["model"] = "claude-haiku-5-5" });
        Add(Protocol(), "Puls cook check, 1 repeat", Mode(), Question("bobotie"), Sse("bobotie", "Hal"), Sse("bobotie", "lo"),
            TurnDone("bobotie"), ConversationDone("bobotie"), "puls-cook bobotie role=puls-cook", ModeDone());
        _run.Complete(0);

        var result = await recording;

        Assert.That(result, Is.EqualTo(new BenchmarkRecorder.Result(true, null)));
        var run = await _store.GetRunAsync(_run.Id);
        Assert.That((run!.Check, run.Repeat, run.Status, run.ExitCode), Is.EqualTo(("cook", 1, BenchmarkRunStatus.Done, (int?)0)));
        Assert.That(run.EndedAt, Is.Not.Null);
        Assert.That(run.Settings!["model"], Is.EqualTo("claude-haiku-5-5"));
        Assert.That(run.Modes!.Single() is { Name: "puls-cook", Repeat: 1, Role: "puls/voice-af" });
        Assert.That(run.Log, Is.EqualTo(new[] { "Puls cook check, 1 repeat", "puls-cook bobotie role=puls-cook" }), "the plain lines, not the events");

        var conversation = (await _store.ConversationsAsync(_run.Id)).Single();
        Assert.That((conversation.Key, conversation.Order, conversation.Role), Is.EqualTo((new ConversationKey("puls-cook", 1, "bobotie"), 1, "puls-cook")));
        Assert.That(conversation.Scores.GetProperty("routed_right").GetBoolean(), Is.True);

        var key = new TurnKey("puls-cook", 1, "bobotie", 1);
        var turn = (await _store.TurnsAsync(_run.Id, key.Conversation)).Single();
        Assert.That((turn.Session, turn.Answer, turn.SpecialistCalls, turn.Error), Is.EqualTo(("sess_bobotie", "Hier is…", 6, (string?)null)));
        var events = await _store.TurnEventsAsync(_run.Id, key);
        Assert.That(events.Select(e => e.Data.GetProperty("delta").GetProperty("text").GetString()), Is.EqualTo(new[] { "Hal", "lo" }));
    }

    [Test]
    public async Task ATurnsToolCounts_AreKept_WhenItEnds()
    {
        // Review of PR #55: counted once when the turn is recorded, so reading a run never needs its events for them.
        string Block(object block) => At(new { @event = "sse", mode = "puls-cook", repeat = 1, task = "bobotie", turn = 1, type = "content_block_start", data = new { content_block = block } });
        var recording = new BenchmarkRecorder(_store).RecordAsync(_run);
        Add(Protocol(), Mode(), Question("bobotie"),
            Block(new { type = "tool_use", id = "u1", name = "WebSearch", input = new { } }),
            Block(new { type = "tool_result", tool_use_id = "u1", content = "Error: limit reached", is_error = true }),
            Block(new { type = "tool_use", id = "u2", name = "FetchUrl", input = new { } }),
            TurnDone("bobotie"), ConversationDone("bobotie"), ModeDone());
        _run.Complete(0);
        await recording;

        var tools = (await _store.TurnsAsync(_run.Id, new ConversationKey("puls-cook", 1, "bobotie"))).Single().Tools!;

        Assert.That((tools.Calls, tools.Failed), Is.EqualTo((2, 1)));
        Assert.That(tools.Reasons.Single(), Is.EqualTo(new BenchmarkTools.Failure("WebSearch", "Error: limit reached", 1)));
    }

    [Test]
    public async Task AHalfDoneRun_IsReadable_WhileItRuns()
    {
        var recording = new BenchmarkRecorder(_store).RecordAsync(_run);
        Add(Protocol(), Mode(), Question("bobotie"), Sse("bobotie", "a"), TurnDone("bobotie"), Question("bobotie", turn: 2), Sse("bobotie", "b", turn: 2));

        await Eventually(async () => (await _store.TurnsAsync(_run.Id, new ConversationKey("puls-cook", 1, "bobotie"))).Count == 1);

        var run = await _store.GetRunAsync(_run.Id);
        Assert.That(run!.Status, Is.EqualTo(BenchmarkRunStatus.Running));
        Assert.That(run.Modes!.Count, Is.EqualTo(1), "the mode is written when it starts");
        var conversation = (await _store.ConversationsAsync(_run.Id)).Single();
        Assert.That((conversation.Order, conversation.Role), Is.EqualTo((1, (string?)null)), "the conversation is there before its scores");

        _run.Complete(0);
        await recording;
    }

    [Test]
    public async Task Conversations_KeepTheOrderTheyStartedIn_AndEachRepeatIsItsOwn()
    {
        var recording = new BenchmarkRecorder(_store).RecordAsync(_run);
        Add(Protocol(), Mode(), Question("cottage-pie"), TurnDone("cottage-pie"), ConversationDone("cottage-pie"),
            Question("bobotie"), TurnDone("bobotie"), ConversationDone("bobotie"), ModeDone(),
            Mode(repeat: 2), Question("cottage-pie", repeat: 2), TurnDone("cottage-pie", repeat: 2), ConversationDone("cottage-pie", repeat: 2));
        _run.Complete(0);
        await recording;

        var conversations = await _store.ConversationsAsync(_run.Id);

        Assert.That(conversations.Select(c => (c.Key.Task, c.Key.Repeat, c.Order)), Is.EqualTo(new[] { ("cottage-pie", 1, 1), ("bobotie", 1, 2), ("cottage-pie", 2, 3) }));
        Assert.That((await _store.GetRunAsync(_run.Id))!.Modes!.Select(m => m.Repeat), Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public async Task ATurnTheRunCutOff_IsKeptWithWhatItHad()
    {
        var recording = new BenchmarkRecorder(_store).RecordAsync(_run);
        Add(Protocol(), Mode(), Question("bobotie"), Sse("bobotie", "Hal"), "Traceback (most recent call last):");
        _run.Complete(1);
        await recording;

        var key = new TurnKey("puls-cook", 1, "bobotie", 1);
        var turn = (await _store.TurnsAsync(_run.Id, key.Conversation)).Single();
        Assert.That((turn.Session, turn.Question, turn.Answer), Is.EqualTo(("sess_bobotie", "bobotie?", (string?)null)));
        Assert.That(turn.Error, Does.Contain("ended before this turn"));
        Assert.That((await _store.TurnEventsAsync(_run.Id, key)).Count, Is.EqualTo(1));
        var run = await _store.GetRunAsync(_run.Id);
        Assert.That((run!.Status, run.ExitCode), Is.EqualTo((BenchmarkRunStatus.Failed, (int?)1)));
        Assert.That(run.Log, Does.Contain("Traceback (most recent call last):"));
    }

    [Test]
    public async Task ARunThatAlreadyEnded_IsRecordedWhole()
    {
        // The recorder attaches like a late listener: the lines so far, then the rest.
        Add(Protocol(), Mode(), Question("bobotie"), TurnDone("bobotie"), ConversationDone("bobotie"), ModeDone());
        _run.Complete(0);

        await new BenchmarkRecorder(_store).RecordAsync(_run);

        Assert.That((await _store.ConversationsAsync(_run.Id)).Single().Role, Is.EqualTo("puls-cook"));
        Assert.That((await _store.GetRunAsync(_run.Id))!.Status, Is.EqualTo(BenchmarkRunStatus.Done));
    }

    [Test]
    public async Task AnUnknownProtocolVersion_IsSaidInTheLog_AndTheRunIsStillKept()
    {
        var recording = new BenchmarkRecorder(_store).RecordAsync(_run);
        Add(Protocol(version: 2), Mode(), Question("bobotie"), TurnDone("bobotie"));
        _run.Complete(0);
        await recording;

        var run = await _store.GetRunAsync(_run.Id);
        Assert.That(run!.Log!.First(), Does.Contain("protocol 2").And.Contain("reads 1"));
        Assert.That((await _store.TurnsAsync(_run.Id, new ConversationKey("puls-cook", 1, "bobotie"))).Count, Is.EqualTo(1));
    }

    /// <summary>A store whose turns can't be written: the first failure stops the recording's writes.</summary>
    private sealed class TurnsFail(IBenchmarkStore inner) : IBenchmarkStore
    {
        public int Writes { get; private set; }

        public Task SaveRunAsync(Run run, CancellationToken c = default) { Writes++; return inner.SaveRunAsync(run, c); }
        public Task<Run?> GetRunAsync(string id, CancellationToken c = default) => inner.GetRunAsync(id, c);
        public Task<IReadOnlyList<Run>> ListRunsAsync(string? check = null, int limit = 50, CancellationToken c = default) => inner.ListRunsAsync(check, limit, c);
        public Task SaveConversationAsync(string run, Conversation conversation, CancellationToken c = default) { Writes++; return inner.SaveConversationAsync(run, conversation, c); }
        public Task<IReadOnlyList<Conversation>> ConversationsAsync(string run, CancellationToken c = default) => inner.ConversationsAsync(run, c);
        public Task SaveTurnAsync(string run, Turn turn, IReadOnlyList<StreamEvent> events, CancellationToken c = default) => throw new TimeoutException("mongo is down");
        public Task<IReadOnlyList<Turn>> TurnsAsync(string run, ConversationKey conversation, CancellationToken c = default) => inner.TurnsAsync(run, conversation, c);
        public Task<IReadOnlyList<StreamEvent>> TurnEventsAsync(string run, TurnKey turn, CancellationToken c = default) => inner.TurnEventsAsync(run, turn, c);
        public Task AddRequestsAsync(string run, IReadOnlyList<Request> requests, CancellationToken c = default) { Writes++; return inner.AddRequestsAsync(run, requests, c); }
        public Task<IReadOnlyList<Request>> RequestsAsync(string run, CancellationToken c = default) => inner.RequestsAsync(run, c);
        public Task<int> CloseOrphansAsync(DateTimeOffset now, CancellationToken c = default) => inner.CloseOrphansAsync(now, c);
        public Task<bool> DeleteRunAsync(string id, CancellationToken c = default) => inner.DeleteRunAsync(id, c);
    }

    [Test]
    public async Task AStoreThatFails_StopsTheRecording_NeverTheRun()
    {
        var failing = new TurnsFail(_store);
        var recording = new BenchmarkRecorder(failing).RecordAsync(_run);
        Add(Protocol(), Mode(), Question("bobotie"), TurnDone("bobotie"), ConversationDone("bobotie"), Question("cottage-pie"), ModeDone());
        _run.Complete(0);

        var result = await recording;

        Assert.That(result.Stored, Is.False);
        Assert.That(result.StoreError, Does.Contain("mongo is down"));
        var run = await _store.GetRunAsync(_run.Id);
        Assert.That(run!.StoreError, Does.Contain("mongo is down"), "the run says what wasn't kept, when the store takes that much");
        Assert.That(run.Status, Is.EqualTo(BenchmarkRunStatus.Done), "the run itself finished");
        var conversation = (await _store.ConversationsAsync(_run.Id)).Single();
        Assert.That(conversation.Role, Is.Null, "nothing after the failure was written: not bobotie's scores, not cottage-pie");
    }

    [Test]
    public async Task AnAttachedRecording_IsKeptByItsRun_SoTheEndCanSayWhetherItWasStored()
    {
        // F31 step 6a: the endpoint attaches the recorder when it starts a run, and its "done" event awaits the
        // recording to say stored: true or false.
        var recorder = new BenchmarkRecorder(_store);

        recorder.Attach(_run);
        Add(Protocol(), Mode());
        _run.Complete(0);

        var recording = recorder.RecordingOf(_run.Id);
        Assert.That(recording, Is.Not.Null);
        Assert.That((await recording!).Stored, Is.True);
        Assert.That(recorder.RecordingOf("unknown"), Is.Null);
    }
}
