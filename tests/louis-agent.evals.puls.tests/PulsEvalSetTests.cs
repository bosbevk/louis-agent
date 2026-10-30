using LouisAgent.Choices;
using LouisAgent.Identities.Puls;
using LouisAgent.Evals.Puls.Shared;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Puls.Evals.Cook;
using static LouisAgent.Evals.Puls.Tests.PulsTrials;

namespace LouisAgent.Evals.Puls.Tests;

/// <summary>
/// Puls's set lists its eval in C#, and its checks leave a turn that failed for the infrastructure Unknown, so a flaky
/// network can never count as the agent failing.
/// </summary>
public class PulsEvalSetTests
{
    /// <summary>
    /// The set lists the cook, its one eval left in C#; the routing eval is a folder beside the identity.
    /// </summary>
    [Test]
    public void TheSetListsTheCook()
    {
        var set = new PulsEvalSet();

        Assert.That(set.Identity, Is.EqualTo(PulsEvalIdentity.Puls.Wire()));
        Assert.That(set.Evals.Select(e => e.Name), Is.EqualTo(new[] { PulsEvalName.Cook.Wire() }));
    }

    /// <summary>The set's own copy of the identity's name is the name the Puls identity goes by.</summary>
    [Test]
    public void TheSetsIdentity_IsTheNameThePulsIdentityGoesBy() =>
        // ADR 0043: the set keeps its own copy of the name (it never references an identity); this project's tests
        // see both and hold the copy to the original.
        Assert.That(new PulsEvalSet().Identity, Is.EqualTo(new PulsIdentity().Name));

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
        ICheck grader = new CookEval().Checks.Single(g => g.Name == name);
        // The failed turn has a whole, good answer: without the infrastructure check each check would pass it, so the
        // test tells a check that looked from one that was merely handed an empty turn (reviewed on 5.4: three
        // checks could be removed and the tests still passed).
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
