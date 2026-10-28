using System.Text.Json;
using LouisAgent.Api.Evals.Reports;
using LouisAgent.Api.Evals.Runs;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Notes;
using LouisAgent.Api.Evals.Store.Recording;
using static LouisAgent.Api.Evals.Store.EvalDocuments;
using static LouisAgent.Api.Evals.Runs.Protocol.EvalProtocol;

namespace LouisAgent.Api.Tests.Evals.Store.Recording;

/// <summary>
/// The recorder listens to a run's lines as the page does and writes them to the store as they come, so a half-done
/// run can already be read, and when the run ends it closes whatever is still open. A store that fails stops the
/// recording, never the run. Here the run is fed by hand: no process, no model.
/// </summary>
public class EvalRecorderTests
{
    /// <summary>The store the recorder writes into.</summary>
    private InMemoryEvalStore _store = null!;

    /// <summary>The run being recorded, fed by hand.</summary>
    private RunningEval _run = null!;

    /// <summary>Gives each test a fresh store and a fresh run.</summary>
    [SetUp]
    public void SetUp()
    {
        _store = new InMemoryEvalStore();
        _run = new RunningEval("run123456789", "cook", 1);
    }

    /// <summary>Turns an event into one of the runner's protocol lines: an <c>@</c> and the event as JSON.</summary>
    private static string At(object e) => "@" + JsonSerializer.Serialize(e);

    /// <summary>Feeds lines to the run, as if the runner had printed them.</summary>
    private void Add(params string[] lines)
    {
        foreach (string line in lines) _run.Add(line);
    }

    /// <summary>The first protocol line, of a given version.</summary>
    private static string Protocol(int version = 1) =>
        At(new
        {
            @event = "protocol",
            version
        });

    /// <summary>The line that opens a mode's repeat.</summary>
    private static string Mode(string mode = SampleMode.Beta, int repeat = 1) =>
        At(new
        {
            @event = "mode",
            mode,
            repeat,
            toolsets = new[] { SampleToolset.Gamma, SampleToolset.Alpha },
            role = "puls/voice-af",
            conversations = 2
        });

    /// <summary>The line that opens a turn with its question.</summary>
    private static string Question(string task, int turn = 1, string mode = SampleMode.Beta, int repeat = 1) =>
        At(new
        {
            @event = "question",
            mode,
            repeat,
            task,
            turn,
            session = $"sess_{task}",
            text = $"{task}?"
        });

    /// <summary>One piece of the answer streaming in.</summary>
    private static string Sse(string task, string text, int turn = 1, string mode = SampleMode.Beta, int repeat = 1) =>
        At(new
        {
            @event = "sse",
            mode,
            repeat,
            task,
            turn,
            type = "content_block_delta",
            data = new { delta = new { text } }
        });

    /// <summary>The line that closes a turn with its answer.</summary>
    private static string TurnDone(string task, int turn = 1, string mode = SampleMode.Beta, int repeat = 1) =>
        At(new
        {
            @event = "turn",
            mode,
            repeat,
            task,
            turn,
            session = $"sess_{task}",
            question = $"{task}?",
            answer = "Hier is…",
            role = SampleRole.Alpha,
            puls_calls = 1,
            specialist_calls = 6,
            hosts = new[] { "www.checkers.co.za" },
            seconds = 30.5,
        });

    /// <summary>The line that closes a conversation with its scores.</summary>
    private static string ConversationDone(string task, string mode = SampleMode.Beta, int repeat = 1) =>
        At(new
        {
            @event = "conversation",
            mode,
            repeat,
            task,
            role = SampleRole.Alpha,
            scores = new { routed_right = true },
            errors = 0,
            seconds = 30.5
        });

    /// <summary>The line that closes a mode's repeat.</summary>
    private static string ModeDone(string mode = SampleMode.Beta, int repeat = 1) =>
        At(new
        {
            @event = "mode_done",
            mode,
            repeat
        });

    /// <summary>Waits, up to two seconds, for something the recorder does in the background.</summary>
    private static async Task Eventually(Func<Task<bool>> condition)
    {
        for (int i = 0; i < 200; i++)
        {
            if (await condition()) return;
            await Task.Delay(10);
        }

        Assert.Fail("the store never got there");
    }

