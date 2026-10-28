using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Api.Evals.Reports;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Evals.Store;
using static LouisAgent.Api.Evals.Store.EvalDocuments;
using static LouisAgent.Api.Evals.Runs.Protocol.EvalProtocol;

namespace LouisAgent.Api.Testing;

/// <summary>
/// What every store of eval runs does, in memory and in MongoDB alike. A write replaces whatever has the same key, and
/// reads come back in the run's order, so the recorder can write a document again as it grows and the page can read
/// the run while it goes. Every test makes its own run ids and check name, so the tests can share a real database
/// without seeing each other's runs. Times are whole milliseconds in UTC, as MongoDB keeps them. The runs' own tests
/// are in <see cref="RunStoreContract"/>, the conversations' in <see cref="ConversationStoreContract"/>.
/// </summary>
/// <remarks>
/// Abstract test (a contract test): the tests are written once, and each store gets a small subclass that only says
/// how to make one, so the in-memory store the unit tests use and the Mongo store can't drift apart.
/// </remarks>
public abstract class EvalStoreContract
{
    /// <summary>The moment the tests start from.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 10, 17, 9, 0, 0, TimeSpan.Zero);

    /// <summary>The store under test.</summary>
    private IEvalStore _store = null!;

    /// <summary>This test's own check name, so its runs never show up in another test's list.</summary>
    private string _check = "";

    /// <summary>A store to test; each test gets a fresh one (a real store may share its database).</summary>
    protected abstract IEvalStore CreateStore();

    /// <summary>A fresh store and a check name of its own before every test.</summary>
    [SetUp]
    public void SetUpStore()
    {
        _store = CreateStore();
        _check = "check-" + Guid.NewGuid().ToString("N")[..8];
    }

    /// <summary>A run id no other test uses, so tests sharing a database never meet.</summary>
    private static string NewId() => Guid.NewGuid().ToString("N")[..12];

    /// <summary>A run of this test's check with only its first fields, still going unless told otherwise.</summary>
    private Run NewRun(string id, DateTimeOffset startedAt, EvalRunStatus status = EvalRunStatus.Running) =>
        new(id, _check, 2, status, startedAt);

    /// <summary>Parses a bit of JSON into a value we can keep.</summary>
    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();

    /// <summary>A scored conversation of one task, by default the beta mode's first repeat.</summary>
    private static Conversation NewConversation(string task, int order, string? role = SampleRole.Alpha,
        string mode = SampleMode.Beta, int repeat = 1) =>
        new(new ConversationKey(mode, repeat, task), order, role, Json("""{"routed_right":true,"priced_ratio":0.75}"""),
            0, 12.5);

    /// <summary>Turn number <paramref name="n"/> of a task's conversation, answered unless told otherwise.</summary>
    private static Turn NewTurn(string task, int n, string? answer = "Hier is…", string mode = SampleMode.Beta,
        int repeat = 1) =>
        new(new TurnKey(mode, repeat, task, n), "sess_" + n, "Vraag " + n, answer, SampleRole.Alpha, null, 1, 7,
            ["www.checkers.co.za"], 41.3, null);

    /// <summary>
    /// One streamed event carrying some text. Its time is the text's length in seconds, so events have different
    /// times.
    /// </summary>
    private static StreamEvent Event(string type, string text) =>
        new(type, Json(JsonSerializer.Serialize(new { delta = new { text } })), T0.AddSeconds(text.Length));

