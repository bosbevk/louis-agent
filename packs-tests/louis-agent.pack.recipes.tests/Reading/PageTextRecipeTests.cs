using LouisAgent.Packs.Recipes.Reading;
using LouisAgent.Packs.Recipes.Recipes;

namespace LouisAgent.Packs.Recipes.Tests.Reading;

/// <summary>
/// A page with no recipe data can still lay its recipe out plainly, under headings. These follow the shapes of real
/// sites, with the words made up: a whole recipe comes out, line by line, and anything doubtful comes out as nothing,
/// because what's kept is served for good.
/// </summary>
public class PageTextRecipeTests
{
    private static readonly Uri Page = new("https://example.com/recipes/pie/");

    /// <summary>A page whose main part is this HTML.</summary>
    private static Recipe? Read(string main, string title = "Pie | A Site") =>
        PageTextRecipe.Read($"<html><head><title>{title}</title></head><body><main>{main}</main></body></html>", Page);

    /// <summary>The texts of these lines.</summary>
    private static IEnumerable<string> Texts(IEnumerable<RecipeIngredient> lines) => lines.Select(line => line.Text);

    /// <summary>The texts of these lines.</summary>
    private static IEnumerable<string> Texts(IEnumerable<RecipeStep> lines) => lines.Select(line => line.Text);

    /// <summary>Headings over plain lines, as Zola Nene's pages have them, under menu headings.</summary>
    [Test]
    public void PlainLines_UnderHeadings_AreARecipe()
    {
        var recipe = Read("<h2>Chakalaka</h2><h4>Pasta</h4><h4>Salad</h4><h3>Ingredients</h3><p>Serves 4</p>" +
                          "<p>2 Tbs oil</p><p>1 onion, diced</p><h3>Instructions</h3><p>Heat the oil.</p><p>Fry the onion.</p>");

        Assert.That(recipe, Is.Not.Null);
        Assert.That(recipe!.Name, Is.EqualTo("Chakalaka"));
        Assert.That(recipe.Yield, Does.Contain("4"));
        Assert.That(Texts(recipe.Ingredients), Is.EqualTo(new[] { "2 Tbs oil", "1 onion, diced" }));
        Assert.That(Texts(recipe.Steps), Is.EqualTo(new[] { "Heat the oil.", "Fry the onion." }));
        Assert.That(recipe.Source, Is.EqualTo(Page));
    }

    /// <summary>
    /// Lists with a part of their own, as Sarah Graham's have them: the times aren't an ingredient, a "For the sauce"
    /// line names a part, and the numbers aren't part of a step. Words after the list aren't steps.
    /// </summary>
    [Test]
    public void Lists_WithParts_AreARecipe()
    {
        var recipe = Read("<h1>Vetkoek</h1><h2>Ingredients</h2><p>Cooks in:15-20 minutes</p><p>Serves:20 vetkoekies</p>" +
                          "<ul><li>2 cups flour</li><li>1 cup milk</li></ul><p>For the Sauce</p><ul><li>1 cup cream</li></ul>" +
                          "<h2>What to do</h2><ol><li>1. Mix it.</li><li>2. Fry it.</li></ol><p>Photo by a friend.</p>");

        Assert.That(recipe, Is.Not.Null);
        Assert.That(recipe!.Name, Is.EqualTo("Vetkoek"));
        Assert.That(recipe.Yield, Is.EqualTo("20 vetkoekies"));
        Assert.That(Texts(recipe.Ingredients), Is.EqualTo(new[] { "2 cups flour", "1 cup milk", "1 cup cream" }));
        Assert.That(recipe.Ingredients.Last().Section, Is.EqualTo("For the Sauce"));
        Assert.That(recipe.Ingredients.First().Section, Is.Null);
        Assert.That(Texts(recipe.Steps), Is.EqualTo(new[] { "Mix it.", "Fry it." }));
    }

    /// <summary>FinGlobal's headings name their part, and its steps run on into a cookie notice and an ad.</summary>
    [Test]
    public void HeadingsThatNameAPart_AndSiteTextAfterTheSteps()
    {
        var recipe = Read("<h1>Koeksisters</h1><p>Ingredients for syrup</p><ul><li>4 cups sugar</li></ul>" +
                          "<p>Ingredients for dough</p><ul><li>4 cups flour</li></ul><p>What to do</p>" +
                          "<ul><li>Boil the syrup.</li><li>Fry the dough.</li></ul>" +
                          "<p>Please accept cookies to access this content</p><h3>Our services</h3><p>Call us about your emigration.</p>");

        Assert.That(recipe, Is.Not.Null);
        Assert.That(recipe!.Ingredients.Select(line => line.Section), Is.EqualTo(new[] { "syrup", "dough" }));
        Assert.That(Texts(recipe.Steps), Is.EqualTo(new[] { "Boil the syrup.", "Fry the dough." }));
    }

