using System.Text.Json;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Testing;
using static LouisAgent.Api.Evals.Store.EvalDocuments;
using LouisAgent.Evals.Protocol.Parts.Conversations;

namespace LouisAgent.Api.Testing;

/// <summary>
/// What the whole store of eval runs does beyond its four record stores, in memory and in MongoDB alike: deleting a run
/// takes everything it kept. Each record's own tests are in its contract (<see cref="RunStoreContract"/>,
/// <see cref="ConversationStoreContract"/>, <see cref="TurnStoreContract"/>, <see cref="RequestStoreContract"/>), the
/// notes' in <see cref="EvalNotesContract"/>. Every test makes its own run ids, so the tests can share a real database.
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

    /// <summary>A store to test; each test gets a fresh one (a real store may share its database).</summary>
    protected abstract IEvalStore CreateStore();

    /// <summary>A fresh store before every test.</summary>
    [SetUp]
    public void SetUpStore() => _store = CreateStore();

    /// <summary>A run id no other test uses, so tests sharing a database never meet.</summary>
    private static string NewId() => Guid.NewGuid().ToString("N")[..12];

    /// <summary>Parses a bit of JSON into a value we can keep.</summary>
    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();

    /// <summary>An ended conversation of bobotie, the beta mode's first repeat.</summary>
    private static Conversation NewConversation() =>
        new(new ConversationKey(SampleMode.Beta, 1, "bobotie"), 1, SampleRole.Alpha,
            new Metrics(1, 0, [], 1, null, null, 12.5, null), 0, 12.5);

    /// <summary>The first turn of bobotie's conversation.</summary>
    private static Turn NewTurn() =>
        new(new TurnKey(SampleMode.Beta, 1, "bobotie", 1), "sess_1", "Vraag 1", "Hier is…", SampleRole.Alpha, null, 1,
            7, ["www.checkers.co.za"], 41.3, null);

    /// <summary>One streamed event, so the turn has something to replay.</summary>
    private static StreamEvent Event() => new("message_start", Json("""{"delta":{"text":"a"}}"""), T0);

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
            await _store.SaveRunAsync(new Run(id, "check-" + id[..6], 2, EvalRunStatus.Done, T0));
            await _store.SaveConversationAsync(id, NewConversation());
            await _store.SaveTurnAsync(id, NewTurn(), [Event()]);
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

    /// <summary>
    /// A run whose own record is gone but whose conversations are still there counts as a run when it's deleted, and
    /// its leftovers go.
    /// </summary>
    [Test]
    public async Task DeletingALeftover_WithNoRunRecord_StillClearsIt()
    {
        string run = NewId();
        await _store.SaveConversationAsync(run, NewConversation());

        Assert.That(await _store.DeleteRunAsync(run), Is.True);

        Assert.That(await _store.ConversationsAsync(run), Is.Empty);
    }
}
