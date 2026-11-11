using System.Globalization;
using System.Xml.Linq;
using LouisAgent.Choices;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Messages;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Svg.Theme;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Svg;

/// <summary>
/// The docs' frame round a finished picture (CL60.2, Louis's pick "Both looks"): a navy title bar on top, an icon,
/// a rule and the title, its part before the first colon in bold; the drawing under it as it was; a legend of the
/// colours it uses; and the caption as a note bar under the drawing, an info icon and its lead in bold.
/// </summary>
/// <remarks>
/// Decorator (Refactoring.Guru): the frame wraps whatever a renderer drew, a flow or the cards, without either
/// renderer knowing, so the docs ask for the framed picture and the chat's tool cards, which will draw with this
/// library too, ask for the bare one. A flag on each renderer would put the same bar in two places, and a bar in
/// <see cref="SvgWriter"/> itself would leave no picture without it.
/// </remarks>
public static class TitledFrame
{
    /// <summary>
    /// How wide a character of the title's 15px font is, measured generously, since no font is measured.
    /// </summary>
    public const double TitleChar = 8.6;

    /// <summary>How wide a character of the note's 12px font is, measured the same way.</summary>
    private const double NoteChar = 7.2;

    /// <summary>
    /// The bar's room round its words, a title line's height, where the title starts, its icon's size.
    /// </summary>
    private const double BarPad = 14, TitleLine = 22, TitleLeft = 64, TitleIcon = 22;

    /// <summary>The room between the bar, the drawing and the note.</summary>
    private const double Gap = 16;

    /// <summary>
    /// The note's room round its words, a note line's height, where its words start, its icon's size.
    /// </summary>
    private const double NotePad = 10, NoteLine = 17, NoteLeft = 38, NoteIcon = 16;

    /// <summary>A legend swatch's size, a legend row's height, and the room after each of its items.</summary>
    private const double Swatch = 12, LegendRow = 20, LegendGap = 22;

    /// <summary>The picture inside the frame: the drawing moved down under the bar, the note under it.</summary>
    public static string Around(string svg, DiagramSource source)
    {
        var root = XElement.Parse(svg);
        var view = root.Attribute("viewBox")!.Value.Split(' ')
            .Select(number => double.Parse(number, CultureInfo.InvariantCulture)).ToArray();
        double pad = -view[0], width = view[2] - 2 * pad, height = view[3] - 2 * pad;

        // The bar and the note are as tall as their words need at the drawing's width.
        var title = Lines(source.Title, width - TitleLeft - BarPad, TitleChar, "title", boldWithoutColon: true);
        var note = source.Caption is { } caption
            ? Lines(caption, width - NoteLeft - NotePad, NoteChar, "note", boldWithoutColon: false)
            : [];
        double bar = 2 * BarPad + TitleLine * title.Count, shift = bar + Gap;
        double noteHeight = note.Count == 0 ? 0 : 2 * NotePad + NoteLine * note.Count;
        // The legend names the tones the chart uses, row after row as the width allows.
        var legend = LegendItems(source, width);
        double legendHeight = legend.Count == 0 ? 0 : Gap + LegendRow * (legend.Max(item => item.Row) + 1);

        // Everything the renderer drew on the ground moves down under the bar, whole.
        var ground = root.Elements().First(element => (string?)element.Attribute("class") == "ground");
        var drawing = ground.ElementsAfterSelf().ToList();
        drawing.ForEach(element => element.Remove());
        var frame = TitleBar(new SvgWriter(), title, width, bar).Group("drawing", 0, shift, drawing);
        foreach (var (tone, meaning, x, row) in legend)
        {
            double top = shift + height + Gap + LegendRow * row;
            frame.Box(x, top, Swatch, Swatch, 3, $"swatch tone-{tone}")
                .Text(x + Swatch + 6, top + Swatch - 1.5, meaning, "legend", "start");
        }

        if (note.Count > 0) NoteBar(frame, note, width, shift + height + legendHeight + Gap, noteHeight);
        ground.AddAfterSelf(frame.Shapes);

        // The picture grows by the bar, the legend and the note, and its style learns their look.
        double outer = view[3] + shift + legendHeight + (note.Count == 0 ? 0 : Gap + noteHeight);
        root.SetAttributeValue("viewBox",
            $"{SvgWriter.N(view[0])} {SvgWriter.N(view[1])} {SvgWriter.N(view[2])} {SvgWriter.N(outer)}");
        root.SetAttributeValue("height", SvgWriter.N(outer));
        ground.SetAttributeValue("height", SvgWriter.N(outer));
        var style = root.Elements().First(element => element.Name.LocalName == "style");
        style.Value += SvgStyle.Frame(SvgTheme.Site);
        return SvgWriter.Serialize(root);
    }

