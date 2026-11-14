using LouisAgent.Diagrams.Composite.Spec;
using LouisAgent.Diagrams.Engine.Components;
using LouisAgent.Diagrams.Engine.Components.Placed;
using LouisAgent.Diagrams.Engine.Keys;
using LouisAgent.Diagrams.Engine.Messages;
using LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Nesting;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Structure;

/// <summary>
/// Places and draws the key of a composite structure diagram (F134.13): a faint rule, its heading, then an item
/// for each thing the picture really has, a small sample of it with what it means beside it.
/// </summary>
/// <remarks>
/// A sample is drawn with the shapes and classes of the real thing, a pin by the code that draws a real pin, so
/// the key can never show a pin or a line that looks other than the picture's own. The rule, the heading and the
/// rows stand as they do in a flow's key.
/// </remarks>
internal static class CompositeKey
{
    /// <summary>
    /// A row's height and how far after its sample an item's words start, which are every key's, where the
    /// heading's words stand under the rule, how far under a row's middle an item's words do, and how long a
    /// line's sample is.
    /// </summary>
    private const double Row = KeyRows.Row, Words = KeyRows.Words, HeadingLine = 22, WordDrop = 4.5, Line = 30;

    /// <summary>
    /// The key's items for a composite, across a width, and how tall the key is. An item only for what the
    /// picture has: a pin each way, a plain line, and a line that runs.
    /// </summary>
    public static (IReadOnlyList<(DrawingMessage Means, double X, double Y, double Wide)> Items, double Height) Of(
        CompositeSpec spec, double width)
    {
        var ports = spec.Ports ?? [];
        double pin = ComponentSizes.PinWidth(DiagramText.Format(DrawingMessage.CompositePort));
        var shown = new List<(DrawingMessage Means, double Wide)>();
        if (ports.Any(port => port.Side == PortSide.In)) shown.Add((DrawingMessage.CompositeAsked, pin));
        if (ports.Any(port => port.Side == PortSide.Out)) shown.Add((DrawingMessage.CompositeNeeded, pin));
        if (spec.Connectors?.Count > 0) shown.Add((DrawingMessage.CompositeTogether, Line));
        if (ports.Count > 0) shown.Add((DrawingMessage.CompositePassedOn, Line));

        // Side by side, each after the one before, and on to a new row at the left when the width is full.
        return KeyRows.Place(shown.Select(item => (item.Means, X: 0.0, Y: 0.0, item.Wide)), item => item.Wide,
            item => DiagramText.Format(item.Means), (item, x, y) => (item.Means, x, y, item.Wide), width);
    }

    /// <summary>The key for a picture that wide, its top at a height, its pins in the class's tone.</summary>
    public static void Draw(SvgWriter writer,
        IReadOnlyList<(DrawingMessage Means, double X, double Y, double Wide)> items, string tone, double top,
        double width)
    {
        writer.Box(0, top, width, 1, 0, "flow-rule")
            .Text(0, top + HeadingLine, DiagramText.Format(DrawingMessage.KeyHeading), "pic-group", "start");
        foreach (var (means, x, y, wide) in items)
        {
            double middle = top + y + Row / 2;
            Sample(writer, means, tone, x, middle, wide);
            writer.Text(x + wide + Words, middle + WordDrop, DiagramText.Format(means), "pic-means", "start");
        }
    }

    /// <summary>An item's sample, by what the item explains, its left at a point and centred on a height.</summary>
    /// <remarks>A switch on what the item means, one arm each (ADR 0051).</remarks>
    private static void Sample(SvgWriter writer, DrawingMessage means, string tone, double x, double y, double wide)
    {
        string word = DiagramText.Format(DrawingMessage.CompositePort);
        string line = $"M{SvgWriter.N(x)} {SvgWriter.N(y)}H{SvgWriter.N(x + wide)}";
        switch (means)
        {
            // What is asked of the class: a pin that points down, as a real one does.
            case DrawingMessage.CompositeAsked:
                ComponentPins.Draw(writer, new PlacedPin("", word, false, x, y, wide), tone, "cmp-key cmp-pin");
                break;
            // What the class needs: a pin that points up.
            case DrawingMessage.CompositeNeeded:
                ComponentPins.Draw(writer, new PlacedPin("", word, true, x, y, wide), tone, "cmp-key cmp-pin");
                break;
            // Two parts that work together: a plain line.
            case DrawingMessage.CompositeTogether:
                writer.Path(line, "cps-key cps-wire", false);
                break;
            // What is passed on: a line with its arrowhead. A sample stands still, so it has no dots.
            default:
                writer.Path(line, "cps-key pic-line pic-edge", true);
                break;
        }
    }
}
