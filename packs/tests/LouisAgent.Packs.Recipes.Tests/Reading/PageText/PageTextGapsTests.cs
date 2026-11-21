using LouisAgent.Packs.Recipes.Reading.PageText;
using LouisAgent.Packs.Recipes.Recipes;

namespace LouisAgent.Packs.Recipes.Tests.Reading.PageText;

/// <summary>
/// Real recipes the reader refused, each for a line it couldn't place: the long run's excluded pages. The shapes are
/// theirs, with the words made shorter.
/// </summary>
public class PageTextGapsTests
{
    private static readonly Uri Page = new("https://example.com/recipes/rolls/");

    /// <summary>The ingredients and the method a page ends with.</summary>
    private const string Ingredients = "<ul><li>2 cups flour</li><li>1 tsp salt</li><li>1 cup milk</li></ul>";

    /// <summary>A page whose main part is this HTML.</summary>
    private static Recipe? Read(string main) => PageTextRecipe.Read(Html(main), Page);

    /// <summary>The whole page around its main part.</summary>
    private static string Html(string main) =>
        $"<html><head><title>Rolls | A Site</title></head><body><main>{main}</main></body></html>";

    /// <summary>The texts of the steps.</summary>
    private static IEnumerable<string> Steps(Recipe recipe) => recipe.Steps.Select(step => step.Text);

    /// <summary>A heading broken over two lines is the heading it says: FinGlobal's "What" over "you need:".</summary>
    [Test]
    public void AHeadingBrokenOverTwoLines_IsAHeading()
    {
        var recipe = Read($"<h1>Rolls</h1><p>What</p><p>you need:</p>{Ingredients}" +
                          "<p>What</p><p>to do:</p><ul><li>Mix it.</li><li>Bake it.</li></ul>");

        Assert.That(recipe, Is.Not.Null);
        Assert.That(Steps(recipe!), Is.EqualTo(new[] { "Mix it.", "Bake it." }));
    }

    /// <summary>A step wrapped onto the next line is one step: FinGlobal's "until it resembles bread crumbs."</summary>
    [Test]
    public void AStepWrappedOntoTheNextLine_IsOneStep()
    {
        var recipe = Read($"<h1>Rolls</h1><h2>Ingredients</h2>{Ingredients}<h2>Method</h2><ul><li>Mix it.</li>" +
                          "<li>Rub the butter into the flour</li></ul><p>until it looks like crumbs.</p><ul><li>Bake it.</li></ul>");

        Assert.That(Steps(recipe!),
            Is.EqualTo(new[] { "Mix it.", "Rub the butter into the flour until it looks like crumbs.", "Bake it." }));
    }

    /// <summary>A small instruction among the lines moves to the method: Sarah Graham's "Whisk together".</summary>
    [Test]
    public void AnInstructionAmongTheLines_MovesToTheMethod()
    {
        var recipe = Read("<h1>Rolls</h1><h2>Ingredients</h2><ul><li>2 cups flour</li><li>1 tsp salt</li><li>1 cup milk</li>" +
                          "<li>Whisk together until smooth</li></ul><h2>What to do</h2><ol><li>Mix it.</li><li>Bake it.</li></ol>");

        Assert.That(recipe!.Ingredients.Select(line => line.Text),
            Is.EqualTo(new[] { "2 cups flour", "1 tsp salt", "1 cup milk" }));
        Assert.That(Steps(recipe), Is.EqualTo(new[] { "Mix it.", "Bake it.", "Whisk together until smooth" }));
    }

    /// <summary>A method written where the ingredients go is still no recipe: that's more than a stray.</summary>
    [Test]
    public void AMethodWhereTheIngredientsGo_IsStillNoRecipe() =>
        Assert.That(Read("<h1>Rolls</h1><h2>Ingredients</h2><ul><li>2 cups flour</li><li>Mix the flour well</li>" +
                         "<li>Add the water slowly</li></ul><h2>Method</h2><ol><li>Bake it.</li></ol>"), Is.Null);

    /// <summary>A heading with a long aside in brackets is still one: Food Fox's "Ingredients: (makes 1...)".</summary>
    [Test]
    public void AHeadingWithALongAside_IsAHeading()
    {
        var recipe = Read("<h1>Rolls</h1><p>Ingredients: (makes 1 sandwich, adjust the quantities for more)</p>" +
                          $"{Ingredients}<p>Method:</p><p>Mix it all and bake it.</p>");

        Assert.That(recipe, Is.Not.Null);
        Assert.That(recipe!.Ingredients, Has.Count.EqualTo(3));
    }

