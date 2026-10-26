using System.Text.Json;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Outcomes;
using LouisAgent.Evals.Reports;
using LouisAgent.Evals.Running;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Tests.Reports;

// F18 step 5c: a mode's trials rolled up into one overall line and one line per category (aggregate and slice: an overall number hides where an agent fails). On made-up
// trials: the overall line is the sum of its parts, a trial in two categories counts in both and once overall, an Unknown and an infrastructure trial are not passed (the latter is
// left out of the rates and counted), a slice of fewer than five counted trials says "too few" and has no rates, never 0. Written first: RunSummary.Of threw NotImplementedException.
public class RunSummaryTests
{
    private static readonly IGrader Route = new FakeGrader("routed-to", GraderKind.FixedRule);
    private static readonly IGrader Judge = new FakeGrader("judge", GraderKind.Model);
    private static readonly IGrader Optional = new FakeGrader("nice-to-have", GraderKind.FixedRule, required: false);
    private static readonly IReadOnlyList<IGrader> Graders = [Route, Judge, Optional];

    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    private static Grade G(IGrader grader, GradeOutcome outcome) => new(grader.Name, grader.Version, outcome == GradeOutcome.Pass ? 1 : 0, outcome, "because");

    private static TrialResult T(string scenario, TrialOutcome outcome, string[]? categories = null, string scores = "{}", GradeOutcome route = GradeOutcome.Pass,
        GradeOutcome judge = GradeOutcome.Pass) =>
        new(scenario, categories ?? [], outcome, [G(Route, route), G(Judge, judge)], Json(scores));

    private static TrialResult Pass(string scenario, params string[] categories) => T(scenario, TrialOutcome.Pass, categories);

    private static TrialResult Fail(string scenario, params string[] categories) =>
        T(scenario, TrialOutcome.Fail, categories, route: GradeOutcome.Fail);

    private static SummaryLine Line(IEnumerable<SummaryLine> lines, string slice) => lines.Single(line => line.Slice == slice);

    private static IReadOnlyList<SummaryLine> Of(params TrialResult[] trials) => RunSummary.Of("puls-cook", trials, Graders);

    [Test]
    public void TheOverallLineComesFirst_ThenEachCategoryByName_AllForTheMode()
    {
        var lines = Of(Pass("a", "shop:pnp", "language:af"), Pass("b", "budget"));

        Assert.That(lines.Select(l => l.Slice), Is.EqualTo(new[] { "overall", "budget", "language:af", "shop:pnp" }));
        Assert.That(lines.Select(l => l.Mode).Distinct(), Is.EqualTo(new[] { "puls-cook" }));
    }

    [Test]
    public void TheOverallCountsAreTheSumOfTheTrials_AndEachCategorysAreTheSumOfItsOwn()
    {
        var lines = Of(Pass("a", "x"), Pass("b", "x"), Fail("c", "x"), Pass("d", "y"), T("e", TrialOutcome.Unknown, ["y"], judge: GradeOutcome.Unknown),
            T("f", TrialOutcome.Infrastructure, ["y"]));

        var overall = Line(lines, "overall");
        Assert.That((overall.N, overall.Passed, overall.Failed, overall.Unknown, overall.Infrastructure), Is.EqualTo((6, 3, 1, 1, 1)));
        var x = Line(lines, "x");
        Assert.That((x.N, x.Passed, x.Failed, x.Unknown, x.Infrastructure), Is.EqualTo((3, 2, 1, 0, 0)));
        var y = Line(lines, "y");
        Assert.That((y.N, y.Passed, y.Failed, y.Unknown, y.Infrastructure), Is.EqualTo((3, 1, 0, 1, 1)));
        Assert.That(x.N + y.N, Is.EqualTo(overall.N), "no trial is in two of these categories, so they sum to the overall line");
    }

    [Test]
    public void ATrialInTwoCategories_CountsInBoth_AndOnceInTheOverall_AndOneWithNoneOnlyInTheOverall()
    {
        var lines = Of(Pass("both", "a", "b"), Pass("none"));

        Assert.That(Line(lines, "overall").N, Is.EqualTo(2));
        Assert.That(Line(lines, "a").N, Is.EqualTo(1));
        Assert.That(Line(lines, "b").N, Is.EqualTo(1));
        Assert.That(lines.Select(l => l.Slice), Is.EqualTo(new[] { "overall", "a", "b" }), "a trial with no category makes no line of its own");
    }

    [Test]
    public void AnUnknownTrialIsNotPassed_AndAnInfrastructureOneIsCountedButLeftOutOfTheRates()
    {
        var trials = new[] { Pass("a"), Pass("b"), Pass("c"), Pass("d"), T("e", TrialOutcome.Unknown, route: GradeOutcome.Unknown), T("f", TrialOutcome.Infrastructure) };

        var overall = Line(RunSummary.Of("m", trials, Graders), "overall");

        Assert.That((overall.N, overall.Passed, overall.Unknown, overall.Infrastructure), Is.EqualTo((6, 4, 1, 1)));
        Assert.That(overall.PassRate, Is.EqualTo(4.0 / 5), "passed over the five that counted: the infrastructure trial is not one of them");
    }

    [Test]
    public void ASliceOfThreeTrialsIsTooFew_WithItsCountsAndNoRates_AndOneOfFiveHasThem()
    {
        var few = Line(Of(Pass("a", "small"), Pass("b", "small"), Fail("c", "small")), "small");
        var five = Line(Of(Pass("a"), Pass("b"), Pass("c"), Pass("d"), Fail("e")), "overall");

        Assert.That(few.TooFew, Is.True);
        Assert.That((few.N, few.Passed, few.Failed), Is.EqualTo((3, 2, 1)), "the counts stay");
        Assert.That((few.PassRate, few.PassAtK, few.PassHatK), Is.EqualTo(((double?)null, (double?)null, (double?)null)), "never 0");
        Assert.That(five.TooFew, Is.False);
        Assert.That((five.PassRate, five.PassAtK, five.PassHatK), Is.EqualTo((0.8, 0.8, 0.8)), "five scenarios, one trial each: pass@1 and pass^1 are the pass rate");
    }

