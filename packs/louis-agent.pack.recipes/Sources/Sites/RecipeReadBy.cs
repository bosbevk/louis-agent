namespace LouisAgent.Packs.Recipes.Sources.Sites;

/// <summary>
/// How a site's recipe pages are read: from the recipe the page publishes for machines, or from the page's text.
/// </summary>
/// <remarks>
/// An enum because it is one of a fixed set, parsed from the table once (ADR 0024). Nothing acts on it yet:
/// <c>ReadRecipe</c> decides page by page whether a page has data, so the column only records what step 0 saw on
/// one page of each site, and F35.2's measure uses it.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum RecipeReadBy
{
    /// <summary>The page publishes a whole recipe in its schema.org data.</summary>
    Data,

    /// <summary>The page has no usable recipe data, so the cook reads its text.</summary>
    Text,
}
