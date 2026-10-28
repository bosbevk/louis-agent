using LouisAgent.Api.Evals.Reports.Trends;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Evals.Protocol.Kinds;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Tests.Evals.Reports.Trends;

/// <summary>
/// An eval's trend per category and per model: a point for each run where the category has a rate. Too few trials
/// means no rate, so a gap in the line, never a 0. Replays, re-scores, held-out runs and runs still going are left
/// out, as the overall trend leaves them out, and older runs with no slices add nothing. Each line is one mode on one
/// model, and the rates are the runner's: the API works none out.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
public class CategoryTrendTests
{
    /// <summary>When the first run started; each run starts some hours after.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 10, 25, 9, 0, 0, TimeSpan.Zero);

    /// <summary>A fresh store for each test.</summary>
    private InMemoryEvalStore _store = null!;

    /// <summary>Starts each test with an empty store.</summary>
    [SetUp]
    public void SetUp() => _store = new InMemoryEvalStore();

    /// <summary>
    /// A slice with six passes in eight trials and the rate you give it (pass^k at half). A slice with too few trials
    /// gets no rates.
    /// </summary>
    private static EvalSlice Slice(string slice, double? rate, string mode = SampleMode.Beta, bool tooFew = false,
        int n = 8) =>
        new(mode, slice, n, 6, 2, 0, 0, ["a"], tooFew, tooFew ? null : rate, tooFew ? null : rate,
            tooFew ? null : rate / 2, [], []);

    /// <summary>Saves a run of the eval with these slices, by default a finished one on Haiku.</summary>
    private async Task Run(string id, int hour, IReadOnlyList<EvalSlice>? slices,
        string? model = "claude-haiku-5-5", EvalRunStatus status = EvalRunStatus.Done,
        RunSplit? split = null, string? rescoreOf = null,
        IReadOnlyDictionary<string, string>? settings = null) =>
        await _store.SaveRunAsync(new Run(id, SampleEval.Beta, 1, status, T0.AddHours(hour), Settings: settings,
            Split: split, RescoreOf: rescoreOf, Slices: slices,
            Models: model is null ? null : [new ModelUse(SampleMode.Beta, ModelLevel.Agent, null, model)]));

    /// <summary>Works out the eval's category trends from the store.</summary>
    private async Task<IReadOnlyList<CategoryTrends.Category>> Compute() =>
        await CategoryTrends.ComputeAsync(_store, SampleEval.Beta);

    /// <summary>
    /// Each category, sorted by name, has a line for the mode on its model, with a point and its rates for each run.
    /// </summary>
    [Test]
    public async Task EachCategory_HasALinePerModeOnItsModel_AndAPointPerRunWithARate()
    {
        await Run("r1", 1, [Slice("overall", 0.5), Slice("language:af", 0.4)]);
        await Run("r2", 2, [Slice("overall", 0.75), Slice("language:af", 0.6)]);

        var categories = await Compute();

        Assert.That(categories.Select(c => c.Name), Is.EqualTo(new[] { "language:af", "overall" }), "sorted by name");
        var line = categories.Single(c => c.Name == "language:af").Lines.Single();
        Assert.That(line.Name, Is.EqualTo($"{SampleMode.Beta} (claude-haiku-5-5)"));
        Assert.That(line.Points.Select(p => (p.Run, p.PassRate, p.PassAtK, p.PassHatK, p.N)),
            Is.EqualTo(new[]
            {
                ("r1", (double?)0.4, (double?)0.4, (double?)0.2, 8),
                ("r2", (double?)0.6, (double?)0.6, (double?)0.3, 8),
            }));
    }

    /// <summary>A run with too few trials in a category leaves a gap in that line, not a zero.</summary>
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

    /// <summary>A category that never had enough trials gets no chart.</summary>
    [Test]
    public async Task ACategoryNoRunEverHadEnoughTrialsFor_HasNoChart()
    {
        await Run("r1", 1, [Slice("overall", 0.5), Slice("tiny", null, tooFew: true, n: 1)]);

        Assert.That((await Compute()).Select(c => c.Name), Is.EqualTo(new[] { "overall" }));
    }

    /// <summary>The same mode on two models makes two lines, never one line across both.</summary>
    [Test]
    public async Task TwoModelsOfOneMode_AreTwoLines_NeverOneLineJoiningThem()
    {
        await Run("r1", 1, [Slice("overall", 0.5)], model: "claude-haiku-5-5");
        await Run("r2", 2, [Slice("overall", 0.9)], model: "claude-opus-5-5");

        var lines = (await Compute()).Single().Lines;

        Assert.That(lines.Select(l => l.Name),
            Is.EquivalentTo(new[] { $"{SampleMode.Beta} (claude-haiku-5-5)", $"{SampleMode.Beta} (claude-opus-5-5)" }));
        Assert.That(lines.All(l => l.Points.Count == 1), Is.True);
    }

    /// <summary>Each mode gets its own line in a category; a mode with no model is named by itself.</summary>
    [Test]
    public async Task EachMode_IsItsOwnLine_InTheSameCategory()
    {
        await Run("r1", 1, [Slice("overall", 0.5), Slice("overall", 0.1, mode: SampleMode.Gamma)]);

        var lines = (await Compute()).Single().Lines;

        Assert.That(lines.Select(l => (l.Name, l.Points.Single().PassRate)),
            Is.EquivalentTo(new[]
            {
                ($"{SampleMode.Beta} (claude-haiku-5-5)", (double?)0.5), (SampleMode.Gamma, (double?)0.1)
            }),
            "a mode the run names no model for is named by itself");
    }

    /// <summary>Replays, re-scores, held-out runs and runs still going add no point.</summary>
    [Test]
    public async Task AReplay_AReScore_AHeldOutRun_AndARunStillGoing_AreNoPoints()
    {
        await Run("kept", 1, [Slice("overall", 0.5)]);
        await Run("replay", 2, [Slice("overall", 0.5)],
            settings: new Dictionary<string, string> { ["replay_of"] = "kept" });
        await Run("rescore", 3, [Slice("overall", 0.9)], rescoreOf: "kept");
        await Run("held", 4, [Slice("overall", 0.5)], split: RunSplit.HeldOut);
        await Run("going", 5, [Slice("overall", 0.5)], status: EvalRunStatus.Running);

        var line = (await Compute()).Single().Lines.Single();

        Assert.That(line.Points.Select(p => p.Run), Is.EqualTo(new[] { "kept" }));
    }

    /// <summary>A run from before the slices has none, so it adds no point.</summary>
    [Test]
    public async Task ARunFromBefore5c_HasNoSlices_AndAddsNoPoint()
    {
        await Run("old", 1, null);
        await Run("new", 2, [Slice("overall", 0.5)]);

        Assert.That((await Compute()).Single().Lines.Single().Points.Select(p => p.Run), Is.EqualTo(new[] { "new" }));
    }

    /// <summary>With no runs there are no categories.</summary>
    [Test]
    public async Task NoRuns_AreNoCategories() =>
        Assert.That(await Compute(), Is.Empty);
}
