using LouisAgent.Choices;

namespace LouisAgent.Diagrams.Nested.Spec;

/// <summary>
/// Which side of the root a neighbour stands on (F134.7): before it, what hands it things or what it reads, or
/// after it, what it calls.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum NeighbourSide
{
    [WireName("before")] Before,
    [WireName("after")] After
}
