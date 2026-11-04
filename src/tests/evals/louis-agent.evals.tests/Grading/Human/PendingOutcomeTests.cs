using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Conformance;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Grading.Outcomes;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Running;
using LouisAgent.Evals.Tests.Protocol;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;
using Microsoft.Extensions.Logging.Abstractions;

namespace LouisAgent.Evals.Tests.Grading.Human;

/// <summary>
/// Pending is a grade nobody has given yet. It is never a pass; a trial's outcome ignores a grader that is not
/// required, so a pending human review changes nothing; the conformance check refuses a human grader that is required
/// (a run would wait on a person); and a re-score writes it again, because a person's grade lives in the API's store
/// and not in the transcript.
/// </summary>
public class PendingOutcomeTests
{
    /// <summary>A grader of the kind given: a human one waits, any other passes.</summary>
    private sealed class Rule(string name, CheckKind kind, bool required) : ICheck
    {
        /// <inheritdoc />
        public string Name => name;

        /// <inheritdoc />
        public int Version => 1;

        /// <inheritdoc />
        public CheckKind Kind => kind;

        /// <inheritdoc />
        public bool Required => required;

        /// <inheritdoc />
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

        /// <summary>A human grader waits for a person; any other passes.</summary>
        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
            ValueTask.FromResult(kind == CheckKind.Human
                ? Verdict.Pending(name, 1, "waits")
                : Verdict.Pass(name, 1, "fine"));
    }

    /// <summary>The required fixed rule.</summary>
    private static readonly ICheck Fixed = new Rule("routed-to", CheckKind.FixedRule, required: true);

    /// <summary>The judge the review reviews.</summary>
    private static readonly ICheck Judge = new Rule("meal-for-people", CheckKind.Model, required: false);

    /// <summary>The human review.</summary>
    private static readonly ICheck Review = new Rule("meal-for-people.human", CheckKind.Human, required: false);

