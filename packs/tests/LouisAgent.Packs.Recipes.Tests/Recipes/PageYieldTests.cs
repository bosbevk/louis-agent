using LouisAgent.Packs.Recipes.Recipes;

namespace LouisAgent.Packs.Recipes.Tests.Recipes;

/// <summary>
/// Taste's recipe data says <c>1 serving</c> on a bobotie with 1 kg of mince, while the same page's text says
/// <c>Serves 6 to 8</c>. Scaling by the data's number would make 4 kg for four people, so when the data says one, the
/// page's own words decide.
/// </summary>
public class PageYieldTests
{
    private static readonly Uri Page = new("https://example.com/recipes/bobotie/");

    /// <summary>A page with recipe data giving this yield, and these words in its body.</summary>
    private static string Html(string yield, string body) =>
        "<html><head><script type=\"application/ld+json\">{\"@type\":\"Recipe\",\"name\":\"Bobotie\",\"recipeYield\":\""
        + yield + "\",\"recipeIngredient\":[\"1 kg mince\"],\"recipeInstructions\":[\"Bake it.\"]}</script></head>"
        + "<body><main><h1>Bobotie</h1>" + body + "</main></body></html>";

    /// <summary>The page's words replace a data yield of one, for people or for what it makes.</summary>
    [TestCase("1 serving", "<p>Serves 6 to 8</p>", "6 to 8 servings")]
    [TestCase("1", "<p>Serves 6</p>", "6 servings")]
    [TestCase("1 serving", "<p>Makes 6 Large or 12 Small pies</p>", "6 Large or 12 Small pies")]
    [TestCase("1 serving", "<div><label>Serves</label> <span>6 to 8</span></div>", "6 to 8 servings")]
    public void AYieldOfOne_GivesWayToThePagesWords(string yield, string body, string expected) =>
        Assert.That(SchemaOrgRecipe.Read(Html(yield, body), Page)!.Yield, Is.EqualTo(expected));

    /// <summary>
    /// Taste marks its real number up in the page itself, outside the main text: <c>itemprop="recipeYield"</c>, as a
    /// number of people or as what it makes.
    /// </summary>
    [TestCase("6 to 8", "6 to 8 servings")]
    [TestCase("Makes 6 large or 12 small ", "6 large or 12 small")]
    public void AYieldOfOne_GivesWayToThePagesOwnMarkup(string marked, string expected)
    {
        string html = Html("1 serving", "<p>A family favourite.</p>")
            .Replace("<body>", $"<body><aside><label>Serves</label> <span itemprop=\"recipeYield\">{marked}</span></aside>");

        Assert.That(SchemaOrgRecipe.Read(html, Page)!.Yield, Is.EqualTo(expected));
    }

    /// <summary>With no words on the page the number isn't trusted either: unknown, and the cook says so.</summary>
    [Test]
    public void AYieldOfOne_WithNoWordsOnThePage_IsUnknown() =>
        Assert.That(SchemaOrgRecipe.Read(Html("1 serving", "<p>A family favourite.</p>"), Page)!.Yield, Is.Null);

    /// <summary>Any other yield is the page's own data, read first as ever (ADR 0067).</summary>
    [TestCase("12 servings")]
    [TestCase("10 pies")]
    [TestCase("4")]
    public void AnyOtherYield_StaysAsTheDataHasIt(string yield) =>
        Assert.That(SchemaOrgRecipe.Read(Html(yield, "<p>Serves 6 to 8</p>"), Page)!.Yield, Is.EqualTo(yield));

    /// <summary>A sentence that happens to start the same way is no yield.</summary>
    [TestCase("<p>Serves as a great lunch for the whole family on a Sunday afternoon in the sun.</p>")]
    [TestCase("<p>Makes me hungry just thinking of it.</p>")]
    public void ASentence_IsNoYield(string body) =>
        Assert.That(SchemaOrgRecipe.Read(Html("1 serving", body), Page)!.Yield, Is.Null);
}
