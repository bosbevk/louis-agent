namespace LouisAgent.Packs.Recipes.Library.Store;

/// <summary>
/// Where kept recipes live: the writing half of the library. It takes a page we don't have yet, hands over all it holds
/// for the feed to publish, and takes out a site's pages when its owner asks.
/// </summary>
/// <remarks>
/// Repository (Fowler, PoEAA) with two strategies (Gang of Four): <see cref="MongoRecipeStore"/> and
/// <see cref="NoRecipeStore"/>, picked once from the settings, so nothing else knows where the recipes are kept.
/// </remarks>
/// <seealso href="https://www.martinfowler.com/eaaCatalog/repository.html">Repository</seealso>
public interface IRecipeStore
{
    /// <summary>Everything kept, for the feed.</summary>
    Task<IReadOnlyList<StoredRecipe>> AllAsync(CancellationToken cancellationToken);

    /// <summary>The one recipe with this id, for its own address on the feed.</summary>
    Task<StoredRecipe?> FindAsync(string id, CancellationToken cancellationToken);

    /// <summary>
    /// Keeps a page we don't have yet, and says whether it was new; one we already hold stays exactly as it was.
    /// </summary>
    Task<bool> KeepAsync(StoredRecipe recipe, CancellationToken cancellationToken);

    /// <summary>Takes out every page of a site, when its owner asks.</summary>
    Task PurgeSiteAsync(string slug, CancellationToken cancellationToken);

    /// <summary>Puts a person's edit in place of the kept recipe, and says whether there was one to change.</summary>
    Task<bool> ReplaceAsync(StoredRecipe recipe, CancellationToken cancellationToken);

    /// <summary>Takes one recipe out, and says whether there was one to take.</summary>
    Task<bool> RemoveAsync(string id, CancellationToken cancellationToken);
}
