using LouisAgent.Packs.Recipes.Recipes;

namespace LouisAgent.Packs.Recipes.Library;

/// <summary>
/// One proper recipe as the library keeps it: the page's own recipe, line by line, with the site it's from and when we
/// read it. It's kept for good, because a recipe doesn't go stale. It holds nothing about who asked, and has no place
/// to put that.
/// </summary>
/// <param name="Recipe">The recipe. Its source stays the site's own page, which is the link and the credit.</param>
/// <param name="SiteSlug">The site in the table it came from.</param>
/// <param name="ReadAt">When it was read.</param>
/// <param name="EditedAt">When a person last changed it by hand; null for the page's own words.</param>
/// <remarks>A record because it is a plain value (ADR 0040).</remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed record StoredRecipe(Recipe Recipe, string SiteSlug, DateTimeOffset ReadAt, DateTimeOffset? EditedAt = null)
{
    /// <summary>The name it goes by in the library and on our API.</summary>
    public string Id => RecipeAddress.IdOf(Recipe.Source);

    /// <summary>The site's own page.</summary>
    public Uri Page => Recipe.Source;

    /// <summary>The recipe's name, which is what a search looks at.</summary>
    public string Title => Recipe.Name;
}
