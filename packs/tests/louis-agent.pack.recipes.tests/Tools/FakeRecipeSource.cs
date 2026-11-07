using LouisAgent.Packs.Recipes.Sources;
using LouisAgent.Packs.Recipes.Sources.Sites;

namespace LouisAgent.Packs.Recipes.Tests.Tools;

/// <summary>
/// A recipe site for the tests that answers every search with the hits it was given, remembers what it was asked,
/// or fails the way a real one does.
/// </summary>
internal sealed class FakeRecipeSource(
    string slug,
    IReadOnlyList<RecipeHit>? hits = null,
    Exception? fails = null) : IRecipeSource
{
    /// <summary>The words it was last asked to search for.</summary>
    public string? Query { get; private set; }

    /// <summary>How many results it was last asked for.</summary>
    public int MaxResults { get; private set; }

    /// <summary>The site this fake stands for, taken from the real table.</summary>
    public RecipeSite Site => RecipeSites.Find(slug)!;

    /// <summary>Answers with its hits, or throws what it was told to.</summary>
    public Task<IReadOnlyList<RecipeHit>> FindAsync(string query, int maxResults,
        CancellationToken cancellationToken = default)
    {
        (Query, MaxResults) = (query, maxResults);
        return fails is null ? Task.FromResult(hits ?? []) : Task.FromException<IReadOnlyList<RecipeHit>>(fails);
    }
}
