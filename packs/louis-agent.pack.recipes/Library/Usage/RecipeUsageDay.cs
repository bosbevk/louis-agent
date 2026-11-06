namespace LouisAgent.Packs.Recipes.Library.Usage;

/// <summary>What the library did on one UTC day.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0046-dates-are-utc.md">ADR 0046: dates are UTC</seealso>
public sealed record RecipeUsageDay(DateOnly Day, long Kept, long Offered, long Served, long Failed);
