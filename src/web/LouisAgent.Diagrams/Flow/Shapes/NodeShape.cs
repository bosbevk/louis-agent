using LouisAgent.Choices;

namespace LouisAgent.Diagrams.Flow.Shapes;

/// <summary>
/// The shape of a flow's box (F134.8), named only where the shape means something: a decision is a box with
/// pointed ends (F134.12, a diamond before), data a slanted box, a start or an end a pill. Left out, a box is the
/// plain box it always was, so a picture of parts and what calls what needs none.
/// </summary>
/// <remarks>
/// The flowchart's own shapes (ISO 5807), Louis's pick of three: "a shape only where it means something". Most of
/// our flows show parts, not a procedure, and there one shape with a tone is the convention.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum NodeShape
{
    [WireName("box")] Box,
    [WireName("decision")] Decision,
    [WireName("data")] Data,
    [WireName("terminal")] Terminal
}