    /// <summary>
    /// FinGlobal's stew: its list of steps starts with a line naming a part, and that mustn't make it read as plain
    /// lines, or the cookie notice after the list becomes a step that's served for good.
    /// </summary>
    [Test]
    public void PartsAmongTheSteps_NameTheirSteps_AndSiteTextStaysOut()
    {
        var recipe = Read("<h1>Stew</h1><p>Ingredients</p><ul><li>1 kg beef</li><li>2 onions</li></ul><p>Method</p>" +
                          "<p>Step 1: Prepare your stew</p><ul><li>Brown the beef.</li></ul>" +
                          "<p>Step 2: Make the dumplings</p><ul><li>Mix the flour.</li></ul>" +
                          "<p>Please accept cookies to access this content</p><h3>Tips</h3>");

        Assert.That(recipe, Is.Not.Null);
        Assert.That(Texts(recipe!.Steps), Is.EqualTo(new[] { "Brown the beef.", "Mix the flour." }));
        Assert.That(recipe.Steps.Select(step => step.Section),
            Is.EqualTo(new[] { "Step 1: Prepare your stew", "Step 2: Make the dumplings" }));
    }

    /// <summary>
    /// A long line between steps could be anything, and stopping there would keep half a recipe as if it were whole, so
    /// the page stays out.
    /// </summary>
    [Test]
    public void ALongLineBetweenTheSteps_IsNothing()
    {
        string notice = "Please accept cookies to access this content, which we show on every page of the site";

        Assert.That(Read("<h1>Stew</h1><p>Ingredients</p><ul><li>1 kg beef</li><li>2 onions</li></ul><p>Method</p>" +
                         $"<ul><li>Brown the beef.</li></ul><p>{notice}</p><ul><li>Mix the flour.</li></ul>"), Is.Null);
    }

    /// <summary>Vrouekeur in Afrikaans: portions first, a bullet and bare numbers, then an author's link.</summary>
    [Test]
    public void AnAfrikaansPage_IsARecipe_AndAnAuthorsLinkIsNoStep()
    {
        var recipe = Read("<h1>Worsrolle</h1><p>• 6 porsies</p><p>Bestanddele</p><p>600 g wors</p><p>60 ml blatjang</p>" +
                          "<p>Metode</p><p>• Stel oond op 220 °C.</p><p>1 Draai die wors.</p><p>2 Bak dit.</p>" +
                          "<p><a href=\"https://example.com/author\">Die skrywer</a></p>");

        Assert.That(recipe, Is.Not.Null);
        Assert.That(recipe!.Yield, Does.Contain("6"));
        Assert.That(Texts(recipe.Ingredients), Is.EqualTo(new[] { "600 g wors", "60 ml blatjang" }));
        Assert.That(Texts(recipe.Steps), Is.EqualTo(new[] { "Stel oond op 220 °C.", "Draai die wors.", "Bak dit." }));
    }

    /// <summary>
    /// Vrouekeur's bobotie: a short line before the list is the first step and not a part, or the oven would vanish
    /// from a recipe that looks whole. A "read also" link isn't an ingredient, and a line in capitals names a part.
    /// </summary>
    [Test]
    public void AFirstStepBeforeTheList_StaysAStep_AndAReadAlsoLineIsNoIngredient()
    {
        var recipe = Read("<h1>Bobotie</h1><h2>Bestanddele</h2><p>2 uie</p><p>800 g maalvleis</p><p>EIERBOLAAG</p><p>2 eiers</p>" +
                          "<p>LEES OOK: <a href=\"https://example.com/x\">Iets anders</a></p><h2>Metode</h2><p>Stel oond op 180 °C.</p>" +
                          "<ul><li>Braai die uie.</li><li>Bak dit.</li></ul>");

        Assert.That(recipe, Is.Not.Null);
        Assert.That(Texts(recipe!.Ingredients), Is.EqualTo(new[] { "2 uie", "800 g maalvleis", "2 eiers" }));
        Assert.That(recipe.Ingredients.Last().Section, Is.EqualTo("EIERBOLAAG"));
        Assert.That(Texts(recipe.Steps), Is.EqualTo(new[] { "Stel oond op 180 °C.", "Braai die uie.", "Bak dit." }));
    }

    /// <summary>A short line in the middle of the steps could be anything, and guessing could lose a step.</summary>
    [Test]
    public void AShortLineInTheMiddleOfTheSteps_IsNothing()
    {
        Assert.That(Read("<h1>Pie</h1><h3>Ingredients</h3><ul><li>1 egg</li><li>1 cup flour</li></ul><h3>Method</h3>" +
                         "<ul><li>Mix it.</li></ul><p>Then rest it.</p><ul><li>Bake it.</li></ul>"), Is.Null);
    }