    [Test]
    public void FiveTrialsOfWhichOnlyThreeCount_AreTooFew()
    {
        var lines = Of(Pass("a"), Pass("b"), Pass("c"), T("d", TrialOutcome.Infrastructure), T("e", TrialOutcome.Infrastructure));

        Assert.That(Line(lines, "overall").TooFew, Is.True, "the rates are from the counted trials");
    }

    [Test]
    public void PassAtKAndPassHatK_AreThoseOfModeTally_ForTheOverallLine()
    {
        // --repeat 3 over three scenarios: a always passes, b sometimes, c never; one infrastructure trial. The same numbers `mode_done` writes.
        var trials = new[]
        {
            Pass("a"), Pass("a"), Pass("a"),
            Pass("b"), Fail("b"), Pass("b"),
            Fail("c"), Fail("c"), T("c", TrialOutcome.Infrastructure),
        };
        var tally = new ModeTally();
        foreach (var trial in trials) tally.Add(trial);

        var overall = Line(RunSummary.Of("m", trials, Graders), "overall");

        Assert.That(overall.TooFew, Is.False);
        Assert.That(overall.PassAtK, Is.EqualTo(tally.Share.PassAtK));
        Assert.That(overall.PassHatK, Is.EqualTo(tally.Share.PassHatK));
        Assert.That((overall.PassAtK, overall.PassHatK), Is.EqualTo((2.0 / 3, 1.0 / 3)));
    }

    [Test]
    public void TheSlicesTasks_AreItsScenarioIds_OnceEach_InOrder()
    {
        var lines = Of(Pass("b", "x"), Pass("a", "x"), Pass("b", "x"), Pass("c"));

        Assert.That(Line(lines, "x").Tasks, Is.EqualTo(new[] { "a", "b" }));
        Assert.That(Line(lines, "overall").Tasks, Is.EqualTo(new[] { "a", "b", "c" }));
    }

    [Test]
    public void TheGraderCounts_AreOfTheRequiredGradersOnly_ByNameAndByKind()
    {
        var lines = Of(Pass("a"), Fail("b"), T("c", TrialOutcome.Unknown, judge: GradeOutcome.Unknown), T("d", TrialOutcome.Infrastructure));

        var overall = Line(lines, "overall");
        Assert.That(overall.Graders.Select(g => (g.Grader, g.Kind, g.Passed, g.Failed, g.Unknown)), Is.EqualTo(new[]
        {
            ("routed-to", GraderKind.FixedRule, 2, 1, 0),
            ("judge", GraderKind.Model, 2, 0, 1),
        }), "the optional grader is not listed; the infrastructure trial is not counted for any grader");
        Assert.That(overall.Kinds.Select(k => (k.Kind, k.Passed, k.Failed, k.Unknown)), Is.EqualTo(new[]
        {
            (GraderKind.FixedRule, 2, 1, 0),
            (GraderKind.Model, 2, 0, 1),
        }));
    }

    [Test]
    public void ATrialWithNoGradeFromARequiredGrader_CountsAsUnknownForIt()
    {
        var bare = new TrialResult("a", [], TrialOutcome.Unknown, [G(Route, GradeOutcome.Pass)], Json("{}"));

        var overall = Line(RunSummary.Of("m", [bare], Graders), "overall");

        Assert.That(overall.Graders.Single(g => g.Grader == "judge").Unknown, Is.EqualTo(1));
    }

    [Test]
    public void TheScoreMeans_AreOfTheNumericKeys_WithTheirOwnN_AStringOrABooleanSkipped_AMissingKeyNotCounted()
    {
        var lines = Of(
            T("a", TrialOutcome.Pass, scores: """{"priced_ratio":1,"routed_right":true,"role":"puls-cook","errors":0}"""),
            T("b", TrialOutcome.Pass, scores: """{"priced_ratio":0.5,"routed_right":false,"role":null}"""),
            T("c", TrialOutcome.Pass, scores: """{"errors":2}"""));

        var means = Line(lines, "overall").Scores.ToDictionary(s => s.Key);
        Assert.That(means.Keys, Is.EquivalentTo(new[] { "priced_ratio", "errors" }), "role (a string) and routed_right (a boolean) are skipped");
        Assert.That(means["priced_ratio"], Is.EqualTo(new ScoreMean("priced_ratio", 0.75, 2)));
        Assert.That(means["errors"], Is.EqualTo(new ScoreMean("errors", 1.0, 2)));
    }

    [Test]
    public void AnInfrastructureTrialsScores_AreNotInTheMeans()
    {
        var lines = Of(T("a", TrialOutcome.Pass, scores: """{"errors":0}"""), T("b", TrialOutcome.Infrastructure, scores: """{"errors":9}"""));

        Assert.That(Line(lines, "overall").Scores.Single(), Is.EqualTo(new ScoreMean("errors", 0, 1)));
    }

    [Test]
    public void NoTrials_AreNoLines() =>
        Assert.That(RunSummary.Of("m", [], Graders), Is.Empty);

    /// <summary>A grader that grades nothing here: the roll-up reads only its name, kind and whether it is required.</summary>
    private sealed class FakeGrader(string name, GraderKind kind, bool required = true) : IGrader
    {
        public string Name => name;
        public int Version => 1;
        public GraderKind Kind => kind;
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();
        public bool Required => required;

        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
