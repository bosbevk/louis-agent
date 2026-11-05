namespace LouisAgent.Packs.Recipes.Library.Feed;

/// <summary>
/// The library as a source: the reading half. It answers a search, gives each recipe an address of its own, and reads
/// one from that address.
/// </summary>
/// <remarks>
/// A Separated Interface from the store on purpose: <see cref="FeedRecipeLibrary"/> reads the open feed as any outsider
/// would, so the reading half knows nothing about where the recipes are kept.
/// </remarks>
public interface IRecipeLibrary
{
    /// <summary>The pages we hold that answer the search, at most <paramref name="most"/>.</summary>
    Task<IReadOnlyList<StoredRecipe>> SearchAsync(string query, int most, CancellationToken cancellationToken);

    /// <summary>The address to read this recipe from.</summary>
    Uri AddressOf(StoredRecipe recipe);

    /// <summary>The recipe's id, if this is one of our addresses.</summary>
    string? IdIn(Uri address);

    /// <summary>The recipe with this id, if we hold it.</summary>
    Task<StoredRecipe?> ReadAsync(string id, CancellationToken cancellationToken);
}
