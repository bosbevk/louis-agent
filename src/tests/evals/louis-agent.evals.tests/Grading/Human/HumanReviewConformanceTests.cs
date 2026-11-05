using LouisAgent.Evals.Api;
using LouisAgent.Evals.Conformance;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Tests.Grading.Human;

// F18 step 6a.1b (Louis: "conformance checks every review"): a human review is only meaningful beside the judge it is checking, so the conformance check holds the rule for
// every eval: each Human grader is named `{x}.human`, and x is a model grader of the same eval. Written first: the check looked only at whether a review was required.
public class HumanReviewConformanceTests
{
    private sealed class Rule(string name, CheckKind kind, bool required = false) : ICheck
    {
        public string Name => name;
        public int Version => 1;
        public CheckKind Kind => kind;
        public bool Required => required;
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
            ValueTask.FromResult(Verdict.Pass(name, 1, "fine"));
    }

    private static readonly ICheck Fixed = new Rule("routed-to", CheckKind.FixedRule, required: true);

    private static async Task<IReadOnlyList<string>> CheckAsync(params ICheck[] graders) =>
        await EvalConformance.CheckAsync(new Eval(graders), [], CancellationToken.None);

    [Test]
    public async Task AReviewOfAModelGraderOfTheSameEval_IsAccepted()
    {
        var failures = await CheckAsync(Fixed, new Rule("meal-for-people", CheckKind.Model),
            new Rule("meal-for-people.human", CheckKind.Human));

        Assert.That(failures.Where(f => f.Contains("human review")), Is.Empty);
    }

    [Test]
    public async Task AReviewWithNoGraderToReview_IsRefused_NamingItAndWhatItReviews()
    {
        var failures = await CheckAsync(Fixed, new Rule("meal-for-people.human", CheckKind.Human));

        var refusal = failures.Single(f => f.Contains("human review"));
        Assert.That(refusal,
            Does.Contain("'meal-for-people.human'").And.Contain("'meal-for-people'").And.Contain("model grader"));
    }

    [Test]
    public async Task AReviewOfAFixedRule_IsRefused_ARuleNeedsNoPerson()
    {
        var failures = await CheckAsync(Fixed, new Rule("routed-to.human", CheckKind.Human));

        Assert.That(failures.Single(f => f.Contains("human review")),
            Does.Contain("'routed-to.human'").And.Contain("'routed-to'"));
    }

    [Test]
    public async Task AReviewNotNamedForAGrader_IsRefused()
    {
        var failures = await CheckAsync(Fixed, new Rule("meal-for-people", CheckKind.Model),
            new Rule("reviewer", CheckKind.Human));

        Assert.That(failures.Single(f => f.Contains("human review")), Does.Contain("'reviewer'"));
    }

    [Test]
    public async Task AReviewNamedOnlyDotHuman_ReviewsNothing_AndIsRefused()
    {
        var failures = await CheckAsync(Fixed, new Rule(".human", CheckKind.Human));

        Assert.That(failures.Where(f => f.Contains("human review")), Is.Not.Empty);
    }

    [Test]
    public async Task AModelGraderWithNoReview_IsFine_NobodyHasToReviewIt()
    {
        var failures = await CheckAsync(Fixed, new Rule("meal-for-people", CheckKind.Model));

        Assert.That(failures.Where(f => f.Contains("human review")), Is.Empty);
    }

    private sealed class Eval(IReadOnlyList<ICheck> graders) : IEval
    {
        public string Name => "reviewed-eval";
        public int Version => 1;
        public string Proves => "A review beside its judge.";

        public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public IReadOnlyList<EvalScenario> Scenarios { get; } = [];
        public IReadOnlyList<ICheck> Checks { get; } = graders;
        public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => throw new NotSupportedException();
    }
}
