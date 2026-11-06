using LouisAgent.Packs.Recipes.Library;
using LouisAgent.Packs.Recipes.Library.Feed;
using LouisAgent.Packs.Recipes.Library.Store;

namespace LouisAgent.Packs.Recipes.Tests.Tools.Reading;

/// <summary>
/// The library's two halves in one list, for the tests: what's kept can be read straight away, with the feed's five
/// minutes left out, or it fails the way a server that's down does.
/// </summary>
internal sealed class FakeRecipeLibrary(bool fails = false) : IRecipeStore, IRecipeLibrary
{
    private const string Address = "https://library.example/v1/feeds/recipes/";

    /// <summary>Everything kept, in order.</summary>
    public List<StoredRecipe> Kept { get; } = [];

    /// <inheritdoc />
    public Task<IReadOnlyList<StoredRecipe>> AllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<StoredRecipe>>(Kept);

    /// <summary>The kept recipe with this id.</summary>
    public Task<StoredRecipe?> FindAsync(string id, CancellationToken cancellationToken) =>
        Task.FromResult(Kept.FirstOrDefault(recipe => recipe.Id == id));

    /// <summary>Keeps a page it doesn't have yet and says if it was new, or fails.</summary>
    public Task<bool> KeepAsync(StoredRecipe recipe, CancellationToken cancellationToken)
    {
        if (fails) throw new TimeoutException("no Mongo");
        if (Kept.Any(kept => kept.Id == recipe.Id)) return Task.FromResult(false);

        Kept.Add(recipe);
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task PurgeSiteAsync(string slug, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Uri AddressOf(StoredRecipe recipe) => new(Address + recipe.Id);

    /// <inheritdoc />
    public string? IdIn(Uri address) =>
        address.AbsoluteUri.StartsWith(Address, StringComparison.Ordinal) ? address.AbsoluteUri[Address.Length..] : null;

    /// <summary>The kept recipe with this id, or fails.</summary>
    public Task<StoredRecipe?> ReadAsync(string id, CancellationToken cancellationToken)
    {
        if (fails) throw new TimeoutException("no feed");
        return FindAsync(id, cancellationToken);
    }

    /// <summary>Every kept page whose title holds the words, or fails.</summary>
    public Task<IReadOnlyList<StoredRecipe>> SearchAsync(string query, int most, CancellationToken cancellationToken)
    {
        if (fails) throw new TimeoutException("no feed");
        return Task.FromResult<IReadOnlyList<StoredRecipe>>(
            [.. Kept.Where(recipe => recipe.Title.Contains(query, StringComparison.OrdinalIgnoreCase)).Take(most)]);
    }
}