    /// <summary>Pending scores 0 and is its own word: not a pass, and not Unknown.</summary>
    [Test]
    public void PendingIsNotAPass_AndHasItsOwnWord()
    {
        var grade = Verdict.Pending("x.human", 1, "waits");

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Pending, 0d)));
        Assert.That(GradeOutcome.Pending, Is.Not.EqualTo(GradeOutcome.Pass).And.Not.EqualTo(GradeOutcome.Unknown));
    }

    /// <summary>A pending grader that isn't required changes no trial's outcome.</summary>
    [Test]
    public void APendingGraderThatIsNotRequired_ChangesNoTrialsOutcome()
    {
        var grades = new[]
        {
            Verdict.Pass("routed-to", 1, "fine"), Verdict.Pending("meal-for-people.human", 1, "waits")
        };

        Assert.That(PassRates.Of(grades, [Fixed, Review]), Is.EqualTo(TrialOutcome.Pass));
        Assert.That(PassRates.Of([Verdict.Fail("routed-to", 1, "no"), grades[1]], [Fixed, Review]),
            Is.EqualTo(TrialOutcome.Fail), "and a fail stays a fail");
    }

    /// <summary>A pending grader that were required would never make a pass.</summary>
    [Test]
    public void APendingGraderThatWereRequired_IsNeverAPass()
    {
        var required = new Rule("meal-for-people.human", CheckKind.Human, required: true);

        var outcome = PassRates.Of([Verdict.Pending("meal-for-people.human", 1, "waits")], [required]);

        Assert.That(outcome, Is.EqualTo(TrialOutcome.Unknown));
    }

    /// <summary>The conformance check takes a human grader that isn't required, and refuses one that is.</summary>
    [Test]
    public async Task TheConformanceCheck_AcceptsAHumanGraderThatIsNotRequired_AndRefusesOneThatIs()
    {
        var accepted = await EvalConformance.CheckAsync(new Eval(Fixed, Judge, Review), [], CancellationToken.None);
        var refused = await EvalConformance.CheckAsync(
            new Eval(Fixed, Judge, new Rule("meal-for-people.human", CheckKind.Human, required: true)), [],
            CancellationToken.None);

        Assert.That(accepted.Where(f => f.Contains("meal-for-people.human")), Is.Empty);
        Assert.That(refused.Where(f => f.Contains("meal-for-people.human")), Is.Not.Empty);
        Assert.That(refused.First(f => f.Contains("meal-for-people.human")),
            Does.Contain("person").And.Contain("required"));
    }

    /// <summary>A grader's name may hold a dot, for a human review's; an eval's name may not.</summary>
    [Test]
    public async Task AGraderNameMayHoldADot_ForAHumanReviews_ButAnEvalsNameMayNot()
    {
        var dotted = await EvalConformance.CheckAsync(
            new Eval(Fixed, new Rule("meal.for.people", CheckKind.Model, required: false),
                new Rule("meal.for.people.human", CheckKind.Human, required: false)), [], CancellationToken.None);
        var evalName = await EvalConformance.CheckAsync(new Eval("a.dotted.eval", Fixed), [], CancellationToken.None);

        Assert.That(dotted.Where(f => f.Contains("not safe")), Is.Empty);
        Assert.That(evalName.Where(f => f.Contains("not safe")), Is.Not.Empty);
    }

    /// <summary>
    /// A run writes the pending grade and a re-score writes it again, both leaving the outcome to the fixed rules.
    /// </summary>
    [Test]
    public async Task ARunWritesThePendingGrade_AndAReScoreWritesItAgain_BothLeavingTheOutcomeToTheFixedRules()
    {
        // A run of the fixed rule and the review over one canned answer.
        var at = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123);
        var eval = new Eval(Fixed, Review);
        var ran = new StringWriter();
        var answer = new[]
        {
            SessionEvent.From("content_block_delta",
                JsonDocument
                    .Parse("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Hallo"}}""")
                    .RootElement.Clone())
        };
        await new EvalRunner(new CannedSessionClient(answer), new ProtocolWriter(ran, new FixedClock(at)),
                new FixedClock(at), NullLogger<EvalRunner>.Instance)
            .RunAsync(eval, new EvalRun("run_old", 1), CancellationToken.None);

        // Its re-score.
        var rescored = new StringWriter();
        await new EvalRunner(new NoSessionClient(), new ProtocolWriter(rescored, new FixedClock(at)),
                new FixedClock(at), NullLogger<EvalRunner>.Instance)
            .RescoreAsync(eval, "run_new", "run_old", Written.Lines(ran), CancellationToken.None);

        foreach (var sink in new[] { ran, rescored })
        {
            var conversation = Written.Events(sink).OfType<ConversationEvent>().Single();
            Assert.That(conversation.Grades.Single(g => g.Grader == "meal-for-people.human").Outcome,
                Is.EqualTo(GradeOutcome.Pending));
            Assert.That(conversation.Outcome, Is.EqualTo(TrialOutcome.Pass), "the fixed rule made the outcome");
        }
    }

    /// <summary>An eval of one scenario and the graders given.</summary>
    private sealed class Eval(string name, params ICheck[] graders) : IEval
    {
        /// <summary>The eval under its usual name.</summary>
        public Eval(params ICheck[] graders) : this("pending-eval", graders)
        {
        }

        /// <inheritdoc />
        public string Name => name;

        /// <inheritdoc />
        public int Version => 1;

        /// <inheritdoc />
        public string Proves => "A pending human grade.";

        /// <summary>One mode.</summary>
        public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<EvalMode>>([
                new EvalMode("cook", SampleRole.Alpha, [SampleToolset.Alpha])
            ]);

        /// <inheritdoc />
        public IReadOnlyList<EvalScenario> Scenarios { get; } =
        [
            new EvalScenario("bobotie", new EvalInput(null, "Africa/Johannesburg", ["Gee my resep"]), default, [],
                DatasetSplit.Dev)
        ];

        /// <inheritdoc />
        public IReadOnlyList<ICheck> Checks { get; } = graders;

        /// <summary>No facts about a turn.</summary>
        public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => new(null, null, 0, 0, [], null);
    }
}
