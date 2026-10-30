using LouisAgent.Choices;
using System.Text.Json;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Conformance;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Runner.Catalog;
using LouisAgent.Evals.Trials;
using LouisAgent.Evals.Runner.Evals.Stub;

namespace LouisAgent.Evals.Runner.Tests.Conformance;

/// <summary>
/// The conformance test: one test per eval the runner can run, of every set and every folder it finds. An eval's
/// references are the files beside it, in <c>Evals/{Folder}/Transcripts</c>; the fake set's are here, beside this test.
/// </summary>
public class EvalConformanceTests
{
    /// <summary>
    /// Every eval the runner knows: every set's, every eval folder copied beside these tests, and the stub, so a new
    /// set or folder is checked with no edit here.
    /// </summary>
    private static IEnumerable<TestCaseData> EveryEval()
    {
        var sets = Enum.GetValues<EvalSetKind>().Select(EvalSets.Create).Append(new FakeSet());
        var files = EvalFolders.Find(Path.Combine(AppContext.BaseDirectory, "EvalFiles"))
            .Select(folder => FileEval.Load(folder.Folder, RunnerChecks.Catalog, folder.Source));
        var catalog = new EvalCatalog(sets.ToList(), new StubEval(), files);
        foreach (var eval in catalog.Evals)
            yield return new TestCaseData(eval).SetName(eval.Name);
    }

    /// <summary>Each eval's references pass its required checks, and each required check fails a known-bad.</summary>
    [TestCaseSource(nameof(EveryEval))]
    public async Task EvalConforms(IEval eval)
    {
        var cases = ConformanceCases.Load(TranscriptsOf(eval), eval.Scenarios);

        var failures = await EvalConformance.CheckAsync(eval, cases, CancellationToken.None);

        Assert.That(failures, Is.Empty, eval.Name);
    }

    /// <summary>The stub's references are the Python stub's conversations, with its known-bad ones.</summary>
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

    /// <summary>Where an eval's transcripts are: beside its file, beside its C#, or here for the fake.</summary>
    private static string TranscriptsOf(IEval eval) => eval switch
    {
        FakeEval => Path.Combine(AppContext.BaseDirectory, "Transcripts", "fake"),
        FileEval file => file.Transcripts,
        _ => EvalFiles.TranscriptsOf(eval)
    };

    /// <summary>A second set, of one eval, to test the conformance check on a set that isn't Puls's.</summary>
    private sealed class FakeSet : IEvalSet
    {
        public string Identity => "fake";
        public IReadOnlyList<IEval> Evals { get; } = [new FakeEval()];
    }

    /// <summary>
    /// A second set's eval, with the general no-error check it would use; its references are in Transcripts/fake.
    /// </summary>
    private sealed class FakeEval : IEval
    {
        public string Name => "fake";
        public int Version => 1;
        public string Proves => "A fake eval, to test the conformance check on a set that isn't Puls's.";

        /// <summary>Its one mode, the stub's delegating one.</summary>
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

        /// <summary>Never asked: conformance records no turn facts.</summary>
        public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => throw new NotImplementedException();
    }
}
