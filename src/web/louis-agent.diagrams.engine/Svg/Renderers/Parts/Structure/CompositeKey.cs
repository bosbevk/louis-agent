using LouisAgent.Diagrams.Composite.Spec;
using LouisAgent.Diagrams.Engine.Components;
using LouisAgent.Diagrams.Engine.Geometry;
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
/// the key can never show a pin or a line that looks other than the picture's own. The rule, the rows and the
/// words are every key's (<see cref="KeyParts"/>).
/// </remarks>
internal static class CompositeKey
{
    /// <summary>How long a line's sample is.</summary>
    private const double Line = 30;

    /// <summary>
    /// The key's items for a composite, across a width, and how tall the key is. An item only for what the
    /// picture has: a pin each way, a plain line, and a line that runs.
    /// </summary>
    public static (IReadOnlyList<CompositeKeyItem> Items, double Height) Of(CompositeSpec spec, double width)
    {
        var ports = spec.Ports ?? [];
        double pin = ComponentSizes.PinWidth(DiagramText.Format(DrawingMessage.CompositePort));
        var shown = new List<CompositeKeyItem>();
        if (ports.Any(port => port.Side == PortSide.In)) shown.Add(new(DrawingMessage.CompositeAsked, pin));
        if (ports.Any(port => port.Side == PortSide.Out)) shown.Add(new(DrawingMessage.CompositeNeeded, pin));
        if (spec.Connectors?.Count > 0) shown.Add(new(DrawingMessage.CompositeTogether, Line));
        if (ports.Count > 0) shown.Add(new(DrawingMessage.CompositePassedOn, Line));

        // Side by side, each after the one before, and on to a new row at the left when the width is full.
        return KeyRows.Place(shown, item => item.Sample, item => DiagramText.Format(item.Means),
            (item, x, y) => item with { X = x, Y = y }, width);
    }

    /// <summary>The key for a picture that wide, its top at a height, its pins in the class's tone.</summary>
    public static void Draw(SvgWriter writer, IReadOnlyList<CompositeKeyItem> items, string tone,
        IGeometry geometry, double top, double width)
    {
        KeyParts.Head(writer, geometry, top, width);
        foreach (var item in items)
        {
            var row = KeyParts.Row(geometry, item.X, top + item.Y, item.Sample);
            Sample(writer, item.Means, tone, row, geometry);
            KeyParts.Words(writer, row, DiagramText.Format(item.Means), "pic-means");
        }
    }

    /// <summary>An item's sample, by what the item explains, in its row.</summary>
    /// <remarks>A switch on what the item means, one arm each (ADR 0051).</remarks>
    private static void Sample(SvgWriter writer, DrawingMessage means, string tone, IBox row,
        IGeometry geometry)
    {
        string word = DiagramText.Format(DrawingMessage.CompositePort);
        switch (means)
        {
            // What is asked of the class: a pin that points down, as a real one does.
            case DrawingMessage.CompositeAsked:
                var asked = ComponentSizes.Pin(geometry, "", word, false, row.Left, row.Middle.Y);
                ComponentPins.Draw(writer, asked, tone, "cmp-key cmp-pin");
                break;
            // What the class needs: a pin that points up.
            case DrawingMessage.CompositeNeeded:
                var needed = ComponentSizes.Pin(geometry, "", word, true, row.Left, row.Middle.Y);
                ComponentPins.Draw(writer, needed, tone, "cmp-key cmp-pin");
                break;
            // Two parts that work together: a plain line.
            case DrawingMessage.CompositeTogether:
                writer.Path(KeyParts.Line(row), "cps-key cps-wire", false);
                break;
            // What is passed on: a line with its arrowhead. A sample stands still, so it has no dots.
            default:
                writer.Path(KeyParts.Line(row), "cps-key pic-line pic-edge", true);
                break;
        }
    }
}
