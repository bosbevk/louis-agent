namespace LouisAgent.Packs.Recipes.Sources.Sites;

/// <summary>
/// One recipe site we may read, as a row of the sites table: what to call it, where it lives, how its recipes are
/// read and searched, when we checked it and what its pages told us.
/// </summary>
/// <remarks>
/// A record because it is a plain value read from content (ADR 0040).
/// </remarks>
/// <param name="Slug">The short lower-case name <c>RECIPE_SOURCES</c> and the tools use.</param>
/// <param name="Name">The site as a person writes it.</param>
/// <param name="Home">Where the site lives; a search page and a host are worked out from it.</param>
/// <param name="ReadBy">What step 0 saw on one of its recipe pages; nothing acts on it yet.</param>
/// <param name="Search">How we search the site.</param>
/// <param name="Checked">The day its <c>robots.txt</c> and page signals were read.</param>
/// <param name="Signals">What those showed that is worth keeping, or null.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed record RecipeSite(
    string Slug,
    string Name,
    Uri Home,
    RecipeReadBy ReadBy,
    RecipeSearch Search,
    DateOnly Checked,
    string? Signals);
