using LouisAgent.Diagrams.Flow.Shapes;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Engine.Flow.Keys;

/// <summary>
/// One item of a flow's key, placed (F134.8): a small sample of a thing, and what it means beside it.
/// </summary>
/// <param name="Kind">What the item explains, which decides the sample drawn.</param>
/// <param name="Words">What it means.</param>
/// <param name="X">Where its sample starts, from the key's left.</param>
/// <param name="Y">The top of its row, from the key's top.</param>
/// <param name="Sample">How wide its sample is. Its words start a little after it.</param>
/// <param name="Tone">The tone a tone's item explains. Plain for every other item.</param>
/// <param name="Shape">The shape a shape's item explains. A plain box for every other item.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed record FlowKeyItem(
    FlowKeyKind Kind,
    string Words,
    double X,
    double Y,
    double Sample,
    DiagramTone Tone = DiagramTone.Plain,
    NodeShape Shape = NodeShape.Box);
