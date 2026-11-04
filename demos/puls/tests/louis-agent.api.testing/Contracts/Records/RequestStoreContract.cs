using LouisAgent.Api.Evals.Store.Requests;
using LouisAgent.Testing;
using static LouisAgent.Api.Evals.Store.EvalDocuments;
using LouisAgent.Evals.Protocol.Parts.Conversations;

namespace LouisAgent.Api.Testing;

/// <summary>
/// What every store of model requests does, in memory and in MongoDB alike: a run's come back by time, only that
/// run's, and what the ledger didn't know stays unknown. Every test makes its own run ids, so the tests can share a
/// real database. Times are whole milliseconds in UTC, as MongoDB keeps them.
/// </summary>
/// <remarks>
/// Abstract test (a contract test): the tests are written once, and each store gets a small subclass that only says
/// how to make one, so the in-memory store the unit tests use and the Mongo store can't drift apart.
/// </remarks>
public abstract class RequestStoreContract
{
    /// <summary>The moment the tests start from.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 10, 17, 9, 0, 0, TimeSpan.Zero);

    /// <summary>The store under test.</summary>
    private IRequestStore _store = null!;

    /// <summary>A store to test; each test gets a fresh one (a real store may share its database).</summary>
    protected abstract IRequestStore CreateStore();

    /// <summary>A fresh store before every test.</summary>
    [SetUp]
    public void SetUpStore() => _store = CreateStore();

    /// <summary>A run id no other test uses, so tests sharing a database never meet.</summary>
    private static string NewId() => Guid.NewGuid().ToString("N")[..12];

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

    /// <summary>Adding no requests is fine, and keeps nothing.</summary>
    [Test]
    public async Task AddingNoRequests_KeepsNothing()
    {
        string run = NewId();

        await _store.AddRequestsAsync(run, []);

        Assert.That(await _store.RequestsAsync(run), Is.Empty);
    }

    /// <summary>Deleting a run's requests takes only that run's, and a second delete finds nothing.</summary>
    [Test]
    public async Task DeletingARunsRequests_TakesOnlyThatRuns_AndSaysWhetherThereWereAny()
    {
        string run = NewId(), other = NewId();
        foreach (string id in new[] { run, other })
        {
            var request = new Request(id, null, T0, "agent_turn", null, null, "m", 1, 1, 0, 0, 0.01m, 10);
            await _store.AddRequestsAsync(id, [request]);
        }

        Assert.That(await _store.DeleteRequestsAsync(run), Is.EqualTo(1));

        Assert.That(await _store.RequestsAsync(run), Is.Empty);
        Assert.That(await _store.DeleteRequestsAsync(run), Is.Zero, "nothing left to delete");
        Assert.That((await _store.RequestsAsync(other)).Count, Is.EqualTo(1), "another run keeps its own");
    }
}