    /// <summary>A conversation's turns come back by number with every field, and only that conversation's.</summary>
    [Test]
    public async Task Turns_ReadBackByNumber_ForTheirConversationOnly()
    {
        // Two turns of bobotie out of order, plus turns of another task and another repeat.
        string run = NewId();
        await _store.SaveTurnAsync(run, NewTurn("bobotie", 2), []);
        await _store.SaveTurnAsync(run, NewTurn("bobotie", 1), []);
        await _store.SaveTurnAsync(run, NewTurn("cottage-pie", 1), []);
        await _store.SaveTurnAsync(run, NewTurn("bobotie", 1, repeat: 2), []);

        var read = await _store.TurnsAsync(run, new ConversationKey(SampleMode.Beta, 1, "bobotie"));

        Assert.That(read.Select(t => t.Key.Turn), Is.EqualTo(new[] { 1, 2 }));
        var first = read[0];
        Assert.That((first.Session, first.Question, first.Answer, first.Role, first.Continued),
            Is.EqualTo(("sess_1", "Vraag 1", "Hier is…", SampleRole.Alpha, (string?)null)));
        Assert.That((first.AgentCalls, first.SpecialistCalls, first.Seconds, first.Error),
            Is.EqualTo((1, 7, 41.3, (string?)null)));
        Assert.That(first.Hosts, Is.EqualTo(new[] { "www.checkers.co.za" }));
    }

    /// <summary>A turn's failure comes back as written, and a turn that didn't fail has none.</summary>
    [Test]
    public async Task ATurnsFailure_ReadsBackAsItWasWritten_AndATurnWithNoneHasNone()
    {
        string run = NewId();
        await _store.SaveTurnAsync(run, NewTurn("bobotie", 1) with { Failure = EvalTurnFailure.ReplayMiss }, []);
        await _store.SaveTurnAsync(run, NewTurn("bobotie", 2), []);

        var read = await _store.TurnsAsync(run, new ConversationKey(SampleMode.Beta, 1, "bobotie"));

        Assert.That(read.Select(t => t.Failure),
            Is.EqualTo(new EvalTurnFailure?[] { EvalTurnFailure.ReplayMiss, null }));
    }

    /// <summary>A turn's stream events are read on their own, in the order they came, with data and time.</summary>
    [Test]
    public async Task ATurnsEvents_ReadBackApart_InTheOrderTheyCame()
    {
        string run = NewId();
        var key = new TurnKey(SampleMode.Beta, 1, "bobotie", 1);
        await _store.SaveTurnAsync(run, NewTurn("bobotie", 1),
            [Event("message_start", "a"), Event("content_block_delta", "Hallo"), Event("message_stop", "")]);

        var events = await _store.TurnEventsAsync(run, key);

        Assert.That(events.Select(e => e.Type),
            Is.EqualTo(new[] { "message_start", "content_block_delta", "message_stop" }));
        Assert.That(events[1].Data.GetProperty("delta").GetProperty("text").GetString(), Is.EqualTo("Hallo"));
        Assert.That(events[1].At, Is.EqualTo(T0.AddSeconds(5)));
    }

    /// <summary>Saving a turn again replaces its events and answer, as the recorder does while it streams.</summary>
    [Test]
    public async Task ATurnSavedAgain_ReplacesItsEvents()
    {
        // First half-done with one event, then finished with two.
        string run = NewId();
        var key = new TurnKey(SampleMode.Beta, 1, "bobotie", 1);
        await _store.SaveTurnAsync(run, NewTurn("bobotie", 1, answer: null), [Event("message_start", "a")]);
        await _store.SaveTurnAsync(run, NewTurn("bobotie", 1),
            [Event("message_start", "a"), Event("message_stop", "")]);

        Assert.That((await _store.TurnEventsAsync(run, key)).Count, Is.EqualTo(2));
        Assert.That((await _store.TurnsAsync(run, key.Conversation)).Single().Answer, Is.EqualTo("Hier is…"));
    }

    /// <summary>
    /// A turn's tool counts are kept on the turn, so opening a run never reads every event to count them. A turn
    /// recorded before the counts were kept has none.
    /// </summary>
    [Test]
    public async Task ATurnsToolCounts_ReadBackWithIt_AndNoneStayNone()
    {
        string run = NewId();
        var counted = NewTurn("bobotie", 1) with
        {
            Tools = new EvalTools.Summary(3, 1,
                [new EvalTools.Failure("WebSearch", "Error: Tavily's limit was reached", 1)]),
        };
        await _store.SaveTurnAsync(run, counted, []);
        await _store.SaveTurnAsync(run, NewTurn("bobotie", 2), []);

        var turns = await _store.TurnsAsync(run, counted.Key.Conversation);

        var tools = turns[0].Tools!;
        Assert.That((tools.Calls, tools.Failed), Is.EqualTo((3, 1)));
        Assert.That(tools.Reasons.Single(),
            Is.EqualTo(new EvalTools.Failure("WebSearch", "Error: Tavily's limit was reached", 1)));
        Assert.That(turns[1].Tools, Is.Null, "a turn recorded before the counts were kept: unknown, not 0 calls");
    }

