using System.Text.Json;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Puls.Evals.Cook;
using LouisAgent.Evals.Puls.Evals.Cook.Expectations;
using LouisAgent.Evals.Puls.Evals.Cook.Scoring;
using LouisAgent.Evals.Puls.Evals.Routing;
using LouisAgent.Evals.Puls.Shared.Expectations;

namespace LouisAgent.Evals.Puls.Tests;

// Puls's datasets (F18 step 3a.2): the Python checks' scenarios as files. Each input must equal the Python's, which
// Fixtures/python-scenarios.json holds as read from benchmarks/*/run_check.py with ast. Each dataset must load strictly with
// Puls's own graders; scenarios compare as sets, since the loader orders them by file name. Written first: the folders didn't exist yet, so every load failed.
public class PulsDatasetsTests
{
    private static IReadOnlyList<IGrader> GradersOf(string dataset) => dataset == "puls-cook" ? new CookEval().Graders : new RoutingEval().Graders;

    [TestCase("puls-cook")]
    [TestCase("puls-routing")]
    public void EachDatasetLoadsStrictly(string name)
    {
        Dataset dataset = Load(name);

        Assert.That(dataset.Manifest.Name, Is.EqualTo(name));
        Assert.That(dataset.Manifest.Version, Is.EqualTo(3));
    }

    [Test]
    public void TheRoutingScenariosAreTwelveQuestions() =>
        Assert.That(Load("puls-routing").Scenarios.Where(s => s.Split == DatasetSplit.Dev).Select(s => s.Id), Is.EquivalentTo(new[]
        {
            "web", "files", "git", "numbers", "chat", "cook", "web-2", "files-2", "git-2", "numbers-2", "chat-2", "cook-2",
        }));

    [Test]
    public void TheCheckAsksInMalmesburyAndInATownWithNoBranch()
    {
        // Moved here from CookSetup.Places: the place of each conversation is the dataset's.
        var places = Load("puls-cook").Scenarios.ToDictionary(s => s.Id, s => s.Input.Place);

        Assert.That(places["cook-malmesbury"], Is.EqualTo("Malmesbury, Wes-Kaap, Suid-Afrika"));
        Assert.That(places["cook-no-branch"], Is.EqualTo("Prince Albert, Wes-Kaap, Suid-Afrika"));
    }

    [Test]
    public void TheCookCategoriesAreTheDecidedOnes()
    {
        var expected = new Dictionary<string, string[]>
        {
            ["cook-af"] = ["language:af", "budget", "follow-up"],
            ["cook-en"] = ["language:en", "follow-up"],
            ["cook-shops"] = ["language:af", "shops:two"],
            ["cook-malmesbury"] = ["language:af", "budget"],
            ["cook-no-branch"] = ["language:af", "shop:no-branch"],
            ["cook-recipe-only"] = ["language:af", "shop:any"],
            ["cook-asks-first"] = ["language:af", "asks-first"],
            ["control-web"] = ["language:af", "control"],
        };

        Assert.That(Load("puls-cook").Scenarios.Where(s => s.Split == DatasetSplit.Dev).ToDictionary(s => s.Id, s => s.Categories.ToArray()), Is.EqualTo(expected));
    }

    [Test]
    public void TheCookScenariosStateTheirRequirements()
    {
        // F18 step 5.4: what each dev scenario asks for, in its `expected`; the control has only its route.
        var expected = new Dictionary<string, string>
        {
            ["control-web"] = """{"routes_to":null}""",
            ["cook-af"] = """{"routes_to":"puls-cook","town":"Stellenbosch","serves":[4,6],"budget":250,"shop":"pnp"}""",
            ["cook-en"] = """{"routes_to":"puls-cook","town":"Pretoria","serves":2,"shop":"pnp"}""",
            ["cook-malmesbury"] = """{"routes_to":"puls-cook","town":"Malmesbury","serves":4,"budget":150,"shop":"pnp"}""",
            ["cook-no-branch"] = """{"routes_to":"puls-cook","town":"Prince Albert","serves":2,"shop":"pnp"}""",
            ["cook-recipe-only"] = """{"routes_to":"puls-cook","town":"Stellenbosch","serves":4,"shop":"any"}""",
            ["cook-asks-first"] = """{"routes_to":null}""",
            ["cook-shops"] = """{"routes_to":"puls-cook","town":"Stellenbosch","serves":4,"shop":"pnp"}""",
        };

        var dev = Load("puls-cook").Scenarios.Where(s => s.Split == DatasetSplit.Dev);
        Assert.That(dev.ToDictionary(s => s.Id, s => Normal(s.Expected)), Is.EqualTo(expected.ToDictionary(e => e.Key, e => Normal(JsonDocument.Parse(e.Value).RootElement))));
    }

