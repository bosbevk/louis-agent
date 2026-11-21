namespace LouisAgent.Packs.Recipes.Sources.Sites;

/// <summary>
/// How a site is searched: through its own search page, or through the web search provider asked for that site only.
/// </summary>
/// <remarks>
/// An enum because it is one of a fixed set, parsed from the table once (ADR 0024). It is what the factory switches
/// on to pick the provider (ADR 0003).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0003-provider-strategies.md">ADR 0003: provider strategies</seealso>
public enum RecipeSearch
{
    /// <summary>The site's own WordPress search page, <c>/?s=</c>, whose results we can read.</summary>
    WordPress,

    /// <summary>The web search provider, limited to the site's host.</summary>
    Web,
}
