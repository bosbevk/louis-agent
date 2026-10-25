using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Trials;
using LouisAgent.Evals.Grading.Outcomes;

namespace LouisAgent.Evals.Tests.Grading.Outcomes;

// A trial's outcome from its grades (F18 step 4.5a, the first part of 5a): only the required graders count; a fail beats an
// unknown, an unknown beats a pass, and a required grader with no grade, or no required grader at all, is Unknown. Written first:
// PassRates.Of threw NotImplementedException on the red run.
public class PassRatesTests
{
    private static Grade Pass(string grader) => Verdict.Pass(grader, 1, "fine");
    private static Grade Fail(string grader) => Verdict.Fail(grader, 1, "wrong");
    private static Grade Unknown(string grader) => Verdict.Unknown(grader, 1, "can't tell");

    [Test]
    public void EveryRequiredGraderPassing_IsAPass()
    {
        var graders = new IGrader[] { new FakeGrader("a", required: true), new FakeGrader("b", required: true) };

        Assert.That(PassRates.Of([Pass("a"), Pass("b")], graders), Is.EqualTo(TrialOutcome.Pass));
    }

    [Test]
    public void ARequiredFail_BesideARequiredUnknown_IsAFail()
    {
        var graders = new IGrader[] { new FakeGrader("a", required: true), new FakeGrader("b", required: true) };

        Assert.That(PassRates.Of([Unknown("a"), Fail("b")], graders), Is.EqualTo(TrialOutcome.Fail));
    }

    [Test]
    public void ARequiredUnknown_AndNoFail_IsUnknown()
    {
        var graders = new IGrader[] { new FakeGrader("a", required: true), new FakeGrader("b", required: true) };

        Assert.That(PassRates.Of([Pass("a"), Unknown("b")], graders), Is.EqualTo(TrialOutcome.Unknown));
    }

    [Test]
    public void AnOptionalFailOrUnknown_StillPasses()
    {
        var graders = new IGrader[] { new FakeGrader("a", required: true), new FakeGrader("b", required: false), new FakeGrader("c", required: false) };

        Assert.That(PassRates.Of([Pass("a"), Fail("b"), Unknown("c")], graders), Is.EqualTo(TrialOutcome.Pass));
    }

    [Test]
    public void ARequiredGraderWithNoGrade_IsUnknown()
    {
        // A grader that never graded can't have passed: the trial isn't a pass until every required check is in.
        var graders = new IGrader[] { new FakeGrader("a", required: true), new FakeGrader("b", required: true) };

        Assert.That(PassRates.Of([Pass("a")], graders), Is.EqualTo(TrialOutcome.Unknown));
    }

    [Test]
    public void NoRequiredGrader_IsUnknown()
    {
        // No trial passes without a check; the catalogue refuses such an eval anyway.
        var graders = new IGrader[] { new FakeGrader("a", required: false) };

        Assert.That(PassRates.Of([Pass("a")], graders), Is.EqualTo(TrialOutcome.Unknown));
    }

    /// <summary>A grader by name and requirement only: these tests never call it.</summary>
    private sealed class FakeGrader(string name, bool required) : IGrader
    {
        public string Name => name;
        public int Version => 1;
        public GraderKind Kind => GraderKind.FixedRule;
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();
        public bool Required => required;

        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
