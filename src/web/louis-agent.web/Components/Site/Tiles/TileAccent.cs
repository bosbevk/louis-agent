namespace LouisAgent.Web.Components.Site.Tiles;

/// <summary>
/// The colour a tile may wear to set it apart (F128.1): the four the eval screens use, red, green, blue and yellow,
/// or none.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum TileAccent
{
    None,
    Red,
    Green,
    Blue,
    Yellow,
}
