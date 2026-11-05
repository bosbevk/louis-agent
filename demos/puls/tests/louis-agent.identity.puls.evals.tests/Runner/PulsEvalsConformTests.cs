using LouisAgent.Evals.Conformance;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Runner.Catalog;
using LouisAgent.PulsEvalRunner;

namespace LouisAgent.Identities.Puls.Evals.Tests.Runner;

/// <summary>
/// One test per eval folder copied beside these tests, loaded with Puls's own lists: a new folder is checked with no edit
/// here. The harness's own conformance tests are in the framework; these prove Puls's evals.
/// </summary>
public class PulsEvalsConformTests
{
    /// <summary>Every eval folder Puls's runner would find.</summary>
    private static IEnumerable<TestCaseData> EveryEval() =>
        EvalFolders.Find(Path.Combine(AppContext.BaseDirectory, "EvalFiles"))
            .Select(folder => FileEval.Load(folder.Folder, PulsCatalogs.All.CheckCatalog,
                PulsCatalogs.All.ReaderCatalog, folder.Source))
            .Select(eval => new TestCaseData(eval).SetName(eval.Name));

    /// <summary>Each eval's references pass its required checks, and each required check fails a known-bad.</summary>
    [TestCaseSource(nameof(EveryEval))]
    public async Task AnEvalConforms(FileEval eval)
    {
        var cases = ConformanceCases.Load(eval.Transcripts, eval.Scenarios);

        var failures = await EvalConformance.CheckAsync(eval, cases, CancellationToken.None);

        Assert.That(failures, Is.Empty, eval.Name);
    }
}
