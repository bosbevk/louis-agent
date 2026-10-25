using System.Text.Json;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Puls.Grading.FixedRules;
using LouisAgent.Evals.Puls.Routing;

namespace LouisAgent.Evals.Puls.Tests.Datasets;

// Puls's datasets (F18 step 3a.2): the Python checks' scenarios as files. Each input must equal the Python's, which
// Fixtures/python-scenarios.json holds as read from benchmarks/*/run_check.py with ast. Each dataset must load strictly with
// Puls's own graders; scenarios compare as sets, since the loader orders them by file name. Written first: the folders didn't exist yet, so every load failed.
public class PulsDatasetsTests
{
    private static readonly IReadOnlyList<IGrader> Graders = [new CookNoError(), new CookRoutedRight(), new RoutingRoutedRight()];

    [TestCase("puls-cook")]
    [TestCase("puls-routing")]
    public void EachDatasetLoadsStrictly(string name)
    {
        Dataset dataset = Load(name);

        Assert.That(dataset.Manifest.Name, Is.EqualTo(name));
        Assert.That(dataset.Manifest.Version, Is.EqualTo(1));
    }

    [Test]
    public void TheCookInputsAreThePythonsConversations() =>
        Assert.That(Inputs(Load("puls-cook")), Is.EquivalentTo(PythonInputs("puls-cook")));

    [Test]
    public void TheRoutingInputsAreThePythonsQuestions() =>
        Assert.That(Inputs(Load("puls-routing")), Is.EquivalentTo(PythonInputs("puls-routing")));

    [Test]
    public void TheRoutingScenariosAreTwelveQuestions_EachOfAKindTheCheckKnows()
    {
        var scenarios = Load("puls-routing").Scenarios;

        Assert.That(scenarios, Has.Count.EqualTo(12));
        foreach (EvalScenario scenario in scenarios)
            Assert.That(RoutingSetup.Kinds.ContainsKey(scenario.Id.Split('-')[0]), Is.True, scenario.Id);
    }

    [Test]
    public void TheCheckAsksInMalmesburyAndInATownWithoutACheckers()
    {
        // Moved here from CookSetup.Places: the place of each conversation is the dataset's.
        var places = Load("puls-cook").Scenarios.ToDictionary(s => s.Id, s => s.Input.Place);

        Assert.That(places["cook-malmesbury"], Is.EqualTo("Malmesbury, Wes-Kaap, Suid-Afrika"));
        Assert.That(places["cook-no-shop"], Is.EqualTo("Prince Albert, Wes-Kaap, Suid-Afrika"));
    }

    [Test]
    public void TheCookCategoriesAreTheDecidedOnes()
    {
        var expected = new Dictionary<string, string[]>
        {
            ["cook-af"] = ["language:af", "budget", "follow-up"],
            ["cook-en"] = ["language:en", "follow-up"],
            ["cook-shops"] = ["language:af"],
            ["cook-malmesbury"] = ["language:af", "budget"],
            ["cook-no-shop"] = ["language:af", "shop:none"],
            ["control-web"] = ["language:af", "control"],
        };

        Assert.That(Load("puls-cook").Scenarios.ToDictionary(s => s.Id, s => s.Categories.ToArray()), Is.EqualTo(expected));
    }

    [Test]
    public void EachRoutingQuestionIsInItsKind()
    {
        foreach (EvalScenario scenario in Load("puls-routing").Scenarios)
            Assert.That(scenario.Categories, Is.EqualTo(new[] { "kind:" + scenario.Id.Split('-')[0] }), scenario.Id);
    }

    private static Dataset Load(string name) => DatasetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Datasets", name), Graders);

    private static List<string> Inputs(Dataset dataset) =>
        dataset.Scenarios.Select(s => Row(s.Id, s.Input.Place, s.Input.TimeZone, s.Input.Messages)).ToList();

    private static List<string> PythonInputs(string dataset) =>
        Fixture().Where(row => row.GetProperty("dataset").GetString() == dataset)
            .Select(row => Row(row.GetProperty("id").GetString()!, row.GetProperty("place").GetString(), row.GetProperty("time_zone").GetString()!,
                row.GetProperty("messages").EnumerateArray().Select(m => m.GetString()!).ToArray()))
            .ToList();

    /// <summary>One input as JSON text, so the loaded and the Python's compare by the same bytes.</summary>
    private static string Row(string id, string? place, string timeZone, IEnumerable<string> messages) =>
        JsonSerializer.Serialize(new { id, place, time_zone = timeZone, messages = messages.ToArray() });

    private static List<JsonElement> Fixture() =>
        JsonSerializer.Deserialize<List<JsonElement>>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "python-scenarios.json")))!;
}
