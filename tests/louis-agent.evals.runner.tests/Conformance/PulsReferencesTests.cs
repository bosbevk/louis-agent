using LouisAgent.Evals.Conformance;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Puls;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Runner.Tests.Conformance;

// F18 step 5.4: a reference conversation is one that is right in every way the eval can tell, not only in the required graders. The conformance
// check holds the required ones; this holds the rest of the fixed rules (the serves rule is not required, yet a reference that forgot the number would teach the wrong thing).
// A model grader is left out: it passes nothing without a judge, and whether its judge agrees with a person is step 6.3's calibration, not a fixed rule of the references.
public class PulsReferencesTests
{
    private static IEnumerable<TestCaseData> References()
    {
        foreach (IEval eval in new PulsEvalSet().Evals)
            foreach (var conformanceCase in ConformanceCases.Load(EvalFiles.TranscriptsOf(eval), eval.Scenarios))
                yield return new TestCaseData(eval, conformanceCase).SetName($"{eval.Name}/{conformanceCase.Scenario}");
    }

    [TestCaseSource(nameof(References))]
    public async Task EveryReferencePassesEveryGrader(IEval eval, ConformanceCase conformanceCase)
    {
        var scenario = eval.Scenarios.Single(s => s.Id == conformanceCase.Scenario);
        var trial = new Trial("conformance", conformanceCase.Mode, 1, scenario, conformanceCase.Reference);

        foreach (IGrader grader in eval.Graders.Where(g => g.Kind == GraderKind.FixedRule))
        {
            var grade = await grader.GradeAsync(trial, CancellationToken.None);

            Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass), $"{grader.Name}: {grade.Reason}");
        }
    }
}
