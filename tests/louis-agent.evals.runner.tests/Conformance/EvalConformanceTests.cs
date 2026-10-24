using System.Text.Json;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Conformance;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Runner.Arguments;
using LouisAgent.Evals.Runner.Catalog;
using LouisAgent.Evals.Runner.Stub;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;

namespace LouisAgent.Evals.Runner.Tests.Conformance;

// The conformance test (F18 step 4.2b): every eval of every set passes the check. In 4.2 it runs on the stub and a fake set; Puls's
// evals meet it when they join PulsEvalSet (step 5). Written first: the check threw NotImplementedException on the red run.
public class EvalConformanceTests
{
    [Test]
    public async Task EveryEvalOfEverySetConforms()
    {
        var catalog = new EvalCatalog([EvalSets.Create(EvalSetKind.Puls), new FakeSet()], new StubEval());

        foreach (IEval eval in catalog.Evals)
        {
            var failures = await EvalConformance.CheckAsync(eval, await CasesOfAsync(eval), CancellationToken.None);
            Assert.That(failures, Is.Empty, eval.Name);
        }
    }

    [Test]
    public void TheStubsReferencesAreThePythonsConversations()
    {
        var mode = new EvalMode("puls-cook", StubEval.Role, ["specialists", "web"]);

        var bobotie = ReferenceTranscripts.Of(mode, "bobotie").Single();

        Assert.That(bobotie.Reference.Turns.Select(turn => turn.Question),
            Is.EqualTo(new[] { "Maak vir my bobotie vir 4 mense.", "En as ek dit vir 6 wil maak?" }));
        Assert.That(bobotie.Reference.Turns.SelectMany(turn => turn.Events).OfType<ErrorReported>(), Is.Empty);
        Assert.That(bobotie.KnownBad.Turns.SelectMany(turn => turn.Events).OfType<ErrorReported>(), Has.Exactly(1).Items);
    }

    /// <summary>The reference cases of an eval, by its name: a new eval adds its own here, and until then it has none (and fails).</summary>
    private static async Task<IReadOnlyList<ConformanceCase>> CasesOfAsync(IEval eval)
    {
        switch (eval.Name)
        {
            case "stub":
            {
                var mode = (await eval.ModesAsync(new StubSessionClient(), CancellationToken.None)).Single(m => m.Name == "puls-cook");
                return ReferenceTranscripts.Of(mode, "bobotie", "cottage-pie");
            }
            case "fake":
                return ReferenceTranscripts.Of(new EvalMode("puls-cook", StubEval.Role, ["specialists", "web"]), "cottage-pie");
            default:
                return [];
        }
    }

    private sealed class FakeSet : IEvalSet
    {
        public string Identity => "fake";
        public IReadOnlyList<IEval> Evals { get; } = [new FakeEval()];
    }

    /// <summary>A second set's eval, so the test covers more than one set, with the common graders it would use.</summary>
    private sealed class FakeEval : IEval
    {
        public string Name => "fake";
        public string? Identity => "fake";
        public int Version => 1;
        public string Proves => "A fake eval, to test the conformance check on a set that isn't Puls's.";

        public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<EvalMode>>([new EvalMode("puls-cook", StubEval.Role, ["specialists", "web"])]);

        public IReadOnlyList<EvalScenario> Scenarios { get; } =
        [
            new EvalScenario("cottage-pie", new EvalInput(null, "Africa/Johannesburg", ["Hoe maak ek cottage pie?"]),
                JsonDocument.Parse("{}").RootElement.Clone(), [], DatasetSplit.Dev),
        ];

        public IReadOnlyList<IGrader> Graders { get; } = [new NoErrorGrader()];

        public EvalScores ScoresOf(Trial trial, IReadOnlyList<Grade> grades) => throw new NotImplementedException();

        public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => throw new NotImplementedException();
    }
}
