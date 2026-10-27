using LouisAgent.Testing;
using LouisAgent.Core.Llm.UsageMapping;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Models;
using LouisAgent.Evals.Running;
using LouisAgent.Evals.Tests.Running;
using LouisAgent.Evals.Tests.Trials;
using LouisAgent.Evals.Trials;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using static LouisAgent.Evals.Tests.Trials.TrialBuilder;

namespace LouisAgent.Evals.Tests.Grading.Models;

// F18 step 6.1: a model grader is an IEvaluator behind IGrader. It hands the evaluator the trial's messages and its answer, maps the one yes-or-no metric to a
// grade (no value is Unknown, never a guessed pass), says what judged, and has its requests recorded as `grading` on the run. Written first: EvaluatorGrader did not exist.
public class EvaluatorGraderTests
{
    private static readonly ConfiguredJudge Judge = new(new ChatConfiguration(new FakeChatClient()), "qwen3:8b");

    private sealed class AskContext(string text) : EvaluationContext("ask", text);

    private static EvaluatorGrader Grader(IEvaluator evaluator, ConfiguredJudge? judge = null, bool noJudge = false) =>
        new(evaluator, noJudge ? null : judge ?? Judge, "meal-for-people", 3, "abc123def456");

    private static BooleanMetric Metric(bool? value, string? reason = "a main dish for four", EvaluationMetricInterpretation? interpretation = null) =>
        new("meal", value, reason) { Interpretation = interpretation };

    [Test]
    public void ItIsAModelGrader_ThatIsNotRequiredUntilItsCalibrated()
    {
        var grader = Grader(new FakeEvaluator(Metric(true)));

        Assert.That(grader.Kind, Is.EqualTo(GraderKind.Model));
        Assert.That(grader.Required, Is.False);
        Assert.That((grader.Name, grader.Version), Is.EqualTo(("meal-for-people", 3)));
    }

    [Test]
    public async Task TheEvaluator_IsHandedTheMessagesAndTheAnswer_AsAChatResponse()
    {
        var evaluator = new FakeEvaluator(Metric(true));
        var trial = Trial(Turn(1, [Text("Bobotie vir vier")]));

        await Grader(evaluator).GradeAsync(trial, CancellationToken.None);

        Assert.That(evaluator.Messages.Select(m => (m.Role, m.Text)), Is.EqualTo(new[] { (ChatRole.User, "Hallo") }), "the user's side of the conversation");
        Assert.That(evaluator.Response!.Text, Is.EqualTo("Bobotie vir vier"));
        Assert.That(evaluator.Chat, Is.SameAs(Judge.Chat), "the judge's own client");
    }

    // F18 step 6.2: a judge needs the scenario's ask ("a meal for four" needs the four), which the bridge hands the evaluator as an EvaluationContext built from the trial.
    [Test]
    public async Task TheContext_IsBuiltFromTheTrial_AndHandedToTheEvaluator()
    {
        var evaluator = new FakeEvaluator(Metric(true));
        var grader = new EvaluatorGrader(evaluator, Judge, "meal-for-people", 3, "abc123def456", context: trial => [new AskContext("for " + trial.Scenario.Id)]);

        await grader.GradeAsync(Trial(Turn(1)), CancellationToken.None);

        Assert.That(evaluator.Context.Select(c => (c.Name, ((TextContent)c.Contents.Single()).Text)), Is.EqualTo(new[] { ("ask", "for bobotie") }));
    }

    [Test]
    public async Task WithNoContext_TheEvaluatorIsHandedNone()
    {
        var evaluator = new FakeEvaluator(Metric(true));

        await Grader(evaluator).GradeAsync(Trial(Turn(1)), CancellationToken.None);

        Assert.That(evaluator.Context, Is.Empty);
    }

    // The judge is built when a trial is first graded, not when the eval is listed: listing the evals reads no settings and opens no ledger.
    [Test]
    public async Task ALazyJudge_IsResolvedOnTheFirstGrade_AndOnlyOnce()
    {
        int resolved = 0;
        var grader = new EvaluatorGrader(new FakeEvaluator(Metric(true)), () => { resolved++; return Judge; }, "meal-for-people", 3, "abc123def456");
        Assert.That(resolved, Is.Zero, "not at construction");

        await grader.GradeAsync(Trial(Turn(1)), CancellationToken.None);
        await grader.GradeAsync(Trial(Turn(1)), CancellationToken.None);

        Assert.That(resolved, Is.EqualTo(1));
    }

    [Test]
    public async Task ALazyJudgeThatIsNone_GivesTheNoJudgeGrade()
    {
        var grader = new EvaluatorGrader(new FakeEvaluator(Metric(true)), () => null, "meal-for-people", 3, "abc123def456");

        var grade = await grader.GradeAsync(Trial(Turn(1)), CancellationToken.None);

        Assert.That((grade.Outcome, grade.Reason), Is.EqualTo((GradeOutcome.Unknown, "no judge configured")));
    }

    [Test]
    public async Task ATrueMetric_IsPass_AFalseOneFail_WithTheMetricsReason()
    {
        var pass = await Grader(new FakeEvaluator(Metric(true, "feeds four"))).GradeAsync(Trial(Turn(1)), CancellationToken.None);
        var fail = await Grader(new FakeEvaluator(Metric(false, "a side dish"))).GradeAsync(Trial(Turn(1)), CancellationToken.None);

        Assert.That((pass.Outcome, pass.Score, pass.Reason), Is.EqualTo((GradeOutcome.Pass, 1d, "feeds four")));
        Assert.That((fail.Outcome, fail.Score, fail.Reason), Is.EqualTo((GradeOutcome.Fail, 0d, "a side dish")));
    }