    /// <summary>A turn we have nothing for has no events, rather than an error.</summary>
    [Test]
    public async Task TheEventsOfAnUnknownTurn_AreEmpty() =>
        Assert.That(await _store.TurnEventsAsync(NewId(), new TurnKey("m", 1, "t", 1)), Is.Empty);

    /// <summary>
    /// A run's model requests come back by time, only that run's, and a price or count we didn't know stays unknown.
    /// </summary>
    [Test]
    public async Task Requests_ReadBackByTime_WithUnknownsKeptUnknown()
    {
        string run = NewId();
        var turn = new TurnKey(SampleMode.Beta, 1, "bobotie", 1);

        // A request some seconds into the run; leave out the cost or the tokens to make them unknown.
        Request At(int seconds, TurnKey? key = null, decimal? cost = 0.01m, long? input = 1200) =>
            new(run, key, T0.AddSeconds(seconds), "agent_turn", "sess_1", null, "claude-haiku-5-5", input, 300, 50_000,
                0, cost, 2100);

        await _store.AddRequestsAsync(run, [At(30, turn), At(10, turn)]);
        await _store.AddRequestsAsync(run, [At(20, key: null, cost: null, input: null)]);
        await _store.AddRequestsAsync(NewId(), [At(5)]);

        var read = await _store.RequestsAsync(run);

        Assert.That(read.Select(r => r.At),
            Is.EqualTo(new[] { T0.AddSeconds(10), T0.AddSeconds(20), T0.AddSeconds(30) }));
        Assert.That(read[0], Is.EqualTo(At(10, turn)));
        Assert.That((read[1].Turn, read[1].Cost, read[1].InputTokens),
            Is.EqualTo(((TurnKey?)null, (decimal?)null, (long?)null)),
            "an unknown price or count stays unknown, never 0");
    }

    /// <summary>
    /// Deleting a run takes everything it kept with it (conversations, turns, events, requests) and leaves other runs
    /// alone; deleting it twice finds nothing the second time.
    /// </summary>
    [Test]
    public async Task DeletingARun_DeletesEverythingItKept()
    {
        // Two runs with one of everything each.
        string run = NewId(), other = NewId();
        var turn = new TurnKey(SampleMode.Beta, 1, "bobotie", 1);
        foreach (string id in new[] { run, other })
        {
            await _store.SaveRunAsync(NewRun(id, T0, EvalRunStatus.Done));
            await _store.SaveConversationAsync(id, NewConversation("bobotie", 1));
            await _store.SaveTurnAsync(id, NewTurn("bobotie", 1), [Event("message_start", "a")]);
            await _store.AddRequestsAsync(id,
                [new Request(id, turn, T0, "agent_turn", "sess_1", null, "m", 1, 1, 0, 0, 0.01m, 10)]);
        }

        Assert.That(await _store.DeleteRunAsync(run), Is.True);

        Assert.That(await _store.GetRunAsync(run), Is.Null);
        Assert.That(await _store.ConversationsAsync(run), Is.Empty);
        Assert.That(await _store.TurnsAsync(run, turn.Conversation), Is.Empty);
        Assert.That(await _store.TurnEventsAsync(run, turn), Is.Empty);
        Assert.That(await _store.RequestsAsync(run), Is.Empty);
        Assert.That(await _store.DeleteRunAsync(run), Is.False, "nothing left to delete");
        Assert.That((await _store.ConversationsAsync(other)).Count, Is.EqualTo(1), "another run keeps its own");
    }
}