    /// <summary>Sarah Graham's sambal: an instruction in the list shows it isn't plain, so the page's out.</summary>
    [TestCase("Mix together and check for seasoning")]
    [TestCase("Meng alles saam")]
    public void AnInstructionAmongTheIngredients_IsNothing(string line)
    {
        Assert.That(Read($"<h1>Pie</h1><h3>Ingredients</h3><ul><li>1 egg</li><li>1 cup flour</li><li>{line}</li></ul><h3>Method</h3><ul><li>Bake it.</li></ul>"), Is.Null);
    }

    /// <summary>An ingredient that only starts like an instruction stays, by its other word or its amount.</summary>
    [TestCase("Mixed herbs, a handful")]
    [TestCase("Add 2 eggs if the mix is dry")]
    public void AnIngredientThatOnlyLooksLikeOne_Stays(string line)
    {
        var recipe = Read($"<h1>Pie</h1><h3>Ingredients</h3><ul><li>1 egg</li><li>{line}</li></ul><h3>Method</h3><ul><li>Bake it.</li></ul>");

        Assert.That(recipe, Is.Not.Null);
    }

    /// <summary>With no heading, as on Rhodes's pages, the page's title names it, without the site's name.</summary>
    [Test]
    public void WithNoHeadingForTheName_ThePagesTitleNamesIt()
    {
        var recipe = Read("<h3>Ingredients</h3><ul><li>500 g wors</li><li>6 rolls</li></ul><h3>Method</h3><ul><li>Braai it.</li></ul>",
            "Gourmet Rolls | Rhodes Food Group");

        Assert.That(recipe?.Name, Is.EqualTo("Gourmet Rolls"));
    }

    /// <summary>A number that starts a step's own sentence stays: only a counted list's numbers come off.</summary>
    [Test]
    public void ANumberThatIsPartOfAStep_Stays()
    {
        var recipe = Read("<h1>Pie</h1><h3>Ingredients</h3><p>1 egg</p><p>1 cup flour</p><h3>Method</h3><p>10 minutes before baking, mix it.</p>");

        Assert.That(Texts(recipe!.Steps), Is.EqualTo(new[] { "10 minutes before baking, mix it." }));
    }

    /// <summary>A part after the steps isn't a step.</summary>
    [Test]
    public void AHeadingAfterTheSteps_EndsThem()
    {
        var recipe = Read("<h1>Pie</h1><h3>Ingredients</h3><p>1 egg</p><p>1 cup flour</p><h3>Method</h3><p>Mix it.</p><h3>Notes</h3><p>Lovely.</p>");

        Assert.That(Texts(recipe!.Steps), Is.EqualTo(new[] { "Mix it." }));
    }

    /// <summary>What isn't plainly a whole recipe is nothing: no guess is ever served for good.</summary>
    [TestCase("<h1>Pie</h1><p>1 egg</p><p>1 cup flour</p><p>Mix it.</p>", TestName = "no headings")]
    [TestCase("<h1>Pie</h1><h3>Ingredients</h3><p>1 egg</p><p>1 cup flour</p>", TestName = "no method")]
    [TestCase("<h1>Pie</h1><h3>Method</h3><p>Mix it.</p><h3>Ingredients</h3><p>1 egg</p><p>1 cup flour</p>", TestName = "method first")]
    [TestCase("<h1>Pie</h1><h3>Ingredients</h3><p>1 egg</p><h3>Method</h3><p>Mix it.</p>", TestName = "one ingredient")]
    [TestCase("<h1>Pie</h1><h3>Ingredients</h3><p>1 egg</p><p>1 cup flour</p><h3>Method</h3>", TestName = "no steps")]
    [TestCase("<h1>Pie</h1><h3>Ingredients</h3><p>1 egg</p><p><a href=\"https://example.com/x\">1 cup flour</a></p><h3>Method</h3><p>Mix it.</p>", TestName = "a link among the ingredients")]
    public void WhatIsNotPlainlyARecipe_IsNothing(string main) => Assert.That(Read(main), Is.Null);

    /// <summary>A long story under an "Ingredients" heading is an article about them, not a list of them.</summary>
    [Test]
    public void AStoryUnderTheHeading_IsNothing()
    {
        string story = string.Concat(Enumerable.Repeat("The best thing about this dish is its long history. ", 8));

        Assert.That(Read($"<h1>Pie</h1><h3>Ingredients</h3><p>1 egg</p><p>{story}</p><h3>Method</h3><p>Mix it.</p>"), Is.Null);
    }

    /// <summary>With no heading and no title there's no name, and a recipe needs one.</summary>
    [Test]
    public void WithNoName_ItIsNothing() =>
        Assert.That(PageTextRecipe.Read("<html><body><main><h3>Ingredients</h3><p>1 egg</p><p>1 cup flour</p><h3>Method</h3><p>Mix it.</p></main></body></html>", Page), Is.Null);
}
