using System.Text.Json;
using LouisAgent.Evals.Conformance;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Trials;
using static LouisAgent.Evals.Tests.Trials.TrialBuilder;

namespace LouisAgent.Evals.Tests.Conformance;

// The conformance check (F18 step 4.2b): each check on an eval built to break it, and one that conforms. Written first: the check
// threw NotImplementedException on the red run.
public class EvalConformanceCheckTests
{
    private static readonly EvalScenario Bobotie = new("bobotie", new EvalInput(null, "Etc/UTC", ["Hallo"]),
        JsonDocument.Parse("{}").RootElement.Clone(), [], DatasetSplit.Dev);

    private static readonly Transcript Clean = new([Turn(1, [Text("Hallo")])]);
    private static readonly Transcript Errored = new([Turn(1, [Error()])]);

    [Test]
    public async Task AConformingEvalHasNoFailures()
    {
        Assert.That(await CheckAsync(Conforming(), Case(Clean, Errored)), Is.Empty);
    }

    [Test]
    public async Task ANameThatIsNotSafeAsAnMCPToolNameFails()
    {
        Assert.That(Joined(await CheckAsync(Conforming(name: "bad name!"), Case(Clean, Errored))), Does.Contain("bad name!"));
    }

    [Test]
    public async Task AVersionBelowOneFails()
    {
        Assert.That(Joined(await CheckAsync(Conforming(version: 0), Case(Clean, Errored))), Does.Contain("version 0"));
    }

    [Test]
    public async Task AnEvalWithNoRequiredFixedRuleFails()
    {
        var eval = Conforming(graders: [new NoErrorGrader(required: false)]);

        Assert.That(Joined(await CheckAsync(eval, Case(Clean, Errored))), Does.Contain("no required fixed-rule"));
    }

    [Test]
    public async Task AnExpectedKeyNoGraderReadsFails()
    {
        var scenario = Bobotie with { Expected = JsonDocument.Parse("""{"shop":true}""").RootElement.Clone() };

        Assert.That(Joined(await CheckAsync(Conforming(scenarios: [scenario]), Case(Clean, Errored))), Does.Contain("'shop'"));
    }

    [Test]
    public async Task AKnownBadTranscriptThatPassesEveryRequiredGraderIsCaught()
    {
        // A grader that passes everything can't be told from a good one: the known-bad transcript exposes it.
        var eval = Conforming(graders: [new FakeGrader("always-pass", _ => new Grade("always-pass", 1, 1, GradeOutcome.Pass, "always"))]);

        Assert.That(Joined(await CheckAsync(eval, Case(Clean, Errored))), Does.Contain("passes every required grader"));
    }

    [Test]
    public async Task AReferenceThatFailsARequiredGraderIsCaught()
    {
        Assert.That(Joined(await CheckAsync(Conforming(), Case(Errored, Errored))), Does.Contain("reference transcript fails"));
    }

    [Test]
    public async Task AGradeWithNoReasonIsCaught()
    {
        var eval = Conforming(graders: [new FakeGrader("no-reason", _ => new Grade("no-reason", 1, 1, GradeOutcome.Pass, " "))]);

        Assert.That(Joined(await CheckAsync(eval, Case(Clean, Errored))), Does.Contain("no reason"));
    }

    [Test]
    public async Task AScoreOutsideZeroToOneIsCaught()
    {
        var eval = Conforming(graders: [new FakeGrader("too-high", _ => new Grade("too-high", 1, 2, GradeOutcome.Pass, "two"))]);

        Assert.That(Joined(await CheckAsync(eval, Case(Clean, Errored))), Does.Contain("between 0 and 1"));
    }

    [Test]
    public async Task AnOutcomeThatDisagreesWithItsScoreIsCaught()
    {
        var eval = Conforming(graders: [new FakeGrader("liar", _ => new Grade("liar", 1, 0, GradeOutcome.Pass, "passed"))]);

        Assert.That(Joined(await CheckAsync(eval, Case(Clean, Errored))), Does.Contain("disagrees"));
    }

    [Test]
    public async Task AGraderThatThrowsIsCaughtNotRethrown()
    {
        var eval = Conforming(graders: [new FakeGrader("broken", _ => throw new InvalidOperationException("boom"))]);

        var failures = Joined(await CheckAsync(eval, Case(Clean, Errored)));

        Assert.That(failures, Does.Contain("threw").And.Contain("boom"));
    }

    [Test]
    public async Task AScenarioWithoutReferenceTranscriptsIsCaught()
    {
        Assert.That(Joined(await CheckAsync(Conforming(), [])), Does.Contain("no reference transcript"));
    }

    private static ConformanceCase Case(Transcript reference, Transcript knownBad, string scenario = "bobotie") =>
        new(scenario, Mode, reference, knownBad);

    private static FakeEval Conforming(string name = "cook", int version = 1, IReadOnlyList<IGrader>? graders = null,
        IReadOnlyList<EvalScenario>? scenarios = null) =>
        new(name, version, graders ?? [new NoErrorGrader()], scenarios ?? [Bobotie]);

    private static async Task<IReadOnlyList<string>> CheckAsync(IEval eval, params ConformanceCase[] cases) =>
        await EvalConformance.CheckAsync(eval, cases, CancellationToken.None);

    private static string Joined(IEnumerable<string> failures) => string.Join("\n", failures);

    private sealed class FakeEval(string name, int version, IReadOnlyList<IGrader> graders, IReadOnlyList<EvalScenario> scenarios) : IEval
    {
        public string Name { get; } = name;
        public string? Identity => "puls";
        public int Version { get; } = version;
        public string Proves => "A fake eval, for the conformance check.";
        public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(LouisAgent.Evals.Api.ISessionClient sessions, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public IReadOnlyList<EvalScenario> Scenarios { get; } = scenarios;
        public IReadOnlyList<IGrader> Graders { get; } = graders;
        public EvalScores ScoresOf(Trial trial, IReadOnlyList<Grade> grades) => throw new NotImplementedException();
        public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => throw new NotImplementedException();
    }

    /// <summary>A grader whose verdict is the function's; a function that throws is a grader that throws.</summary>
    private sealed class FakeGrader(string name, Func<Trial, Grade> grade) : IGrader
    {
        public string Name { get; } = name;
        public int Version => 1;
        public GraderKind Kind => GraderKind.FixedRule;
        public bool Required => true;
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();
        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) => ValueTask.FromResult(grade(trial));
    }
}
