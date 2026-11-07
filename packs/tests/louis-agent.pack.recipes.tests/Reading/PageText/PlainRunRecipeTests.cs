using LouisAgent.Packs.Recipes.Reading.PageText;
using LouisAgent.Packs.Recipes.Recipes;

namespace LouisAgent.Packs.Recipes.Tests.Reading.PageText;

/// <summary>
/// Many real recipes have no headings at all: a yield, a run of short lines with amounts, then sentences. These follow
/// the shapes of real pages with the words made up, and what only looks like that shape comes out as nothing, because
/// what's kept is served for good.
/// </summary>
public class PlainRunRecipeTests
{
    private static readonly Uri Page = new("https://example.com/recipes/pie/");

    /// <summary>A page whose main part is this HTML.</summary>
    private static Recipe? Read(string main) =>
        PlainRunRecipe.Read($"<html><head><title>Pie | A Site</title></head><body><main>{main}</main></body></html>", Page);

    /// <summary>The ingredients, each with its part.</summary>
    private static IEnumerable<(string?, string)> Lines(Recipe recipe) =>
        recipe.Ingredients.Select(line => (line.Section, line.Text));

    /// <summary>The texts of the steps.</summary>
    private static IEnumerable<string> Steps(Recipe recipe) => recipe.Steps.Select(step => step.Text);

    /// <summary>Plain lines under parts ending in a colon, then sentences, as Zola Nene's pages have them.</summary>
    [Test]
    public void PlainLinesUnderParts_ThenSentences_AreARecipe()
    {
        var recipe = Read("<h2>Chicken Pie</h2><p>Serves 4</p><p>For the chicken:</p><p>1 whole chicken</p><p>2 carrots</p>" +
                          "<p>Water</p><p>For the pastry:</p><p>225g flour</p><p>125g butter</p><p>Place it all in a pot.</p>" +
                          "<p>Preheat the oven to 180C</p><p>Serve hot.</p>");

        Assert.That(recipe, Is.Not.Null);
        Assert.That(recipe!.Name, Is.EqualTo("Chicken Pie"));
        Assert.That(recipe.Yield, Does.Contain("4"));
        Assert.That(Lines(recipe), Is.EqualTo(new (string?, string)[]
        {
            ("For the chicken", "1 whole chicken"), ("For the chicken", "2 carrots"), ("For the chicken", "Water"),
            ("For the pastry", "225g flour"), ("For the pastry", "125g butter")
        }));
        Assert.That(Steps(recipe), Is.EqualTo(new[] { "Place it all in a pot.", "Preheat the oven to 180C", "Serve hot." }));
    }

    /// <summary>
    /// A part named by one word, and a video after the steps, as Kate Tin's page has them. The lines above the run
    /// aren't ingredients, and "oil, for frying" is one with no amount.
    /// </summary>
    [Test]
    public void APartNamedByOneWord_AndAVideoAfterTheSteps_AreHandled()
    {
        var recipe = Read("<h1>Koeksisters</h1><p>Recipe by a friend</p><p>Makes 4 dozen</p><p>1250g flour</p><p>5 eggs</p>" +
                          "<p>oil, for frying</p><p>Syrup</p><p>12 cups sugar</p><p>6 cups water</p><p>Sift the flour.</p>" +
                          "<p>Fry the dough.</p><p>WATCH MY GREAT ADVENTURE HERE AND SEE HOW SHE MAKES HER KOEKSISTERS:</p><p>&lt;iframe src=\"https://example.com/v\"&gt;&lt;/iframe&gt;</p>");

        Assert.That(recipe!.Yield, Is.EqualTo("4 dozen"));
        Assert.That(Lines(recipe), Is.EqualTo(new (string?, string)[]
        {
            (null, "1250g flour"), (null, "5 eggs"), (null, "oil, for frying"), ("Syrup", "12 cups sugar"),
            ("Syrup", "6 cups water")
        }));
        Assert.That(Steps(recipe), Is.EqualTo(new[] { "Sift the flour.", "Fry the dough." }));
    }

    /// <summary>A list then steps counted "1.)", with a tip after them, as Cupcakes &amp; Couscous has them.</summary>
    [Test]
    public void AListThenCountedSteps_AreARecipe()
    {
        var recipe = Read("<h2>Pot Pies</h2><h3>POT PIES</h3><ul><li>Servings: 4</li><li>Print</li></ul>" +
                          "<ul><li>250g chicken</li><li>2 tbsp flour</li><li>salt and pepper</li><li>1 egg</li></ul>" +
                          "<p>1.) Cut the chicken.</p><p>2.) Bake it.</p><p>Make ahead tip: make the filling first.</p>");

        Assert.That(recipe!.Name, Is.EqualTo("Pot Pies"));
        Assert.That(recipe.Yield, Does.Contain("4"));
        Assert.That(Lines(recipe).Select(line => line.Item2),
            Is.EqualTo(new[] { "250g chicken", "2 tbsp flour", "salt and pepper", "1 egg" }));
        Assert.That(Steps(recipe), Is.EqualTo(new[] { "Cut the chicken.", "Bake it." }));
    }

