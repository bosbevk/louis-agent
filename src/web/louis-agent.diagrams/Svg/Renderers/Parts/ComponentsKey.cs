using LouisAgent.Choices;
using LouisAgent.Diagrams.Components;
using LouisAgent.Diagrams.Components.Placed;
using LouisAgent.Diagrams.Components.Placed.Keys;
using LouisAgent.Diagrams.Messages;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Svg.Renderers.Parts;

/// <summary>
/// Draws the key of a component diagram (F134.7): its heading, then a row for each thing the picture uses, each a
/// small sample of the thing with what it means beside it.
/// </summary>
/// <remarks>
/// A sample is drawn with the same shapes as the real thing, marked <c>cmp-key</c>, so the key can never show a
/// pin or a line that looks other than the picture's own.
/// </remarks>
internal static class ComponentsKey
{
    /// <summary>How wide a sample is, and where the words beside a swatch, a line and a pill start.</summary>
    private const double Sample = 76, SwatchWords = 38, Words = 88;

    /// <summary>The key, where the layout put it.</summary>
    public static void Draw(SvgWriter writer, PlacedKey key)
    {
        writer.Text(key.X, key.Y + 10, DiagramText.Format(DiagramMessage.ComponentKey), "pic-group", "start");
        foreach (var (row, at) in key.Rows.Select((row, at) => (row, at)))
            DrawRow(writer, row, key.X, key.Y + KeyLayout.Heading + at * KeyLayout.Row + KeyLayout.Row / 2);
    }

    /// <summary>One row: its sample at the left, by the row's kind, and its words beside it.</summary>
    private static void DrawRow(SvgWriter writer, KeyRow row, double x, double y)
    {
        double words = row.Kind switch
        {
            KeyRowKind.Tone => Swatch(writer, row, x, y),
            KeyRowKind.HandedDown => Pin(writer, DiagramMessage.ComponentHandedDown, false, Tone(row), x, y),
            KeyRowKind.SaidBackUp => Pin(writer, DiagramMessage.ComponentSaidBackUp, true, Tone(row), x, y),
            KeyRowKind.Reads => Read(writer, x, y),
            KeyRowKind.Holds => Pill(writer, Tone(row), x, y),
            _ => 0
        };
        writer.Text(x + words, y + 4, row.Words, "pic-note", "start");
    }

    /// <summary>A tone's sample: a small panel in that tone, with its marker. Says where its words start.</summary>
    private static double Swatch(SvgWriter writer, KeyRow row, double x, double y)
    {
        string tone = Tone(row);
        writer.Box(x, y - 8, 26, 16, 5, $"pic-swatch pic-tint pic-edge{tone}")
            .Box(x + 4, y - 4, 3, 8, 1.5, $"pic-fill{tone}");
        return SwatchWords;
    }

    /// <summary>A pin's sample: a real pin with its own word on it. Says where the words beside it start.</summary>
    private static double Pin(SvgWriter writer, DiagramMessage word, bool up, string tone, double x, double y)
    {
        string name = DiagramText.Format(word);
        var sample = new PlacedPin("", name, up, x, y, ComponentSizes.PinWidth(name));
        ComponentPins.Draw(writer, sample, tone, "cmp-key cmp-pin");
        return sample.Width + 12;
    }

    /// <summary>A read's sample: a short dashed line with its arrow. Says where its words start.</summary>
    private static double Read(SvgWriter writer, double x, double y)
    {
        string line = $"M{SvgWriter.N(x)} {SvgWriter.N(y)}H{SvgWriter.N(x + Sample)}";
        writer.Path(line, "cmp-key pic-line pic-edge reads", true);
        return Words;
    }

    /// <summary>A held thing's sample: a small pill with its dot. Says where its words start.</summary>
    private static double Pill(SvgWriter writer, string tone, double x, double y)
    {
        writer.Box(x, y - 10, Sample, 20, 10, $"cmp-key cmp-pill pic-tint pic-edge{tone}")
            .Circle(x + 12, y, 3.5, $"pic-fill{tone}");
        return Words;
    }

    /// <summary>A row's tone as a shape's class, with the space before it. Nothing for plain.</summary>
    private static string Tone(KeyRow row) => row.Tone == DiagramTone.Plain ? "" : $" tone-{row.Tone.Wire()}";
}