    /// <summary>
    /// A whole run ends up in the store: the run, its log, its mode, its conversation, its turn and the turn's stream
    /// events.
    /// </summary>
    [Test]
    public async Task AWholeRun_IsInTheStore_WhenItEnds()
    {
        // One mode, one conversation of one turn whose answer streams in two pieces, with two plain log lines.
        var recording = new EvalRecorder(_store).RecordAsync(_run,
            new Dictionary<string, string> { ["model"] = "claude-haiku-5-5" });
        Add(Protocol(), "Puls cook check, 1 repeat", Mode(), Question("bobotie"), Sse("bobotie", "Hal"),
            Sse("bobotie", "lo"),
            TurnDone("bobotie"), ConversationDone("bobotie"), SampleRole.Fill("<RoleAlpha> bobotie role=<RoleAlpha>"),
            ModeDone());
        _run.Complete(0);

        var result = await recording;

        Assert.That(result, Is.EqualTo(new EvalRecorder.Result(true, null)));
        var run = await _store.GetRunAsync(_run.Id);
        Assert.That((run!.Check, run.Repeat, run.Status, run.ExitCode),
            Is.EqualTo(("cook", 1, EvalRunStatus.Done, (int?)0)));
        Assert.That(run.EndedAt, Is.Not.Null);
        Assert.That(run.Settings!["model"], Is.EqualTo("claude-haiku-5-5"));
        Assert.That(run.Modes!.Single() is { Name: SampleMode.Beta, Repeat: 1, Role: "puls/voice-af" });
        Assert.That(run.Log,
            Is.EqualTo(new[] { "Puls cook check, 1 repeat", SampleRole.Fill("<RoleAlpha> bobotie role=<RoleAlpha>") }),
            "the plain lines, not the events");

        var conversation = (await _store.ConversationsAsync(_run.Id)).Single();
        Assert.That((conversation.Key, conversation.Order, conversation.Role),
            Is.EqualTo((new ConversationKey(SampleMode.Beta, 1, "bobotie"), 1, SampleRole.Alpha)));
        Assert.That(conversation.Scores.GetProperty("routed_right").GetBoolean(), Is.True);

        var key = new TurnKey(SampleMode.Beta, 1, "bobotie", 1);
        var turn = (await _store.TurnsAsync(_run.Id, key.Conversation)).Single();
        Assert.That((turn.Session, turn.Answer, turn.SpecialistCalls, turn.Error),
            Is.EqualTo(("sess_bobotie", "Hier is…", 6, (string?)null)));
        var events = await _store.TurnEventsAsync(_run.Id, key);
        Assert.That(events.Select(e => e.Data.GetProperty("delta").GetProperty("text").GetString()),
            Is.EqualTo(new[] { "Hal", "lo" }));
    }

    /// <summary>The run's split is kept, read from the very first line.</summary>
    [Test]
    public async Task ARunsSplit_IsKept_FromItsFirstLine()
    {
        var recording = new EvalRecorder(_store).RecordAsync(_run);
        Add(
            At(new
            {
                @event = "protocol",
                version = 1,
                eval = SampleEval.Beta,
                eval_version = 2,
                commit = "abc",
                split = "held_out"
            }), Mode(), ModeDone());
        _run.Complete(0);
        await recording;

        Assert.That((await _store.GetRunAsync(_run.Id))!.Split, Is.EqualTo(EvalRunSplit.HeldOut));
    }

    /// <summary>
    /// A turn that failed because of the infrastructure is kept that way, so a stored run never grades it against the
    /// agent.
    /// </summary>
    [Test]
    public async Task ATurnsInfrastructureFailure_IsKept_SoAStoredRunNeverGradesItAsTheAgents()
    {
        var recording = new EvalRecorder(_store).RecordAsync(_run);
        Add(Protocol(), Mode(), Question("bobotie"),
            At(new
            {
                @event = "turn",
                mode = SampleMode.Beta,
                repeat = 1,
                task = "bobotie",
                turn = 1,
                session = "sess_bobotie",
                question = "bobotie?",
                answer = "",
                role = (string?)null,
                seconds = 600.0,
                failure = "api_error"
            }),
            ConversationDone("bobotie"), ModeDone());
        _run.Complete(0);
        await recording;

        var turn = (await _store.TurnsAsync(_run.Id, new ConversationKey(SampleMode.Beta, 1, "bobotie"))).Single();

        Assert.That(turn.Failure, Is.EqualTo(EvalTurnFailure.ApiError));
    }

