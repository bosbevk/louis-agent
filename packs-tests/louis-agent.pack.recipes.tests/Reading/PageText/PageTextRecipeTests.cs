using LouisAgent.Packs.Recipes.Reading.PageText;
using LouisAgent.Packs.Recipes.Recipes;

namespace LouisAgent.Packs.Recipes.Tests.Reading.PageText;

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

    /// <summary>
    /// Cooksister's bobotie: an ingredient can carry a long note, a second recipe on the page ends the first one's
    /// steps with its name staying out, and the site's sharing buttons are no steps.
    /// </summary>
    [Test]
    public void ALongNote_AndASecondRecipeOnThePage_LeaveTheFirstWhole()
    {
        string note = "6 lemon leaves (or 3 bay leaves as a poor substitute) -you could grate a bit of the rind of the lemon " +
                      "before you squeeze it for the juice and add it to the bay leaves but add carefully so as not to allow " +
                      "the lemon to dominate";
        var recipe = Read($"<h1>Bobotie revisited</h1><p>Ingredients:</p><p>1kg mince</p><p>{note}</p><p>Method:</p>" +
                          "<p>Fry the onion in oil till golden.</p><p>Bake it uncovered.</p><p>YELLOW RICE Serves 4</p>" +
                          "<p>Ingredients:</p><p>2 cups rice</p><p>Method:</p><p>Cook the rice.</p>");

        Assert.That(recipe, Is.Not.Null);
        Assert.That(Texts(recipe!.Ingredients), Is.EqualTo(new[] { "1kg mince", note }));
        Assert.That(Texts(recipe.Steps), Is.EqualTo(new[] { "Fry the onion in oil till golden.", "Bake it uncovered." }));
    }

    /// <summary>A plain list of steps ends at the page's sharing buttons, which start with a lone dash.</summary>
    [Test]
    public void SharingButtonsAfterPlainSteps_AreNoSteps()
    {
        var recipe = Read("<h1>Pie</h1><p>Ingredients:</p><p>1 egg</p><p>1 cup flour</p><p>Method:</p><p>Mix it well.</p><p>Bake it.</p>" +
                          "<p>-</p><p>Share</p><p>-</p><p>Pin</p>");

        Assert.That(Texts(recipe!.Steps), Is.EqualTo(new[] { "Mix it well.", "Bake it." }));
    }

    /// <summary>Woolworths: its own wording for the method, with its site's note between the lists.</summary>
    [Test]
    public void CookingInstructions_IsAMethod()
    {
        var recipe = Read("<h1>Pie</h1><h3>Ingredients</h3><ul><li>1 egg</li><li>2 cups flour</li></ul><p>We are committed to a good business journey.</p>" +
                          "<h3>Cooking Instructions</h3><ol><li>1. Mix it</li><li>2. Bake it</li></ol><p>You could enjoy this too</p>");

        Assert.That(Texts(recipe!.Ingredients), Is.EqualTo(new[] { "1 egg", "2 cups flour" }));
        Assert.That(Texts(recipe.Steps), Is.EqualTo(new[] { "Mix it", "Bake it" }));
    }

    /// <summary>Rooi Rose: a short line ending in a colon names a part, and a long first line is a step.</summary>
    [Test]
    public void AColonNamesAPart_AndALongFirstLineIsAStep()
    {
        var recipe = Read("<h1>Melktert</h1><h4>Bestanddele</h4><p>Deeg:</p><ul><li>500 ml meel</li></ul><p>Vulsel:</p><ul><li>3 eiers</li></ul>" +
                          "<h4>Metode</h4><p>Voorverhit die oond tot 180 °C. Spuit 'n pan van 20 cm met kleefwerende kossproei.</p><ul><li>Sif die meel.</li><li>Bak dit.</li></ul>" +
                          "<p><a href=\"https://example.com/tag\">melktert</a></p>");

        Assert.That(recipe!.Ingredients.Select(line => line.Section), Is.EqualTo(new[] { "Deeg", "Vulsel" }));
        Assert.That(recipe.Steps, Has.Count.EqualTo(3));
        Assert.That(recipe.Steps[0].Text, Does.StartWith("Voorverhit"));
    }

    /// <summary>Food Lover's Market: tab labels above the real headings hold nothing, so they're no list.</summary>
    [Test]
    public void TabLabelsAboveTheHeadings_AreSkipped()
    {
        var recipe = Read("<h1>Bobotie</h1><p>Ingredients</p><p>Directions</p><p>Ingredients</p><h4>Bobotie</h4><p>1 onion</p><p>500g mince</p>" +
                          "<p>Custard topping:</p><p>2 eggs</p><p>Directions</p><p>Fry the onion.</p><p>Bake it.</p>");

        Assert.That(Texts(recipe!.Ingredients), Is.EqualTo(new[] { "1 onion", "500g mince", "2 eggs" }));
        Assert.That(recipe.Ingredients[2].Section, Is.EqualTo("Custard topping"));
        Assert.That(Texts(recipe.Steps), Is.EqualTo(new[] { "Fry the onion.", "Bake it." }));
    }

    /// <summary>Clover: a short plain line among numbered steps names a part, wherever it stands.</summary>
    [Test]
    public void AShortNameAmongTheSteps_NamesAPart()
    {
        var recipe = Read("<h1>Cups</h1><h3>INGREDIENTS</h3><p>Bobotie</p><p>• 1 onion</p><p>• 400 g mince</p><h3>Method</h3><p>Bobotie</p><p>1. Fry it.</p>" +
                          "<p>Pancake Cups</p><p>2. Line the pan.</p><p>3. Bake it.</p>");

        Assert.That(Texts(recipe!.Steps), Is.EqualTo(new[] { "Fry it.", "Line the pan.", "Bake it." }));
        Assert.That(recipe.Steps.Select(step => step.Section), Is.EqualTo(new[] { "Bobotie", "Pancake Cups", "Pancake Cups" }));
    }

    /// <summary>A first line with an amount or an instruction's word is a step, even with no full stop.</summary>
    [TestCase("Stel oond op 180 °C")]
    [TestCase("Preheat the oven")]
    public void AnOvenLineBeforeTheList_StaysAStep_WithNoFullStop(string first)
    {
        var recipe = Read($"<h1>Pie</h1><h3>Ingredients</h3><ul><li>1 egg</li><li>1 cup flour</li></ul><h3>Method</h3><p>{first}</p><ul><li>Mix it.</li></ul>");

        Assert.That(Texts(recipe!.Steps), Is.EqualTo(new[] { first, "Mix it." }));
    }

    /// <summary>FinGlobal: headings written as phrases, and each list's opening sentence isn't part of it.</summary>
    [Test]
    public void HeadingsAsPhrases_AndAListsOpeningLine()
    {
        var recipe = Read("<h1>Bobotie</h1><h3>Ingredients needed for chicken bobotie</h3><p>To prepare this easy recipe, gather the following ingredients:</p><ul><li>500g mince</li><li>1 onion</li></ul>" +
                          "<h3>What are the steps for this bobotie recipe?</h3><p>Follow these simple instructions to recreate this beloved bobotie:</p><ul><li>Fry it.</li><li>Bake it.</li></ul><h3>Frequently asked questions</h3><ul><li>Taste: mild.</li></ul>");

        Assert.That(Texts(recipe!.Ingredients), Is.EqualTo(new[] { "500g mince", "1 onion" }));
        Assert.That(Texts(recipe.Steps), Is.EqualTo(new[] { "Fry it.", "Bake it." }));
    }

    /// <summary>
    /// Inspired Living: an ingredient in a list may link to another recipe and keeps its words, and a heading in the
    /// method that names a part of the ingredients doesn't end the steps, or half the recipe would go.
    /// </summary>
    [Test]
    public void ALinkedIngredient_AndAMethodHeadingForAKnownPart()
    {
        var recipe = Read("<h1>Chicken</h1><h3>Ingredients:</h3><ul><li>6 chicken breasts</li><li>1 tsp <a href=\"https://example.com/p\">Coriander Pesto</a></li></ul><h3>Oven Roasted Potatoes:</h3><ul><li>8 potatoes</li></ul>" +
                          "<h3>Method</h3><p>Coat the chicken.</p><h4>Oven Roasted Potatoes</h4><p>Roast the potatoes.</p><p>Images: ©2018 the site.</p><h3>Disclosure</h3>");

        Assert.That(Texts(recipe!.Ingredients), Is.EqualTo(new[] { "6 chicken breasts", "1 tsp Coriander Pesto", "8 potatoes" }));
        Assert.That(Texts(recipe.Steps), Is.EqualTo(new[] { "Coat the chicken.", "Roast the potatoes." }));
        Assert.That(recipe.Steps[1].Section, Is.EqualTo("Oven Roasted Potatoes"));
    }

    /// <summary>Zola Nene's kebabs: a "Serves" among plain steps means a second recipe, so the page's out.</summary>
    [Test]
    public void AServesLineAmongPlainSteps_IsNothing()
    {
        Assert.That(Read("<h1>Kebabs</h1><h3>Ingredients</h3><p>Serves 4</p><p>1 chicken</p><p>2 apricots</p><h3>Instructions</h3><p>Soak the sticks.</p>" +
                         "<p>Cook them.</p><p>Serves 4</p><p>For the Kebabs:</p><p>500g mince</p>"), Is.Null);
    }

    /// <summary>Clover and others: in plain lines, a part's name or a label in capitals isn't a step.</summary>
    [Test]
    public void InPlainSteps_APartsNameIsNoStep_AndALabelIsLeftOut()
    {
        var recipe = Read("<h1>Tart</h1><h3>Ingredients</h3><p>1 egg</p><p>1 cup flour</p><h3>Method</h3><p>For the pastry:</p><p>Rub in the butter.</p>" +
                          "<p>CRUST:</p><p>Bake it blind.</p><p>CLOVER INGREDIENTS USED IN THIS RECIPE</p>");

        Assert.That(Texts(recipe!.Steps), Is.EqualTo(new[] { "Rub in the butter.", "Bake it blind." }));
        Assert.That(recipe.Steps.Select(step => step.Section), Is.EqualTo(new[] { "For the pastry", "CRUST" }));
    }

    /// <summary>Lucky Star: a list item that's only a label names a part, and isn't a step.</summary>
    [Test]
    public void AListItemThatIsOnlyALabel_NamesAPart()
    {
        var recipe = Read("<h1>Tart</h1><h3>Ingredients</h3><ul><li>1 egg</li><li>1 cup flour</li></ul><h3>Method</h3><ul><li>CRUST:</li><li>Rub it in.</li><li>Bake it.</li></ul>");

        Assert.That(Texts(recipe!.Steps), Is.EqualTo(new[] { "Rub it in.", "Bake it." }));
        Assert.That(recipe.Steps[0].Section, Is.EqualTo("CRUST"));
    }

    /// <summary>A list that starts its count again in each part keeps no number in its steps.</summary>
    [Test]
    public void ACountThatStartsAgain_LeavesNoNumberInTheSteps()
    {
        var recipe = Read("<h1>Rolls</h1><h3>Ingredients</h3><ul><li>1 egg</li><li>1 cup flour</li></ul><h3>Method</h3><p>Dough</p><ol><li>1. Mix it.</li><li>2. Roll it.</li></ol>" +
                          "<p>Filling</p><ol><li>1. Fry it.</li><li>2. Bake it.</li></ol>");

        Assert.That(Texts(recipe!.Steps), Is.EqualTo(new[] { "Mix it.", "Roll it.", "Fry it.", "Bake it." }));
    }

    /// <summary>A heading such as "Recipes" names no dish: the next heading does, or the page's title.</summary>
    [TestCase("<h1>Recipes</h1><h2>Bobotie Bake</h2>", "Bobotie Bake")]
    [TestCase("<h1>Recipes</h1>", "Pie")]
    public void AHeadingThatNamesNoDish_IsSkipped(string headings, string name)
    {
        var recipe = Read($"{headings}<h3>Ingredients</h3><ul><li>1 egg</li><li>1 cup flour</li></ul><h3>Method</h3><ul><li>Bake it.</li></ul>");

        Assert.That(recipe?.Name, Is.EqualTo(name));
    }

    /// <summary>A real page wrote an attribute twice on one tag: the page still reads.</summary>
    [Test]
    public void ATagWithAnAttributeTwice_StillReads()
    {
        var recipe = PageTextRecipe.Read("<html><head><title>Pie</title><meta property=\"og:image\" property=\"og:image\" content=\"/a.jpg\"></head>" +
                                         "<body><main><img alt=\"Pie\" fetchpriority=\"high\" fetchpriority=\"low\" src=\"/b.jpg\"><h3>Ingredients</h3>" +
                                         "<p>1 egg</p><p>1 cup flour</p><h3>Method</h3><p>Mix it.</p></main></body></html>", Page);

        Assert.That(recipe?.Name, Is.EqualTo("Pie"));
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
