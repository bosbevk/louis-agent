using LouisAgent.Api.Contracts.Admin.Evals;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Choices;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Puls.Components.Admin.EvalsPage;

namespace LouisAgent.Puls.Tests.Components.Admin.EvalsPage;

/// <summary>
/// The eval card as a person sees it (CL26): the latest run up top with a ring per mode, the runs before it as a
/// timeline where a replay links to its recording, and every grader in the fold.
/// </summary>
public class EvalCardTests
{
    /// <summary>When the runs started; the card only shows it.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    /// <summary>A finished run of two modes, replaying another run if it says so.</summary>
    private static EvalRunRow Run(string id, string? replayOf = null) =>
        new(new StoredRunRow(id, "cook", 1, "done", 0, T0, T0.AddMinutes(9), new RunTotalRow(0.07m, "USD", 1000, 90),
                null, null, "cook", 10, null, null, null, replayOf),
            [
                new ModeRow(SampleMode.Alpha, 1, [], null, null, 0.25, 0.25, 1, 0, 0),
                new ModeRow(SampleMode.Beta, 1, [], null, null, 1.0, 1.0, 1, 0, 0)
            ]);

    /// <summary>An eval with three graders and these runs, newest first.</summary>
    private static EvalRow Eval(IReadOnlyList<EvalRunRow> runs) =>
        new("cook", SampleIdentity.Alpha, 10, "Meals", true, new EvalDatasetRow("meals", 7), ["budget", "follow-up"],
        [
            new("no-error", 1, CheckKind.FixedRule.Wire()),
            new("meal", 2, CheckKind.Model.Wire()),
            new("meal.human", 2, CheckKind.Human.Wire())
        ], [SampleMode.Alpha, SampleMode.Beta], [], runs.Count == 0, runs);

    /// <summary>The card for an eval with these runs.</summary>
    private static IRenderedComponent<EvalCard> Render(BunitContext ctx, IReadOnlyList<EvalRunRow> runs) =>
        ctx.Render<EvalCard>(p => p.Add(x => x.Eval, Eval(runs)));

    /// <summary>The latest run's scorecard has a ring per mode, each with its pass@k in the middle.</summary>
    [Test]
    public void TheScorecard_HasARingPerMode()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, [Run("run_2"), Run("run_1")]);

        var rings = cut.FindAll(".eval-score .donut-centre strong").Select(s => s.TextContent).ToList();
        Assert.That(rings, Is.EqualTo(new[] { "25%", "100%" }));
    }

    /// <summary>A replay on the timeline is tagged and links to the recording it replayed.</summary>
    [Test]
    public void AReplayOnTheTimeline_LinksToItsRecording()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, [Run("run_3"), Run("run_2", replayOf: "run_1"), Run("run_1")]);

        var tag = cut.Find(".eval-timeline .run-origin.is-replay");
        Assert.That(tag.TextContent, Is.EqualTo("replay of run_1"));
        Assert.That(tag.GetAttribute("href"), Is.EqualTo("admin/benchmarks/run_1"));
        Assert.That(cut.FindAll(".eval-timeline > li"), Has.Count.EqualTo(2), "the latest is in the scorecard");
    }

    /// <summary>The fold holds every grader, by kind, with the categories and modes.</summary>
    [Test]
    public void TheFold_HoldsEveryGrader()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, [Run("run_1")]);

        var graders = cut.FindAll(".eval-checks .eval-grader").Select(g => g.TextContent.Trim()).ToList();
        Assert.That(graders, Is.EqualTo(new[] { "no-error v1", "meal v2", "meal.human v2" }));
        Assert.That(cut.Find(".eval-checks .fold-note").TextContent,
            Is.EqualTo("3 graders · 2 categories · 2 modes"));
    }

    /// <summary>An eval that never ran has no scorecard and no timeline, but still says what it checks.</summary>
    [Test]
    public void AnEvalThatNeverRan_ShowsOnlyWhatItChecks()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, []);

        Assert.That(cut.FindAll(".eval-score"), Is.Empty);
        Assert.That(cut.FindAll(".eval-timeline"), Is.Empty);
        Assert.That(cut.FindAll(".eval-checks"), Has.Count.EqualTo(1));
    }
}
