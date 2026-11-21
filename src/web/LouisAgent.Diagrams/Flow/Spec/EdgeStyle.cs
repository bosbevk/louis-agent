using LouisAgent.Choices;

namespace LouisAgent.Diagrams.Flow.Spec;

/// <summary>How an arrow is drawn: a solid line, or dashed for what is optional, later or only read.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum EdgeStyle
{
    [WireName("solid")] Solid,
    [WireName("dashed")] Dashed
}
