using LouisAgent.Testing;
using LouisAgent.Api.Benchmarks.Reports;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.Tests.Benchmarks.Reports;

// F18 step 5c.4: a check's trend per category, per model: a point per run and category that has a rate. A slice with too few trials has no rate, so it is a gap in the line, not a 0 (ADR 0007);
// a replay, a re-score, a held-out run and a run still going are left out as the overall trend leaves them out (a trend point means "the agent was measured here"); a run from before 5c has no
// slices and adds no point. A line is a mode on one model: two models of one mode are two lines. The API computes no rate: the points are the runner's. Written first: the trend threw
// NotImplementedException.
public class CategoryTrendTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 25, 9, 0, 0, TimeSpan.Zero);
    private InMemoryBenchmarkStore _store = null!;

    [SetUp]
    public void SetUp() => _store = new InMemoryBenchmarkStore();

    private static BenchmarkSlice Slice(string slice, double? rate, string mode = SampleMode.Beta, bool tooFew = false, int n = 8) =>
        new(mode, slice, n, 6, 2, 0, 0, ["a"], tooFew, tooFew ? null : rate, tooFew ? null : rate, tooFew ? null : rate / 2, [], [], []);

    private async Task Run(string id, int hour, IReadOnlyList<BenchmarkSlice>? slices, string? model = "claude-haiku-5-5", BenchmarkRunStatus status = BenchmarkRunStatus.Done,
        BenchmarkRunSplit? split = null, string? rescoreOf = null, IReadOnlyDictionary<string, string>? settings = null) =>
        await _store.SaveRunAsync(new Run(id, "puls-cook", 1, status, T0.AddHours(hour), Settings: settings, Split: split, RescoreOf: rescoreOf, Slices: slices,
            Models: model is null ? null : [new ModelUse(SampleMode.Beta, BenchmarkModelLevel.Agent, null, model)]));

    private async Task<IReadOnlyList<CategoryTrends.Category>> Compute() => await CategoryTrends.ComputeAsync(_store, "puls-cook");

    [Test]
    public async Task EachCategory_HasALinePerModeOnItsModel_AndAPointPerRunWithARate()
    {
        await Run("r1", 1, [Slice("overall", 0.5), Slice("language:af", 0.4)]);
        await Run("r2", 2, [Slice("overall", 0.75), Slice("language:af", 0.6)]);

        var categories = await Compute();

        Assert.That(categories.Select(c => c.Name), Is.EqualTo(new[] { "language:af", "overall" }), "sorted by name");
        var line = categories.Single(c => c.Name == "language:af").Lines.Single();
        Assert.That(line.Name, Is.EqualTo($"{SampleMode.Beta} (claude-haiku-5-5)"));
        Assert.That(line.Points.Select(p => (p.Run, p.PassRate, p.PassAtK, p.PassHatK, p.N)), Is.EqualTo(new[]
        {
            ("r1", (double?)0.4, (double?)0.4, (double?)0.2, 8),
            ("r2", (double?)0.6, (double?)0.6, (double?)0.3, 8),
        }));
    }

    [Test]
    public async Task ASliceWithTooFewTrials_IsAGapInTheLine_NotAZero()
    {
        await Run("r1", 1, [Slice("overall", 0.5), Slice("budget", 0.5)]);
        await Run("r2", 2, [Slice("overall", 0.5), Slice("budget", null, tooFew: true, n: 2)]);
        await Run("r3", 3, [Slice("overall", 0.5), Slice("budget", 1.0)]);

        var line = (await Compute()).Single(c => c.Name == "budget").Lines.Single();

        Assert.That(line.Points.Select(p => p.Run), Is.EqualTo(new[] { "r1", "r3" }), "r2 has no point: a gap");
        Assert.That(line.Points.Select(p => p.PassRate), Has.None.Zero);
    }

    [Test]
    public async Task ACategoryNoRunEverHadEnoughTrialsFor_HasNoChart()
    {
        await Run("r1", 1, [Slice("overall", 0.5), Slice("tiny", null, tooFew: true, n: 1)]);

        Assert.That((await Compute()).Select(c => c.Name), Is.EqualTo(new[] { "overall" }));
    }

    [Test]
    public async Task TwoModelsOfOneMode_AreTwoLines_NeverOneLineJoiningThem()
    {
        await Run("r1", 1, [Slice("overall", 0.5)], model: "claude-haiku-5-5");
        await Run("r2", 2, [Slice("overall", 0.9)], model: "claude-opus-5-5");

        var lines = (await Compute()).Single().Lines;

        Assert.That(lines.Select(l => l.Name), Is.EquivalentTo(new[] { $"{SampleMode.Beta} (claude-haiku-5-5)", $"{SampleMode.Beta} (claude-opus-5-5)" }));
        Assert.That(lines.All(l => l.Points.Count == 1), Is.True);
    }

    [Test]
    public async Task EachMode_IsItsOwnLine_InTheSameCategory()
    {
        await Run("r1", 1, [Slice("overall", 0.5), Slice("overall", 0.1, mode: SampleMode.Gamma)]);

        var lines = (await Compute()).Single().Lines;

        Assert.That(lines.Select(l => (l.Name, l.Points.Single().PassRate)), Is.EquivalentTo(new[] { ($"{SampleMode.Beta} (claude-haiku-5-5)", (double?)0.5), (SampleMode.Gamma, (double?)0.1) }), "a mode the run names no model for is named by itself");
    }

    [Test]
    public async Task AReplay_AReScore_AHeldOutRun_AndARunStillGoing_AreNoPoints()
    {
        await Run("kept", 1, [Slice("overall", 0.5)]);
        await Run("replay", 2, [Slice("overall", 0.5)], settings: new Dictionary<string, string> { ["replay_of"] = "kept" });
        await Run("rescore", 3, [Slice("overall", 0.9)], rescoreOf: "kept");
        await Run("held", 4, [Slice("overall", 0.5)], split: BenchmarkRunSplit.HeldOut);
        await Run("going", 5, [Slice("overall", 0.5)], status: BenchmarkRunStatus.Running);

        var line = (await Compute()).Single().Lines.Single();

        Assert.That(line.Points.Select(p => p.Run), Is.EqualTo(new[] { "kept" }));
    }

    [Test]
    public async Task ARunFromBefore5c_HasNoSlices_AndAddsNoPoint()
    {
        await Run("old", 1, null);
        await Run("new", 2, [Slice("overall", 0.5)]);

        Assert.That((await Compute()).Single().Lines.Single().Points.Select(p => p.Run), Is.EqualTo(new[] { "new" }));
    }

    [Test]
    public async Task NoRuns_AreNoCategories() =>
        Assert.That(await Compute(), Is.Empty);
}
