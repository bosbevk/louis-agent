using LouisAgent.Packs.Recipes.Library;
using LouisAgent.Packs.Recipes.Sources.Sites;

namespace LouisAgent.Packs.Recipes.Sources.Gathering;

/// <summary>
/// A site's search with the pages we've found no recipe on left out. It asks the site for extra to make up for them, so
/// an excluded page costs the cook no place in the list.
/// </summary>
/// <remarks>
/// Decorator (Gang of Four): every way of searching goes through <see cref="IRecipeSource"/>, so wrapping it covers a
/// search of one site and a search of several in one place, and neither has to know the list exists. Without it the
/// same filter sat in both, and the first one left the list short.
/// </remarks>
/// <seealso href="https://refactoring.guru/design-patterns/decorator">Decorator</seealso>
public sealed class SourceWithoutExcludedPages(IRecipeSource source, RecipeShelf shelf) : IRecipeSource
{
    /// <inheritdoc />
    public RecipeSite Site => source.Site;

    /// <summary>The site's hits without the excluded pages, as many as were asked for if the site has them.</summary>
    public async Task<IReadOnlyList<RecipeHit>> FindAsync(string query, int maxResults, CancellationToken cancellationToken)
    {
        var excluded = await shelf.ExcludedIdsAsync(cancellationToken);
        int asked = maxResults + Math.Min(excluded.Count, maxResults);

        var hits = await source.FindAsync(query, asked, cancellationToken);
        return [.. hits.Where(hit => !excluded.Contains(RecipeAddress.IdOf(hit.Url))).Take(maxResults)];
    }
}
