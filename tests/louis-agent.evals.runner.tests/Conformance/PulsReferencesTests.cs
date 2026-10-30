using LouisAgent.Evals.Conformance;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Runner.Catalog;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Runner.Tests.Conformance;

/// <summary>
/// A reference conversation is one that is right in every way the eval can tell, not only in its required checks.
/// The conformance check holds the required ones; this holds the rest of the fixed rules of every eval folder found
/// (the serves rule is not required, yet a reference that forgot the number would teach the wrong thing).
/// </summary>
/// <remarks>
/// A model check is left out: it passes nothing without a judge, and whether its judge agrees with a person is the
/// calibration's to say, not a fixed rule of the references.
/// </remarks>
public class PulsReferencesTests
{
    /// <summary>Every reference of every eval folder copied beside these tests, one case each.</summary>
    private static IEnumerable<TestCaseData> References()
    {
        foreach (var folder in EvalFolders.Find(Path.Combine(AppContext.BaseDirectory, "EvalFiles")))
        {
            var eval = FileEval.Load(folder.Folder, RunnerChecks.Catalog, folder.Source);
            foreach (var conformanceCase in ConformanceCases.Load(eval.Transcripts, eval.Scenarios))
                yield return new TestCaseData(eval, conformanceCase).SetName($"{eval.Name}/{conformanceCase.Scenario}");
        }
    }

    /// <summary>Each reference passes every fixed rule of its eval, required or not.</summary>
    [TestCaseSource(nameof(References))]
    public async Task EveryReferencePassesEveryGrader(IEval eval, ConformanceCase conformanceCase)
    {
        var scenario = eval.Scenarios.Single(s => s.Id == conformanceCase.Scenario);
        var trial = new Trial("conformance", conformanceCase.Mode, 1, scenario, conformanceCase.Reference);

        foreach (var grader in eval.Checks.Where(g => g.Kind == CheckKind.FixedRule))
        {
            var grade = await grader.GradeAsync(trial, CancellationToken.None);

            Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass), $"{grader.Name}: {grade.Reason}");
        }
    }
}
