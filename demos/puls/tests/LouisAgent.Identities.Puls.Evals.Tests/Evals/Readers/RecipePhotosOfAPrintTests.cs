using LouisAgent.Composition.Messages;
using LouisAgent.Core.Messages;
using LouisAgent.Identities.Puls.Evals.Readers;
using LouisAgent.Packs.Recipes.Messages;

namespace LouisAgent.Identities.Puls.Tests.Evals.Readers;

/// <summary>
/// The cook eval's <c>recipe-image</c> check reads a <c>ReadRecipe</c> print now (F35.2 step 2): the one image line
/// inside the pack's untrusted marks is the recipe's photo, and nothing else is.
/// </summary>
public class RecipePhotosOfAPrintTests
{
    /// <summary>A print as the tool gives it: framed between the marks, the image line after the method.</summary>
    private static string Print() => string.Join("\n",
        MessageText.Format(RecipeToolMessage.UntrustedFrame),
        "# Pork bobotie",
        "Makes 4 to 6 · prep 20 min · cook 30 min",
        "## Ingredients",
        "1. 500 g pork mince",
        "## Method",
        "1. Fry the mince.",
        "![Pork bobotie](https://www.foodandhome.co.za/wp-content/uploads/2026/09/x.jpg)",
        "Author: Food & Home",
        "Link: https://www.foodandhome.co.za/recipes/pork-bobotie",
        MessageText.Format(RecipeToolMessage.FrameEnd));

    /// <summary>The print's one image is found, and the author's and the page's links are not images.</summary>
    [Test]
    public void TheRecipePhoto_OfAReadRecipePrint_IsItsOneImageLine() =>
        Assert.That(new RecipePhotos().Images(Print()),
            Is.EqualTo(new[] { "https://www.foodandhome.co.za/wp-content/uploads/2026/09/x.jpg" }));
}