    /// <summary>
    /// A turn's tool counts (calls, failures and why) are worked out once, when it is recorded, so reading a run never
    /// needs its events for them.
    /// </summary>
    [Test]
    public async Task ATurnsToolCounts_AreKept_WhenItEnds()
    {
        // A content block streaming into bobotie's first turn.
        string Block(object block) => At(new
        {
            @event = "sse",
            mode = SampleMode.Beta,
            repeat = 1,
            task = "bobotie",
            turn = 1,
            type = "content_block_start",
            data = new { content_block = block }
        });

        // Two tool calls, the first of which fails.
        var recording = new EvalRecorder(_store).RecordAsync(_run);
        Add(Protocol(), Mode(), Question("bobotie"),
            Block(new
            {
                type = "tool_use",
                id = "u1",
                name = "WebSearch",
                input = new { }
            }),
            Block(new
            {
                type = "tool_result",
                tool_use_id = "u1",
                content = "Error: limit reached",
                is_error = true
            }),
            Block(new
            {
                type = "tool_use",
                id = "u2",
                name = "FetchUrl",
                input = new { }
            }),
            TurnDone("bobotie"), ConversationDone("bobotie"), ModeDone());
        _run.Complete(0);
        await recording;

        var tools = (await _store.TurnsAsync(_run.Id, new ConversationKey(SampleMode.Beta, 1, "bobotie"))).Single()
            .Tools!;

        Assert.That((tools.Calls, tools.Failed), Is.EqualTo((2, 1)));
        Assert.That(tools.Reasons.Single(),
            Is.EqualTo(new EvalTools.Failure("WebSearch", "Error: limit reached", 1)));
    }

    /// <summary>
    /// You can read a run while it is still going: its finished turns, its mode, and a conversation that has no
    /// scores yet.
    /// </summary>
    [Test]
    public async Task AHalfDoneRun_IsReadable_WhileItRuns()
    {
        // The first turn is done, the second is still streaming.
        var recording = new EvalRecorder(_store).RecordAsync(_run);
        Add(Protocol(), Mode(), Question("bobotie"), Sse("bobotie", "a"), TurnDone("bobotie"),
            Question("bobotie", turn: 2), Sse("bobotie", "b", turn: 2));

        // The recorder writes in the background, so wait for the finished turn to land.
        await Eventually(async () =>
            (await _store.TurnsAsync(_run.Id, new ConversationKey(SampleMode.Beta, 1, "bobotie"))).Count == 1);

        var run = await _store.GetRunAsync(_run.Id);
        Assert.That(run!.Status, Is.EqualTo(EvalRunStatus.Running));
        Assert.That(run.Modes!.Count, Is.EqualTo(1), "the mode is written when it starts");
        var conversation = (await _store.ConversationsAsync(_run.Id)).Single();
        Assert.That((conversation.Order, conversation.Role), Is.EqualTo((1, (string?)null)),
            "the conversation is there before its scores");

        // Let the run end, so the recording doesn't outlive the test.
        _run.Complete(0);
        await recording;
    }

    /// <summary>Conversations keep the order they started in, and each repeat is its own.</summary>
    [Test]
    public async Task Conversations_KeepTheOrderTheyStartedIn_AndEachRepeatIsItsOwn()
    {
        var recording = new EvalRecorder(_store).RecordAsync(_run);
        Add(Protocol(), Mode(), Question("cottage-pie"), TurnDone("cottage-pie"), ConversationDone("cottage-pie"),
            Question("bobotie"), TurnDone("bobotie"), ConversationDone("bobotie"), ModeDone(),
            Mode(repeat: 2), Question("cottage-pie", repeat: 2), TurnDone("cottage-pie", repeat: 2),
            ConversationDone("cottage-pie", repeat: 2));
        _run.Complete(0);
        await recording;

        var conversations = await _store.ConversationsAsync(_run.Id);

        Assert.That(conversations.Select(c => (c.Key.Task, c.Key.Repeat, c.Order)),
            Is.EqualTo(new[] { ("cottage-pie", 1, 1), ("bobotie", 1, 2), ("cottage-pie", 2, 3) }));
        Assert.That((await _store.GetRunAsync(_run.Id))!.Modes!.Select(m => m.Repeat), Is.EqualTo(new[] { 1, 2 }));
    }

