using LouisAgent.Choices;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Identities.Puls.Evals.Checks.Shared;
using static LouisAgent.Identities.Puls.Tests.Evals.Checks.PulsTrials;

namespace LouisAgent.Identities.Puls.Tests.Evals.Checks;

/// <summary>
/// The cook's checks leave a turn that failed for the infrastructure Unknown, so a flaky network can never count as
/// the agent failing.
/// </summary>
public class InfrastructureFailureTests
{
    /// <summary>
    /// Each of the cook's fixed checks leaves a turn the infrastructure broke Unknown, never a fail, even when the turn
    /// holds a whole, good answer.
    /// </summary>
    [TestCase("cook-no-error")]
    [TestCase("routed-to")]
    [TestCase("within-budget")]
    [TestCase("estimates-at-shop")]
    [TestCase("serves-as-asked")]
    [TestCase("meal-for-people")]
    public async Task ATurnThatFailedForTheInfrastructure_IsUnknown_NeverAFail(string name)
    {
        var grader = CookFile.Eval.Checks.Single(g => g.Name == name);
        // The failed turn holds a whole, good answer: without the infrastructure check each check would pass it, so the
        // test tells a check that looked apart from one that was merely handed an empty turn.
        const string answer = """
                              Hoender vir 4 mense.

                              | Ingredient | For the recipe | To buy | Checkers |
                              |---|---|---|---|
                              | Hoender | 1 kg | Hoender 1 kg | [R69,99](https://www.checkers.co.za/p/1) |
                              | **Totaal** | | | **R69,99** |
                              """;
        var trial = Trial(CookEvalMode, "cook",
            """{"routes_to":"puls-cook","town":"Malmesbury","serves":4,"budget":150,"shop":"checkers"}""",
            "Malmesbury, Wes-Kaap, Suid-Afrika",
            Turn(1,
                [
                    Start(PulsEvalRole.Cook.Wire()),
                    SpecialistCall("FindProducts", """{"shop":"checkers","area":"Malmesbury","query":"hoender"}"""),
                    Text(answer)
                ],
                failure: TurnFailure.Timeout));

        var grade = await grader.GradeAsync(trial, CancellationToken.None);

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
        Assert.That(grade.Reason, Does.Contain("infrastructure"),
            "Unknown because of the failure, not because there was nothing to read");
    }
}
