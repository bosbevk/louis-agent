using LouisAgent.Packs.Recipes.Recipes;
using LouisAgent.Packs.Recipes.Recipes.Tidying;
using LouisAgent.Packs.Recipes.Scaling;
using LouisAgent.Packs.Recipes.Tools;

namespace LouisAgent.Packs.Recipes.Tests.Recipes.Tidying;

/// <summary>
/// A page's recipe as a cook can follow it, line by line. Taste keeps a whole method in one step, and the answers that
/// dropped a clause dropped it from those; the lines here are the ones the library really holds.
/// </summary>
public class RecipeTidyTests
{
    /// <summary>A recipe with these steps and a line of ingredients.</summary>
    private static Recipe WithSteps(params string[] steps) => new("Pie", null, null, null, null,
        [new RecipeIngredient(null, "1 onion")], [.. steps.Select(step => new RecipeStep(null, step))], null, null,
        new Uri("https://example.com/pie"));

    /// <summary>A recipe with these ingredients and a step.</summary>
    private static Recipe WithIngredients(params RecipeIngredient[] lines) => new("Pie", null, null, null, null,
        lines, [new RecipeStep(null, "Cook it.")], null, null, new Uri("https://example.com/pie"));

    /// <summary>The steps of the recipe tidied, as text.</summary>
    private static string[] StepsOf(params string[] steps) =>
        [.. RecipeTidy.Of(WithSteps(steps)).Steps.Select(step => step.Text)];

    /// <summary>A method written as one numbered blob becomes its steps, without their numbers.</summary>
    [TestCase("1. Heat the pan.2. Mix the sambal.3. Serve it.", "Heat the pan.", "Mix the sambal.", "Serve it.")]
    [TestCase("1. Heat the pan. 2. Mix the sambal. 3. Serve it.", "Heat the pan.", "Mix the sambal.", "Serve it.")]
    [TestCase("1 To make it, stir.2 Once soft, add salt.3 Add the lamb.", "To make it, stir.", "Once soft, add salt.",
        "Add the lamb.")]
    [TestCase("1. Whip the cream (for 3 minutes).2. Fold it in.", "Whip the cream (for 3 minutes).", "Fold it in.")]
    public void ANumberedBlob_BecomesItsSteps(string blob, params string[] steps) =>
        Assert.That(StepsOf(blob), Is.EqualTo(steps));

    /// <summary>A number that isn't the next step stays where it is: an amount, a size, a time.</summary>
    [TestCase("1. Add 2 cups flour. 3 Eggs go in last.")]
    [TestCase("Bake at 180 C for 2 hours. 3 Times a week is plenty.")]
    public void ANumberThatIsntTheNextStep_Stays(string step) =>
        Assert.That(StepsOf(step), Is.EqualTo(new[] { step }));

    /// <summary>Paragraphs glued with nothing between them come apart, and a decimal doesn't.</summary>
    [Test]
    public void GluedParagraphs_ComeApart()
    {
        Assert.That(StepsOf("Preheat the oven to 180 C.Thinly slice the apples."),
            Is.EqualTo(new[] { "Preheat the oven to 180 C.", "Thinly slice the apples." }));
        Assert.That(StepsOf("Add 2.5 cups of flour."), Is.EqualTo(new[] { "Add 2.5 cups of flour." }));
    }

    /// <summary>A long paragraph is cut at its sentences into parts of a few, with every word kept.</summary>
    [Test]
    public void ALongParagraph_IsCutAtItsSentences_WithEveryWordKept()
    {
        string sentence = "Stir the mixture over a low heat until the sugar has dissolved and it begins to thicken. ";
        string paragraph = string.Concat(Enumerable.Repeat(sentence, 12)).Trim();

        string[] parts = StepsOf(paragraph);

        Assert.That(parts, Has.Length.GreaterThan(1));
        Assert.That(parts, Has.All.Length.LessThanOrEqualTo(400));
        Assert.That(string.Join(' ', parts), Is.EqualTo(paragraph));
    }

    /// <summary>A step of ordinary length stays as the page wrote it.</summary>
    [Test]
    public void AnOrdinaryStep_Stays() =>
        Assert.That(StepsOf("Fry the onion. Add the mince. Season well."),
            Is.EqualTo(new[] { "Fry the onion. Add the mince. Season well." }));

