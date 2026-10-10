using louis_agent.core.usage;

namespace louis_agent.core.tests;

// F1-S1: rounds numbered per turn; the scope flows across await and nests.
public class UsageScopeTests
{
    [Test]
    public void Current_OutsideAnyScope_IsNull()
    {
        Assert.That(UsageScope.Current, Is.Null);
    }

    [Test]
    public void Begin_MakesTheScopeCurrent()
    {
        using var scope = UsageScope.Begin(UsagePurpose.Summary);

        Assert.That(UsageScope.Current, Is.SameAs(scope));
        Assert.That(scope.Purpose, Is.EqualTo(UsagePurpose.Summary));
    }

    [Test]
    public void NextRound_CountsFromOne()
    {
        using var scope = UsageScope.Begin();

        Assert.That(new[] { scope.NextRound(), scope.NextRound(), scope.NextRound() }, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void NextRound_NewScope_StartsAgainAtOne()
    {
        // Each turn opens its own scope, so the second turn's first request is round 1 again.
        using (var firstTurn = UsageScope.Begin())
        {
            firstTurn.NextRound();
            firstTurn.NextRound();
        }

        using var secondTurn = UsageScope.Begin();
        Assert.That(secondTurn.NextRound(), Is.EqualTo(1));
    }

    [Test]
    public void NextRound_ConcurrentRequests_GetDistinctRounds()
    {
        using var scope = UsageScope.Begin();

        int[] rounds = new int[1000];
        Parallel.For(0, rounds.Length, i => rounds[i] = scope.NextRound());

        Assert.That(rounds.Order(), Is.EqualTo(Enumerable.Range(1, rounds.Length)));
    }

    [Test]
    public void Dispose_NestedScope_RestoresOuter()
    {
        using var outer = UsageScope.Begin();
        using (UsageScope.Begin(UsagePurpose.Summary))
        {
            Assert.That(UsageScope.Current, Is.Not.SameAs(outer));
        }

        Assert.That(UsageScope.Current, Is.SameAs(outer));
    }

    [Test]
    public void Dispose_Twice_DoesNotClobberANewerScope()
    {
        var first = UsageScope.Begin();
        first.Dispose();
        using var second = UsageScope.Begin();

        first.Dispose();

        Assert.That(UsageScope.Current, Is.SameAs(second));
    }

    [Test]
    public async Task Current_FlowsAcrossAwait()
    {
        using var scope = UsageScope.Begin();

        await Task.Yield();
        UsageScope? seenOnAnotherThread = await Task.Run(() => UsageScope.Current);

        Assert.That(UsageScope.Current, Is.SameAs(scope));
        Assert.That(seenOnAnotherThread, Is.SameAs(scope));
    }

    [Test]
    public async Task Begin_InsideAnAwaitedCall_DoesNotLeakToTheCaller()
    {
        // AsyncLocal changes made inside an async method stay inside it: a host's scope can't leak into the next turn.
        using var outer = UsageScope.Begin();

        await OpenScopeWithoutDisposingAsync();

        Assert.That(UsageScope.Current, Is.SameAs(outer));
    }

    [Test]
    public async Task Begin_InsideAnAsyncIterator_IsLostAfterItsFirstYield()
    {
        // Why hosts that stream from an iterator (the API's SSE endpoint) can't just call Begin: each MoveNextAsync runs
        // in the caller's context, so the scope the iterator opened is gone after it yields.
        var seen = new List<UsageScope?>();
        await foreach (var _ in ScopeInIterator(seen)) { }

        Assert.That(seen[0], Is.Not.Null);
        Assert.That(seen[1], Is.Null);
    }

    private static async IAsyncEnumerable<int> ScopeInIterator(List<UsageScope?> seen)
    {
        using var scope = UsageScope.Begin();
        seen.Add(UsageScope.Current);
        yield return 1;
        await Task.Yield();
        seen.Add(UsageScope.Current);
    }

    [Test]
    public void Begin_Nested_InheritsHostSessionAndTurn()
    {
        // A summary opens its own scope inside the turn's; its records must still say whose turn it was.
        using var turn = UsageScope.Begin(host: "api", session: "sess_1", turn: 3);
        using var summary = UsageScope.Begin(UsagePurpose.Summary);

        Assert.That((summary.Host, summary.Session, summary.Turn, summary.Purpose), Is.EqualTo(("api", "sess_1", (int?)3, UsagePurpose.Summary)));
    }

    [Test]
    public void Begin_Nested_GivenValuesWinOverTheOuterOnes()
    {
        using var outer = UsageScope.Begin(host: "api", session: "sess_1", turn: 3);
        using var inner = UsageScope.Begin(session: "sess_2", turn: 1);

        Assert.That((inner.Host, inner.Session, inner.Turn), Is.EqualTo(("api", "sess_2", (int?)1)));
    }

    [Test]
    public void Begin_Nested_InheritsTagsAndMergesGivenOnes()
    {
        using var turn = UsageScope.Begin(tags: new UsageTags("fix:e8b0", "run-1", "order-service"));
        using var summary = UsageScope.Begin(UsagePurpose.Summary);
        using var retagged = UsageScope.Begin(tags: new UsageTags("fix:other", null, null));

        Assert.That(summary.Tags, Is.EqualTo(new UsageTags("fix:e8b0", "run-1", "order-service")));
        Assert.That(retagged.Tags, Is.EqualTo(new UsageTags("fix:other", "run-1", "order-service")), "a given tag wins, the rest are inherited");
    }

    [Test]
    public void Begin_NoTags_HasNone()
    {
        using var scope = UsageScope.Begin();

        Assert.That(scope.Tags, Is.EqualTo(UsageTags.None));
    }

    [Test]
    public void Activate_MakesTheScopeCurrentAndRestoresThePreviousOne()
    {
        using var outer = UsageScope.Begin();
        var other = UsageScope.Begin(host: "api");
        other.Dispose();

        using (other.Activate())
            Assert.That(UsageScope.Current, Is.SameAs(other));

        Assert.That(UsageScope.Current, Is.SameAs(outer));
    }

    [Test]
    public async Task Activate_AroundEachStep_ReachesTheInnerStreamFromAnIterator()
    {
        // The API's pattern: an SSE iterator activates the turn's scope around each MoveNextAsync of the engine's stream,
        // so every model request inside it, including those after a yield, sees the scope.
        var seen = new List<string?>();
        await foreach (var _ in OuterIterator(seen)) { }

        Assert.That(seen, Is.EqualTo(new[] { "api", "api", "api" }));
    }

    private static async IAsyncEnumerable<int> OuterIterator(List<string?> seen)
    {
        var usage = UsageScope.Begin(host: "api");
        await using var inner = InnerStream(seen).GetAsyncEnumerator();
        while (true)
        {
            bool more;
            using (usage.Activate()) more = await inner.MoveNextAsync();
            if (!more) break;
            yield return inner.Current;
        }
    }

    private static async IAsyncEnumerable<int> InnerStream(List<string?> seen)
    {
        for (int i = 0; i < 3; i++)
        {
            await Task.Yield();
            seen.Add(UsageScope.Current?.Host);
            yield return i;
        }
    }

    private static async Task OpenScopeWithoutDisposingAsync()
    {
        await Task.Yield();
        UsageScope.Begin(UsagePurpose.Summary);
    }
}