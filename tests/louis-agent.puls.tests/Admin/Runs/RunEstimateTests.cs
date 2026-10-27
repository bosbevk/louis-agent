using LouisAgent.Testing;
using LouisAgent.Puls.Admin.Runs;

namespace LouisAgent.Puls.Tests.Admin.Runs;

// The Benchmarks page rework (Louis, 2026-10-26: "make it user friendly"): the Run button says what a run will roughly cost, from the last run that measured the agent (not a
// re-score, which cost nothing, and not a failed run), scaled to the repeats chosen; and a check is selected when the page opens, the one with the newest run, so the runs list is
// never empty on arrival. Written first: neither class existed.
public class RunEstimateTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 26, 8, 0, 0, TimeSpan.Zero);

    private static RunSummary Run(string id, string check, int repeat, decimal? cost, int hoursAgo, string status = "done", string? rescoreOf = null) =>
        new(id, check, repeat, status, At.AddHours(-hoursAgo), cost is null ? null : new TotalSummary(cost, 10), null, null, rescoreOf);

    [Test]
    public void TheEstimate_IsTheLastMeasuredRunsCostPerRepeat_TimesTheRepeatsChosen()
    {
        var runs = new[] { Run("new", SampleEval.Beta, 3, 0.24m, hoursAgo: 1), Run("old", SampleEval.Beta, 1, 0.10m, hoursAgo: 5) };

        Assert.That(RunEstimate.For(SampleEval.Beta, runs, repeats: 2), Is.EqualTo(0.16m));
    }

    [Test]
    public void AReScore_AFailedRun_AndAnotherChecksRun_AreNotMeasures()
    {
        var runs = new[]
        {
            Run("rescore", SampleEval.Beta, 1, 0m, hoursAgo: 1, rescoreOf: "old"),
            Run("broken", SampleEval.Beta, 1, 0.5m, hoursAgo: 2, status: "failed"),
            Run("other", SampleEval.Gamma, 1, 9m, hoursAgo: 3),
            Run("old", SampleEval.Beta, 1, 0.10m, hoursAgo: 4),
        };

        Assert.That(RunEstimate.For(SampleEval.Beta, runs, repeats: 1), Is.EqualTo(0.10m));
    }

    [Test]
    public void NoMeasuredRun_OrAnUnknownCost_GivesNoEstimate()
    {
        Assert.That(RunEstimate.For(SampleEval.Beta, [], repeats: 1), Is.Null);
        Assert.That(RunEstimate.For(SampleEval.Beta, [Run("unpriced", SampleEval.Beta, 1, null, hoursAgo: 1)], repeats: 1), Is.Null);
    }

    [Test]
    public void TheDefaultCheck_IsTheOneWithTheNewestRun_ElseTheFirstCheck()
    {
        var checks = new[] { "stub", SampleEval.Beta, SampleEval.Gamma };
        var runs = new[] { Run("a", SampleEval.Gamma, 1, 1m, hoursAgo: 3), Run("b", SampleEval.Beta, 1, 1m, hoursAgo: 1) };

        Assert.That(RunEstimate.DefaultCheck(checks, runs), Is.EqualTo(SampleEval.Beta));
        Assert.That(RunEstimate.DefaultCheck(checks, []), Is.EqualTo("stub"));
        Assert.That(RunEstimate.DefaultCheck([], []), Is.Null);
    }

    // 6.0b (the 30ab27f..6533d8d review): the text beside the Run button left the razor page for this class. Written first: only the page had it.
    private static string Dollars(decimal cost) => "$" + cost.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

    [Test]
    public void TheStub_IsFree_WhateverItsRuns()
    {
        Assert.That(RunEstimate.Text("stub", [Run("a", "stub", 1, 9m, hoursAgo: 1)], 3, Dollars), Is.EqualTo("free"));
    }

    [Test]
    public void TheText_SaysTheCostAndTheRepeats_InThePagesMoney()
    {
        var runs = new[] { Run("new", SampleEval.Beta, 3, 0.24m, hoursAgo: 1) };

        Assert.That(RunEstimate.Text(SampleEval.Beta, runs, 2, Dollars), Is.EqualTo("≈ $0.16 for 2 repeats"));
        Assert.That(RunEstimate.Text(SampleEval.Beta, runs, 1, Dollars), Is.EqualTo("≈ $0.08 for 1 repeat"), "one repeat is singular");
    }

    [Test]
    public void AnUnknownCost_GivesNoText_NotAZero()
    {
        Assert.That(RunEstimate.Text(SampleEval.Beta, [], 2, Dollars), Is.Empty);
        Assert.That(RunEstimate.Text(SampleEval.Beta, [Run("unpriced", SampleEval.Beta, 1, null, hoursAgo: 1)], 2, Dollars), Is.Empty);
    }
}
