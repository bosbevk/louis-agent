using LouisAgent.Evals.Puls.Shared;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Choices;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Puls.Messages;
using LouisAgent.Evals.Puls.Evals.Cook;
using static LouisAgent.Evals.Puls.Tests.PulsTrials;

namespace LouisAgent.Evals.Puls.Tests.Evals.Cook;

/// <summary>
/// The cook check as an eval of the harness: its name, its two modes, its scenarios, its graders and what it says of
/// a turn. Written first, in F18 step 5.3, when the eval still threw.
/// </summary>
public class CookEvalTests
{
    /// <summary>The eval under test.</summary>
    private static readonly CookEval Eval = new();

    /// <summary>It runs under its dataset's name, and its portal title is a message.</summary>
    [Test]
    public void ItIsPulsCooksEval_UnderTheNameItsDatasetCarries()
    {
        Assert.That((Eval.Name, Eval.Version), Is.EqualTo((PulsEvalName.Cook.Wire(), 10)));
        Assert.That(Eval.Proves, Is.EqualTo(MessageText.Format(PulsEvalMessage.CookProves)),
            "the portal's title is a message enum member (ADR 0009)");
    }

    /// <summary>One mode gives Puls the cook's tools itself, the other lets it hand off to the cook.</summary>
    [Test]
    public async Task ItsModesArePulsWithTheCooksToolsAndPulsWithItsCook_BothOnPulsVoice()
    {
        var modes = await Eval.ModesAsync(new CannedSessionClient(), CancellationToken.None);

        Assert.That(modes.Select(m => (m.Name, m.Role, string.Join(",", m.Toolsets))),
            Is.EqualTo(new[]
            {
                (CookMode.Tools.Wire(), Role, "web,shops"), (CookMode.Cook.Wire(), Role, "specialists,web"),
            }));
    }

    /// <summary>Its scenarios are the cook dataset's, dev and held out, with their places.</summary>
    [Test]
    public void ItsScenariosAreTheCookDatasets()
    {
        Assert.That(Eval.Scenarios.Where(s => s.Split == DatasetSplit.Dev).Select(s => s.Id), Is.EquivalentTo(
            new[]
            {
                PulsCookScenarioIds.CookAf, PulsCookScenarioIds.CookEn, "cook-shops", "cook-malmesbury",
                "cook-no-branch", "cook-recipe-only", "cook-asks-first", "control-web"
            }));
        Assert.That(Eval.Scenarios.Where(s => s.Split == DatasetSplit.HeldOut).Select(s => s.Id), Is.EquivalentTo(
            new[] { "cook-paarl", "cook-worcester-budget", "cook-follow-up" }));
        Assert.That(Eval.Scenarios.Single(s => s.Id == "cook-malmesbury").Input.Place,
            Is.EqualTo("Malmesbury, Wes-Kaap, Suid-Afrika"));
        Assert.That(Eval.Scenarios.Single(s => s.Id == "cook-no-branch").Input.Place,
            Is.EqualTo("Prince Albert, Wes-Kaap, Suid-Afrika"));
    }

    /// <summary>
    /// The fixed rules that must hold come first, the untooled shops among them; the serving rule, the meal judge and
    /// its human review are measured but never fail a run.
    /// </summary>
    [Test]
    public void ItsGraders_AreTheRequiredRulesFirst_ThenTheOnesThatAreNot() =>
        Assert.That(Eval.Checks.Select(g => (g.Name, g.Required, g.Kind)),
            Is.EqualTo(new[]
            {
                ("cook-no-error", true, CheckKind.FixedRule), ("routed-to", true, CheckKind.FixedRule),
                ("within-budget", true, CheckKind.FixedRule), ("estimates-at-shop", true, CheckKind.FixedRule),
                ("untooled-shops", true, CheckKind.FixedRule), ("recipe-image", true, CheckKind.FixedRule),
                ("serves-as-asked", false, CheckKind.FixedRule), ("meal-for-people", false, CheckKind.Model),
                ("meal-for-people.human", false, CheckKind.Human),
            }));

    /// <summary>What it says of a turn is what Puls's turn view reads: who answered, the calls, the sites.</summary>
    [Test]
    public void ATurnsFacts_AreWhatTheTurnViewReads()
    {
        var turn = Turn(1, [Start(PulsEvalRole.Cook.Wire()), Fetch("https://www.checkers.co.za/x"), Error()]);

        var facts = Eval.TurnFactsOf(turn, CookEvalMode);

        Assert.That(facts.Role, Is.EqualTo(PulsEvalRole.Cook.Wire()));
        Assert.That((facts.AgentCalls, facts.SpecialistCalls), Is.EqualTo((2, 0)));
        Assert.That(facts.Hosts, Is.EqualTo(new[] { "checkers.co.za" }));
        Assert.That(facts.Error, Is.EqualTo("overloaded"));
    }
}
