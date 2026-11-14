using LouisAgent.Diagrams.Engine.Components;
using LouisAgent.Diagrams.Engine.Components.Placed.Keys;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Keys;
using LouisAgent.Diagrams.Engine.Messages;
using LouisAgent.Diagrams.Engine.Svg.Paths;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Nesting;

/// <summary>
/// Draws the key of a component diagram (F134.7): its heading, then a row for each thing the picture uses, each a
/// small sample of the thing with what it means beside it.
/// </summary>
/// <remarks>
/// A sample is drawn with the same shapes as the real thing, marked <c>cmp-key</c>, so the key can never show a
/// pin or a line that looks other than the picture's own. A tone's sample is a small badge filled with it, as in
/// a flow's key, since no panel is tinted in its tone (F134.12, Louis of the swatches it had: "old look").
/// </remarks>
internal static class ComponentsKey
{
    /// <summary>How wide the sample of a line and of a pill is, and the room between a sample and its words.</summary>
    private const double Sample = 76, AfterSample = 12;

    /// <summary>A tone's badge, its corner, and how far after it its words start.</summary>
    private const double Badge = 18, BadgeCorner = 6, BadgeWords = 10;

    /// <summary>
    /// How far under the key's top its heading's line is, how tall a pill's sample is, where the pill's dot stands
    /// from its left, and the dot's size.
    /// </summary>
    private const double HeadingLine = 10, PillTall = 20, DotIn = 12, Dot = 3.5;

    /// <summary>The key, where the layout put it.</summary>
    public static void Draw(SvgWriter writer, PlacedKey key, IGeometry geometry)
    {
        string heading = DiagramText.Format(DrawingMessage.KeyHeading);
        writer.Text(key.Box.Left, key.Box.Top + HeadingLine, heading, "pic-group", "start");
        foreach (var (row, at) in key.Rows.Select((row, at) => (row, at)))
        {
            // One row: its sample at the left, and its words beside it, a little under the row's middle line.
            var line = KeyLayout.RowBox(key, at, geometry);
            double words = Sampled(writer, row, line, geometry);
            writer.Text(line.Left + words, line.Middle.Y + KeyParts.WordDrop, row.Words, "pic-means", "start");
        }
    }

    /// <summary>
    /// A row's sample, by the row's kind, at the left of its line. Says how far from there its words start.
    /// </summary>
    /// <remarks>A switch on the row's kind, one arm each (ADR 0051).</remarks>
    private static double Sampled(SvgWriter writer, KeyRow row, IBox line, IGeometry geometry) => row.Kind switch
    {
        KeyRowKind.Tone => ToneBadge(writer, row, line, geometry),
        KeyRowKind.HandedDown => Pin(writer, DrawingMessage.ComponentHandedDown, false, row, line, geometry),
        KeyRowKind.SaidBackUp => Pin(writer, DrawingMessage.ComponentSaidBackUp, true, row, line, geometry),
        KeyRowKind.Reads => Read(writer, line),
        KeyRowKind.Holds => Pill(writer, row, line, geometry),
        _ => 0
    };

    /// <summary>A tone's sample: a small badge filled with that tone. Says where its words start.</summary>
    private static double ToneBadge(SvgWriter writer, KeyRow row, IBox line, IGeometry geometry)
    {
        var badge = KeyParts.Sample(geometry, line, Badge, Badge);
        writer.Box(badge, BadgeCorner, $"pic-fill{PictureParts.Tone(row.Tone)}");
        return Badge + BadgeWords;
    }

    /// <summary>A pin's sample: a real pin with its own word on it. Says where the words beside it start.</summary>
    private static double Pin(SvgWriter writer, DrawingMessage word, bool up, KeyRow row, IBox line,
        IGeometry geometry)
    {
        var sample = ComponentSizes.Pin(geometry, "", DiagramText.Format(word), up, line.Left, line.Middle.Y);
        ComponentPins.Draw(writer, sample, PictureParts.Tone(row.Tone), "cmp-key cmp-pin");
        return sample.Box.Width + AfterSample;
    }

    /// <summary>A read's sample: a short dashed line with its arrow. Says where its words start.</summary>
    private static double Read(SvgWriter writer, IBox line)
    {
        string path = PathWriter.Level(line.Left, line.Left + Sample, line.Middle.Y);
        writer.Path(path, "cmp-key pic-line pic-edge reads", true);
        return Sample + AfterSample;
    }

    /// <summary>A held thing's sample: a small pill with its dot. Says where its words start.</summary>
    private static double Pill(SvgWriter writer, KeyRow row, IBox line, IGeometry geometry)
    {
        string tone = PictureParts.Tone(row.Tone);
        var pill = KeyParts.Sample(geometry, line, Sample, PillTall);
        writer.Box(pill, pill.HalfHeight, $"cmp-key cmp-pill pic-tint pic-edge{tone}")
            .Circle(pill.Left + DotIn, line.Middle.Y, Dot, $"pic-fill{tone}");
        return Sample + AfterSample;
    }
}
