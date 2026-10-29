using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Puls.Evals.Cook;
using LouisAgent.Evals.Puls.Evals.Cook.Expectations;
using LouisAgent.Evals.Puls.Evals.Cook.Scoring;
using LouisAgent.Evals.Puls.Evals.Routing;
using LouisAgent.Evals.Puls.Shared;
using LouisAgent.Evals.Puls.Shared.Expectations;

namespace LouisAgent.Evals.Puls.Tests;

/// <summary>
/// Puls's datasets (F18 step 3a.2; Inspect's Sample shape since step 7d): each loads strictly with Puls's own graders,
/// and each scenario asks what the eval needs. Scenarios compare as sets, since the loader orders them by file name.
/// </summary>
public class PulsDatasetsTests
{
    /// <summary>The graders of the eval that owns this dataset.</summary>
    internal static IReadOnlyList<IGrader> GradersOf(PulsEvalName dataset) =>
        dataset == PulsEvalName.Cook ? new CookEval().Graders : new RoutingEval().Graders;

    /// <summary>Each dataset loads strictly, under its name and at its version.</summary>
    [TestCase(PulsEvalName.Cook, 7)]
    [TestCase(PulsEvalName.Routing, 4)]
    public void EachDatasetLoadsStrictly(PulsEvalName name, int version)
    {
        Dataset dataset = Load(name);

        Assert.That(dataset.Manifest.Name, Is.EqualTo(name.Wire()));
        Assert.That(dataset.Manifest.Version, Is.EqualTo(version));
    }

    /// <summary>The routing's dev questions are two of each kind.</summary>
    [Test]
    public void TheRoutingScenariosAreTwelveQuestions() =>
        Assert.That(Load(PulsEvalName.Routing).Scenarios.Where(s => s.Split == DatasetSplit.Dev).Select(s => s.Id),
            Is.EquivalentTo(new[]
            {
                PulsTaskIds.Web, PulsTaskIds.Files, PulsTaskIds.Git, PulsTaskIds.Numbers, PulsTaskIds.Chat,
                PulsTaskIds.Cook, "web-2", "files-2", "git-2", "numbers-2", "chat-2", "cook-2",
            }));

    /// <summary>
    /// The place of each conversation is the dataset's: Malmesbury, and a town where PnP has no branch.
    /// </summary>
    [Test]
    public void TheCheckAsksInMalmesburyAndInATownWithNoBranch()
    {
        var places = Load(PulsEvalName.Cook).Scenarios.ToDictionary(s => s.Id, s => s.Input.Place);

        Assert.That(places["cook-malmesbury"], Is.EqualTo("Malmesbury, Wes-Kaap, Suid-Afrika"));
        Assert.That(places["cook-no-branch"], Is.EqualTo("Prince Albert, Wes-Kaap, Suid-Afrika"));
    }

    /// <summary>The cook's dev scenarios carry the categories decided in step 5c.</summary>
    [Test]
    public void TheCookCategoriesAreTheDecidedOnes()
    {
        var expected = new Dictionary<string, string[]>
        {
            [PulsCookScenarioIds.CookAf] = ["language:af", "budget", "follow-up"],
            [PulsCookScenarioIds.CookEn] = ["language:en", "follow-up"],
            ["cook-shops"] = ["language:af", "shops:two"],
            ["cook-malmesbury"] = ["language:af", "budget"],
            ["cook-no-branch"] = ["language:af", "shop:no-branch"],
            ["cook-recipe-only"] = ["language:af", "shop:any"],
            ["cook-asks-first"] = ["language:af", "asks-first"],
            ["control-web"] = ["language:af", "control"],
        };

        Assert.That(
            Load(PulsEvalName.Cook).Scenarios.Where(s => s.Split == DatasetSplit.Dev)
                .ToDictionary(s => s.Id, s => s.Categories.ToArray()), Is.EqualTo(expected));
    }

