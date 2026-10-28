using LouisAgent.Testing;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Outcomes;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Reports;
using LouisAgent.Evals.Running;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Tests.Reports;

/// <summary>
/// A mode's trials rolled up into one overall line and one line per category, on made-up trials: the overall line is
/// the sum of its parts, a trial in two categories counts in both and once overall, an Unknown and an infrastructure
/// trial are not passed (the latter is left out of the rates and counted), and a slice of fewer than five counted
/// trials says "too few" and has no rates, never 0.
/// </summary>
public class RunSummaryTests
{
    /// <summary>A required fixed rule.</summary>
    private static readonly IGrader Route = new FakeGrader("routed-to", GraderKind.FixedRule);

    /// <summary>A required model grader.</summary>
    private static readonly IGrader Judge = new FakeGrader("judge", GraderKind.Model);

    /// <summary>A grader that is not required, so the roll-up leaves it out.</summary>
    private static readonly IGrader Optional = new FakeGrader("nice-to-have", GraderKind.FixedRule, required: false);

    /// <summary>The eval's graders.</summary>
    private static readonly IReadOnlyList<IGrader> Graders = [Route, Judge, Optional];

    /// <summary>A grade from the grader with that outcome, scoring 1 for a pass and 0 otherwise.</summary>
    private static Grade G(IGrader grader, GradeOutcome outcome) => new(grader.Name, grader.Version,
        outcome == GradeOutcome.Pass ? 1 : 0, outcome, "because");

    /// <summary>A trial of the scenario, with its outcome, categories and the two required grades.</summary>
    private static TrialResult T(string scenario, TrialOutcome outcome, string[]? categories = null,
        GradeOutcome route = GradeOutcome.Pass, GradeOutcome judge = GradeOutcome.Pass) =>
        new(scenario, categories ?? [], outcome, [G(Route, route), G(Judge, judge)]);

    /// <summary>A trial that passed.</summary>
    private static TrialResult Pass(string scenario, params string[] categories) =>
        T(scenario, TrialOutcome.Pass, categories);

    /// <summary>A trial that failed its route.</summary>
    private static TrialResult Fail(string scenario, params string[] categories) =>
        T(scenario, TrialOutcome.Fail, categories, route: GradeOutcome.Fail);

    /// <summary>The line of one slice.</summary>
    private static SummaryLine Line(IEnumerable<SummaryLine> lines, string slice) =>
        lines.Single(line => line.Slice == slice);

    /// <summary>The roll-up of the trials, for one mode.</summary>
    private static IReadOnlyList<SummaryLine> Of(params TrialResult[] trials) =>
        RunSummary.Of(SampleMode.Beta, trials, Graders);

    /// <summary>The overall line comes first, then each category by name, all for the mode.</summary>
    [Test]
    public void TheOverallLineComesFirst_ThenEachCategoryByName_AllForTheMode()
    {
        var lines = Of(Pass("a", "shop:pnp", "language:af"), Pass("b", "budget"));

        Assert.That(lines.Select(l => l.Slice), Is.EqualTo(new[] { "overall", "budget", "language:af", "shop:pnp" }));
        Assert.That(lines.Select(l => l.Mode).Distinct(), Is.EqualTo(new[] { SampleMode.Beta }));
    }

    /// <summary>The overall counts are the sum of the trials, and each category's the sum of its own.</summary>
    [Test]
    public void TheOverallCountsAreTheSumOfTheTrials_AndEachCategorysAreTheSumOfItsOwn()
    {
        var lines = Of(Pass("a", "x"), Pass("b", "x"), Fail("c", "x"), Pass("d", "y"),
            T("e", TrialOutcome.Unknown, ["y"], judge: GradeOutcome.Unknown),
            T("f", TrialOutcome.Infrastructure, ["y"]));

        var overall = Line(lines, "overall");
        Assert.That((overall.N, overall.Passed, overall.Failed, overall.Unknown, overall.Infrastructure),
            Is.EqualTo((6, 3, 1, 1, 1)));
        var x = Line(lines, "x");
        Assert.That((x.N, x.Passed, x.Failed, x.Unknown, x.Infrastructure), Is.EqualTo((3, 2, 1, 0, 0)));
        var y = Line(lines, "y");
        Assert.That((y.N, y.Passed, y.Failed, y.Unknown, y.Infrastructure), Is.EqualTo((3, 1, 0, 1, 1)));
        Assert.That(x.N + y.N, Is.EqualTo(overall.N),
            "no trial is in two of these categories, so they sum to the overall line");
    }

