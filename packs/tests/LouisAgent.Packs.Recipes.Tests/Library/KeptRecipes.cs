using LouisAgent.Packs.Recipes.Library;
using LouisAgent.Packs.Recipes.Recipes;

namespace LouisAgent.Packs.Recipes.Tests.Library;

/// <summary>A proper recipe as the library keeps it, for the tests that need one on a shelf.</summary>
internal static class KeptRecipes
{
    /// <summary>A small whole recipe on this page and site.</summary>
    public static StoredRecipe Of(string page, string slug = "taste", string name = "Pie") => new(
        new Recipe(name, "4 people", TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(45), null,
            [new RecipeIngredient(null, "1 kg mince")], [new RecipeStep(null, "Bake it.")],
            new Uri("https://example.com/pie.jpg"), "A cook", new Uri(page)),
        slug,
        DateTimeOffset.UnixEpoch);
}
