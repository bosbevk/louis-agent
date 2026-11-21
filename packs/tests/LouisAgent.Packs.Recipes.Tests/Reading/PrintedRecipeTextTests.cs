using LouisAgent.Packs.Recipes.Reading;

namespace LouisAgent.Packs.Recipes.Tests.Reading;

/// <summary>
/// A text page's print ends at the recipe. The lines here are real: The Kate Tin's page ran on into 92 readers' comments,
/// with their names, which an open feed must not publish.
/// </summary>
public class PrintedRecipeTextTests
{
    private static readonly string Recipe =
        "# Malva Pudding\n\n## Ingredients\n\n1 cup sugar\n\n## What to do\n\nBake it. " + new string('x', 400);

    /// <summary>What follows the recipe on real pages goes, whichever way the page writes it.</summary>
    [TestCase("# 92 Comments")]
    [TestCase("#### Comments")]
    [TestCase("#### Share this recipe")]
    [TestCase("#### Related recipes")]
    [TestCase("### Verwante artikels:")]
    [TestCase("## You have Successfully Subscribed!")]
    [TestCase("previous post")]
    [TestCase("Leave a Reply")]
    [TestCase("Lewer kommentaar")]
    public void WhatFollowsTheRecipe_Goes(string line)
    {
        string text = PrintedRecipeText.Cut(Recipe + "\n\n" + line + "\n\nSusan: lovely, my gran made this");

        Assert.That(text, Is.EqualTo(Recipe));
    }

    /// <summary>A sentence that happens to mention one isn't where the recipe ends.</summary>
    [TestCase("Read the comments on my last post for what my family made of this pudding, since they all had a view.")]
    [TestCase("Serve with the related sauce.")]
    public void ASentenceInTheRecipe_Stays(string line) =>
        Assert.That(PrintedRecipeText.Cut(Recipe + "\n\n" + line + "\n\nStir."), Does.EndWith("Stir."));

    /// <summary>A heading before the recipe, such as a menu's, can't take the recipe with it.</summary>
    [Test]
    public void AHeadingBeforeTheRecipe_DoesntCutIt()
    {
        string text = "#### Related recipes\n\nHome\n\n" + Recipe;

        Assert.That(PrintedRecipeText.Cut(text), Is.EqualTo(text));
    }

    /// <summary>
    /// A long page's window starts just above its ingredients, so a long story can't push the recipe past the cut:
    /// Cooksister's recipe started 8,500 characters in, and the cook had to fetch the page again.
    /// </summary>
    [Test]
    public void ALongStory_DoesNotPushTheRecipePastTheWindow()
    {
        string story = string.Concat(Enumerable.Repeat(new string('s', 90) + "\n", 100));
        string text = story + "Serves: 4\nIngredients\n- 1 egg\n";

        int start = PrintedRecipeText.RecipeStart(text);

        Assert.That(text[start..], Does.StartWith(new string('s', 90)), "starts at a whole line");
        Assert.That(text[start..], Does.Contain("Serves: 4\nIngredients"), "with the lines just above it");
        Assert.That(text.Length - start, Is.LessThan(500), "and not the story");
    }

    /// <summary>A page with no ingredients heading starts at the top, and so does one with it near the top.</summary>
    [TestCase("A long story with no heading at all.\nAnd more of it.\n")]
    [TestCase("Ingredients\n- 1 egg\n")]
    [TestCase("# Pie\nIngredients\n- 1 egg\n")]
    public void WithNothingToSkip_TheWindowStartsAtTheTop(string text) =>
        Assert.That(PrintedRecipeText.RecipeStart(text), Is.EqualTo(0));

    /// <summary>A page with none of these comes back as it was.</summary>
    [Test]
    public void APageWithNone_StaysAsItIs() => Assert.That(PrintedRecipeText.Cut(Recipe), Is.EqualTo(Recipe));
}
