using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.Tests.Benchmarks.Store;

/// <summary>
/// What every benchmark store keeps on a run beyond the first fields, in memory and in MongoDB alike (F18 steps 5b.2 and 5c.2): the run it re-scored, a total of a known zero, and the
/// roll-up's slices as the runner wrote them. Apart from <see cref="BenchmarkStoreContract"/> because that file is at its size limit; the same rules (a run before a field reads null).
/// </summary>
public abstract class BenchmarkRunFieldsContract
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 17, 9, 0, 0, TimeSpan.Zero);

    private IBenchmarkStore _store = null!;

    /// <summary>A store to test; each test gets a fresh one.</summary>
    protected abstract IBenchmarkStore CreateStore();

    [SetUp]
    public void SetUpStore() => _store = CreateStore();

    private static string NewId() => Guid.NewGuid().ToString("N")[..12];

    private static Run NewRun(string id) => new(id, "check-" + id[..6], 2, BenchmarkRunStatus.Running, T0);

    // F18 step 6a.3: the question of a human review is kept with the run's graders, in both stores.
    [Test]
    public async Task AHumanReviewsQuestion_ReadsBackWithTheRunsGraders()
    {
        string id = NewId();
        var graders = new[] { new Grader("routed-to", 1, BenchmarkGraderKind.FixedRule), new Grader("meal-for-people.human", 1, BenchmarkGraderKind.Human, "Is this a meal for the people asked for?") };
        await _store.SaveRunAsync(NewRun(id) with { Graders = graders });

        var read = (await _store.GetRunAsync(id))!;

        Assert.That(read.Graders, Is.EqualTo(graders));
        Assert.That(read.Graders![0].Question, Is.Null);
    }

    [Test]
    public async Task ARunsRescoreOf_ReadsBackAsItWasWritten_AndARunBeforeItHasNone()
    {
        string rescored = NewId(), ordinary = NewId();

        await _store.SaveRunAsync(NewRun(rescored) with { RescoreOf = "old123456789" });
        await _store.SaveRunAsync(NewRun(ordinary));

        Assert.That((await _store.GetRunAsync(rescored))!.RescoreOf, Is.EqualTo("old123456789"));
        Assert.That((await _store.GetRunAsync(ordinary))!.RescoreOf, Is.Null);
    }

    [Test]
    public async Task ATotalOfZero_ReadsBackAsZero_NotAsUnknown()
    {
        // A re-score asked nothing, so its cost is known to be 0; null is "unknown" (ADR 0007).
        string id = NewId();

        await _store.SaveRunAsync(NewRun(id) with { Total = new Total(0m, "USD", 0, 0) });

        Assert.That((await _store.GetRunAsync(id))!.Total, Is.EqualTo(new Total(0m, "USD", 0, 0)));
    }

    [Test]
    public async Task ARunsSlices_ReadBackAsTheRunnerWroteThem_AndARunBeforeThemHasNone()
    {
        string rolled = NewId(), older = NewId();
        var slices = new[]
        {
            new BenchmarkSlice(SampleMode.Beta, "overall", 8, 6, 1, 1, 0, ["a", "b"], false, 0.75, 1.0, 0.5,
                [new SliceGrader("routed-to", BenchmarkGraderKind.FixedRule, 7, 1, 0)], [new SliceKind(BenchmarkGraderKind.FixedRule, 7, 1, 0)], [new SliceScore("priced_ratio", 0.5, 8)]),
            new BenchmarkSlice(SampleMode.Beta, "language:af", 3, 3, 0, 0, 0, ["a"], true, null, null, null, [], [], []),
        };

        await _store.SaveRunAsync(NewRun(rolled) with { Slices = slices });
        await _store.SaveRunAsync(NewRun(older));

        // A record holds its lists by reference, so the slices are compared by what they say.
        Assert.That(JsonSerializer.Serialize((await _store.GetRunAsync(rolled))!.Slices), Is.EqualTo(JsonSerializer.Serialize(slices)), "every field, a rate left out still null");
        Assert.That((await _store.GetRunAsync(older))!.Slices, Is.Null, "never an empty list, never zeros");
    }
}