    /// <summary>Two lists with nothing between them, the second being steps, as Bibby's Kitchen has them.</summary>
    [Test]
    public void TwoListsInARow_AreIngredientsThenSteps()
    {
        var recipe = Read("<h1>Oxtail Soup</h1><h5>Oxtail Soup</h5><p>Serves&nbsp;8</p>" +
                          "<ul><li>2 tablespoons oil</li><li>700g oxtail</li><li>1 onion, diced</li><li>tomato paste</li></ul>" +
                          "<ul><li>Heat the oil in a saucepan.</li><li>Brown the oxtail.</li></ul><p>3.5.3208</p>");

        Assert.That(recipe!.Yield, Does.Contain("8"));
        Assert.That(Lines(recipe).Select(line => line.Item1), Is.All.Null);
        Assert.That(Lines(recipe!).Select(line => line.Item2),
            Is.EqualTo(new[] { "2 tablespoons oil", "700g oxtail", "1 onion, diced", "tomato paste" }));
        Assert.That(Steps(recipe!), Is.EqualTo(new[] { "Heat the oil in a saucepan.", "Brown the oxtail." }));
    }

    /// <summary>
    /// Headings as parts, a yield with no word for it, and bare counted steps that look like amounts until they end in
    /// a full stop, as Vrouekeur has them.
    /// </summary>
    [Test]
    public void HeadingsAsParts_AndBareCountedSteps_AreARecipe()
    {
        var recipe = Read("<h1>Melktert</h1><h3>Melktert in 'n glas</h3><p>12 glasies</p><h3>KORS</h3>" +
                          "<p>½ pak beskuitjies<br>52 ml botter</p><h3>VULSEL</h3><p>500 ml melk<br>1 groot eier<br>fyn kaneel</p>" +
                          "<p>1 Kap beskuitjies fyn en meng met botter.<br>2 Roer melk oor lae hitte.</p>");

        Assert.That(recipe!.Yield, Is.EqualTo("12 glasies"));
        Assert.That(Lines(recipe), Is.EqualTo(new (string?, string)[]
        {
            ("KORS", "½ pak beskuitjies"), ("KORS", "52 ml botter"), ("VULSEL", "500 ml melk"), ("VULSEL", "1 groot eier"),
            ("VULSEL", "fyn kaneel")
        }));
        Assert.That(Steps(recipe), Is.EqualTo(new[] { "Kap beskuitjies fyn en meng met botter.", "Roer melk oor lae hitte." }));
    }

    /// <summary>
    /// Each part's ingredients with its own method straight under them, a link in a part's line, and a yield in
    /// brackets, as The Food Fox has them. The link after the last step ends it.
    /// </summary>
    [Test]
    public void EachPartWithItsOwnMethod_IsOneRecipe()
    {
        var recipe = Read("<h1>Leek Pie</h1><p>Ingredients: (serves 6)</p>" +
                          "<p>For the filling: (from <a href=\"https://example.com/book\">my book</a>)</p>" +
                          "<p>30 ml oil</p><p>1,5 kg chicken</p><p>1 onion</p><p>2 leeks</p><p>Preheat the oven and fry it all.</p>" +
                          "<p>For the pastry:</p><p>280 g flour</p><p>250 g butter</p><p>Mix the flour and butter.</p>" +
                          "<p>Made with <a href=\"https://example.com/wine\">a wine farm</a>.</p>");

        Assert.That(recipe!.Yield, Does.Contain("6"));
        Assert.That(Lines(recipe), Is.EqualTo(new (string?, string)[]
        {
            ("For the filling", "30 ml oil"), ("For the filling", "1,5 kg chicken"), ("For the filling", "1 onion"),
            ("For the filling", "2 leeks"), ("For the pastry", "280 g flour"), ("For the pastry", "250 g butter")
        }));
        Assert.That(Steps(recipe), Is.EqualTo(new[] { "Preheat the oven and fry it all.", "Mix the flour and butter." }));
    }

    /// <summary>
    /// Numbered parts in the method, each opened by a sentence that isn't a step, and a last part with only a sentence,
    /// as FinGlobal has them.
    /// </summary>
    [Test]
    public void NumberedPartsInTheMethod_KeepTheirStepsAndNotTheirOpenings()
    {
        var recipe = Read("<h1>Rolls</h1><h2>Rolls recipe</h2><p>For the dough:</p><ul><li>3 cups flour</li><li>1 tsp salt</li></ul>" +
                          "<p>For the rolls:</p><ul><li>500g boerewors</li><li>Flour for dusting</li></ul>" +
                          "<p>Step 1: Make the dough</p><p>A dough like this one is simple.</p>" +
                          "<ul><li>Mix the flour and salt.</li><li>Let it rise.</li></ul>" +
                          "<p>Step 2: Wrap them</p><p>This is where it starts:</p><ul><li>Wrap the boerewors.</li></ul>" +
                          "<p>Step 3: Serve and enjoy</p><p>Serve them with relish.</p><h3>Tips</h3><ul><li>Use good meat.</li></ul>");

        Assert.That(Lines(recipe!).Select(line => line.Item2),
            Is.EqualTo(new[] { "3 cups flour", "1 tsp salt", "500g boerewors", "Flour for dusting" }));
        Assert.That(recipe!.Steps.Select(step => (step.Section, step.Text)), Is.EqualTo(new (string?, string)[]
        {
            ("Step 1: Make the dough", "Mix the flour and salt."), ("Step 1: Make the dough", "Let it rise."),
            ("Step 2: Wrap them", "Wrap the boerewors."), ("Step 3: Serve and enjoy", "Serve them with relish.")
        }));
    }