    /// <summary>What each dev scenario asks for, in its criteria (step 5.4); the control has only its route.</summary>
    [Test]
    public void TheCookScenariosStateTheirRequirements()
    {
        var expected = new Dictionary<string, string>
        {
            ["control-web"] = """{"routes_to":null}""",
            [PulsCookScenarioIds.CookAf] = """
                {"routes_to":"puls-cook","town":"Stellenbosch","serves":[4,6],"budget":250,"shop":"pnp",
                 "recipe_image":true}
                """,
            [PulsCookScenarioIds.CookEn] =
                """{"routes_to":"puls-cook","town":"Pretoria","serves":2,"shop":"pnp","recipe_image":true}""",
            ["cook-malmesbury"] = """
                {"routes_to":"puls-cook","town":"Malmesbury","serves":4,"budget":150,"shop":"pnp",
                 "recipe_image":true}
                """,
            ["cook-no-branch"] = """
                {"routes_to":"puls-cook","town":"Prince Albert","serves":2,"shop":"no-branch",
                 "recipe_image":true}
                """,
            ["cook-recipe-only"] =
                """{"routes_to":"puls-cook","town":"Stellenbosch","serves":4,"shop":"any","recipe_image":true}""",
            ["cook-asks-first"] = """{"routes_to":null}""",
            ["cook-shops"] =
                """{"routes_to":"puls-cook","town":"Stellenbosch","serves":4,"shop":"pnp","recipe_image":true}""",
        };

        var dev = Load(PulsEvalName.Cook).Scenarios.Where(s => s.Split == DatasetSplit.Dev);
        Assert.That(dev.ToDictionary(s => s.Id, s => Normal(s.Expected)),
            Is.EqualTo(expected.ToDictionary(e => e.Key, e => Normal(JsonDocument.Parse(e.Value).RootElement))));
    }

    /// <summary>
    /// The held-out cook scenarios are new, never used while tuning a prompt (step 5.4), and state their requirements
    /// like the dev ones.
    /// </summary>
    [Test]
    public void TheHeldOutCookScenariosAreASecondTownAnEnglishBudgetAndAFollowUp()
    {
        var held = Load(PulsEvalName.Cook).Scenarios.Where(s => s.Split == DatasetSplit.HeldOut)
            .ToDictionary(s => s.Id);

        Assert.That(held.Keys, Is.EquivalentTo(new[] { "cook-paarl", "cook-worcester-budget", "cook-follow-up" }));
        Assert.That(held["cook-paarl"].Input.Place, Does.StartWith("Paarl"));
        Assert.That(CookExpectations.BudgetOf(held["cook-worcester-budget"]), Is.EqualTo(180m));
        Assert.That(held["cook-worcester-budget"].Categories, Is.EqualTo(new[] { "language:en", "budget" }));
        Assert.That(held["cook-follow-up"].Input.Messages, Has.Count.EqualTo(2));
        Assert.That(CookExpectations.ServesOf(held["cook-follow-up"]), Is.EqualTo(new[] { 4, 6 }));
        Assert.That(held["cook-follow-up"].Categories, Is.EqualTo(new[] { "language:af", "follow-up" }));
        Assert.That(held.Values.Select(s => CookExpectations.TownOf(s)),
            Is.EqualTo(new[] { "Somerset-Wes", "Paarl", "Worcester" }).Or
                .EquivalentTo(new[] { "Somerset-Wes", "Paarl", "Worcester" }));
        Assert.That(held.Values.Select(s => CookExpectations.ShopOf(s)), Is.All.EqualTo(ShopExpectation.Pnp));
    }

    /// <summary>The routing's held-out questions are one new question of each kind.</summary>
    [Test]
    public void TheHeldOutRoutingQuestionsAreOneNewOneOfEachKind()
    {
        var held = Load(PulsEvalName.Routing).Scenarios.Where(s => s.Split == DatasetSplit.HeldOut).ToList();

        Assert.That(held.Select(s => s.Id),
            Is.EquivalentTo(new[] { "web-3", "files-3", "git-3", "numbers-3", "chat-3", "cook-3" }));
        Assert.That(held.Select(s => ExpectedRoute.Of(s)),
            Is.EquivalentTo(new string?[] { null, null, null, null, null, PulsEvalRole.Cook.Wire() }));
    }

