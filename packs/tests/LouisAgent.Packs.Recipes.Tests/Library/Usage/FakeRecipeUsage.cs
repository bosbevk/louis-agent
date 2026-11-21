using LouisAgent.Packs.Recipes.Library.Usage;

namespace LouisAgent.Packs.Recipes.Tests.Library.Usage;

/// <summary>
/// The counter for the tests: it notes each add and gives the days it was handed, or fails like a server that's down.
/// </summary>
internal sealed class FakeRecipeUsage(bool fails = false, params RecipeUsageDay[] days) : IRecipeUsage
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

    /// <summary>The days it was handed, or fails.</summary>
    public Task<IReadOnlyList<RecipeUsageDay>> DaysAsync(CancellationToken cancellationToken)
    {
        if (fails) throw new TimeoutException("no Mongo");
        return Task.FromResult<IReadOnlyList<RecipeUsageDay>>(days);
    }
}
