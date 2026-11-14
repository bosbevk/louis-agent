using LouisAgent.Diagrams.Engine.Flow.Placed;
using LouisAgent.Diagrams.Engine.Keys;
using LouisAgent.Diagrams.Engine.Messages;
using LouisAgent.Diagrams.Engine.Tones;
using LouisAgent.Diagrams.Flow.Shapes;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Engine.Flow.Keys;

/// <summary>
/// Places a flow's key under its drawing (F134.8): a rule and "how to read it", then an item for each tone and
/// each shape the picture really uses and one for what a line's colour says, side by side across the drawing's
/// width, on to a new row when a row is full.
/// </summary>
/// <remarks>
/// A pure function of the placed boxes, as a nested picture's key is of its boxes, so a key never explains a
/// tone or a shape the picture doesn't have. It runs across the width, where a component's is a column, because a
/// flow is wide and has no column to put it in.
/// </remarks>
public static class FlowKeyLayout
{
    /// <summary>
    /// The room the key's rule and its heading take, and how tall a row of items is: the same as in the other
    /// kinds' keys (F134.12).
    /// </summary>
    public const double Heading = KeyRows.Heading, Row = KeyRows.Row;

    /// <summary>How far after its sample an item's words start.</summary>
    public const double Words = KeyRows.Words;

    /// <summary>How wide a tone's small badge is, and how wide the sample of a shape or of a line.</summary>
    private const double Badge = 18, Wide = 30;

    /// <summary>The key's items for the boxes a flow placed, across a width, and how tall the key is.</summary>
    public static (IReadOnlyList<FlowKeyItem> Items, double Height) Of(IReadOnlyList<PlacedNode> nodes, double width)
    {
        // What the picture uses, in the tones' and the shapes' own order, and last what a line's colour says.
        var tones = nodes.Select(node => node.Node.Tone).Where(tone => tone != DiagramTone.Plain).Distinct().Order()
            .Select(tone => new FlowKeyItem(FlowKeyKind.Tone, ToneMeanings.Of(tone), 0, 0, Badge, Tone: tone));
        var shapes = nodes.Select(node => node.Node.Shape).Where(shape => shape != NodeShape.Box).Distinct().Order()
            .Select(shape => new FlowKeyItem(FlowKeyKind.Shape, Meaning(shape), 0, 0, Wide, Shape: shape));
        var line = new FlowKeyItem(FlowKeyKind.Line, DiagramText.Format(DrawingMessage.FlowLine), 0, 0, Wide);

        // Side by side, each after the one before, and on to a new row at the left when the width is full.
        return KeyRows.Place(tones.Concat(shapes).Append(line), item => item.Sample, item => item.Words,
            (item, x, y) => item with { X = x, Y = y }, width);
    }

    /// <summary>The few words for what a shape means.</summary>
    private static string Meaning(NodeShape shape) => DiagramText.Format(shape switch
    {
        NodeShape.Decision => DrawingMessage.FlowDecision,
        NodeShape.Data => DrawingMessage.FlowData,
        _ => DrawingMessage.FlowTerminal
    });
}
