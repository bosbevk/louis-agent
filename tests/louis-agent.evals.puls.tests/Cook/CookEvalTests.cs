using System.Text.Json;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Puls.Cook;
using LouisAgent.Evals.Trials;
using static LouisAgent.Evals.Puls.Tests.PulsTrials;

namespace LouisAgent.Evals.Puls.Tests.Cook;

// F18 step 5.3: the cook check as an eval of the harness. Written first: the eval threw NotImplementedException on the red run.
public class CookEvalTests
{
    private static readonly CookEval Eval = new();

    [Test]
    public void ItIsPulsCooksEval_UnderTheNameItsDatasetCarries()
    {
        Assert.That((Eval.Name, Eval.Identity, Eval.Version), Is.EqualTo(("puls-cook", "puls", 1)));
        Assert.That(Eval.Proves, Is.Not.Empty);
    }

    [Test]
    public async Task ItsModesArePulsWithTheCooksToolsAndPulsWithItsCook_BothOnPulsVoice()
    {
        var modes = await Eval.ModesAsync(new CannedSessionClient(), CancellationToken.None);

        Assert.That(modes.Select(m => (m.Name, m.Role, string.Join(",", m.Toolsets))), Is.EqualTo(new[]
        {
            ("puls-tools", Role, "web,shops"),
            ("puls-cook", Role, "specialists,web"),
        }));
    }

    [Test]
    public void ItsScenariosAreTheCookDatasets()
    {
        Assert.That(Eval.Scenarios.Select(s => s.Id), Is.EquivalentTo(
            new[] { "cook-af", "cook-en", "cook-shops", "cook-malmesbury", "cook-no-shop", "control-web" }));
        Assert.That(Eval.Scenarios.Single(s => s.Id == "cook-malmesbury").Input.Place, Is.EqualTo("Malmesbury, Wes-Kaap, Suid-Afrika"));
        Assert.That(Eval.Scenarios.Single(s => s.Id == "cook-no-shop").Input.Place, Is.EqualTo("Prince Albert, Wes-Kaap, Suid-Afrika"));
    }

    [Test]
    public void ItsGradersAreTheTwoRequiredFixedRules() =>
        Assert.That(Eval.Graders.Select(g => (g.Name, g.Required, g.Kind)),
            Is.EqualTo(new[] { ("cook-no-error", true, GraderKind.FixedRule), ("cook-routed-right", true, GraderKind.FixedRule) }));

    [Test]
    public void ItsScoresAreTheCookScorers_KeyForKey()
    {
        var trial = Trial(CookMode, "cook-af", Turn(1, [Start("puls-cook"), Fetch("https://www.checkers.co.za/x"), Text("Resep.")]));

        var scores = Eval.ScoresOf(trial, []);

        Assert.That(Json(scores), Is.EqualTo(Json(CookScorer.ScoreTrial(trial))));
        Assert.That(Json(scores), Does.Contain("\"routed_right\":true"));
    }

    [Test]
    public void ATurnsFacts_AreWhatTheTurnViewReads()
    {
        var turn = Turn(1, [Start("puls-cook"), Fetch("https://www.checkers.co.za/x"), Error()]);

        var facts = Eval.TurnFactsOf(turn, CookMode);

        Assert.That(facts.Role, Is.EqualTo("puls-cook"));
        Assert.That((facts.AgentCalls, facts.SpecialistCalls), Is.EqualTo((2, 0)));
        Assert.That(facts.Hosts, Is.EqualTo(new[] { "checkers.co.za" }));
        Assert.That(facts.Error, Is.EqualTo("overloaded"));
    }

    private static string Json(object scores) => JsonSerializer.Serialize(scores, scores.GetType(), EvalJson.SnakeCase);
}
