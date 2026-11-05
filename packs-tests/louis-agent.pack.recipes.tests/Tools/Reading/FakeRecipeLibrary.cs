using LouisAgent.Packs.Recipes.Library;
using LouisAgent.Packs.Recipes.Library.Feed;
using LouisAgent.Packs.Recipes.Library.Store;

namespace LouisAgent.Packs.Recipes.Tests.Tools.Reading;

/// <summary>
/// The library's two halves in one list, for the tests: what's kept can be found straight away, with the feed's five
/// minutes left out, or it fails the way a server that's down does.
/// </summary>
internal sealed class FakeRecipeLibrary(bool fails = false) : IRecipeStore, IRecipeLibrary
{
    /// <summary>Everything kept, in order.</summary>
    public List<StoredRecipe> Kept { get; } = [];

    /// <inheritdoc />
    public Task<IReadOnlyList<StoredRecipe>> AllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<StoredRecipe>>(Kept);

    /// <summary>Keeps it, or fails.</summary>
    public Task KeepAsync(StoredRecipe recipe, CancellationToken cancellationToken)
    {
        if (fails) throw new TimeoutException("no Mongo");
        Kept.Add(recipe);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task PurgeSiteAsync(string slug, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>The page by its one spelling, or fails.</summary>
    public Task<StoredRecipe?> FindAsync(Uri page, CancellationToken cancellationToken)
    {
        if (fails) throw new TimeoutException("no feed");
        return Task.FromResult(Kept.FirstOrDefault(recipe => RecipeAddress.Of(recipe.Page) == RecipeAddress.Of(page)));
    }

    /// <summary>Every kept page whose title holds the words, or fails.</summary>
    public Task<IReadOnlyList<StoredRecipe>> SearchAsync(string query, int most, CancellationToken cancellationToken)
    {
        if (fails) throw new TimeoutException("no feed");
        return Task.FromResult<IReadOnlyList<StoredRecipe>>(
            [.. Kept.Where(recipe => recipe.Title.Contains(query, StringComparison.OrdinalIgnoreCase)).Take(most)]);
    }
}
