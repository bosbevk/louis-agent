namespace LouisAgent.Packs.Recipes.Library.Feed;

/// <summary>The library when no address is set: it holds nothing, so the cook works as it did before there was one.</summary>
/// <remarks>Null Object, so a read and a search never have to ask whether there's a library.</remarks>
public sealed class NoRecipeLibrary : IRecipeLibrary
{
    /// <inheritdoc />
    public Task<StoredRecipe?> FindAsync(Uri page, CancellationToken cancellationToken) =>
        Task.FromResult<StoredRecipe?>(null);

    /// <inheritdoc />
    public Task<IReadOnlyList<StoredRecipe>> SearchAsync(string query, int most, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<StoredRecipe>>([]);
}
