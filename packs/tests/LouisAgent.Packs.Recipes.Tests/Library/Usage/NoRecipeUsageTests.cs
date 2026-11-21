using LouisAgent.Packs.Recipes.Library.Usage;

namespace LouisAgent.Packs.Recipes.Tests.Library.Usage;

/// <summary>With no database the counter counts nothing and holds nothing, and asking it costs nothing.</summary>
public class NoRecipeUsageTests
{
    /// <summary>An add goes nowhere, and there's never a day to show.</summary>
    [Test]
    public async Task ItCountsNothing()
    {
        var usage = new NoRecipeUsage();

        await usage.AddAsync(RecipeUsageKind.Served, 1, new DateOnly(2026, 11, 6), CancellationToken.None);

        Assert.That(await usage.DaysAsync(CancellationToken.None), Is.Empty);
    }
}
