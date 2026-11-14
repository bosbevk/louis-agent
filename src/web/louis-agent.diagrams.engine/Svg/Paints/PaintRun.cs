namespace LouisAgent.Diagrams.Engine.Svg.Paints;

/// <summary>
/// Which way a paint runs over the shape it fills: across it from the left, down it from the top, from its top
/// left corner to its bottom right, or from its middle out.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
internal enum PaintRun
{
    Across,
    Down,
    Slant,
    Out
}