    /// <summary>A trial in two categories counts in both and once overall; one with none only overall.</summary>
    [Test]
    public void ATrialInTwoCategories_CountsInBoth_AndOnceInTheOverall_AndOneWithNoneOnlyInTheOverall()
    {
        var lines = Of(Pass("both", "a", "b"), Pass("none"));

        Assert.That(Line(lines, "overall").N, Is.EqualTo(2));
        Assert.That(Line(lines, "a").N, Is.EqualTo(1));
        Assert.That(Line(lines, "b").N, Is.EqualTo(1));
        Assert.That(lines.Select(l => l.Slice), Is.EqualTo(new[] { "overall", "a", "b" }),
            "a trial with no category makes no line of its own");
    }

    /// <summary>
    /// An Unknown trial is not passed, and an infrastructure one is counted but left out of the rates.
    /// </summary>
    [Test]
    public void AnUnknownTrialIsNotPassed_AndAnInfrastructureOneIsCountedButLeftOutOfTheRates()
    {
        var trials = new[]
        {
            Pass("a"), Pass("b"), Pass("c"), Pass("d"), T("e", TrialOutcome.Unknown, route: GradeOutcome.Unknown),
            T("f", TrialOutcome.Infrastructure)
        };

        var overall = Line(RunSummary.Of("m", trials, Graders), "overall");

        Assert.That((overall.N, overall.Passed, overall.Unknown, overall.Infrastructure), Is.EqualTo((6, 4, 1, 1)));
        Assert.That(overall.PassRate, Is.EqualTo(4.0 / 5),
            "passed over the five that counted: the infrastructure trial is not one of them");
    }

    /// <summary>
    /// A slice of three trials is too few: it keeps its counts and has no rates; one of five has them.
    /// </summary>
    [Test]
    public void ASliceOfThreeTrialsIsTooFew_WithItsCountsAndNoRates_AndOneOfFiveHasThem()
    {
        var few = Line(Of(Pass("a", "small"), Pass("b", "small"), Fail("c", "small")), "small");
        var five = Line(Of(Pass("a"), Pass("b"), Pass("c"), Pass("d"), Fail("e")), "overall");

        Assert.That(few.TooFew, Is.True);
        Assert.That((few.N, few.Passed, few.Failed), Is.EqualTo((3, 2, 1)), "the counts stay");
        Assert.That((few.PassRate, few.PassAtK, few.PassHatK),
            Is.EqualTo(((double?)null, (double?)null, (double?)null)), "never 0");
        Assert.That(five.TooFew, Is.False);
        Assert.That((five.PassRate, five.PassAtK, five.PassHatK), Is.EqualTo((0.8, 0.8, 0.8)),
            "five scenarios, one trial each: pass@1 and pass^1 are the pass rate");
    }

    /// <summary>Exactly four counted trials are too few, and exactly five have rates.</summary>
    [Test]
    public void ExactlyFourCountedTrials_AreTooFew_AndExactlyFiveHaveRates()
    {
        // The threshold is five: one short of it has no rates, and the fifth trial gives them (a change of the
        // constant fails here).
        var four = Line(Of(Pass("a"), Pass("b"), Pass("c"), Fail("d")), "overall");
        var five = Line(Of(Pass("a"), Pass("b"), Pass("c"), Fail("d"), Pass("e")), "overall");

        Assert.That(RunSummary.MinimumForRates, Is.EqualTo(5));
        Assert.That((four.TooFew, four.PassRate), Is.EqualTo((true, (double?)null)));
        Assert.That((five.TooFew, five.PassRate), Is.EqualTo((false, (double?)0.8)));
    }

    /// <summary>Five trials of which only three count are too few: the rates come from the counted ones.</summary>
    [Test]
    public void FiveTrialsOfWhichOnlyThreeCount_AreTooFew()
    {
        var lines = Of(Pass("a"), Pass("b"), Pass("c"), T("d", TrialOutcome.Infrastructure),
            T("e", TrialOutcome.Infrastructure));

        Assert.That(Line(lines, "overall").TooFew, Is.True, "the rates are from the counted trials");
    }

    /// <summary>The overall line's pass@k and pass^k are the mode's, the same as its mode's end gives.</summary>
    [Test]
    public void PassAtKAndPassHatK_AreThoseOfModeTally_ForTheOverallLine()
    {
        // --repeat 3 over three scenarios: a always passes, b sometimes, c never; one infrastructure trial.
        var trials = new[]
        {
            Pass("a"), Pass("a"), Pass("a"), Pass("b"), Fail("b"), Pass("b"), Fail("c"), Fail("c"),
            T("c", TrialOutcome.Infrastructure),
        };
        var tally = new ModeTally();
        foreach (var trial in trials) tally.Add(trial);

        var overall = Line(RunSummary.Of("m", trials, Graders), "overall");

        Assert.That(overall.TooFew, Is.False);
        Assert.That(overall.PassAtK, Is.EqualTo(tally.Share.PassAtK));
        Assert.That(overall.PassHatK, Is.EqualTo(tally.Share.PassHatK));
        Assert.That((overall.PassAtK, overall.PassHatK), Is.EqualTo((2.0 / 3, 1.0 / 3)));
    }

