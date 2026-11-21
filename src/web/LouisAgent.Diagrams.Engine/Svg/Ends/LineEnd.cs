using LouisAgent.Choices;

namespace LouisAgent.Diagrams.Engine.Svg.Ends;

/// <summary>
/// What a line ends in, at either end (F134.11): nothing, the arrowhead every picture has, a hollow triangle, a
/// diamond or an open arrowhead. A class chart says a link's kind with them, as a UML chart does.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
internal enum LineEnd
{
    [WireName("none")] None,
    [WireName("arrow")] Arrow,
    [WireName("triangle")] Triangle,
    [WireName("diamond")] Diamond,
    [WireName("open")] Open
}
