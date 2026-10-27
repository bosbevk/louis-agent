using LouisAgent.Evals.Puls.Shared;
using LouisAgent.Choices;
using System.Text.Json;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Puls.Messages;
using LouisAgent.Evals.Trials;
using LouisAgent.Evals.Puls.Evals.Cook;
using LouisAgent.Evals.Puls.Evals.Cook.Scoring;
using static LouisAgent.Evals.Puls.Tests.PulsTrials;

namespace LouisAgent.Evals.Puls.Tests.Evals.Cook;

// F18 step 5.3: the cook check as an eval of the harness. Written first: the eval threw NotImplementedException on the red run.
public class CookEvalTests
{
    private static readonly CookEval Eval = new();

    [Test]
    public void ItIsPulsCooksEval_UnderTheNameItsDatasetCarries()
    {
        Assert.That((Eval.Name, Eval.Version), Is.EqualTo((PulsEvalName.Cook.Wire(), 9)));
        Assert.That(Eval.Proves, Is.EqualTo(MessageText.Format(PulsEvalMessage.CookProves)), "the portal's title is a message enum member (ADR 0009)");
    }

    [Test]
    public async Task ItsModesArePulsWithTheCooksToolsAndPulsWithItsCook_BothOnPulsVoice()
    {
        var modes = await Eval.ModesAsync(new CannedSessionClient(), CancellationToken.None);

        Assert.That(modes.Select(m => (m.Name, m.Role, string.Join(",", m.Toolsets))), Is.EqualTo(new[]
        {
            (CookMode.Tools.Wire(), Role, "web,shops"),
            (CookMode.Cook.Wire(), Role, "specialists,web"),
        }));
    }

    [Test]
    public void ItsScenariosAreTheCookDatasets()
    {
        Assert.That(Eval.Scenarios.Where(s => s.Split == DatasetSplit.Dev).Select(s => s.Id), Is.EquivalentTo(
            new[] { "cook-af", "cook-en", "cook-shops", "cook-malmesbury", "cook-no-branch", "cook-recipe-only", "cook-asks-first", "control-web" }));
        Assert.That(Eval.Scenarios.Where(s => s.Split == DatasetSplit.HeldOut).Select(s => s.Id), Is.EquivalentTo(
            new[] { "cook-paarl", "cook-worcester-budget", "cook-follow-up" }));
        Assert.That(Eval.Scenarios.Single(s => s.Id == "cook-malmesbury").Input.Place, Is.EqualTo("Malmesbury, Wes-Kaap, Suid-Afrika"));
        Assert.That(Eval.Scenarios.Single(s => s.Id == "cook-no-branch").Input.Place, Is.EqualTo("Prince Albert, Wes-Kaap, Suid-Afrika"));
    }

    [Test]
    public void ItsGradersAreTheRouteTheErrorsTheBudgetTheEstimatesAndTheRecipeImage_AndTheServesRuleAndTheMealJudgeAndItsHumanReviewThatAreNotRequired() =>
        Assert.That(Eval.Graders.Select(g => (g.Name, g.Required, g.Kind)), Is.EqualTo(new[]
        {
            ("cook-no-error", true, GraderKind.FixedRule),
            ("routed-to", true, GraderKind.FixedRule),
            ("within-budget", true, GraderKind.FixedRule),
            ("estimates-at-shop", true, GraderKind.FixedRule),
            ("recipe-image", true, GraderKind.FixedRule),
            ("serves-as-asked", false, GraderKind.FixedRule),
            ("meal-for-people", false, GraderKind.Model),
            ("meal-for-people.human", false, GraderKind.Human),
        }));

    [Test]
    public void ItsScoresAreTheCookScorers_KeyForKey()
    {
        var trial = Trial(CookEvalMode, "cook-af", """{"routes_to":"puls-cook","serves":4}""", null, Turn(1, [Start(PulsEvalRole.Cook.Wire()), Fetch("https://www.checkers.co.za/x"), Text("Resep.")]));

        var scores = Eval.ScoresOf(trial, []);

        Assert.That(Json(scores), Is.EqualTo(Json(CookScorer.ScoreTrial(trial))));
        Assert.That(Json(scores), Does.Contain("\"routed_right\":true"));
    }

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

    private static string Json(object scores) => JsonSerializer.Serialize(scores, scores.GetType(), EvalJson.SnakeCase);
}
