namespace LouisAgent.Packs.Recipes.Library.Feed;

/// <summary>
/// The library as a source: the reading half. It finds a page we hold, and the pages that answer a search.
/// </summary>
/// <remarks>
/// A Separated Interface from the store on purpose: <see cref="FeedRecipeLibrary"/> reads the open feed as any outsider
/// would, so the reading half knows nothing about where the recipes are kept.
/// </remarks>
public interface IRecipeLibrary
{
    /// <summary>The page, if we hold it.</summary>
    Task<StoredRecipe?> FindAsync(Uri page, CancellationToken cancellationToken);

    /// <summary>The pages we hold that answer the search, at most <paramref name="most"/>.</summary>
    Task<IReadOnlyList<StoredRecipe>> SearchAsync(string query, int most, CancellationToken cancellationToken);
}