    /// <summary>
    /// A turn the run was cut off in is kept with what it had so far, and the run is marked failed with its output in
    /// the log.
    /// </summary>
    [Test]
    public async Task ATurnTheRunCutOff_IsKeptWithWhatItHad()
    {
        var recording = new EvalRecorder(_store).RecordAsync(_run);
        Add(Protocol(), Mode(), Question("bobotie"), Sse("bobotie", "Hal"), "Traceback (most recent call last):");
        _run.Complete(1);
        await recording;

        var key = new TurnKey(SampleMode.Beta, 1, "bobotie", 1);
        var turn = (await _store.TurnsAsync(_run.Id, key.Conversation)).Single();
        Assert.That((turn.Session, turn.Question, turn.Answer),
            Is.EqualTo(("sess_bobotie", "bobotie?", (string?)null)));
        Assert.That(turn.Error, Does.Contain("ended before this turn"));
        Assert.That((await _store.TurnEventsAsync(_run.Id, key)).Count, Is.EqualTo(1));
        var run = await _store.GetRunAsync(_run.Id);
        Assert.That((run!.Status, run.ExitCode), Is.EqualTo((EvalRunStatus.Failed, (int?)1)));
        Assert.That(run.Log, Does.Contain("Traceback (most recent call last):"));
    }

    /// <summary>A run that has already ended is still recorded whole, like a listener that joins late.</summary>
    [Test]
    public async Task ARunThatAlreadyEnded_IsRecordedWhole()
    {
        // The recorder attaches like a late listener: the lines so far, then the rest.
        Add(Protocol(), Mode(), Question("bobotie"), TurnDone("bobotie"), ConversationDone("bobotie"), ModeDone());
        _run.Complete(0);

        await new EvalRecorder(_store).RecordAsync(_run);

        Assert.That((await _store.ConversationsAsync(_run.Id)).Single().Role, Is.EqualTo(SampleRole.Alpha));
        Assert.That((await _store.GetRunAsync(_run.Id))!.Status, Is.EqualTo(EvalRunStatus.Done));
    }

    /// <summary>A protocol version we do not know is said in the log, and the run is still kept.</summary>
    [Test]
    public async Task AnUnknownProtocolVersion_IsSaidInTheLog_AndTheRunIsStillKept()
    {
        var recording = new EvalRecorder(_store).RecordAsync(_run);
        Add(Protocol(version: 2), Mode(), Question("bobotie"), TurnDone("bobotie"));
        _run.Complete(0);
        await recording;

        var run = await _store.GetRunAsync(_run.Id);
        Assert.That(run!.Log!.First(), Does.Contain("protocol 2").And.Contain("reads 1"));
        Assert.That((await _store.TurnsAsync(_run.Id, new ConversationKey(SampleMode.Beta, 1, "bobotie"))).Count,
            Is.EqualTo(1));
    }

    /// <summary>
    /// A store that hands everything to a real one except writing a turn, which fails. It counts the other writes.
    /// </summary>
    /// <remarks>
    /// Decorator: it wraps the in-memory store and changes one method, so the test can break exactly one write and
    /// still read back everything else that was stored.
    /// </remarks>
    private sealed class TurnsFail(IEvalStore inner) : IEvalStore
    {
        /// <summary>How many other writes went through.</summary>
        public int Writes { get; private set; }

        /// <summary>Counts the write and passes it on.</summary>
        public Task SaveRunAsync(Run run, CancellationToken c = default)
        {
            Writes++;
            return inner.SaveRunAsync(run, c);
        }

        /// <summary>Passes the read on.</summary>
        public Task<Run?> GetRunAsync(string id, CancellationToken c = default) => inner.GetRunAsync(id, c);

        /// <summary>Passes the read on.</summary>
        public Task<IReadOnlyList<Run>> ListRunsAsync(string? check = null, int limit = 50,
            CancellationToken c = default) => inner.ListRunsAsync(check, limit, c);

        /// <summary>Counts the write and passes it on.</summary>
        public Task SaveConversationAsync(string run, Conversation conversation, CancellationToken c = default)
        {
            Writes++;
            return inner.SaveConversationAsync(run, conversation, c);
        }

        /// <summary>Passes the read on.</summary>
        public Task<IReadOnlyList<Conversation>> ConversationsAsync(string run, CancellationToken c = default) =>
            inner.ConversationsAsync(run, c);

        /// <summary>Fails, as if the database were down.</summary>
        public Task SaveTurnAsync(string run, Turn turn, IReadOnlyList<StreamEvent> events,
            CancellationToken c = default) => throw new TimeoutException("mongo is down");

