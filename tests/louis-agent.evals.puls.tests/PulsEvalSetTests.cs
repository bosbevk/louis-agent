using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Puls.Cook;
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
    [TestCase("routed-to")]
    [TestCase("within-budget")]
    [TestCase("priced-at-shop")]
    [TestCase("serves-as-asked")]
    public async Task ATurnThatFailedForTheInfrastructure_IsUnknown_NeverAFail(string name)
    {
        IGrader grader = new CookEval().Graders.Single(g => g.Name == name);
        var trial = Trial(CookMode, "cook", """{"routes_to":"puls-cook","town":"Malmesbury","serves":4,"budget":150,"shop":"checkers"}""", "Malmesbury, Wes-Kaap, Suid-Afrika",
            Turn(1, [], failure: LouisAgent.Evals.Trials.TrialFailure.Timeout));

        var grade = await grader.GradeAsync(trial, CancellationToken.None);

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }
}
