using LouisAgent.Packs.Recipes.Library;
using LouisAgent.Packs.Recipes.Recipes;
using LouisAgent.Packs.Recipes.Sources;
using LouisAgent.Packs.Recipes.Sources.Sites;
using LouisAgent.Packs.Recipes.Tests.Providers;
using LouisAgent.Packs.Recipes.Tests.Tools.Reading;
using LouisAgent.Packs.Recipes.Tools;
using Microsoft.Extensions.Time.Testing;

namespace LouisAgent.Packs.Recipes.Tests.Recipes;

/// <summary>
/// Nearly half of the answers in a run used a page we held with no yield, so nothing could be scaled to the people
/// asked for. A third of those pages do say how many they feed, in words we didn't read. The shapes are real pages'.
/// </summary>
public class PageWordsYieldTests
{
    private static readonly Uri Page = new("https://example.com/recipes/bobotie/");

    /// <summary>A page whose recipe data gives no yield, and these words in its body.</summary>
    private static string Html(string body) =>
        "<html><head><script type=\"application/ld+json\">{\"@type\":\"Recipe\",\"name\":\"Bobotie\","
        + "\"recipeIngredient\":[\"1 kg mince\"],\"recipeInstructions\":[\"Bake it.\"]}</script></head>"
        + "<body><main><h1>Bobotie</h1>" + body + "</main></body></html>";

    /// <summary>However a page words it, on one line or two, in a bracket or in Afrikaans, it's the yield.</summary>
    [TestCase("<p>Serves 6</p>", "6 servings")]
    [TestCase("<p>serves 6</p>", "6 servings")]
    [TestCase("<p>Serves:</p><p>8-10</p>", "8-10 servings")]
    [TestCase("<p>SERVES</p><p>10</p>", "10 servings")]
    [TestCase("<p>Serves</p><p>6+</p>", "6+ servings")]
    [TestCase("<p>Serves 4 to 6 persons</p>", "4 to 6 servings")]
    [TestCase("<p>MILK TART (serves 8)</p>", "8 servings")]
    [TestCase("<p>Ingredients: (serves 6)</p>", "6 servings")]
    [TestCase("<p>(8-10 servings, more if used only as a relish)</p>", "8-10 servings")]
    [TestCase("<p>Servings: 4</p>", "4 servings")]
    [TestCase("<p>Feeds 6</p>", "6 servings")]
    [TestCase("<p>Bedien 4</p>", "4 servings")]
    [TestCase("<p>Porsies: 6</p>", "6 servings")]
    [TestCase("<p>Makes 12 pies</p>", "12 pies")]
    [TestCase("<p>Yield: 12 rolls</p>", "12 rolls")]
    [TestCase("<p><strong>BOBOTIE</strong> Serves 4 to 6 persons</p>", "4 to 6 servings")]
    [TestCase("<h3>Serves</h3><h2>6+</h2>", "6+ servings")]
    public void APagesWords_GiveTheYield_WhenItsDataHasNone(string body, string expected) =>
        Assert.That(SchemaOrgRecipe.Read(Html(body), Page)!.Yield, Is.EqualTo(expected));

    /// <summary>What only mentions serving, or says how much of one part it makes, is no yield of the dish.</summary>
    [TestCase("<p>Hi Sue, that recipe makes approximately 36 medium ones</p>")]
    [TestCase("<p>CREAMY SAUCE (enough to serve 2)</p>")]
    [TestCase("<p>Step 5: Serve and enjoy</p><p>Your rolls are ready.</p>")]
    [TestCase("<p>Serve</p><p>4 slices each</p>")]
    [TestCase("<p>Makes a 20 cm two-layer cake</p>")]
    [TestCase("<p>Bake for 20 minutes.</p>")]
    [TestCase("<p>Mix it well and bake. Serves 4 with rice</p>")]
    public void WhatOnlyMentionsServing_IsNoYield(string body) =>
        Assert.That(SchemaOrgRecipe.Read(Html(body), Page)!.Yield, Is.Null);

    /// <summary>A page with no data, read by its headings, keeps the yield its words give.</summary>
    [Test]
    public async Task APageReadByItsHeadings_KeepsTheYieldItsWordsGive()
    {
        const string page = "<html><head><title>Pie</title></head><body><main><h1>Pie</h1><p>Serves:</p><p>8</p>" +
                            "<p>A lovely pie for a family.</p><h2>Ingredients</h2><ul><li>1 kg mince</li><li>2 eggs</li></ul>" +
                            "<h2>Method</h2><ol><li>Bake it.</li></ol></main></body></html>";
        var library = new FakeRecipeLibrary();
        var shelf = new RecipeShelf(library, library, [RecipeSites.Find("taste")!], new FakeTimeProvider(DateTimeOffset.UnixEpoch));
        var tools = new RecipeTools(new Dictionary<string, IRecipeSource>(), new FakeSiteBrowser(page), new FakePhotoFetcher(), shelf);

        await tools.ReadRecipe("https://taste.co.za/recipes/pie/");

        Assert.That(library.Kept.Single().Recipe.Yield, Is.EqualTo("8 servings"));
    }
}
