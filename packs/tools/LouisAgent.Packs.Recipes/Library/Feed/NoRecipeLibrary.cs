namespace LouisAgent.Packs.Recipes.Library.Feed;

/// <summary>The library when no address is set: it holds nothing, so the cook works as it did before.</summary>
/// <remarks>Null Object, so a read and a search never have to ask whether there's a library.</remarks>
public sealed class NoRecipeLibrary : IRecipeLibrary
{
    /// <inheritdoc />
    public Task<IReadOnlyList<StoredRecipe>> SearchAsync(string query, int most, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<StoredRecipe>>([]);

    /// <summary>It has no address of its own, so a recipe stays at the site's.</summary>
    public Uri AddressOf(StoredRecipe recipe) => recipe.Page;

    /// <inheritdoc />
    public string? IdIn(Uri address) => null;

    /// <inheritdoc />
    public Task<StoredRecipe?> ReadAsync(string id, CancellationToken cancellationToken) =>
        Task.FromResult<StoredRecipe?>(null);
}