        /// <summary>Passes the read on.</summary>
        public Task<IReadOnlyList<Turn>> TurnsAsync(string run, ConversationKey conversation,
            CancellationToken c = default) => inner.TurnsAsync(run, conversation, c);

        /// <summary>Passes the read on.</summary>
        public Task<IReadOnlyList<StreamEvent>>
            TurnEventsAsync(string run, TurnKey turn, CancellationToken c = default) =>
            inner.TurnEventsAsync(run, turn, c);

        /// <summary>Counts the write and passes it on.</summary>
        public Task AddRequestsAsync(string run, IReadOnlyList<Request> requests, CancellationToken c = default)
        {
            Writes++;
            return inner.AddRequestsAsync(run, requests, c);
        }

        /// <summary>Passes the read on.</summary>
        public Task<IReadOnlyList<Request>> RequestsAsync(string run, CancellationToken c = default) =>
            inner.RequestsAsync(run, c);

        /// <summary>Passes it on.</summary>
        public Task<int> CloseOrphansAsync(DateTimeOffset now, CancellationToken c = default) =>
            inner.CloseOrphansAsync(now, c);

        /// <summary>Passes it on.</summary>
        public Task<bool> DeleteRunAsync(string id, CancellationToken c = default) => inner.DeleteRunAsync(id, c);

        /// <summary>Passes it on.</summary>
        public Task<bool> DeleteAsync(string id, CancellationToken c = default) => inner.DeleteAsync(id, c);

        /// <summary>Passes it on.</summary>
        public Task<bool> DeleteConversationsAsync(string run, CancellationToken c = default) =>
            inner.DeleteConversationsAsync(run, c);

        /// <summary>Passes it on.</summary>
        public Task<bool> DeleteTurnsAsync(string run, CancellationToken c = default) => inner.DeleteTurnsAsync(run, c);

        /// <summary>Passes the note on to the real store.</summary>
        public Task<NoteResult> AddNoteAsync(string run, string? text, DateTimeOffset at,
            ConversationKey? conversation = null, CancellationToken c = default) =>
            inner.AddNoteAsync(run, text, at, conversation, c);

        /// <summary>Passes the read on.</summary>
        public Task<IReadOnlyList<RunNote>> NotesAsync(string run, CancellationToken c = default) =>
            inner.NotesAsync(run, c);
    }

    /// <summary>When the store fails, the recording stops and says why, but the run itself still finishes.</summary>
    [Test]
    public async Task AStoreThatFails_StopsTheRecording_NeverTheRun()
    {
        var failing = new TurnsFail(_store);
        var recording = new EvalRecorder(failing).RecordAsync(_run);
        Add(Protocol(), Mode(), Question("bobotie"), TurnDone("bobotie"), ConversationDone("bobotie"),
            Question("cottage-pie"), ModeDone());
        _run.Complete(0);

        var result = await recording;

        Assert.That(result.Stored, Is.False);
        Assert.That(result.StoreError, Does.Contain("mongo is down"));
        var run = await _store.GetRunAsync(_run.Id);
        Assert.That(run!.StoreError, Does.Contain("mongo is down"),
            "the run says what wasn't kept, when the store takes that much");
        Assert.That(run.Status, Is.EqualTo(EvalRunStatus.Done), "the run itself finished");
        var conversation = (await _store.ConversationsAsync(_run.Id)).Single();
        Assert.That(conversation.Role, Is.Null,
            "nothing after the failure was written: not bobotie's scores, not cottage-pie");
    }

    /// <summary>
    /// A recording attached to a run is kept by it, so the end can say whether everything was stored.
    /// </summary>
    [Test]
    public async Task AnAttachedRecording_IsKeptByItsRun_SoTheEndCanSayWhetherItWasStored()
    {
        // The endpoint attaches the recorder when it starts a run, and its "done" event waits for the recording to
        // say stored: true or false.
        var recorder = new EvalRecorder(_store);

        recorder.Attach(_run);
        Add(Protocol(), Mode());
        _run.Complete(0);

        var recording = recorder.RecordingOf(_run.Id);
        Assert.That(recording, Is.Not.Null);
        Assert.That((await recording!).Stored, Is.True);
        Assert.That(recorder.RecordingOf("unknown"), Is.Null);
    }
}
