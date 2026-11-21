using LouisAgent.Packs.Recipes.Reading.PageText;
using LouisAgent.Packs.Recipes.Recipes;

namespace LouisAgent.Packs.Recipes.Tests.Reading.PageText;

/// <summary>
/// A method told as numbered parts with plain sentences under each, and no list, as FinGlobal's vetkoek pages have it.
/// The shapes are real pages', with the words made up.
/// </summary>
public class PageTextStepPartsTests
{
    private static readonly Uri Page = new("https://example.com/recipes/vetkoek/");

    /// <summary>A page whose main part is this HTML.</summary>
    private static Recipe? Read(string main) =>
        PageTextRecipe.Read($"<html><head><title>Vetkoek | A Site</title></head><body><main>{main}</main></body></html>", Page);

    /// <summary>Each step with the part it sits under.</summary>
    private static IEnumerable<(string?, string)> Steps(Recipe recipe) =>
        recipe.Steps.Select(step => (step.Section, step.Text));

    /// <summary>
    /// An ingredients heading and no method heading, so "Step 1" starts the method. What's between the list and it
    /// isn't an ingredient, and the cookie notice after the last step isn't a step.
    /// </summary>
    [Test]
    public void StepOne_StartsTheMethod_WhenNoHeadingDoes()
    {
        var recipe = Read("<h1>Vetkoek</h1><h3>Ingredients for Vetkoek</h3><p>This recipe makes about ten.</p>" +
                          "<ul><li>1 cup mieliemeel</li><li>2 cups flour</li></ul><p>Optional Fillings</p><ul><li>Cheese</li></ul>" +
                          "<p>Simple guide to making Vetkoek</p><p>Step 1: Combine the dry ingredients</p><p>Mix it in a bowl.</p>" +
                          "<p>Step 2: Fry the vetkoek</p><p>Heat the oil.</p><p>Fry until golden.</p>" +
                          "<p>Please accept cookies to access this content</p>");

        Assert.That(recipe, Is.Not.Null);
        Assert.That(recipe!.Ingredients.Select(line => line.Text),
            Is.EqualTo(new[] { "1 cup mieliemeel", "2 cups flour", "Cheese" }));
        Assert.That(Steps(recipe), Is.EqualTo(new (string?, string)[]
        {
            ("Step 1: Combine the dry ingredients", "Mix it in a bowl."), ("Step 2: Fry the vetkoek", "Heat the oil."),
            ("Step 2: Fry the vetkoek", "Fry until golden.")
        }));
    }

    /// <summary>
    /// "Vetkoek ingredients" and "How to make" as headings, "Here's what you'll need" naming no part, and the opening
    /// sentence before "Step 1" isn't a step.
    /// </summary>
    [Test]
    public void TheDishsOwnHeadings_AreTheIngredientsAndTheMethod()
    {
        var recipe = Read("<h1>Vetkoek</h1><h3>Vetkoek ingredients</h3><p>It calls for pantry basics.</p>" +
                          "<p>Here’s what you’ll need:</p><ul><li>3 cups flour</li><li>1 tsp salt</li></ul>" +
                          "<p>That’s it! The flour is the secret.</p><h3>How to make Vetkoek: A simple guide</h3>" +
                          "<p>Ready to get cooking? Follow these steps.</p><p>Step 1: Mix the dough</p><p>Mix the flour and salt.</p>" +
                          "<p>Step 2: Fry</p><p>Fry them in hot oil.</p><h3>How to serve your Vetkoek</h3><ul><li>With jam.</li></ul>");

        Assert.That(recipe!.Ingredients.Select(line => (line.Section, line.Text)),
            Is.EqualTo(new (string?, string)[] { (null, "3 cups flour"), (null, "1 tsp salt") }));
        Assert.That(Steps(recipe), Is.EqualTo(new (string?, string)[]
        {
            ("Step 1: Mix the dough", "Mix the flour and salt."), ("Step 2: Fry", "Fry them in hot oil.")
        }));
    }

    /// <summary>A numbered line with no full stop and sentences under it names a part, however long it is.</summary>
    [Test]
    public void NumberedLines_WithSentencesUnderThem_AreParts()
    {
        var recipe = Read("<h1>Vetkoek</h1><ul><li>Yields: 12-15 vetkoek</li></ul><p>Ingredients</p><p>For the Vetkoek dough:</p>" +
                          "<ul><li>4 cups flour</li><li>1 tsp salt</li></ul>" +
                          "<h3>Step-by-step instructions</h3><ol><li>1. Activate the yeast and mix the dough</li></ol>" +
                          "<p>Combine the flour and yeast.</p><p>Pour in the water.</p><ol><li>2. Shape and fry the vetkoek</li></ol>" +
                          "<p>Fry them for three minutes.</p><h3>Pro tips</h3><ul><li>Use hot oil.</li></ul>");

        Assert.That(recipe!.Yield, Is.EqualTo("12-15 vetkoek"));
        Assert.That(Steps(recipe), Is.EqualTo(new (string?, string)[]
        {
            ("Step 1: Activate the yeast and mix the dough", "Combine the flour and yeast."),
            ("Step 1: Activate the yeast and mix the dough", "Pour in the water."),
            ("Step 2: Shape and fry the vetkoek", "Fry them for three minutes.")
        }));
    }

    /// <summary>A plainly numbered method is still steps: a numbered sentence has nothing to name.</summary>
    [Test]
    public void NumberedSentences_StayTheSteps()
    {
        var recipe = Read("<h1>Vetkoek</h1><h3>Ingredients</h3><ul><li>4 cups flour</li><li>1 tsp salt</li></ul>" +
                          "<h3>Method</h3><ol><li>1. Mix it all.</li><li>2. Fry it.</li></ol><p>Enjoy them hot.</p>");

        Assert.That(Steps(recipe!), Is.EqualTo(new (string?, string)[] { (null, "Mix it all."), (null, "Fry it.") }));
    }
}