    /// <summary>
    /// The prompts ask first about a meal that doesn't say for how many or where they'll shop, and never hand it on;
    /// so a question that should reach the cook states them, and its criteria repeat them for the graders.
    /// </summary>
    [Test]
    public void EveryQuestionForTheCook_NamesItsTownItsPeopleAndItsShop_ElsePulsWouldAskFirst()
    {
        foreach (var name in Enum.GetValues<PulsEvalName>())
            foreach (EvalScenario scenario in Load(name).Scenarios.Where(s => ExpectedRoute.Of(s) is not null))
            {
                string first = scenario.Input.Messages[0];
                Assert.That(Servings.Of(first), Is.Not.Empty, $"{scenario.Id}: the people");
                Assert.That(first, Does.Contain("PnP").Or.Contain("maak nie saak").Or.Contain("doesn't matter"),
                    $"{scenario.Id}: the shop");
                if (name == PulsEvalName.Cook) AssertTheCriteriaMatchTheQuestion(scenario, first);
            }
    }

    /// <summary>A cook scenario's criteria name the people, the town and the budget its first question names.</summary>
    private static void AssertTheCriteriaMatchTheQuestion(EvalScenario scenario, string first)
    {
        var expected = CookExpectations.Of(scenario);
        Assert.That(Servings.Of(first), Does.Contain(expected.Serves[0]), $"{scenario.Id}: the first turn's people");
        Assert.That(first + scenario.Input.Place, Does.Contain(expected.Town), $"{scenario.Id}: the town");
        if (expected.Budget is { } budget)
            Assert.That(first, Does.Contain("R" + budget.ToString("0")),
                $"{scenario.Id}: the budget the grader checks is the one the question names");
    }

    /// <summary>
    /// A question that leaves out the people and the shop is one Puls answers itself, by asking first.
    /// </summary>
    [Test]
    public void AQuestionThatLeavesOutThePeopleAndTheShop_IsOneWherePulsAsksFirst()
    {
        var asks = Load(PulsEvalName.Cook).Scenarios.Single(s => s.Id == "cook-asks-first");

        Assert.That(ExpectedRoute.Of(asks), Is.Null, "Puls answers it itself, with its question");
        Assert.That(Servings.Of(asks.Input.Messages[0]), Is.Empty);
    }

    /// <summary>The held-out set is for a release check: a question tuned against would tell nothing.</summary>
    [Test]
    public void NoHeldOutQuestionIsAlsoADevQuestion()
    {
        foreach (var name in Enum.GetValues<PulsEvalName>())
        {
            var scenarios = Load(name).Scenarios;
            var dev = scenarios.Where(s => s.Split == DatasetSplit.Dev).SelectMany(s => s.Input.Messages).ToHashSet();
            Assert.That(
                scenarios.Where(s => s.Split == DatasetSplit.HeldOut).SelectMany(s => s.Input.Messages)
                    .Where(dev.Contains), Is.Empty, name.Wire());
        }
    }

    /// <summary>A routing question about a meal expects the cook, and every other question nobody.</summary>
    [Test]
    public void TheRoutingQuestionsExpectTheCookForAMealAndNobodyOtherwise()
    {
        foreach (EvalScenario scenario in Load(PulsEvalName.Routing).Scenarios)
            Assert.That(ExpectedRoute.Of(scenario),
                scenario.Id.StartsWith("cook") ? Is.EqualTo(PulsEvalRole.Cook.Wire()) : Is.Null, scenario.Id);
    }

    /// <summary>Each routing question carries its kind as its one category.</summary>
    [Test]
    public void EachRoutingQuestionIsInItsKind()
    {
        foreach (EvalScenario scenario in Load(PulsEvalName.Routing).Scenarios)
            Assert.That(scenario.Categories, Is.EqualTo(new[] { "kind:" + scenario.Id.Split('-')[0] }), scenario.Id);
    }

    /// <summary>A JSON value as compact text, so two criteria compare whatever their spacing.</summary>
    private static string Normal(JsonElement element) => JsonSerializer.Serialize(element);

    /// <summary>The dataset of one of Puls's evals, loaded with that eval's graders.</summary>
    internal static Dataset Load(PulsEvalName name) =>
        DatasetLoader.Load(EvalFiles.DatasetOf(name == PulsEvalName.Cook ? typeof(CookEval) : typeof(RoutingEval)),
            GradersOf(name));
}