    /// <summary>The parts of a step keep its section.</summary>
    [Test]
    public void ThePartsOfAStep_KeepItsSection()
    {
        var recipe = WithSteps("x") with { Steps = [new RecipeStep("Sauce", "1. Stir it.2. Pour it.")] };

        Assert.That(RecipeTidy.Of(recipe).Steps, Is.EqualTo(new[]
        {
            new RecipeStep("Sauce", "Stir it."), new RecipeStep("Sauce", "Pour it.")
        }));
    }

    /// <summary>A heading among the lines becomes the section of the lines below it, not a line to buy.</summary>
    [TestCase("For the sauce:", "For the sauce")]
    [TestCase("SYRUP", "SYRUP")]
    [TestCase("FOR THE SAUCE", "FOR THE SAUCE")]
    [TestCase("FILLING</h3.", "FILLING")]
    [TestCase("For the sprout salad, toss:", "For the sprout salad, toss")]
    public void AHeadingAmongTheLines_BecomesASection(string line, string section)
    {
        var recipe = WithIngredients(new RecipeIngredient(null, "1 onion"), new RecipeIngredient(null, line),
            new RecipeIngredient(null, "2 cups milk"));

        Assert.That(RecipeTidy.Of(recipe).Ingredients, Is.EqualTo(new[]
        {
            new RecipeIngredient(null, "1 onion"), new RecipeIngredient(section, "2 cups milk")
        }));
    }

    /// <summary>A page's own section starts afresh, so a heading doesn't run on past it.</summary>
    [Test]
    public void APagesOwnSection_EndsAHeading()
    {
        var nuts = new RecipeIngredient("Topping", "1 cup nuts");
        var recipe = WithIngredients(new RecipeIngredient(null, "For the sauce:"), new RecipeIngredient(null, "1 cup milk"),
            nuts);

        Assert.That(RecipeTidy.Of(recipe).Ingredients.Last(), Is.EqualTo(nuts));
    }

    /// <summary>What a page's buttons and dates left among the lines goes, and a sentence ending in a colon.</summary>
    [TestCase("Pin dit")]
    [TestCase("Pin it")]
    [TestCase("11/02/2020")]
    [TestCase("The instructions for making carrot cake are fairly simple to follow. Here they are:")]
    public void WhatAPageLeftAmongTheLines_Goes(string line)
    {
        var recipe = WithIngredients(new RecipeIngredient(null, line), new RecipeIngredient(null, "1 onion"));

        Assert.That(RecipeTidy.Of(recipe).Ingredients, Is.EqualTo(new[] { new RecipeIngredient(null, "1 onion") }));
    }

    /// <summary>A page that writes every line in capitals has ingredients, not headings.</summary>
    [Test]
    public void APageAllInCapitals_KeepsItsLines()
    {
        var lines = new[]
        {
            new RecipeIngredient(null, "SALT"), new RecipeIngredient(null, "PEPPER"), new RecipeIngredient(null, "OIL")
        };

        Assert.That(RecipeTidy.Of(WithIngredients(lines)).Ingredients, Is.EqualTo(lines));
    }

    /// <summary>The print and the scaler number the same steps: the scaler's "Step 2" is the print's line 2.</summary>
    [Test]
    public void ThePrintAndTheScaler_NumberTheSameSteps()
    {
        var recipe = WithSteps("1. Heat the pan.2. Add 1 cup water.") with { Yield = "2 servings" };

        Assert.That(RecipePrinter.Print(recipe), Does.Contain("2. Add 1 cup water."));
        Assert.That(RecipeScaler.Print(recipe, 4, 0), Does.Contain("Step 2: 1 cup becomes 2 cup"));
    }

    /// <summary>Tidying what's tidy changes nothing, as the print and the scaler both tidy.</summary>
    [Test]
    public void TidyingTwice_IsTidyingOnce()
    {
        var recipe = WithSteps("1. Heat the pan.2. Mix it.") with
        {
            Ingredients = [new RecipeIngredient(null, "SYRUP"), new RecipeIngredient(null, "1 cup sugar")]
        };

        var once = RecipeTidy.Of(recipe);
        var twice = RecipeTidy.Of(once);

        Assert.That(twice.Steps, Is.EqualTo(once.Steps));
        Assert.That(twice.Ingredients, Is.EqualTo(once.Ingredients));
    }
}
