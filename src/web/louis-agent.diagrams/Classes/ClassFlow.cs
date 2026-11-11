using LouisAgent.Choices;
using LouisAgent.Diagrams.Classes.Spec;
using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Classes;

/// <summary>
/// Turns a class chart into a flow, so the flows' layout, router, tones, chips and checks draw it: each type a box
/// with its kind under its name and its members under a divider, each relation an arrow whose style and word say
/// what kind of link it is.
/// </summary>
/// <remarks>
/// Adapter (Refactoring.Guru): a class chart and a flow are the same boxes and arrows with different words, so the
/// classes kind is translated into the flow's spec rather than given a layout of its own, and every fix the flows get
/// reaches the class charts too (Claude's pick Q1 while unattended, CL60.4).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0103-a-diagram-is-data-on-a-row-grid.md">ADR 0103: a diagram is data</seealso>
public static class ClassFlow
{
    /// <summary>The flow a class chart is drawn as.</summary>
    public static GraphSpec Of(ClassesSpec spec) =>
        new(spec.Types.Select(type => new GraphNode(type.Id, type.Name, type.Kind, Group: type.Group, Icon: type.Icon,
                Tone: type.Tone, Members: type.Members)).ToList(),
            spec.Relations.Select(Edge).ToList(),
            spec.Direction,
            spec.Groups);

    /// <summary>
    /// A relation's arrow: dashed for a link in name (implements, uses, creates), solid for one in fact (a kind of,
    /// holds); its word the relation's own, or its kind's for a kind of and implements, which always say so.
    /// </summary>
    private static GraphEdge Edge(ClassRelation relation) =>
        relation.Kind switch
        {
            RelationKind.Inherits => new GraphEdge(relation.From, relation.To, "is a"),
            RelationKind.Implements => new GraphEdge(relation.From, relation.To, "implements", EdgeStyle.Dashed),
            RelationKind.Holds => new GraphEdge(relation.From, relation.To, relation.Word ?? "holds"),
            RelationKind.Uses => new GraphEdge(relation.From, relation.To, relation.Word ?? "uses", EdgeStyle.Dashed),
            RelationKind.Creates => new GraphEdge(relation.From, relation.To, relation.Word ?? "creates", EdgeStyle.Dashed),
            _ => throw new NotSupportedException(relation.Kind.Wire())
        };
}
