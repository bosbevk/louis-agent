using System.Text.Json;
using LouisAgent.Evals.Conformance;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Protocol.Kinds;
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
        Assert.That(Joined(await CheckAsync(Conforming(name: "bad name!"), Case(Clean, Errored))),
            Does.Contain("bad name!"));
    }

    [Test]
    public async Task AVersionBelowOneFails()
    {
        Assert.That(Joined(await CheckAsync(Conforming(version: 0), Case(Clean, Errored))), Does.Contain("version 0"));
    }

    [Test]
    public async Task AnEvalWithNoRequiredFixedRuleFails()
    {
        var eval = Conforming(graders: [new NoErrorCheck(required: false)]);

        Assert.That(Joined(await CheckAsync(eval, Case(Clean, Errored))), Does.Contain("no required fixed-rule"));
    }

    [Test]
    public async Task AnExpectedKeyNoGraderReadsFails()
    {
        var scenario = Bobotie with { Expected = JsonDocument.Parse("""{"shop":true}""").RootElement.Clone() };

        Assert.That(Joined(await CheckAsync(Conforming(scenarios: [scenario]), Case(Clean, Errored))),
            Does.Contain("'shop'"));
    }

    [Test]
    public async Task AKnownBadTranscriptThatPassesEveryRequiredGraderIsCaught()
    {
        // A grader that passes everything can't be told from a good one: the known-bad transcript exposes it.
        var eval = Conforming(graders:
            [new FakeCheck("always-pass", _ => new Grade("always-pass", 1, 1, GradeOutcome.Pass, "always"))]);

        Assert.That(Joined(await CheckAsync(eval, Case(Clean, Errored))), Does.Contain("passes every required grader"));
    }

    [Test]
    public async Task AReferenceThatFailsARequiredGraderIsCaught()
    {
        Assert.That(Joined(await CheckAsync(Conforming(), Case(Errored, Errored))),
            Does.Contain("reference transcript fails"));
    }

    [Test]
    public async Task AGradeWithNoReasonIsCaught()
    {
        var eval = Conforming(graders:
            [new FakeCheck("no-reason", _ => new Grade("no-reason", 1, 1, GradeOutcome.Pass, " "))]);

        Assert.That(Joined(await CheckAsync(eval, Case(Clean, Errored))), Does.Contain("no reason"));
    }

    [Test]
    public async Task AScoreOutsideZeroToOneIsCaught()
    {
        var eval = Conforming(graders:
            [new FakeCheck("too-high", _ => new Grade("too-high", 1, 2, GradeOutcome.Pass, "two"))]);

        Assert.That(Joined(await CheckAsync(eval, Case(Clean, Errored))), Does.Contain("between 0 and 1"));
    }

    [Test]
    public async Task AnOutcomeThatDisagreesWithItsScoreIsCaught()
    {
        var eval = Conforming(graders:
            [new FakeCheck("liar", _ => new Grade("liar", 1, 0, GradeOutcome.Pass, "passed"))]);

        Assert.That(Joined(await CheckAsync(eval, Case(Clean, Errored))), Does.Contain("disagrees"));
    }

    [Test]
    public async Task AGraderThatThrowsIsCaughtNotRethrown()
    {
        var eval = Conforming(graders: [new FakeCheck("broken", _ => throw new InvalidOperationException("boom"))]);

        var failures = Joined(await CheckAsync(eval, Case(Clean, Errored)));

        Assert.That(failures, Does.Contain("threw").And.Contain("boom"));
    }

    // F18 step 6.1: a judge that times out throws OperationCanceledException too (HttpClient's); only the check's own cancel stops the check.
    [Test]
    public async Task AGradersOwnTimeoutIsReportedAsAThrow_AndTheChecksCancelStillStopsIt()
    {
        var eval = Conforming(graders:
            [new FakeCheck("slow", _ => throw new TaskCanceledException("the judge timed out"))]);

        Assert.That(Joined(await CheckAsync(eval, Case(Clean, Errored))),
            Does.Contain("threw").And.Contain("the judge timed out"));

        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        Assert.That(async () => await EvalConformance.CheckAsync(eval, [Case(Clean, Errored)], cancelled.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public async Task AScenarioWithoutReferenceTranscriptsIsCaught()
    {
        Assert.That(Joined(await CheckAsync(Conforming(), [])), Does.Contain("no reference transcript"));
    }

    [Test]
    public async Task AKnownBadMustFailEveryRequiredGrader()
    {
        // A grader that passes every trial fails no known-bad: an error-only known-bad never caught the stub's within-time grader.
        var eval = Conforming(graders: [new NoErrorCheck(), new FakeCheck("within", _ => Passed("within"))]);

        Assert.That(Joined(await CheckAsync(eval, Case(Clean, Errored))),
            Does.Contain("no known-bad transcript of any scenario fails the required grader 'within'"));
    }

    [Test]
    public async Task AnUnknownIsNotAFail()
    {
        // Unknown means the grader can't tell: it doesn't show that a grader fails on a known-bad transcript.
        var unknownOnError = new FakeCheck("unknown-on-error", trial =>
            EngineEvents.StreamedError(trial.Transcript.Turns[0])
                ? new Grade("unknown-on-error", 1, 0, GradeOutcome.Unknown, "can't tell")
                : Passed("unknown-on-error"));

        Assert.That(Joined(await CheckAsync(Conforming(graders: [unknownOnError]), Case(Clean, Errored))),
            Does.Contain("no known-bad transcript of any scenario fails the required grader 'unknown-on-error'"));
    }

    [Test]
    public async Task ANoKnownBadAtAllFails()
    {
        var none = new ConformanceCase("bobotie", Mode, Clean, []);

        Assert.That(Joined(await CheckAsync(Conforming(), none)),
            Does.Contain("no known-bad transcript of any scenario fails the required grader 'no-error'"));
    }

    [Test]
    public async Task TwoGradersWithOneNameFail()
    {
        var eval = Conforming(graders: [new NoErrorCheck(), new NoErrorCheck()]);

        Assert.That(Joined(await CheckAsync(eval, Case(Clean, Errored))), Does.Contain("two graders named 'no-error'"));
    }

    [Test]
    public async Task AGraderVersionBelowOneFails()
    {
        var eval = Conforming(graders: [new FakeCheck("old", _ => Passed("old"), version: 0)]);

        Assert.That(Joined(await CheckAsync(eval, Case(Clean, Errored))), Does.Contain("grader 'old' has version 0"));
    }

    [Test]
    public async Task AGraderMayFailAnyScenariosKnownBad()
    {
        // Each required grader fails a known-bad of some scenario, not all: A on the first scenario's, B on the second's. The eval conforms.
        var eval = Conforming(
            graders:
            [
                new FakeCheck("a", trial => Judge("a", trial, "one")),
                new FakeCheck("b", trial => Judge("b", trial, "two"))
            ],
            scenarios: [Named("one"), Named("two")]);

        Assert.That(Joined(await CheckAsync(eval,
            new ConformanceCase("one", Mode, Clean, [new KnownBad("bad", Errored)]),
            new ConformanceCase("two", Mode, Clean, [new KnownBad("bad", Errored)]))), Is.Empty);
    }

    [Test]
    public async Task AScenarioWithNoKnownBadConformsWhenTheOthersCoverEveryGrader()
    {
        var eval = Conforming(graders: [new NoErrorCheck()], scenarios: [Named("one"), Named("two")]);

        Assert.That(Joined(await CheckAsync(eval,
            new ConformanceCase("one", Mode, Clean, [new KnownBad("bad", Errored)]),
            new ConformanceCase("two", Mode, Clean, []))), Is.Empty);
    }

    [Test]
    public async Task AGraderNameThatIsNotSafeAsAnMCPToolNameFails()
    {
        var eval = Conforming(graders: [new FakeCheck("bad name!", _ => Passed("bad name!"))]);

        Assert.That(Joined(await CheckAsync(eval, Case(Clean, Errored))),
            Does.Contain("grader name 'bad name!' is not safe"));
    }

    /// <summary>A grader's verdict on the first turn: a fail for the scenario it names, a pass for the others.</summary>
    private static Grade Judge(string name, Trial trial, string failsOn) =>
        EngineEvents.StreamedError(trial.Transcript.Turns[0]) && trial.Scenario.Id == failsOn
            ? Failed(name)
            : Passed(name);

    private static Grade Failed(string name) => new(name, 1, 0, GradeOutcome.Fail, "failed");

    private static EvalScenario Named(string id) => Bobotie with { Id = id };

    private static Grade Passed(string name) => new(name, 1, 1, GradeOutcome.Pass, "passed");

    private static ConformanceCase Case(Transcript reference, Transcript knownBad, string scenario = "bobotie") =>
        new(scenario, Mode, reference, [new KnownBad("bad", knownBad)]);

    private static FakeEval Conforming(string name = "cook", int version = 1, IReadOnlyList<ICheck>? graders = null,
        IReadOnlyList<EvalScenario>? scenarios = null) =>
        new(name, version, graders ?? [new NoErrorCheck()], scenarios ?? [Bobotie]);

    private static async Task<IReadOnlyList<string>> CheckAsync(IEval eval, params ConformanceCase[] cases) =>
        await EvalConformance.CheckAsync(eval, cases, CancellationToken.None);

    private static string Joined(IEnumerable<string> failures) => string.Join("\n", failures);

    private sealed class FakeEval(
        string name,
        int version,
        IReadOnlyList<ICheck> graders,
        IReadOnlyList<EvalScenario> scenarios) : IEval
    {
        public string Name { get; } = name;
        public int Version { get; } = version;
        public string Proves => "A fake eval, for the conformance check.";

        public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(LouisAgent.Evals.Api.ISessionClient sessions,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public IReadOnlyList<EvalScenario> Scenarios { get; } = scenarios;
        public IReadOnlyList<ICheck> Checks { get; } = graders;
        public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => throw new NotImplementedException();
    }

    /// <summary>A grader whose verdict is the function's; a function that throws is a grader that throws.</summary>
    private sealed class FakeCheck(string name, Func<Trial, Grade> grade, int version = 1) : ICheck
    {
        public string Name { get; } = name;
        public int Version { get; } = version;
        public CheckKind Kind => CheckKind.FixedRule;
        public bool Required => true;
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
            ValueTask.FromResult(grade(trial));
    }
}
