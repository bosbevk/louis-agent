using LouisAgent.Choices;
using System.Text.Json;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Conformance;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Runner.Catalog;
using LouisAgent.Evals.Trials;
using LouisAgent.Evals.Runner.Evals.Stub;

namespace LouisAgent.Evals.Runner.Tests.Conformance;

// The conformance test (F18 step 4.2b): one test per eval, of every set. An eval's references are the files beside it, in
// Evals/{Folder}/Transcripts (EvalFiles finds them for any eval, F18 step 5.4b); the fake set's are here, beside this test.
public class EvalConformanceTests
{
    private static IEnumerable<TestCaseData> EvalsOfEverySet()
    {
        // Every set the runner knows, so a new set's evals are checked with no edit here.
        var sets = Enum.GetValues<EvalSetKind>().Select(EvalSets.Create).Append(new FakeSet());
        var catalog = new EvalCatalog(sets.ToList(), new StubEval());
        foreach (IEval eval in catalog.Evals)
            yield return new TestCaseData(eval).SetName(eval.Name);
    }

    [TestCaseSource(nameof(EvalsOfEverySet))]
    public async Task EvalConforms(IEval eval)
    {
        var cases = ConformanceCases.Load(
            eval is FakeEval
                ? Path.Combine(AppContext.BaseDirectory, "Transcripts", "fake")
                : EvalFiles.TranscriptsOf(eval), eval.Scenarios);

        var failures = await EvalConformance.CheckAsync(eval, cases, CancellationToken.None);

        Assert.That(failures, Is.Empty, eval.Name);
    }

    [Test]
    public void TheStubsReferencesAreThePythonsConversations()
    {
        var cases = ConformanceCases.Load(EvalFiles.TranscriptsOf(new StubEval()), new StubEval().Scenarios);

        var bobotie = cases.Single(c => c.Scenario == "bobotie");
        Assert.That(bobotie.Mode.Name, Is.EqualTo(StubMode.Delegating.Wire()));
        Assert.That(bobotie.Reference.Turns.Select(turn => turn.Question),
            Is.EqualTo(new[] { "Maak vir my bobotie vir 4 mense.", "En as ek dit vir 6 wil maak?" }));
        Assert.That(bobotie.KnownBad.Select(k => k.Label), Is.EqualTo(new[] { "error", "slow" }));
    }

    private sealed class FakeSet : IEvalSet
    {
        public string Identity => "fake";
        public IReadOnlyList<IEval> Evals { get; } = [new FakeEval()];
    }

    /// <summary>A second set's eval, with the common no-error grader it would use; its references are in Transcripts/fake.</summary>
    private sealed class FakeEval : IEval
    {
        public string Name => "fake";
        public int Version => 1;
        public string Proves => "A fake eval, to test the conformance check on a set that isn't Puls's.";

        public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<EvalMode>>([
                new EvalMode(StubMode.Delegating.Wire(), StubEval.Role,
                    [StubToolset.Specialists.Wire(), StubToolset.Web.Wire()])
            ]);

        public IReadOnlyList<EvalScenario> Scenarios { get; } =
        [
            new EvalScenario("cottage-pie", new EvalInput(null, "Africa/Johannesburg", ["Hoe maak ek cottage pie?"]),
                JsonDocument.Parse("{}").RootElement.Clone(), [], DatasetSplit.Dev),
        ];

        public IReadOnlyList<ICheck> Checks { get; } = [new NoErrorCheck()];

        public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => throw new NotImplementedException();
    }
}
