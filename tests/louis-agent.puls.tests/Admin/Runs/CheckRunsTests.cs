using LouisAgent.Puls.Admin.Runs;

namespace LouisAgent.Puls.Tests.Admin.Runs;

/// <summary>
/// The runs list under the cards: the chosen check's runs, the models they ran on, the ones the model filter shows,
/// and what they cost together.
/// </summary>
public class CheckRunsTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 26, 8, 0, 0, TimeSpan.Zero);

    /// <summary>Three runs of the check (one priced, one free, one unknown) and one of another check.</summary>
    private static readonly RunSummary[] Runs =
    [
        Run("a", SampleEval.Beta, 0.25m, ["haiku"]),
        Run("b", SampleEval.Beta, 0m, ["sonnet", "haiku"]),
        Run("c", SampleEval.Beta, null, null),
        Run("d", SampleEval.Gamma, 9m, ["opus"])
    ];

    /// <summary>The check's runs, the models they name, and their known costs added up.</summary>
    [Test]
    public void TheChecksRuns_TheirModels_AndTheirCost()
    {
        var runs = CheckRuns.For(SampleEval.Beta, Runs, model: null);

        Assert.That(runs.Runs.Select(r => r.Id), Is.EqualTo(new[] { "a", "b", "c" }));
        Assert.That(runs.Shown, Is.EqualTo(runs.Runs));
        Assert.That(runs.Models, Is.EqualTo(new[] { "haiku", "sonnet" }));
        Assert.That(runs.Cost, Is.EqualTo(0.25m));
    }

    /// <summary>A model picked shows only the runs on it; the count and the cost stay the check's.</summary>
    [Test]
    public void AModelPicked_ShowsOnlyItsRuns()
    {
        var runs = CheckRuns.For(SampleEval.Beta, Runs, model: "sonnet");

        Assert.That(runs.Shown.Select(r => r.Id), Is.EqualTo(new[] { "b" }));
        Assert.That(runs.Runs, Has.Count.EqualTo(3));
    }

    /// <summary>No check chosen, or no cost known: no runs, and no cost rather than 0.</summary>
    [Test]
    public void NothingKnown_IsNoCost()
    {
        Assert.That(CheckRuns.For(null, Runs, model: null).Runs, Is.Empty);
        Assert.That(CheckRuns.For(SampleEval.Alpha, [Run("e", SampleEval.Alpha, null, null)], null).Cost, Is.Null);
    }

    /// <summary>A run of a check, with its cost and models.</summary>
    private static RunSummary Run(string id, string check, decimal? cost, IReadOnlyList<string>? models) =>
        new(id, check, 1, "done", At, cost is null ? null : new TotalSummary(cost, 1), null, models);
}
