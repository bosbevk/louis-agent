using LouisAgent.Choices;

namespace LouisAgent.Web.Components.Site.Tiles;

/// <summary>
/// The colour a tile or a demo may wear to set it apart (F128.1): the four the eval screens use, red, green, blue and
/// yellow, and since F127.1 a purple, so each of the showroom's five demos has its own; or none. Its wire name is the
/// colour's class, <c>accent-green</c>.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum TileAccent
{
    [WireName("none")] None,
    [WireName("red")] Red,
    [WireName("green")] Green,
    [WireName("blue")] Blue,
    [WireName("yellow")] Yellow,
    [WireName("purple")] Purple,
}