    /// <summary>A slice's tasks are its scenario ids, once each, in order.</summary>
    [Test]
    public void TheSlicesTasks_AreItsScenarioIds_OnceEach_InOrder()
    {
        var lines = Of(Pass("b", "x"), Pass("a", "x"), Pass("b", "x"), Pass("c"));

        Assert.That(Line(lines, "x").Tasks, Is.EqualTo(new[] { "a", "b" }));
        Assert.That(Line(lines, "overall").Tasks, Is.EqualTo(new[] { "a", "b", "c" }));
    }

    /// <summary>The grader counts are of the required graders only, by name and by kind.</summary>
    [Test]
    public void TheGraderCounts_AreOfTheRequiredGradersOnly_ByNameAndByKind()
    {
        var lines = Of(Pass("a"), Fail("b"), T("c", TrialOutcome.Unknown, judge: GradeOutcome.Unknown),
            T("d", TrialOutcome.Infrastructure));

        var overall = Line(lines, "overall");
        Assert.That(overall.Graders.Select(g => (g.Grader, g.Kind, g.Passed, g.Failed, g.Unknown)),
            Is.EqualTo(new[] { ("routed-to", GraderKind.FixedRule, 2, 1, 0), ("judge", GraderKind.Model, 2, 0, 1), }),
            "the optional grader is not listed; the infrastructure trial is not counted for any grader");
        Assert.That(overall.Kinds.Select(k => (k.Kind, k.Passed, k.Failed, k.Unknown)),
            Is.EqualTo(new[] { (GraderKind.FixedRule, 2, 1, 0), (GraderKind.Model, 2, 0, 1), }));
    }

    /// <summary>A trial with no grade from a required grader counts as Unknown for it.</summary>
    [Test]
    public void ATrialWithNoGradeFromARequiredGrader_CountsAsUnknownForIt()
    {
        var bare = new TrialResult("a", [], TrialOutcome.Unknown, [G(Route, GradeOutcome.Pass)]);

        var overall = Line(RunSummary.Of("m", [bare], Graders), "overall");

        Assert.That(overall.Graders.Single(g => g.Grader == "judge").Unknown, Is.EqualTo(1));
    }

    /// <summary>
    /// A grader's mean score is the mean of the scores it gave over the counted trials, so partial credit shows where
    /// a pass or a fail hides it.
    /// </summary>
    [Test]
    public void AGradersMeanScore_IsTheMeanOfTheScoresItGave()
    {
        // The judge gives partial credit: 1, 0.5 and 0.
        TrialResult Scored(string scenario, double score) => new(scenario, [], TrialOutcome.Pass,
            [G(Route, GradeOutcome.Pass), new Grade(Judge.Name, 1, score, GradeOutcome.Pass, "because")]);

        var overall = Line(Of(Scored("a", 1), Scored("b", 0.5), Scored("c", 0)), "overall");

        Assert.That(overall.Graders.Single(g => g.Grader == "judge").ScoreMean, Is.EqualTo(0.5));
        Assert.That(overall.Graders.Single(g => g.Grader == "routed-to").ScoreMean, Is.EqualTo(1));
    }

    /// <summary>An infrastructure trial's grades are not the agent's, so they stay out of the mean.</summary>
    [Test]
    public void AnInfrastructureTrialsGrades_AreNotInTheMean()
    {
        var lines = Of(T("a", TrialOutcome.Pass), T("b", TrialOutcome.Infrastructure, route: GradeOutcome.Fail));

        Assert.That(Line(lines, "overall").Graders.Single(g => g.Grader == "routed-to").ScoreMean, Is.EqualTo(1));
    }

    /// <summary>A grader that graded none of the counted trials has no mean, never 0.</summary>
    [Test]
    public void AGraderThatGaveNoGrade_HasNoMean()
    {
        var bare = new TrialResult("a", [], TrialOutcome.Unknown, [G(Route, GradeOutcome.Pass)]);

        var overall = Line(RunSummary.Of("m", [bare], Graders), "overall");

        Assert.That(overall.Graders.Single(g => g.Grader == "judge").ScoreMean, Is.Null);
    }

    /// <summary>No trials make no lines.</summary>
    [Test]
    public void NoTrials_AreNoLines() =>
        Assert.That(RunSummary.Of("m", [], Graders), Is.Empty);

    /// <summary>
    /// A grader that grades nothing here: the roll-up reads only its name, kind and whether it is required.
    /// </summary>
    private sealed class FakeGrader(string name, GraderKind kind, bool required = true) : IGrader
    {
        /// <inheritdoc />
        public string Name => name;

        /// <inheritdoc />
        public int Version => 1;

        /// <inheritdoc />
        public GraderKind Kind => kind;

        /// <inheritdoc />
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

        /// <inheritdoc />
        public bool Required => required;

        /// <summary>Never called: the roll-up reads the grades it is given.</summary>
        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
