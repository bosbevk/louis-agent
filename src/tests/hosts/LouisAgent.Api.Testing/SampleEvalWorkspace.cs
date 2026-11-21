using System.Text.Json;
using LouisAgent.Api.Evals.Drafts.Samples;

namespace LouisAgent.Api.Testing;

/// <summary>
/// A workspace of the tests' own with one eval's scenarios in it: a dev scenario with a follow-up, expected values and
/// categories, a held-out one, and the name of one it doesn't have. The scenario and draft tests read it in place of a
/// product's eval, so the eval studio is proved on the framework alone (F166.1).
/// </summary>
/// <remarks>
/// Fresh Fixture (Meszaros, xUnit Test Patterns), Louis's pick 1 of three: each test writes the folder for itself, so
/// the values it checks sit beside it here instead of in a product's eval or in the framework's template, which stays
/// the smallest eval a person copies. The files go through <see cref="SampleBody"/> and its file options, the shape
/// the API reads them in.
/// </remarks>
/// <seealso href="http://xunitpatterns.com/Fresh%20Fixture.html">Fresh Fixture (Meszaros)</seealso>
public sealed class SampleEvalWorkspace
{
    /// <summary>The eval's folder in the workspace, under one of the roots the API reads evals from.</summary>
    public const string Folder = "src/evals/sample";

    /// <summary>The dev scenario.</summary>
    public const string Dinner = "dinner";

    /// <summary>The held-out scenario, found after the dev ones.</summary>
    public const string HeldOutDinner = "dinner-held-out";

    /// <summary>A scenario the folder doesn't have.</summary>
    public const string Missing = "dinner-missing";

    /// <summary>What the dev scenario asks.</summary>
    public const string Question = "What can I cook tonight for four people for under 250? Give a shopping list.";

    /// <summary>The dev scenario's one follow-up.</summary>
    public const string FollowUp = "Make it for six people instead. What does it cost now?";

    /// <summary>Where the dev scenario's user is.</summary>
    public const string Place = "Springfield";

    /// <summary>Where the held-out scenario's user is.</summary>
    public const string HeldOutPlace = "Shelbyville";

    /// <summary>The dev scenario's expected budget.</summary>
    public const int Budget = 250;

    /// <summary>The dev scenario's expected shop.</summary>
    public const string Shop = "corner-shop";

    /// <summary>The dev scenario's categories.</summary>
    public static IReadOnlyList<string> Categories { get; } = ["budget", "follow-up"];

    /// <summary>The workspace's root folder, a new one for each workspace.</summary>
    public string Root { get; }

    private SampleEvalWorkspace(string root) => Root = root;

    /// <summary>A new workspace, its eval's scenarios written in it.</summary>
    public static SampleEvalWorkspace Create()
    {
        var workspace = new SampleEvalWorkspace(Directory.CreateTempSubdirectory("sample-eval-").FullName);

        var dinnerExpected = new SampleExpected([4, 6], Budget, Shop);
        workspace.Write("dev", Scenario(Dinner, Question, Place, [FollowUp], dinnerExpected, Categories));
        var heldOutExpected = new SampleExpected([2], Budget, Shop);
        workspace.Write("held-out",
            Scenario(HeldOutDinner, "Something for two tonight?", HeldOutPlace, [], heldOutExpected, ["budget"]));

        return workspace;
    }

    /// <summary>A scenario as its file holds it.</summary>
    private static SampleBody Scenario(string id, string question, string place, IReadOnlyList<string> followUps,
        SampleExpected expected, IReadOnlyList<string> categories)
    {
        var values = JsonSerializer.SerializeToElement(expected, SampleBody.FileJson);
        var metadata = new SampleBodyMetadata(place, "Etc/UTC", followUps, values, categories);
        return new SampleBody(id, [new SampleBodyMessage("user", question)], metadata);
    }

    /// <summary>Writes a scenario into its split's folder.</summary>
    private void Write(string split, SampleBody scenario)
    {
        string folder = Path.Combine(Root, Folder, "Dataset", "scenarios", split);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, scenario.Id + ".json"),
            JsonSerializer.Serialize(scenario, SampleBody.FileJson));
    }

    /// <summary>The values a scenario expects: how many it serves, its budget and its shop.</summary>
    private sealed record SampleExpected(IReadOnlyList<int> Serves, int Budget, string Shop);
}