    [Test]
    public void TheHeldOutCookScenariosAreASecondTownAnEnglishBudgetAndAFollowUp()
    {
        // F18 step 5.4: new, never used while tuning a prompt; each states its requirements like the dev ones.
        var held = Load("puls-cook").Scenarios.Where(s => s.Split == DatasetSplit.HeldOut).ToDictionary(s => s.Id);

        Assert.That(held.Keys, Is.EquivalentTo(new[] { "cook-paarl", "cook-worcester-budget", "cook-follow-up" }));
        Assert.That(held["cook-paarl"].Input.Place, Does.StartWith("Paarl"));
        Assert.That(CookExpectations.BudgetOf(held["cook-worcester-budget"]), Is.EqualTo(180m));
        Assert.That(held["cook-worcester-budget"].Categories, Is.EqualTo(new[] { "language:en", "budget" }));
        Assert.That(held["cook-follow-up"].Input.Messages, Has.Count.EqualTo(2));
        Assert.That(CookExpectations.ServesOf(held["cook-follow-up"]), Is.EqualTo(new[] { 4, 6 }));
        Assert.That(held["cook-follow-up"].Categories, Is.EqualTo(new[] { "language:af", "follow-up" }));
        Assert.That(held.Values.Select(s => CookExpectations.TownOf(s)), Is.EqualTo(new[] { "Somerset-Wes", "Paarl", "Worcester" }).Or.EquivalentTo(new[] { "Somerset-Wes", "Paarl", "Worcester" }));
        Assert.That(held.Values.Select(s => CookExpectations.ShopOf(s)), Is.All.EqualTo(ShopExpectation.Pnp));
    }

    [Test]
    public void TheHeldOutRoutingQuestionsAreOneNewOneOfEachKind()
    {
        var held = Load("puls-routing").Scenarios.Where(s => s.Split == DatasetSplit.HeldOut).ToList();

        Assert.That(held.Select(s => s.Id), Is.EquivalentTo(new[] { "web-3", "files-3", "git-3", "numbers-3", "chat-3", "cook-3" }));
        Assert.That(held.Select(s => ExpectedRoute.Of(s)), Is.EquivalentTo(new string?[] { null, null, null, null, null, "puls-cook" }));
    }

    [Test]
    public void EveryQuestionForTheCook_NamesItsTownItsPeopleAndItsShop_ElsePulsWouldAskFirst()
    {
        // The prompts (Puls's routing, the cook's report): a meal that doesn't say for how many or where they'll shop is asked about first, never handed
        // on. So a question that should reach the cook states them, and the scenario's `expected` repeats them for the graders.
        foreach (string name in new[] { "puls-cook", "puls-routing" })
            foreach (EvalScenario scenario in Load(name).Scenarios.Where(s => ExpectedRoute.Of(s) is not null))
            {
                string first = scenario.Input.Messages[0];
                Assert.That(Servings.Of(first), Is.Not.Empty, $"{scenario.Id}: the people");
                Assert.That(first, Does.Contain("PnP").Or.Contain("maak nie saak").Or.Contain("doesn't matter"), $"{scenario.Id}: the shop");
                if (name == "puls-cook")
                {
                    var expected = CookExpectations.Of(scenario);
                    Assert.That(Servings.Of(first), Does.Contain(expected.Serves[0]), $"{scenario.Id}: the first turn's people");
                    Assert.That(first + scenario.Input.Place, Does.Contain(expected.Town), $"{scenario.Id}: the town");
                    if (expected.Budget is { } budget)
                        Assert.That(first, Does.Contain("R" + budget.ToString("0")), $"{scenario.Id}: the budget the grader checks is the one the question names");
                }
            }
    }

    [Test]
    public void AQuestionThatLeavesOutThePeopleAndTheShop_IsOneWherePulsAsksFirst()
    {
        var asks = Load("puls-cook").Scenarios.Single(s => s.Id == "cook-asks-first");

        Assert.That(ExpectedRoute.Of(asks), Is.Null, "Puls answers it itself, with its question");
        Assert.That(Servings.Of(asks.Input.Messages[0]), Is.Empty);
    }

    [Test]
    public void NoHeldOutQuestionIsAlsoADevQuestion()
    {
        // The held-out set is for a release check: a question tuned against would tell nothing.
        foreach (string name in new[] { "puls-cook", "puls-routing" })
        {
            var scenarios = Load(name).Scenarios;
            var dev = scenarios.Where(s => s.Split == DatasetSplit.Dev).SelectMany(s => s.Input.Messages).ToHashSet();
            Assert.That(scenarios.Where(s => s.Split == DatasetSplit.HeldOut).SelectMany(s => s.Input.Messages).Where(dev.Contains), Is.Empty, name);
        }
    }

    [Test]
    public void TheRoutingQuestionsExpectTheCookForAMealAndNobodyOtherwise()
    {
        foreach (EvalScenario scenario in Load("puls-routing").Scenarios)
            Assert.That(ExpectedRoute.Of(scenario), scenario.Id.StartsWith("cook") ? Is.EqualTo("puls-cook") : Is.Null, scenario.Id);
    }

    private static string Normal(JsonElement element) => JsonSerializer.Serialize(element);

    [Test]
    public void EachRoutingQuestionIsInItsKind()
    {
        foreach (EvalScenario scenario in Load("puls-routing").Scenarios)
            Assert.That(scenario.Categories, Is.EqualTo(new[] { "kind:" + scenario.Id.Split('-')[0] }), scenario.Id);
    }

    private static Dataset Load(string name) =>
        DatasetLoader.Load(EvalFiles.DatasetOf(name == "puls-cook" ? typeof(CookEval) : typeof(RoutingEval)), GradersOf(name));
}
