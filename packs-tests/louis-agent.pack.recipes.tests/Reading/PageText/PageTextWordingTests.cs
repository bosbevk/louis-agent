using LouisAgent.Packs.Recipes.Reading;
using LouisAgent.Packs.Recipes.Reading.PageText;
using LouisAgent.Packs.Recipes.Recipes;

namespace LouisAgent.Packs.Recipes.Tests.Reading.PageText;

/// <summary>
/// Sites that word their headings their own way, and a page whose only articles are cards. The shapes are real pages',
/// with the words made up.
/// </summary>
public class PageTextWordingTests
{
    private static readonly Uri Page = new("https://example.com/recipes/rolls/");

    /// <summary>A page whose main part is this HTML.</summary>
    private static Recipe? Read(string main) =>
        PageTextRecipe.Read($"<html><head><title>Rolls | A Site</title></head><body><main>{main}</main></body></html>", Page);

    /// <summary>"What you need" with its yield in brackets and "what to do", as Jan Braai's pages have them.</summary>
    [Test]
    public void WhatYouNeed_WithItsYield_IsTheIngredientsHeading()
    {
        var recipe = Read("<h1>Rolls</h1><h2>what you need (makes 6)</h2><ul><li>2 cups flour</li><li>1 tsp salt</li></ul>" +
                          "<h2>what to do</h2><ul><li>Mix it.</li><li>Braai it.</li></ul>");

        Assert.That(recipe, Is.Not.Null);
        Assert.That(recipe!.Yield, Is.EqualTo("6"));
        Assert.That(recipe.Ingredients.Select(line => line.Text), Is.EqualTo(new[] { "2 cups flour", "1 tsp salt" }));
        Assert.That(recipe.Steps.Select(step => step.Text), Is.EqualTo(new[] { "Mix it.", "Braai it." }));
    }

    /// <summary>A yield in brackets on its own line, "Feeds" for people, as other Jan Braai pages have it.</summary>
    [Test]
    public void FeedsInBrackets_IsTheYield()
    {
        var recipe = Read("<h1>Rolls</h1><h2>WHAT YOU NEED</h2><p>(Feeds 6)</p><ul><li>2 cups flour</li><li>1 tsp salt</li></ul>" +
                          "<h2>WHAT TO DO</h2><ul><li>Mix it.</li></ul>");

        Assert.That(recipe!.Yield, Does.Contain("6"));
        Assert.That(recipe.Ingredients.Select(line => line.Text), Is.EqualTo(new[] { "2 cups flour", "1 tsp salt" }));
    }

    /// <summary>A cook's aside after the steps isn't one of them, and "Tip the batter" still is.</summary>
    [Test]
    public void AnAside_EndsTheSteps()
    {
        var recipe = Read("<h1>Rolls</h1><h2>Ingredients</h2><p>2 cups flour</p><p>1 tsp salt</p><h2>Method</h2>" +
                          "<p>Tip the batter in.</p><p>TIP Use an old tin.</p><p>Bake it.</p>");

        Assert.That(recipe!.Steps.Select(step => step.Text), Is.EqualTo(new[] { "Tip the batter in." }));
    }

    /// <summary>A method whose every step has its dash on the line above, as one Jan Braai page has it.</summary>
    [Test]
    public void DashesOnTheirOwnLines_AreStillSteps()
    {
        var recipe = Read("<h1>Rolls</h1><h2>what you need (makes 6)</h2><ul><li>2 cups flour</li><li>1 tsp salt</li></ul>" +
                          "<h2>what to do</h2><ul><li><p>Mix it.</p></li><li><p>Braai it.</p></li></ul>");

        Assert.That(recipe!.Steps.Select(step => step.Text), Is.EqualTo(new[] { "Mix it.", "Braai it." }));
    }

    /// <summary>
    /// "What you need for" names a part and "What you need to do" is the method, and a sentence straight under a part
    /// opens it without being a step, as FinGlobal has them.
    /// </summary>
    [Test]
    public void WhatYouNeedFor_IsAPart_AndWhatYouNeedToDo_IsTheMethod()
    {
        var recipe = Read("<h1>Rolls</h1><p>What you need for filling:</p><ul><li>600g boerewors</li><li>2 onions</li></ul>" +
                          "<p>What you need for the relish:</p><ul><li>350g tomatoes</li></ul><p>What you need to do:</p>" +
                          "<ul><li>Fry the boerewors.</li><li>Fry the onions.</li></ul><p>Prepping the rolls:</p>" +
                          "<p>The time for building the rolls is now, so here we go!</p>" +
                          "<ul><li>Smear on mustard.</li></ul><p>All that's left is to enjoy it!</p>");

        Assert.That(recipe!.Ingredients.Select(line => (line.Section, line.Text)), Is.EqualTo(new (string?, string)[]
        {
            ("filling", "600g boerewors"), ("filling", "2 onions"), ("the relish", "350g tomatoes")
        }));
        Assert.That(recipe.Steps.Select(step => step.Text),
            Is.EqualTo(new[] { "Fry the boerewors.", "Fry the onions.", "Smear on mustard." }));
    }

    /// <summary>Several articles and no main are cards: the page's own words are outside them.</summary>
    [Test]
    public void SeveralArticles_AndNoMain_AreCards_SoTheBodyIsRead()
    {
        var (_, text) = HtmlText.ToText("<html><body><h1>Rolls</h1><p>The recipe.</p><article>Card one</article>" +
                                        "<article>Card two</article></body></html>", Page);

        Assert.That(text, Does.Contain("The recipe."));
    }

    /// <summary>One article is the page, and a main still wins over any articles.</summary>
    [TestCase("<body><p>The menu.</p><article>The recipe.</article></body>", TestName = "one article")]
    [TestCase("<body><p>The menu.</p><main>The recipe.<article>Card one</article><article>Card two</article></main></body>",
        TestName = "a main with articles")]
    public void ThePagesOwnPart_StillWins(string body)
    {
        var (_, text) = HtmlText.ToText($"<html>{body}</html>", Page);

        Assert.That(text, Does.Contain("The recipe.").And.Not.Contain("The menu."));
    }
}
