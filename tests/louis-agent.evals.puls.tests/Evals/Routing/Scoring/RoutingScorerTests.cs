using LouisAgent.Choices;
using LouisAgent.Evals.Puls.Evals.Routing;
using LouisAgent.Evals.Puls.Evals.Routing.Scoring;
using LouisAgent.Evals.Puls.Shared;

namespace LouisAgent.Evals.Puls.Tests.Evals.Routing.Scoring;

/// <summary>
/// Where each kind of question should go, and the two setups the routing check compares: Puls with everything but
/// the hand-off, and Puls as it's built. Only cooking has a role that's on, so every other question belongs to Puls.
/// </summary>
public class RoutingScorerTests
{
    /// <summary>A meal goes to the cook; any other question has nobody to go to.</summary>
    [Test]
    public void TheExpectedRole_IsTheScenariosRoute_TheCookForAMealAndNobodyOtherwise()
    {
        Assert.That(Dev.Route("cook-2"), Is.EqualTo(PulsEvalRole.Cook.Wire()));
        foreach (string task in new[] { PulsTaskIds.Web, "files-2", PulsTaskIds.Git, PulsTaskIds.Numbers, "chat-2" })
            Assert.That(Dev.Route(task), Is.Null, task);
    }

    /// <summary>The baseline keeps every toolset the instance has, except the ones that hand a question on.</summary>
    [Test]
    public void TheBaseline_IsEveryAvailableToolsetButTheDelegatingOnes() =>
        Assert.That(
            RoutingScorer.Baseline([
                PulsToolset.Files.Wire(), PulsToolset.Git.Wire(), PulsToolset.Web.Wire(),
                PulsToolset.Specialists.Wire(), PulsToolset.Minions.Wire(), PulsToolset.Skills.Wire()
            ]),
            Is.EqualTo(new[]
            {
                PulsToolset.Files.Wire(), PulsToolset.Git.Wire(), PulsToolset.Web.Wire(), PulsToolset.Skills.Wire()
            }));

    /// <summary>The delegated mode is Puls with its specialists and the web, as it's built.</summary>
    [Test]
    public void TheDelegatedMode_IsPulsAsBuilt() =>
        Assert.That(RoutingSetup.Delegated,
            Is.EqualTo(new[] { PulsToolset.Specialists.Wire(), PulsToolset.Web.Wire() }));
}