    /// <summary>A list under a story, with a heading only over the method, as FinGlobal's curry has it.</summary>
    [Test]
    public void AHeadingOnlyOverTheMethod_IsNoStep()
    {
        var recipe = Read("<h1>Curry</h1><h3>The ingredients</h3><p>The key to any dish is what goes in.</p><h3>Delve deeper</h3>" +
                          "<p>Unveil its secrets.</p><ul><li>1 kg chicken</li><li>250 g onion</li><li>2 tbsp oil</li><li>4 cloves</li></ul>" +
                          "<p>Instructions:</p><ul><li>Heat the oil.</li><li>Add the chicken.</li></ul><p>Please accept cookies</p>");

        Assert.That(recipe!.Steps.Select(step => (step.Section, step.Text)),
            Is.EqualTo(new (string?, string)[] { (null, "Heat the oil."), (null, "Add the chicken.") }));
    }

    /// <summary>
    /// A yield written "6 Servings" isn't an ingredient, a line with a link to a shop still is, and a step with no
    /// words isn't a step, as Real Meal Revolution and The Daley Plate have them.
    /// </summary>
    [Test]
    public void AYieldALinkAndAnEmptyStep_AreHandled()
    {
        var recipe = Read("<h1>Bobotie</h1><p>YIELDS</p><p>6 Servings</p><p>1lb chicken thighs</p>" +
                          "<p>1 tablespoon paprika, like <a href=\"https://example.com/shop/a/very/long/address/that/" +
                          "goes/on/for/more/than/a/line/ever/would/products/paprika\">our brand</a></p>" +
                          "<p>1 onion</p><p>2 carrots</p><p>Fry the onion.</p><p>Bake it.</p><p>&#x2800;</p>");

        Assert.That(recipe!.Yield, Does.Contain("6"));
        Assert.That(Lines(recipe).Select(line => line.Item2),
            Is.EqualTo(new[] { "1lb chicken thighs", "1 tablespoon paprika, like our brand", "1 onion", "2 carrots" }));
        Assert.That(Steps(recipe), Is.EqualTo(new[] { "Fry the onion.", "Bake it." }));
    }

    /// <summary>A whole method in one long paragraph is still a method, as Kate Tin's cakes have it.</summary>
    [Test]
    public void AWholeMethodInOneLongParagraph_IsARecipe()
    {
        const string method = "Preheat the oven to 160C and grease and line three cake tins with baking paper. Boil the " +
                              "water, then add the oil and the sugar and stir until the sugar has melted completely. " +
                              "Whisk in the chocolate and the cocoa, then the eggs, then fold in the flour. Divide it " +
                              "between the tins and bake for forty minutes, then leave to cool before you ice the cake.";

        var recipe = Read("<h1>Cake</h1><p>Serves 6-8</p><p>1 litre water</p><p>240ml oil</p><p>450g sugar</p><p>4 eggs</p>" +
                          $"<p>{method}</p><p>TIP Use a good cocoa.</p>");

        Assert.That(Steps(recipe!), Is.EqualTo(new[] { method }));
    }

    /// <summary>What only looks like a run of ingredients is nothing: no guess is ever served for good.</summary>
    [TestCase("<h1>Pie</h1><p>1 egg</p><p>2 cups flour</p><p>Mix it.</p><p>Bake it.</p>", TestName = "too few lines")]
    [TestCase("<h1>Pie</h1><p>1 egg</p><p>2 cups flour</p><p>1 onion</p><p>3 leeks</p>", TestName = "no steps")]
    [TestCase("<h1>Pie</h1><p>1 egg</p><p>2 cups flour</p><p>1 onion</p><p>3 leeks</p><p>Mix it.</p>", TestName = "one step")]
    [TestCase("<h1>Pie</h1><p>Eggs</p><p>Flour</p><p>2 onions</p><p>Salt</p><p>Butter</p><p>Mix it.</p><p>Bake it.</p>", TestName = "few amounts")]
    [TestCase("<h1>Gravy</h1><p>Energy 100 kJ</p><p>Protein 2 g</p><p>Fat 1 g</p><p>Sugar 3 g</p><p>Sodium 40 mg</p>" +
              "<p>Keep it cool.</p><p>Shake it well.</p>", TestName = "a product's nutrition is no recipe")]
    [TestCase("<h1>News</h1><p>We met in 2011 and love it.</p><p>It was a long drive of 400km to the farm and back.</p>", TestName = "a story")]
    public void WhatOnlyLooksLikeARun_IsNothing(string main) => Assert.That(Read(main), Is.Null);
}
