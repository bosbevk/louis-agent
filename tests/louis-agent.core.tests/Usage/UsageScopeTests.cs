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

    private static async Task OpenScopeWithoutDisposingAsync()
    {
        await Task.Yield();
        UsageScope.Begin(UsagePurpose.Summary);
    }
}