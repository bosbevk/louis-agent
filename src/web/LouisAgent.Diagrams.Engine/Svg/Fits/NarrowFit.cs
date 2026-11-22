using LouisAgent.Diagrams.Engine.Cards;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.Diagrams.Engine.Svg.Fits;

/// <summary>
/// A picture's narrow form, for a phone (F169.5): a flow running down with the columns and the row gap its author
/// set for the wide drawing dropped, and a cards picture running down from its trunk. Every other kind has none
/// today, and neither has a cards picture whose links leave more than one card.
/// </summary>
/// <remarks>
/// Louis's pick "A: both drawn for a phone", over a picture that slides sideways and one that opens large: a picture
/// is data, so its phone drawing is the same data told one more thing, and nobody redraws it. Whether a picture has
/// a narrow form is asked here, of the engine, and never worked out by the page that holds it (ADR 0123).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0118-a-diagram-is-data-our-renderer-draws.md">ADR 0118: a diagram is data our renderer draws</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public static class NarrowFit
{
    /// <summary>The picture told how to run on a narrow screen, or nothing when it has no such form.</summary>
    public static DiagramSource? Of(DiagramSource source) => source switch
    {
        { Kind: DiagramKind.Flow, Flow: { } flow } => source with { Flow = Down(flow) },
        { Kind: DiagramKind.Cards, Cards: { } cards } when OverviewLayout.CanRunDown(cards) =>
            source with { Cards = cards with { Direction = FlowDirection.Down } },
        _ => null
    };

    /// <summary>Whether the picture has a narrow form.</summary>
    public static bool Has(DiagramSource source) => Of(source) is not null;

    /// <summary>
    /// A flow running down, every box in the rank its arrows give it, since the columns and the row gap were set
    /// for a drawing across.
    /// </summary>
    private static GraphSpec Down(GraphSpec flow) => flow with
    {
        Direction = FlowDirection.Down,
        RowGap = null,
        Nodes = [.. flow.Nodes.Select(node => node with { Column = null })]
    };
}