    /// <summary>
    /// The legend's swatches, one for each tone a flow's boxes or a sequence's heads wear, in the tones' order, each
    /// with what it means
    /// (Louis: "can we have a legend for the color"), placed left to right and onto a new row when the drawing's
    /// width is full; a chart without colours, and the cards, have none.
    /// </summary>
    private static List<(string Tone, string Meaning, double X, int Row)> LegendItems(DiagramSource source,
        double width)
    {
        var tones = (source.Flow?.Nodes ?? []).Select(node => node.Tone)
            .Concat((source.Sequence?.Participants ?? []).Select(head => head.Tone))
            .Where(tone => tone != DiagramTone.Plain).Distinct().Order();
        var items = new List<(string, string, double, int)>();
        double x = 0;
        int row = 0;
        foreach (var tone in tones)
        {
            string meaning = DiagramText.Format(Meaning(tone), new { });
            double room = Swatch + 6 + meaning.Length * NoteChar + LegendGap;
            if (x > 0 && x + room - LegendGap > width) (x, row) = (0, row + 1);
            items.Add((tone.Wire(), meaning, x, row));
            x += room;
        }

        return items;
    }

    /// <summary>What a tone means in every docs chart.</summary>
    private static DiagramMessage Meaning(DiagramTone tone) => tone switch
    {
        DiagramTone.Blue => DiagramMessage.LegendBlue,
        DiagramTone.Green => DiagramMessage.LegendGreen,
        DiagramTone.Violet => DiagramMessage.LegendViolet,
        DiagramTone.Teal => DiagramMessage.LegendTeal,
        DiagramTone.Red => DiagramMessage.LegendRed,
        _ => DiagramMessage.LegendAmber
    };

    /// <summary>The navy bar: its icon, a thin rule, and the title's lines after them.</summary>
    private static SvgWriter TitleBar(SvgWriter writer, List<List<(string, string)>> lines, double width, double bar)
    {
        writer.Box(0, 0, width, bar, 10, "titlebar")
            .Icon(BarPad + 2, bar / 2 - TitleIcon / 2, TitleIcon, DiagramIcon.Stack, "title-icon")
            .Box(TitleLeft - 14, BarPad, 1, bar - 2 * BarPad, 0, "title-rule");
        for (int at = 0; at < lines.Count; at++)
            writer.Spans(TitleLeft, BarPad + 16 + TitleLine * at, "heading", lines[at]);
        return writer;
    }

    /// <summary>The note bar under the drawing: an info icon by its first line, and the caption's lines.</summary>
    private static void NoteBar(SvgWriter writer, List<List<(string, string)>> lines, double width, double top,
        double height)
    {
        writer.Box(0, top, width, height, 8, "notebar")
            .Icon(12, top + NotePad, NoteIcon, DiagramIcon.Info, "note-icon");
        for (int at = 0; at < lines.Count; at++)
            writer.Spans(NoteLeft, top + NotePad + 12 + NoteLine * at, "notes", lines[at]);
    }

    /// <summary>
    /// Words wrapped to a width, each line its spans: the part up to the first colon is the head, in bold, and the
    /// rest plain; words without a colon are all head in a title, as a card overview's is, and all plain in a note.
    /// </summary>
    private static List<List<(string, string)>> Lines(string text, double width, double charWidth, string kind,
        bool boldWithoutColon)
    {
        int colon = text.IndexOf(": ", StringComparison.Ordinal);
        (string head, string rest) = (colon, boldWithoutColon) switch
        {
            ( >= 0, _) => (text[..(colon + 1)], text[(colon + 2)..]),
            (_, true) => (text, ""),
            _ => ("", text)
        };
        var words = head.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(word => (word, $"{kind}-head"))
            .Concat(rest.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(word => (word, $"{kind}-rest")));

        // Words taken in order while a line holds them; a word too long for any line stands on one of its own.
        int most = Math.Max(8, (int)(width / charWidth));
        var lines = new List<List<(string Word, string Cls)>> { new() };
        foreach (var word in words)
        {
            int length = lines[^1].Sum(taken => taken.Word.Length + 1) + word.Item1.Length;
            if (lines[^1].Count > 0 && length > most) lines.Add([]);
            lines[^1].Add(word);
        }

        // Each line's words in one span a class: the head's, then the rest's.
        return
        [
            .. lines.Select(line => line.GroupBy(taken => taken.Cls)
                .Select(span => (string.Join(' ', span.Select(taken => taken.Word)), span.Key)).ToList())
        ];
    }
}
