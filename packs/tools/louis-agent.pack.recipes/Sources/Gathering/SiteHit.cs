using LouisAgent.Packs.Recipes.Sources.Sites;

namespace LouisAgent.Packs.Recipes.Sources.Gathering;

/// <summary>A page a search found, and the site that found it.</summary>
/// <remarks>
/// A record because it is a plain value (ADR 0040).
/// </remarks>
public sealed record SiteHit(RecipeSite Site, RecipeHit Hit);
