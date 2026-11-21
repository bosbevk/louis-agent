using LouisAgent.Choices;

namespace LouisAgent.Diagrams.Composite.Spec;

/// <summary>
/// Which way a port of a composite structure diagram faces (F134.13): in, for what is asked of the class, or out,
/// for what the class needs from outside.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum PortSide
{
    [WireName("in")] In,
    [WireName("out")] Out
}
