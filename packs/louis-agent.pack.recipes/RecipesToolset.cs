namespace LouisAgent.Packs.Recipes;

/// <summary>
/// The toolsets this pack offers, by the names they travel under. A member's lower-case name is its wire name. The
/// pack keeps its own copy of the name (ADR 0043), so it never needs another pack's or an identity's.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public enum RecipesToolset
{
    Recipes,
}
