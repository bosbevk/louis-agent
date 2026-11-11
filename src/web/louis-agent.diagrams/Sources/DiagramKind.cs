using LouisAgent.Choices;

namespace LouisAgent.Diagrams.Sources;

/// <summary>
/// What a diagram draws. A flow is the site's boxes and arrows; cards are the overview pictures in Louis's figure.
/// The sequences and classes come with CL60.3 and CL60.4.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum DiagramKind
{
    [WireName("flow")] Flow,
    [WireName("cards")] Cards
}
