namespace LouisAgent.Packs.Recipes.Recipes.Tidying;

/// <summary>
/// A page's recipe as a cook can follow it: a method in separate steps, the headings among the ingredients as sections,
/// and what the page's buttons left there gone. The words stay the page's.
/// </summary>
/// <remarks>
/// A normaliser on the way out, used by the print and the scaler, so their numbers match and the recipes the library
/// already holds are tidied too with nothing to migrate. Tidying what's tidy changes nothing.
/// </remarks>
internal static class RecipeTidy
{
    /// <summary>The same recipe, line by line.</summary>
    public static Recipe Of(Recipe recipe) => recipe with
    {
        Ingredients = IngredientHeadings.Of(recipe.Ingredients),
        Steps = [.. recipe.Steps.SelectMany(step => StepParts.Of(step.Text).Select(part => step with { Text = part }))]
    };
}
