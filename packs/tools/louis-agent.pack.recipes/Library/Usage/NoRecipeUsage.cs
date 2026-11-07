namespace LouisAgent.Packs.Recipes.Library.Usage;

/// <summary>The counter when no database is set: it counts nothing, holds nothing, and nobody has to check.</summary>
/// <remarks>Null Object, so the shelf counts the same way with or without a library.</remarks>
public sealed class NoRecipeUsage : IRecipeUsage
{
    /// <inheritdoc />
    public Task AddAsync(RecipeUsageKind kind, long by, DateOnly day, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    /// <inheritdoc />
    public Task<IReadOnlyList<RecipeUsageDay>> DaysAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<RecipeUsageDay>>([]);
}
