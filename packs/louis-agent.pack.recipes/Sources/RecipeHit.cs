namespace LouisAgent.Packs.Recipes.Sources;

/// <summary>One page a search found: what the site calls it and where it is.</summary>
/// <remarks>
/// A record because it is a plain value (ADR 0040).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed record RecipeHit(string Name, Uri Url);