    [Test]
    public async Task NoValue_OrAnInterpretationWithNoRating_IsUnknown_NeverAGuessedPass()
    {
        var noValue = await Grader(new FakeEvaluator(Metric(null, "cannot tell from this answer"))).GradeAsync(Trial(Turn(1)), CancellationToken.None);
        var inconclusive = await Grader(new FakeEvaluator(Metric(true, "unsure", new EvaluationMetricInterpretation(EvaluationRating.Inconclusive, failed: false))))
            .GradeAsync(Trial(Turn(1)), CancellationToken.None);
        var noMetric = await Grader(new FakeEvaluator()).GradeAsync(Trial(Turn(1)), CancellationToken.None);

        Assert.That(noValue.Outcome, Is.EqualTo(GradeOutcome.Unknown));
        Assert.That(noValue.Reason, Is.EqualTo("cannot tell from this answer"));
        Assert.That(inconclusive.Outcome, Is.EqualTo(GradeOutcome.Unknown), "a rating of inconclusive is no rating");
        Assert.That(noMetric.Outcome, Is.EqualTo(GradeOutcome.Unknown));
        Assert.That(noMetric.Reason, Is.Not.Empty, "a grade always says why");
    }

    [Test]
    public async Task AMetricWithNoReason_StillGetsOne()
    {
        var grade = await Grader(new FakeEvaluator(Metric(true, reason: null))).GradeAsync(Trial(Turn(1)), CancellationToken.None);

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
        Assert.That(grade.Reason, Is.Not.Empty);
    }

    [Test]
    public async Task WithNoJudgeConfigured_TheGradeIsUnknown_AndTheEvaluatorIsNeverCalled()
    {
        var evaluator = new FakeEvaluator(Metric(true));

        var grade = await Grader(evaluator, noJudge: true).GradeAsync(Trial(Turn(1)), CancellationToken.None);

        Assert.That((grade.Outcome, grade.Reason), Is.EqualTo((GradeOutcome.Unknown, "no judge configured")));
        Assert.That(evaluator.Calls, Is.Zero);
        Assert.That(grade.Judge, Is.Null, "nothing judged");
    }

    [Test]
    public async Task AnInfrastructureFailure_IsUnknown_AndNoJudgeIsAsked()
    {
        var evaluator = new FakeEvaluator(Metric(true));

        var grade = await Grader(evaluator).GradeAsync(Trial(Turn(1, failure: TrialFailure.Transport)), CancellationToken.None);

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
        Assert.That(evaluator.Calls, Is.Zero, "a flake is not paid for");
    }

    [Test]
    public async Task EachGrade_SaysWhatJudged_TheModelAndThePromptsHash()
    {
        var grade = await Grader(new FakeEvaluator(Metric(true))).GradeAsync(Trial(Turn(1)), CancellationToken.None);

        Assert.That(grade.Judge, Is.EqualTo(new GradeJudge("qwen3:8b", "abc123def456")));
        Assert.That(grade.Version, Is.EqualTo(3), "the grader's version is the grade's own");
    }

    [Test]
    public async Task AnErrorThrown_IsUnknownWithItsReason_WhenTheEvalGradesTheTrial_NeverPass()
    {
        var eval = new GradedEval(Grader(new FakeEvaluator(new InvalidOperationException("ollama refused the connection"))));

        var grades = await TrialGrading.GradeAsync(eval, Trial(Turn(1)), new CapturingLogger(), CancellationToken.None);

        Assert.That(grades[0].Outcome, Is.EqualTo(GradeOutcome.Unknown));
        Assert.That(grades[0].Reason, Does.Contain("ollama refused the connection"));
    }

    [Test]
    public async Task ItsRequests_AreRecordedAsGrading_TaggedLikeTheTurns_OnTheConversationsLastTurn()
    {
        var sink = new ListUsageSink();
        var recorded = new UsageRecordingChatClient(new FakeChatClient(_ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "yes"))), sink, "qwen3:8b");
        var judge = new ConfiguredJudge(new ChatConfiguration(recorded), "qwen3:8b");
        var evaluator = new FakeEvaluator(Metric(true)) { OnEvaluate = chat => chat.ChatClient.GetResponseAsync([new ChatMessage(ChatRole.User, "judge this")]) };

        await Grader(evaluator, judge).GradeAsync(Trial(Turn(1)), CancellationToken.None);

        var row = sink.Records.Single();
        Assert.That(row.Purpose, Is.EqualTo(UsagePurpose.Grading));
        Assert.That(UsagePurpose.Grading, Is.EqualTo("grading"));
        // F18 step 6.1b (the review): tagged as the turns are, {run}:{mode}:{repeat}, with the conversation's session and its last turn, so the API's join places the row on
        // its conversation and the slice's cost counts the judging; a row with no turn made every slice's cost unknown.
        Assert.That(row.Run, Is.EqualTo($"run_1:{SampleMode.Beta}:1"));
        Assert.That(row.Task, Is.EqualTo("bobotie"));
        Assert.That((row.Session, row.Turn), Is.EqualTo(("sess_1", 1)));
        Assert.That(UsageScope.Current, Is.Null, "the scope closed with the grade");
    }
}
