using LouisAgent.Packs.Recipes.Sources.Sites;

namespace LouisAgent.Packs.Recipes.Sources;

/// <summary>
/// One recipe site we can search. Search for a word and you get the pages it found, in the site's order. When the
/// site refuses, you get a <see cref="RecipeSourceException"/> the cook can read.
/// </summary>
/// <remarks>
/// Strategy (GoF), picked by the row's <c>search</c> column (ADR 0003): the callers hold the interface and never
/// ask which way a site is searched.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0003-provider-strategies.md">ADR 0003: provider strategies</seealso>
public interface IRecipeSource
{
    /// <summary>The site this source searches.</summary>
    RecipeSite Site { get; }

    /// <summary>The pages the site found for <paramref name="query"/>, at most <paramref name="maxResults"/>.</summary>
    Task<IReadOnlyList<RecipeHit>> FindAsync(string query, int maxResults,
        CancellationToken cancellationToken = default);
}
