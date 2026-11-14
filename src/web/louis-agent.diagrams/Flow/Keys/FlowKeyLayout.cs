using LouisAgent.Diagrams.Flow.Placed;
using LouisAgent.Diagrams.Flow.Shapes;
using LouisAgent.Diagrams.Messages;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Flow.Keys;

/// <summary>
/// Places a flow's key under its drawing (F134.8): "how to read it", then an item for each tone and each shape the
/// picture really uses and one for what a line's colour says, side by side across the drawing's width, on to a new
/// row when a row is full.
/// </summary>
/// <remarks>
/// A pure function of the placed boxes, as a component diagram's key is of its boxes, so a key never explains a
/// tone or a shape the picture doesn't have. It runs across the width, where a component's is a column, because a
/// flow is wide and has no column to put it in.
/// </remarks>
public static class FlowKeyLayout
{
    /// <summary>The room the key's heading takes, and how tall a row of items is.</summary>
    public const double Heading = 22, Row = 26;

    /// <summary>Where an item's words start after its sample.</summary>
    public const double Words = 38;

    /// <summary>
    /// How wide a character of an item's words is taken to be, generously, since no font is measured, and the room
    /// between two items.
    /// </summary>
    private const double Char = 6.3, Gap = 26;

    /// <summary>The key's items for the boxes a flow placed, across a width, and how tall the key is.</summary>
    public static (IReadOnlyList<FlowKeyItem> Items, double Height) Of(IReadOnlyList<PlacedNode> nodes, double width)
    {
        // What the picture uses, in the tones' and the shapes' own order, and last what a line's colour says.
        var tones = nodes.Select(node => node.Node.Tone).Where(tone => tone != DiagramTone.Plain).Distinct().Order()
            .Select(tone => new FlowKeyItem(FlowKeyKind.Tone, ToneMeanings.Of(tone), 0, 0, Tone: tone));
        var shapes = nodes.Select(node => node.Node.Shape).Where(shape => shape != NodeShape.Box).Distinct().Order()
            .Select(shape => new FlowKeyItem(FlowKeyKind.Shape, Meaning(shape), 0, 0, Shape: shape));
        var line = new FlowKeyItem(FlowKeyKind.Line, DiagramText.Format(DiagramMessage.FlowLine), 0, 0);

        // Side by side, each after the one before, and on to a new row at the left when the width is full.
        var items = new List<FlowKeyItem>();
        double x = 0, y = Heading;
        foreach (var item in tones.Concat(shapes).Append(line))
        {
            double room = Words + item.Words.Length * Char;
            if (x > 0 && x + room > width) (x, y) = (0, y + Row);
            items.Add(item with { X = x, Y = y });
            x += room + Gap;
        }

        return (items, y + Row);
    }

    /// <summary>The few words for what a shape means.</summary>
    private static string Meaning(NodeShape shape) => DiagramText.Format(shape switch
    {
        NodeShape.Decision => DiagramMessage.FlowDecision,
        NodeShape.Data => DiagramMessage.FlowData,
        _ => DiagramMessage.FlowTerminal
    });
}
