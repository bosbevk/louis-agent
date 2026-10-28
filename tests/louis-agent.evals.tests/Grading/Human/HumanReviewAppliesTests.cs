using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Grading.Human;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Running;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;
using Microsoft.Extensions.Logging.Abstractions;
using static LouisAgent.Evals.Tests.Trials.TrialBuilder;

namespace LouisAgent.Evals.Tests.Grading.Human;

// F18 step 6a.4 (Louis): a review asks its judge's question, and a scenario the judge has nothing to judge (no `serves` for the meal judge: the control and the ask-first scenario) has no question to
// ask a person either. So a review takes its judge's expected keys, gives no grade at all where the scenario lacks them (nothing waits for a person, no panel appears), and is version 2.
// Written first: the review took no keys and graded every trial Pending.
public class HumanReviewAppliesTests
{
    private sealed class Judge(params string[] keys) : IGrader
    {
        public string Name => "meal-for-people";
        public int Version => 1;
        public GraderKind Kind => GraderKind.Model;
        public bool Required => false;
        public IReadOnlySet<string> ExpectedKeys { get; } = keys.ToHashSet();

        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
            ValueTask.FromResult(Verdict.Pass(Name, 1, "fine"));
    }

    private static Trial With(string expected) =>
        new("run_1", Mode, 1,
            new EvalScenario("s", new EvalInput(null, "Africa/Johannesburg", ["Hallo"]), Json(expected), [],
                DatasetSplit.Dev), new Transcript([Turn(1)]));

    [Test]
    public void AReviewTakesItsJudgesKeys_AndIsVersionTwo()
    {
        var review = HumanReviewGrader.For(new Judge("serves"), "Is this a meal?");

        Assert.That(review.ExpectedKeys, Is.EquivalentTo(new[] { "serves" }));
        Assert.That(review.Version, Is.EqualTo(2));
    }

    [Test]
    public void ItAppliesWhereTheScenarioHasEveryKey_AndNotWhereOneIsMissing()
    {
        var review = HumanReviewGrader.For(new Judge("serves"), "q");

        Assert.That(review.AppliesTo(With("""{"serves":4,"budget":150}""")), Is.True);
        Assert.That(review.AppliesTo(With("""{"routes_to":null}""")), Is.False, "a control: no meal asked");
        Assert.That(review.AppliesTo(With("{}")), Is.False);
    }

    [Test]
    public void AReviewWhoseJudgeReadsNoKeys_AppliesToEveryScenario()
    {
        Assert.That(HumanReviewGrader.For(new Judge(), "q").AppliesTo(With("{}")), Is.True);
        Assert.That(HumanReviewGrader.For("meal-for-people", "q").AppliesTo(With("{}")), Is.True);
    }

    [Test]
    public async Task InARun_ATrialItDoesNotApplyTo_HasNoGradeFromIt_AndOneItAppliesToHasAPendingOne()
    {
        var eval = new OneEval([new Judge("serves"), HumanReviewGrader.For(new Judge("serves"), "q")]);

        var control = await TrialGrading.GradeAsync(eval, With("""{"routes_to":null}"""), NullLogger.Instance,
            CancellationToken.None);
        var meal = await TrialGrading.GradeAsync(eval, With("""{"serves":4}"""), NullLogger.Instance,
            CancellationToken.None);

        Assert.That(control.Select(g => g.Grader), Is.EqualTo(new[] { "meal-for-people" }));
        Assert.That(meal.Select(g => (g.Grader, g.Outcome)),
            Is.EqualTo(
                new[] { ("meal-for-people", GradeOutcome.Pass), ("meal-for-people.human", GradeOutcome.Pending) }));
    }

    private sealed class OneEval(IReadOnlyList<IGrader> graders) : IEval
    {
        public string Name => "applies-eval";
        public int Version => 1;
        public string Proves => "A review that applies only where its judge does.";

        public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions,
            CancellationToken cancellationToken) => ValueTask.FromResult<IReadOnlyList<EvalMode>>([Mode]);

        public IReadOnlyList<EvalScenario> Scenarios { get; } = [];
        public IReadOnlyList<IGrader> Graders { get; } = graders;
        public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => new(null, null, 0, 0, [], null);
    }
}
