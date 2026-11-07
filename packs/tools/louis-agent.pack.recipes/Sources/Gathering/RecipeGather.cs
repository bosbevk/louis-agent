using LouisAgent.Packs.Recipes.Sources.Sites;

namespace LouisAgent.Packs.Recipes.Sources.Gathering;

/// <summary>
/// What one search over several sites came back with: the hits, the sites it asked, what any of them said when it
/// refused, and whether our library was asked too.
/// </summary>
/// <remarks>
/// A record because it is a plain value (ADR 0040).
/// </remarks>
public sealed record RecipeGather(
    IReadOnlyList<SiteHit> Hits,
    IReadOnlyList<RecipeSite> Asked,
    IReadOnlyList<string> Refusals,
    bool AskedLibrary = false);
