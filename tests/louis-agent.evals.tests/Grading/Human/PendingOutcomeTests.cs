using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Conformance;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Grading.Human;
using LouisAgent.Evals.Grading.Outcomes;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Reports;
using LouisAgent.Evals.Running;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;
using Microsoft.Extensions.Logging.Abstractions;
using static LouisAgent.Evals.Tests.Trials.TrialBuilder;

namespace LouisAgent.Evals.Tests.Grading.Human;

// F18 step 6a.1: Pending is a grade nobody has given yet. It is never a pass; it is its own count in the roll-up (not Unknown); a trial's outcome ignores a grader that is not required,
// so a pending human review changes nothing; the conformance check refuses a human grader that is required (a run would wait on a person); and a re-score writes it again, because a
// person's grade lives in the API's store and not in the transcript. Written first: GradeOutcome had no Pending.
public class PendingOutcomeTests
{
    private sealed class Rule(string name, GraderKind kind, bool required) : IGrader
    {
        public string Name => name;
        public int Version => 1;
        public GraderKind Kind => kind;
        public bool Required => required;
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
            ValueTask.FromResult(kind == GraderKind.Human
                ? Verdict.Pending(name, 1, "waits")
                : Verdict.Pass(name, 1, "fine"));
    }

    private static readonly IGrader Fixed = new Rule("routed-to", GraderKind.FixedRule, required: true);
    private static readonly IGrader Judge = new Rule("meal-for-people", GraderKind.Model, required: false);
    private static readonly IGrader Review = new Rule("meal-for-people.human", GraderKind.Human, required: false);

    [Test]
    public void PendingIsNotAPass_AndHasItsOwnWord()
    {
        var grade = Verdict.Pending("x.human", 1, "waits");

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Pending, 0d)));
        Assert.That(GradeOutcome.Pending, Is.Not.EqualTo(GradeOutcome.Pass).And.Not.EqualTo(GradeOutcome.Unknown));
    }

    [Test]
    public void APendingGraderThatIsNotRequired_ChangesNoTrialsOutcome()
    {
        var grades = new[]
            { Verdict.Pass("routed-to", 1, "fine"), Verdict.Pending("meal-for-people.human", 1, "waits") };

        Assert.That(PassRates.Of(grades, [Fixed, Review]), Is.EqualTo(TrialOutcome.Pass));
        Assert.That(PassRates.Of([Verdict.Fail("routed-to", 1, "no"), grades[1]], [Fixed, Review]),
            Is.EqualTo(TrialOutcome.Fail), "and a fail stays a fail");
    }

    [Test]
    public void APendingGraderThatWereRequired_IsNeverAPass()
    {
        var required = new Rule("meal-for-people.human", GraderKind.Human, required: true);

        var outcome = PassRates.Of([Verdict.Pending("meal-for-people.human", 1, "waits")], [required]);

        Assert.That(outcome, Is.EqualTo(TrialOutcome.Unknown));
    }

    [Test]
    public async Task TheConformanceCheck_AcceptsAHumanGraderThatIsNotRequired_AndRefusesOneThatIs()
    {
        var accepted = await EvalConformance.CheckAsync(new Eval(Fixed, Judge, Review), [], CancellationToken.None);
        var refused = await EvalConformance.CheckAsync(
            new Eval(Fixed, Judge, new Rule("meal-for-people.human", GraderKind.Human, required: true)), [],
            CancellationToken.None);

        Assert.That(accepted.Where(f => f.Contains("meal-for-people.human")), Is.Empty);
        Assert.That(refused.Where(f => f.Contains("meal-for-people.human")), Is.Not.Empty);
        Assert.That(refused.First(f => f.Contains("meal-for-people.human")),
            Does.Contain("person").And.Contain("required"));
    }

    [Test]
    public async Task AGraderNameMayHoldADot_ForAHumanReviews_ButAnEvalsNameMayNot()
    {
        var dotted = await EvalConformance.CheckAsync(
            new Eval(Fixed, new Rule("meal.for.people", GraderKind.Model, required: false),
                new Rule("meal.for.people.human", GraderKind.Human, required: false)), [], CancellationToken.None);
        var evalName = await EvalConformance.CheckAsync(new Eval("a.dotted.eval", Fixed), [], CancellationToken.None);

        Assert.That(dotted.Where(f => f.Contains("not safe")), Is.Empty);
        Assert.That(evalName.Where(f => f.Contains("not safe")), Is.Not.Empty);
    }

    [Test]
    public async Task ARunWritesThePendingGrade_AndAReScoreWritesItAgain_BothLeavingTheOutcomeToTheFixedRules()
    {
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
        string[] stored = ran.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var rescored = new StringWriter();

        await new EvalRunner(new NoSessionClient(), new ProtocolWriter(rescored, new FixedClock(at)),
                new FixedClock(at), NullLogger<EvalRunner>.Instance)
            .RescoreAsync(eval, "run_new", "run_old", stored, CancellationToken.None);

        foreach (string text in new[] { ran.ToString(), rescored.ToString() })
        {
            var conversation = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => JsonDocument.Parse(l.TrimStart('@')).RootElement.Clone())
                .Single(e => e.GetProperty("event").GetString() == "conversation");
            var grades = conversation.GetProperty("grades").EnumerateArray()
                .ToDictionary(g => g.GetProperty("grader").GetString()!, g => g.GetProperty("outcome").GetString());
            Assert.That(grades["meal-for-people.human"], Is.EqualTo("Pending"));
            Assert.That(conversation.GetProperty("outcome").GetString(), Is.EqualTo("Pass"),
                "the fixed rule made the outcome");
        }
    }

    private sealed class Eval(string name, params IGrader[] graders) : IEval
    {
        public Eval(params IGrader[] graders) : this("pending-eval", graders)
        {
        }

        public string Name => name;
        public int Version => 1;
        public string Proves => "A pending human grade.";

        public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<EvalMode>>([
                new EvalMode("cook", SampleRole.Alpha, [SampleToolset.Alpha])
            ]);

        public IReadOnlyList<EvalScenario> Scenarios { get; } =
        [
            new EvalScenario("bobotie", new EvalInput(null, "Africa/Johannesburg", ["Gee my resep"]), default, [],
                DatasetSplit.Dev)
        ];

        public IReadOnlyList<IGrader> Graders { get; } = graders;
        public EvalScores ScoresOf(Trial trial, IReadOnlyList<Grade> grades) => new Scores();
        public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => new(null, null, 0, 0, [], null);
    }

    private sealed record Scores : EvalScores;
}
