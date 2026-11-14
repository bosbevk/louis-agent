using System.Globalization;
using System.Xml.Linq;
using LouisAgent.Diagrams.Engine.Svg.Theme;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.Diagrams.Engine.Svg;

/// <summary>
/// The docs' frame round a finished picture: the caption as a note bar under the drawing, an info icon and its
/// lead in bold, the drawing itself as it was. Every kind of picture writes its own title and its own key inside
/// the drawing (F134.7 to F134.11), so the note is all the frame adds.
/// </summary>
/// <remarks>
/// Decorator (Refactoring.Guru): the frame wraps whatever a renderer drew without the renderer knowing, so the
/// docs ask for the framed picture and the chat's tool cards, which draw with this library too, ask for the bare
/// one. A flag on each renderer would put the same note in every one of them.
/// </remarks>
public static class TitledFrame
{
    /// <summary>
    /// How wide a character of the note's 12px font is, measured generously, since no font is measured.
    /// </summary>
    private const double NoteChar = 7.2;

    /// <summary>The room between the drawing and the note.</summary>
    private const double Gap = 16;

    /// <summary>
    /// The note's room round its words, a note line's height, where its words start, its icon's size.
    /// </summary>
    private const double NotePad = 10, NoteLine = 17, NoteLeft = 38, NoteIcon = 16;

    /// <summary>The picture inside the frame: the drawing as it was, and the note under it.</summary>
    public static string Around(string svg, DiagramSource source)
    {
        var root = XElement.Parse(svg);
        var view = root.Attribute("viewBox")!.Value.Split(' ')
            .Select(number => double.Parse(number, CultureInfo.InvariantCulture)).ToArray();
        double pad = -view[0], width = view[2] - 2 * pad, height = view[3] - 2 * pad;

        // The note is as tall as its words need at the drawing's width.
        var note = source.Caption is { } caption ? Lines(caption, width - NoteLeft - NotePad) : [];
        double noteHeight = note.Count == 0 ? 0 : 2 * NotePad + NoteLine * note.Count;

        // Everything the renderer drew on the ground is kept together, whole, and the note goes under it.
        var ground = root.Elements().First(element => (string?)element.Attribute("class") == "ground");
        var drawing = ground.ElementsAfterSelf().ToList();
        drawing.ForEach(element => element.Remove());
        var frame = new SvgWriter().Group("drawing", 0, 0, drawing);
        if (note.Count > 0) NoteBar(frame, note, width, height + Gap, noteHeight);
        ground.AddAfterSelf(frame.Shapes);

        // The picture grows by the note, and its style learns the note's look.
        double outer = view[3] + (note.Count == 0 ? 0 : Gap + noteHeight);
        root.SetAttributeValue("viewBox",
            $"{SvgWriter.N(view[0])} {SvgWriter.N(view[1])} {SvgWriter.N(view[2])} {SvgWriter.N(outer)}");
        root.SetAttributeValue("height", SvgWriter.N(outer));
        ground.SetAttributeValue("height", SvgWriter.N(outer));
        var style = root.Elements().First(element => element.Name.LocalName == "style");
        style.Value += SvgStyle.Frame(SvgTheme.Site);
        return SvgWriter.Serialize(root);
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
    /// A caption wrapped to a width, each line its spans: the part up to the first colon is the head, in bold, and
    /// the rest plain. A caption without a colon is all plain.
    /// </summary>
    private static List<List<(string, string)>> Lines(string text, double width)
    {
        int colon = text.IndexOf(": ", StringComparison.Ordinal);
        (string head, string rest) = colon >= 0 ? (text[..(colon + 1)], text[(colon + 2)..]) : ("", text);
        var words = head.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(word => (word, "note-head"))
            .Concat(rest.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(word => (word, "note-rest")));

        // Words taken in order while a line holds them; a word too long for any line stands on one of its own.
        int most = Math.Max(8, (int)(width / NoteChar));
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