    /// <summary>A sentence between listed steps belongs to the method: FinGlobal's glaze, Cupcakes' note.</summary>
    [TestCase("While it cools, make the glaze.")]
    [TestCase("Note – the pastry can also be made in a processor.")]
    public void ASentenceBetweenListedSteps_IsAStep(string sentence)
    {
        var recipe = Read($"<h1>Rolls</h1><h2>Ingredients</h2>{Ingredients}<h2>Method</h2><ul><li>Mix it.</li>" +
                          $"<li>Bake it.</li></ul><p>{sentence}</p><ul><li>Beat the sugar.</li><li>Pour it over.</li></ul>");

        Assert.That(Steps(recipe!), Is.EqualTo(new[] { "Mix it.", "Bake it.", sentence, "Beat the sugar.", "Pour it over." }));
    }

    /// <summary>A heading with no words over the method ends nothing: Bibby's empty <c>######</c>.</summary>
    [Test]
    public void AHeadingWithNoWords_EndsNothing() =>
        Assert.That(PageTextRecipe.Steps(["######", "- Preheat it.", "- Bake it."], [])!.Select(step => step.Text),
            Is.EqualTo(new[] { "Preheat it.", "Bake it." }));

    /// <summary>A lone dash over each line is no ingredient: Jackie Cameron's and Lucky Star's lists.</summary>
    [Test]
    public void ALoneDash_IsNoIngredient()
    {
        var recipe = Read("<h1>Rolls</h1><h2>Ingredients</h2><p>-</p><p>2 cups flour</p><p>-</p><p>1 tsp salt</p>" +
                          "<h2>Method</h2><ul><li>Mix it.</li></ul>");

        Assert.That(recipe!.Ingredients.Select(line => line.Text), Is.EqualTo(new[] { "2 cups flour", "1 tsp salt" }));
    }

    /// <summary>A note in stars ends the steps, as an aside does: FinGlobal's "*Notes:" over its own bullets.</summary>
    [Test]
    public void ANoteInStars_EndsTheSteps()
    {
        var recipe = Read($"<h1>Rolls</h1><p>What you need:</p>{Ingredients}<p>What to do:</p>" +
                          "<p>Step 1: Butter your bread on both sides.</p><p>Step 2: Braai it slowly.</p><p>*Notes:</p>" +
                          "<ul><li>Keep the fire low.</li><li>Use butter or mayo.</li></ul>");

        Assert.That(Steps(recipe!),
            Is.EqualTo(new[] { "Step 1: Butter your bread on both sides.", "Step 2: Braai it slowly." }));
    }

    /// <summary>A heading with no words between the lines and the method is passed over: Bibby's run.</summary>
    [Test]
    public void AHeadingWithNoWords_AfterARun_IsPassedOver()
    {
        var recipe = PlainRunRecipe.Read(Html("<h1>Cupcakes</h1><ul><li>1 egg</li><li>2 cups flour</li><li>1 tsp salt</li>" +
                                              "<li>1 cup milk</li></ul><h6></h6><h6>######</h6><ul><li>Preheat the oven.</li>" +
                                              "<li>Bake them.</li></ul>"), Page);

        Assert.That(recipe, Is.Not.Null);
        Assert.That(Steps(recipe!), Is.EqualTo(new[] { "Preheat the oven.", "Bake them." }));
    }

    /// <summary>A link line after the lines is left out, not the end of the page: Vrouekeur's "Klik hier".</summary>
    [Test]
    public void ALinkLineAfterTheLines_IsLeftOut()
    {
        var recipe = PlainRunRecipe.Read(Html("<h1>Melktert</h1><p>1 liter melk</p><p>3 eiers</p><p>2 k meel</p>" +
                                              "<p>1 ml sout</p><p><a href=\"https://example.com/x\">Klik hier</a></p><h2>Metode</h2>" +
                                              "<ul><li>Kook die melk.</li><li>Bak dit.</li></ul>"), Page);

        Assert.That(recipe, Is.Not.Null);
        Assert.That(recipe!.Ingredients, Has.Count.EqualTo(4));
        Assert.That(Steps(recipe), Is.EqualTo(new[] { "Kook die melk.", "Bak dit." }));
    }
}
