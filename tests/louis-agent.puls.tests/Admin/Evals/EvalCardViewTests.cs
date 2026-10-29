using LouisAgent.Api.Contracts.Admin.Evals;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Choices;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Puls.Admin.Evals;

namespace LouisAgent.Puls.Tests.Admin.Evals;

/// <summary>
/// What an eval's card shows, worked out before any markup: the latest run up top with each mode's rates, the
/// runs before it as a timeline that says which were replays or re-scores, and the graders grouped by kind (CL26).
/// </summary>
public class EvalCardViewTests
{
    /// <summary>When the runs started; the view only passes it through.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 10, 29, 18, 0, 0, TimeSpan.Zero);

    /// <summary>A finished run with these modes, re-scoring or replaying another run if it says so.</summary>
    private static EvalRunRow Run(string id, IReadOnlyList<ModeRow> modes, string? rescoreOf = null,
        string? replayOf = null) =>
        new(new StoredRunRow(id, "cook", 1, "done", 0, T0, T0.AddMinutes(9), null, null, null, "cook", 10, null,
            rescoreOf, null, replayOf), modes);

    /// <summary>One repeat of a mode with its rates, or with none, as a mode from before the rates.</summary>
    private static ModeRow Mode(string name, int repeat, double? passAtK, double? passHatK) =>
        new(name, repeat, [], null, null, passAtK, passHatK, passAtK is null ? null : repeat, 0, 0);

    /// <summary>An eval with these graders and these runs, newest first, as the API lists them.</summary>
    private static EvalRow Eval(IReadOnlyList<EvalRunRow> runs, IReadOnlyList<EvalGraderRow>? graders = null) =>
        new("cook", SampleIdentity.Alpha, 10, "Meals", true, null, [], graders ?? [], [], [], runs.Count == 0, runs);

    /// <summary>The newest run leads the card, and the timeline under it holds the ones before, newest first.</summary>
    [Test]
    public void TheNewestRun_LeadsTheCard_AndTheTimelineHoldsTheOthers()
    {
        var view = EvalCardView.Of(Eval([Run("run_3", []), Run("run_2", []), Run("run_1", [])]));

        Assert.That(view.Latest?.Run.Run.Id, Is.EqualTo("run_3"));
        Assert.That(view.Runs.Select(r => r.Run.Run.Id), Is.EqualTo(new[] { "run_2", "run_1" }));
    }

    /// <summary>Each mode shows the rates of its last repeat, the runner's numbers over every repeat so far.</summary>
    [Test]
    public void EachMode_ShowsItsLastRepeatsRates()
    {
        var run = Run("run_1",
        [
            Mode(SampleMode.Alpha, 1, 0.25, 0.25),
            Mode(SampleMode.Beta, 1, 1.0, 1.0),
            Mode(SampleMode.Alpha, 2, 0.5, 0.25),
            Mode(SampleMode.Beta, 2, 1.0, 0.875)
        ]);

        var latest = EvalCardView.Of(Eval([run])).Latest!;

        Assert.That(latest.Modes.Select(m => (m.Mode, m.K, m.PassAtK, m.PassHatK)),
            Is.EqualTo(new[]
            {
                (SampleMode.Alpha, (int?)2, (double?)0.5, (double?)0.25),
                (SampleMode.Beta, 2, 1.0, 0.875)
            }));
    }

    /// <summary>A mode with no rates keeps them empty, so the card shows a dash, never 0.</summary>
    [Test]
    public void AModeWithNoRates_KeepsThemEmpty()
    {
        var latest = EvalCardView.Of(Eval([Run("run_1", [Mode(SampleMode.Alpha, 1, null, null)])])).Latest!;

        Assert.That(latest.Modes.Single().PassAtK, Is.Null);
        Assert.That(latest.Modes.Single().PassHatK, Is.Null);
    }

    /// <summary>A run says whether it was recorded, replayed or re-scored, and which run it came from.</summary>
    [TestCase(null, null, RunOrigin.Recorded, null)]
    [TestCase(null, "run_0", RunOrigin.Replay, "run_0")]
    [TestCase("run_0", null, RunOrigin.Rescore, "run_0")]
    public void ARun_SaysWhereItCameFrom(string? rescoreOf, string? replayOf, RunOrigin origin, string? source)
    {
        var view = EvalCardView.Of(Eval([Run("run_2", []), Run("run_1", [], rescoreOf, replayOf)]));

        Assert.That(view.Runs.Single().Origin, Is.EqualTo(origin));
        Assert.That(view.Runs.Single().Source, Is.EqualTo(source));
        Assert.That(view.Latest!.Origin, Is.EqualTo(RunOrigin.Recorded), "the latest says it too");
    }

    /// <summary>The graders group by what they judge with, in the order each kind first comes.</summary>
    [Test]
    public void TheGraders_GroupByKind_InTheOrderTheyCome()
    {
        var view = EvalCardView.Of(Eval([],
        [
            new("no-error", 1, GraderKind.FixedRule.Wire()),
            new("meal", 2, GraderKind.Model.Wire()),
            new("budget", 2, GraderKind.FixedRule.Wire()),
            new("meal.human", 2, GraderKind.Human.Wire())
        ]));

        Assert.That(view.Graders.Select(g => (g.Kind, string.Join(",", g.Graders.Select(x => x.Name)))),
            Is.EqualTo(new[] { ("fixed rule", "no-error,budget"), ("model", "meal"), ("a person", "meal.human") }));
    }

    /// <summary>An eval that never ran has no scorecard and an empty timeline.</summary>
    [Test]
    public void AnEvalThatNeverRan_HasNoScorecard()
    {
        var view = EvalCardView.Of(Eval([]));

        Assert.That(view.Latest, Is.Null);
        Assert.That(view.Runs, Is.Empty);
    }
}
