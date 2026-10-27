using LouisAgent.Choices;
using LouisAgent.Evals.Puls.Shared;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Puls.Evals.Cook;
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
        Assert.That(set.Evals.Select(e => e.Name), Is.EqualTo(new[] { PulsEvalName.Cook.Wire(), PulsEvalName.Routing.Wire() }));
    }

    [TestCase("cook-no-error")]
    [TestCase("routed-to")]
    [TestCase("within-budget")]
    [TestCase("estimates-at-shop")]
    [TestCase("serves-as-asked")]
    [TestCase("meal-for-people")]
    public async Task ATurnThatFailedForTheInfrastructure_IsUnknown_NeverAFail(string name)
    {
        IGrader grader = new CookEval().Graders.Single(g => g.Name == name);
        // The failed turn has a whole, good answer: without the infrastructure check each grader would pass it, so the test tells a grader that
        // checked from one that was merely handed an empty turn (reviewed on 5.4: three checks could be removed and the tests still passed).
        const string answer = """
            Hoender vir 4 mense.

            | Ingredient | For the recipe | To buy | Checkers |
            |---|---|---|---|
            | Hoender | 1 kg | Hoender 1 kg | [R69,99](https://www.checkers.co.za/p/1) |
            | **Totaal** | | | **R69,99** |
            """;
        var trial = Trial(CookMode, "cook", """{"routes_to":"puls-cook","town":"Malmesbury","serves":4,"budget":150,"shop":"checkers"}""", "Malmesbury, Wes-Kaap, Suid-Afrika",
            Turn(1, [Start("puls-cook"), SpecialistCall("FindProducts", """{"shop":"checkers","area":"Malmesbury","query":"hoender"}"""), Text(answer)],
                failure: LouisAgent.Evals.Trials.TrialFailure.Timeout));

        var grade = await grader.GradeAsync(trial, CancellationToken.None);

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
        Assert.That(grade.Reason, Does.Contain("infrastructure"), "Unknown because of the failure, not because there was nothing to read");
    }
}
