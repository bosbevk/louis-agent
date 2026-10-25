using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Puls.Grading.FixedRules;
using static LouisAgent.Evals.Puls.Tests.PulsTrials;

namespace LouisAgent.Evals.Puls.Tests;

// F18 step 5.3: Puls's set lists its two evals, and its graders leave a turn that failed for the infrastructure Unknown (step 5.1 now records
// such turns, so a flaky network can never count as the agent failing).
public class PulsEvalSetTests
{
    [Test]
    public void TheSetListsTheCookAndTheRoutingEvals()
    {
        var set = new PulsEvalSet();

        Assert.That(set.Identity, Is.EqualTo("puls"));
        Assert.That(set.Evals.Select(e => e.Name), Is.EqualTo(new[] { "puls-cook", "puls-routing" }));
    }

    [TestCase("cook-no-error")]
    [TestCase("cook-routed-right")]
    [TestCase("routing-routed-right")]
    public async Task ATurnThatFailedForTheInfrastructure_IsUnknown_NeverAFail(string name)
    {
        IGrader grader = name switch
        {
            "cook-no-error" => new CookNoError(),
            "cook-routed-right" => new CookRoutedRight(),
            _ => new RoutingRoutedRight(),
        };
        var trial = Trial(CookMode, "cook", Turn(1, [], failure: LouisAgent.Evals.Trials.TrialFailure.Timeout));

        var grade = await grader.GradeAsync(trial, CancellationToken.None);

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }
}
