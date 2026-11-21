using LouisAgent.Choices;

namespace LouisAgent.Diagrams.Flow.Spec;

/// <summary>Which way a flow runs: its ranks as columns to the right, as on the site, or as rows down.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum FlowDirection
{
    [WireName("right")] Right,
    [WireName("down")] Down
}
