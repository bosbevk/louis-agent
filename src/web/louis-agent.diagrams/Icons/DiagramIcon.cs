using LouisAgent.Choices;

namespace LouisAgent.Diagrams.Icons;

/// <summary>
/// The icons a card may wear, by the names a diagram's file uses. Step 4 draws each one from Tabler Icons (MIT) and
/// adds the rest of the set the overviews need.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum DiagramIcon
{
    [WireName("chip")] Chip,
    [WireName("user")] User,
    [WireName("box")] Box
}
