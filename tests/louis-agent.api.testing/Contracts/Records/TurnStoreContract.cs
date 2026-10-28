using System.Text.Json;
using LouisAgent.Api.Evals.Reports;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Evals.Store.Turns;
using LouisAgent.Testing;
using static LouisAgent.Api.Evals.Store.EvalDocuments;
using static LouisAgent.Api.Evals.Runs.Protocol.EvalProtocol;

namespace LouisAgent.Api.Testing;

/// <summary>
/// What every store of turns does, in memory and in MongoDB alike. Saving a turn again replaces it and its events, a
/// conversation's turns come back by number, and the events come back on their own, in the order they came. Every
/// test makes its own run ids, so the tests can share a real database. Times are whole milliseconds in UTC, as MongoDB
/// keeps them.
/// </summary>
/// <remarks>
/// Abstract test (a contract test): the tests are written once, and each store gets a small subclass that only says
/// how to make one, so the in-memory store the unit tests use and the Mongo store can't drift apart.
/// </remarks>
public abstract class TurnStoreContract
{
    /// <summary>The moment the tests start from.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 10, 17, 9, 0, 0, TimeSpan.Zero);

    /// <summary>The store under test.</summary>
    private ITurnStore _store = null!;

    /// <summary>A store to test; each test gets a fresh one (a real store may share its database).</summary>
    protected abstract ITurnStore CreateStore();

    /// <summary>A fresh store before every test.</summary>
    [SetUp]
    public void SetUpStore() => _store = CreateStore();

    /// <summary>A run id no other test uses, so tests sharing a database never meet.</summary>
    private static string NewId() => Guid.NewGuid().ToString("N")[..12];

    /// <summary>Parses a bit of JSON into a value we can keep.</summary>
    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();

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
    /// Deleting a run's turns takes their events too, only that run's, and a second delete finds nothing.
    /// </summary>
    [Test]
    public async Task DeletingARunsTurns_TakesTheirEvents_OnlyThatRuns_AndSaysWhetherThereWereAny()
    {
        string run = NewId(), other = NewId();
        var key = new TurnKey(SampleMode.Beta, 1, "bobotie", 1);
        await _store.SaveTurnAsync(run, NewTurn("bobotie", 1), [Event("message_start", "a")]);
        await _store.SaveTurnAsync(other, NewTurn("bobotie", 1), [Event("message_start", "a")]);

        Assert.That(await _store.DeleteTurnsAsync(run), Is.True);

        Assert.That(await _store.TurnsAsync(run, key.Conversation), Is.Empty);
        Assert.That(await _store.TurnEventsAsync(run, key), Is.Empty);
        Assert.That(await _store.DeleteTurnsAsync(run), Is.False, "nothing left to delete");
        Assert.That((await _store.TurnEventsAsync(other, key)).Count, Is.EqualTo(1), "another run keeps its own");
    }
}
