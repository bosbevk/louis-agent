using LouisAgent.Testing;
using LouisAgent.Api.Benchmarks.Reviews;
using LouisAgent.Api.Benchmarks.Reviews.Store;
using LouisAgent.Api.Benchmarks.Runs;

namespace LouisAgent.Api.Testing;

/// <summary>
/// F18 step 6a.2: what every store of people's grades does, in memory and in MongoDB alike. A grade is added, never overwritten: the latest of a reviewer's grades on a conversation is
/// the one that counts, and the earlier ones are kept with their times (history). Reads come back oldest first. Every test makes its own run ids, so they share a real database
/// without seeing each other's grades; times are whole milliseconds in UTC, as MongoDB keeps them.
/// </summary>
public abstract class HumanReviewStoreContract
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 26, 9, 0, 0, TimeSpan.Zero);
    private static readonly BenchmarkProtocol.ConversationKey Bobotie = new(SampleMode.Beta, 1, "bobotie");

    private IHumanReviewStore _store = null!;

    protected abstract IHumanReviewStore CreateStore();

    [SetUp]
    public void SetUpStore() => _store = CreateStore();

    private static string NewRun() => Guid.NewGuid().ToString("N")[..12];

    private static HumanGrade Grade(string run, int seconds, HumanVerdict verdict = HumanVerdict.Pass,
        string reviewer = "Louis", string note = "", BenchmarkProtocol.ConversationKey? key = null,
        string grader = "meal-for-people") =>
        new(run, key ?? Bobotie, grader, verdict, note, reviewer, T0.AddSeconds(seconds));

    [Test]
    public async Task AGrade_ReadsBackWithEverythingItWasGivenWith()
    {
        string run = NewRun();
        var grade = Grade(run, 1, HumanVerdict.Fail, "Louis", "a side dish");

        await _store.AddAsync(grade);

        Assert.That(await _store.OfRunAsync(run), Is.EqualTo(new[] { grade }));
    }

    [Test]
    public async Task ASecondGradeOfTheSameConversationByTheSameReviewer_DoesNotReplaceTheFirst_BothAreKept()
    {
        string run = NewRun();
        var first = Grade(run, 1, HumanVerdict.Fail);
        var second = Grade(run, 5, HumanVerdict.Pass, note: "on second thoughts");

        await _store.AddAsync(second);
        await _store.AddAsync(first);

        Assert.That(await _store.OfRunAsync(run), Is.EqualTo(new[] { first, second }), "history, oldest first");
    }

    [Test]
    public async Task GradesComeBackOldestFirst_AcrossConversationsAndReviewers()
    {
        string run = NewRun();
        var one = Grade(run, 3, reviewer: "Anna");
        var two = Grade(run, 1, key: new(SampleMode.Gamma, 2, SampleScenario.Alpha));
        var three = Grade(run, 2, HumanVerdict.Unsure);

        foreach (var grade in new[] { one, two, three }) await _store.AddAsync(grade);

        Assert.That((await _store.OfRunAsync(run)).Select(g => g.At.Second), Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public async Task AnotherRunsGrades_AreNotThisRuns()
    {
        string mine = NewRun(), other = NewRun();
        await _store.AddAsync(Grade(mine, 1));
        await _store.AddAsync(Grade(other, 2));

        Assert.That((await _store.OfRunAsync(mine)).Select(g => g.Run), Is.EqualTo(new[] { mine }));
        Assert.That(await _store.OfRunAsync(NewRun()), Is.Empty);
    }

    [Test]
    public async Task UnsureIsKept_ItIsAGradeToo()
    {
        string run = NewRun();
        await _store.AddAsync(Grade(run, 1, HumanVerdict.Unsure, note: "cannot tell"));

        Assert.That((await _store.OfRunAsync(run)).Single().Verdict, Is.EqualTo(HumanVerdict.Unsure));
    }

    [Test]
    public async Task DeletingARun_TakesItsGradesWithIt_AndNoOthers()
    {
        string gone = NewRun(), kept = NewRun();
        await _store.AddAsync(Grade(gone, 1));
        await _store.AddAsync(Grade(gone, 2));
        await _store.AddAsync(Grade(kept, 3));

        int deleted = await _store.DeleteRunAsync(gone);

        Assert.That(deleted, Is.EqualTo(2));
        Assert.That(await _store.OfRunAsync(gone), Is.Empty);
        Assert.That(await _store.OfRunAsync(kept), Has.Count.EqualTo(1));
    }
}
