namespace LouisAgent.Packs.Recipes.Library.Store;

/// <summary>The store when no database is set: it keeps nothing and holds nothing, and nobody has to check for that.</summary>
/// <remarks>Null Object, so the tools and the feed work the same with or without a library.</remarks>
public sealed class NoRecipeStore : IRecipeStore
{
    /// <inheritdoc />
    public Task<IReadOnlyList<StoredRecipe>> AllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<StoredRecipe>>([]);

    /// <inheritdoc />
    public Task KeepAsync(StoredRecipe recipe, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task PurgeSiteAsync(string slug, CancellationToken cancellationToken) => Task.CompletedTask;
}
