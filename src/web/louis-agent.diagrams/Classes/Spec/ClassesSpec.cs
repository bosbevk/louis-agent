using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Classes.Spec;

/// <summary>
/// A class chart: the types it shows, the relations between them, which way it runs, and the frames that group its
/// types, as a flow's do.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0103-a-diagram-is-data-on-a-row-grid.md">ADR 0103: a diagram is data</seealso>
public sealed record ClassesSpec(
    IReadOnlyList<ClassType> Types,
    IReadOnlyList<ClassRelation> Relations,
    FlowDirection Direction = FlowDirection.Right,
    IReadOnlyList<GraphGroup>? Groups = null);
