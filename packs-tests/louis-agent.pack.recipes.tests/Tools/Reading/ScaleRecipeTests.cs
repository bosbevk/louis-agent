using LouisAgent.Packs.Recipes.Library;
using LouisAgent.Packs.Recipes.Recipes;
using LouisAgent.Packs.Recipes.Sources;
using LouisAgent.Packs.Recipes.Sources.Sites;
using LouisAgent.Packs.Recipes.Tests.Library;
using LouisAgent.Packs.Recipes.Tests.Providers;
using LouisAgent.Packs.Recipes.Tools;
using Microsoft.Extensions.Time.Testing;

namespace LouisAgent.Packs.Recipes.Tests.Tools.Reading;

/// <summary>
/// The cook asks for a recipe scaled and gets every line worked out, from the recipe we hold, by the link it read. A
/// page we don't hold says so, and the cook does the sums as it did.
/// </summary>
public class ScaleRecipeTests
{
    private const string Page = "https://taste.co.za/recipes/pie/";

    private static readonly RecipeSite Taste = RecipeSites.Find("taste")!;

    /// <summary>The tools over a library holding this recipe, and the link a search would give for it.</summary>
    private static (RecipeTools Tools, string OurLink) Holding(string? yield)
    {
        var library = new FakeRecipeLibrary();
        var kept = KeptRecipes.Of(Page);
        var recipe = kept.Recipe with
        {
            Yield = yield,
            Ingredients =
            [
                new RecipeIngredient(null, "1 kg mince"), new RecipeIngredient("Sauce", "2 cups water"),
                new RecipeIngredient("Sauce", "salt, to taste"), new RecipeIngredient("Sauce", "zest of 1 lemon")
            ],
            Steps = [new RecipeStep(null, "Add 1 cup water and bake for 40 minutes.")]
        };
        var held = kept with { Recipe = recipe };
        library.Kept.Add(held);

        var shelf = new RecipeShelf(library, library, [Taste], new FakeTimeProvider(DateTimeOffset.UnixEpoch));
        var tools = new RecipeTools(new Dictionary<string, IRecipeSource>(), new FakeSiteBrowser(""), new FakePhotoFetcher(), shelf);
        return (tools, library.AddressOf(held).AbsoluteUri);
    }

    /// <summary>From our link: every line scaled to the people, numbered as the print is, framed as data.</summary>
    [Test]
    public async Task OurLink_GivesEveryLineScaled_ToThePeople()
    {
        var (tools, link) = Holding("4 servings");

        string result = await tools.ScaleRecipe(link, people: 6);

        Assert.That(result, Does.StartWith("--- untrusted recipe data"));
        Assert.That(result, Does.Contain("1.5").And.Contain("6"));
        Assert.That(result, Does.Contain("1. 1.5 kg mince"));
        Assert.That(result, Does.Contain("### Sauce\n2. 3 cups water"));
        Assert.That(result, Does.Contain("3. salt, to taste\n"), "no number, nothing to say");
        Assert.That(result, Does.Contain("4. zest of 1 lemon (not scaled)"), "the cook must know to do this one");
    }

    /// <summary>The site's own link finds the same recipe, since the cook may have read that one.</summary>
    [Test]
    public async Task TheSitesLink_FindsTheSameRecipe()
    {
        var (tools, _) = Holding("4 servings");

        Assert.That(await tools.ScaleRecipe(Page, people: 8), Does.Contain("1. 2 kg mince"));
    }

    /// <summary>The amounts in the steps are listed by step, with what they become.</summary>
    [Test]
    public async Task TheAmountsInTheSteps_AreListed()
    {
        var (tools, link) = Holding("4 servings");

        string result = await tools.ScaleRecipe(link, people: 8);

        Assert.That(result, Does.Contain("Step 1: 1 cup becomes 2 cup"));
        Assert.That(result, Does.Not.Contain("40 minutes becomes"));
    }

    /// <summary>A yield that isn't people keeps the recipe as written, and says how to ask for more.</summary>
    [Test]
    public async Task AYieldThatIsntPeople_KeepsTheRecipe_AndSaysSo()
    {
        var (tools, link) = Holding("10 pies");

        string result = await tools.ScaleRecipe(link, people: 6);

        Assert.That(result, Does.Contain("1. 1 kg mince"));
        Assert.That(result, Does.Contain("`times`"));
    }

    /// <summary>With no people, as for a cake, the recipe stays as written.</summary>
    [Test]
    public async Task WithNoPeople_TheRecipeStaysAsWritten()
    {
        var (tools, link) = Holding("1 cake");

        Assert.That(await tools.ScaleRecipe(link), Does.Contain("1. 1 kg mince"));
    }

    /// <summary>Asked for a factor, it uses that one, whatever the yield says.</summary>
    [Test]
    public async Task AskedForATimes_ItUsesThatFactor()
    {
        var (tools, link) = Holding("10 pies");

        Assert.That(await tools.ScaleRecipe(link, people: 6, times: 2), Does.Contain("1. 2 kg mince"));
    }

    /// <summary>A page we don't hold says so and names what to do instead, and a bad address is refused.</summary>
    [Test]
    public async Task APageWeDontHold_SendsTheCookToCalculate()
    {
        var (tools, _) = Holding("4 servings");

        Assert.That(await tools.ScaleRecipe("https://example.org/other/", people: 6), Does.Contain("Calculate"));
        Assert.That(await tools.ScaleRecipe("not an address", people: 6), Does.Contain("address"));
    }
}
