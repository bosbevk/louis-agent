using System.Text.Json;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Readers;
using LouisAgent.Packs.Recipes.Checks;

namespace LouisAgent.Packs.Recipes.Tests.Checks;

/// <summary>
/// The pack's checks can be named from a file: each is in the pack's list under its name, shows its settings in the
/// catalogue's listing, and builds the check through the reader the file names.
/// </summary>
public class RecipesChecksTests
{
    /// <summary>The readers a file here can name: one link reader.</summary>
    private static readonly ReaderCatalog Readers = new([FakeLinks.None]);

    /// <summary>The pack's list holds its recipe rule, a fixed rule.</summary>
    [Test]
    public void ThePacksList_HoldsItsRecipeRule()
    {
        Assert.That(RecipesChecks.All.Select(entry => entry.Type), Is.EqualTo(new[] { "recipe-from-source" }));
        Assert.That(RecipesChecks.All.Select(entry => entry.Kind), Has.All.EqualTo(CheckKind.FixedRule));
    }

    /// <summary>The catalogue lists the rule with its one setting, the reader.</summary>
    [Test]
    public void TheCatalogueListing_ShowsTheRuleWithItsSettings()
    {
        var listing = new CheckCatalog(RecipesChecks.All).Listing();

        Assert.That(listing.Select(line => (line.Type, line.Kind)),
            Is.EqualTo(new[] { ("recipe-from-source", CheckKind.FixedRule) }));
        Assert.That(listing.Single().Parameters.ToJsonString(), Does.Contain("reader"));
    }

    /// <summary>The type, from its settings, builds the check under its name, version and required.</summary>
    [Test]
    public void TheType_BuildsTodaysCheck()
    {
        var built = Build("""{"reader": "fake-links"}""");

        Assert.That(built, Is.TypeOf<RecipeFromSource>());
        Assert.That((built.Name, built.Version, built.Required), Is.EqualTo(("recipe-from-source", 1, true)));
    }

    /// <summary>
    /// A setting the type doesn't take, a blank reader or a reader the catalogue doesn't have is refused, naming what
    /// was wrong.
    /// </summary>
    [TestCase("""{"limit": 100}""", "limit")]
    [TestCase("""{"reader": " "}""", "reader")]
    [TestCase("""{"reader": "rand-total"}""", "rand-total")]
    public void ABadSetting_IsRefused(string settings, string named) =>
        Assert.That(Assert.Throws<InvalidDataException>(() => Build(settings))!.Message, Does.Contain(named));

    /// <summary>
    /// Builds the check the way an eval file will: from the pack's list, by its type, from its settings, with the
    /// readers to name from.
    /// </summary>
    private static ICheck Build(string settings) =>
        new CheckCatalog(RecipesChecks.All).Get("recipe-from-source")
            .Create(JsonDocument.Parse(settings).RootElement.Clone(), new CheckContext(string.Empty, [], Readers));
}
