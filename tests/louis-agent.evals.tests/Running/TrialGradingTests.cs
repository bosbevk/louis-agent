using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Running;
using LouisAgent.Evals.Trials;
using Microsoft.Extensions.Logging;
using LouisAgent.Evals.Tests.Trials;
using static LouisAgent.Evals.Tests.Trials.TrialBuilder;

namespace LouisAgent.Evals.Tests.Running;

// Grading a trial in the runner (F18 step 4.5a): every grader of the eval in the eval's order; one that throws gives Unknown with its
// reason and one warning, and the next grader still runs; a cancel of the run stops it, a grader's own timeout does not. Written first: TrialGrading.GradeAsync threw
// NotImplementedException on the red run.
public class TrialGradingTests
{
    [Test]
    public async Task TheGradesComeInTheEvalsOrder()
    {
        var eval = new GradedEval(new PassingGrader("zebra"), new PassingGrader("apple"), new PassingGrader("mango"));

        var grades = await TrialGrading.GradeAsync(eval, Trial(Turn(1)), new CapturingLogger(), CancellationToken.None);

        Assert.That(grades.Select(g => g.Grader), Is.EqualTo(new[] { "zebra", "apple", "mango" }));
    }

    [Test]
    public async Task AGraderThatThrows_GivesUnknownWithItsReason_AndOneWarning_AndTheNextGraderStillRuns()
    {
        var eval = new GradedEval(new ThrowingGrader("broken"), new PassingGrader("fine"));
        var log = new CapturingLogger();

        var grades = await TrialGrading.GradeAsync(eval, Trial(Turn(1)), log, CancellationToken.None);

        Assert.That(grades.Select(g => (g.Grader, g.Outcome)), Is.EqualTo(new[] { ("broken", GradeOutcome.Unknown), ("fine", GradeOutcome.Pass) }));
        Assert.That(grades[0].Reason, Does.Contain("the index was out of range"));
        Assert.That(log.Warnings, Has.Count.EqualTo(1));
        Assert.That(log.Warnings[0], Does.Contain("broken").And.Contain("stub-cook").And.Contain("bobotie").And.Contain("the index was out of range"));
    }

    [Test]
    public void ACancelOfTheRunsOwnToken_Propagates()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var eval = new GradedEval(new CancellingGrader(), new PassingGrader("never"));

        Assert.That(async () => await TrialGrading.GradeAsync(eval, Trial(Turn(1)), new CapturingLogger(), cancelled.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }

    // F18 step 6.1 (the note kept for step 6): a judge's own timeout is an OperationCanceledException too (HttpClient's), and it is the grader's trouble, not the run's.
    [Test]
    public async Task AGradersOwnTimeout_IsUnknown_WhenTheRunWasNotCancelled_AndTheNextGraderStillRuns()
    {
        var eval = new GradedEval(new CancellingGrader(), new PassingGrader("fine"));
        var log = new CapturingLogger();

        var grades = await TrialGrading.GradeAsync(eval, Trial(Turn(1)), log, CancellationToken.None);

        Assert.That(grades.Select(g => (g.Grader, g.Outcome)), Is.EqualTo(new[] { ("cancels", GradeOutcome.Unknown), ("fine", GradeOutcome.Pass) }));
        Assert.That(log.Warnings, Has.Count.EqualTo(1));
    }

    private sealed class PassingGrader(string name) : IGrader
    {
        public string Name => name;
        public int Version => 1;
        public GraderKind Kind => GraderKind.FixedRule;
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();
        public bool Required => true;

        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) => ValueTask.FromResult(Verdict.Pass(Name, Version, "fine"));
    }

    private sealed class ThrowingGrader(string name) : IGrader
    {
        public string Name => name;
        public int Version => 1;
        public GraderKind Kind => GraderKind.FixedRule;
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();
        public bool Required => true;

        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) => throw new IndexOutOfRangeException("the index was out of range");
    }

    private sealed class CancellingGrader : IGrader
    {
        public string Name => "cancels";
        public int Version => 1;
        public GraderKind Kind => GraderKind.FixedRule;
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();
        public bool Required => true;

        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) => throw new OperationCanceledException();
    }
}
