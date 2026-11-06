using LouisAgent.Packs.Recipes.Library.Usage;

namespace LouisAgent.Packs.Recipes.Tests.Library.Usage;

/// <summary>The counter for the tests: it notes each add, or fails the way a server that's down does.</summary>
internal sealed class FakeRecipeUsage(bool fails = false) : IRecipeUsage
{
    /// <summary>Every add, in order.</summary>
    public List<(RecipeUsageKind Kind, long By, DateOnly Day)> Added { get; } = [];

    /// <summary>Notes the add, or fails.</summary>
    public Task AddAsync(RecipeUsageKind kind, long by, DateOnly day, CancellationToken cancellationToken)
    {
        if (fails) throw new TimeoutException("no Mongo");
        Added.Add((kind, by, day));
        return Task.CompletedTask;
    }

    /// <summary>These tests only count; the report's own tests read days.</summary>
    public Task<IReadOnlyList<RecipeUsageDay>> DaysAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<RecipeUsageDay>>([]);
}
