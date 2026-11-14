using System.Globalization;
using System.Xml.Linq;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Svg.Theme;
using LouisAgent.Diagrams.Engine.Words;
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
    /// <summary>The room between the drawing and the note.</summary>
    private const double Gap = 16;

    /// <summary>
    /// The note's room round its words, a note line's height, where its words start, its icon's size.
    /// </summary>
    private const double NotePad = 10, NoteLine = 17, NoteLeft = 38, NoteIcon = 16;

    /// <summary>
    /// The note bar's corner, where its icon stands from its left, and how far under the bar's own room its
    /// first line of words stands.
    /// </summary>
    private const double NoteCorner = 8, IconLeft = 12, FirstLine = 12;

    /// <summary>However narrow the drawing, a line of the note holds at least this many letters.</summary>
    private const int FewestLetters = 8;

    /// <summary>The note's words in their 12px font, measured generously, since no font is measured.</summary>
    private static readonly Face NoteFace = new(7.2);

    /// <summary>
    /// The picture inside the frame: the drawing as it was, and the note under it, in a bar made by the geometry
    /// the picture was drawn with.
    /// </summary>
    public static string Around(string svg, DiagramSource source, IGeometry geometry)
    {
        var root = XElement.Parse(svg);

        // What the file shows, and inside it the drawing: the file keeps the same room round it on every side.
        var shown = Shown(root, geometry);
        var drawn = shown.Grown(shown.Left);

        // The note is as tall as its words need at the drawing's width.
        var note = source.Caption is { } caption ? Lines(caption, drawn.Width - NoteLeft - NotePad) : [];
        double noteHeight = note.Count == 0 ? 0 : 2 * NotePad + NoteLine * note.Count;

        // Everything the renderer drew on the ground is kept together, whole, and the note goes under it.
        var ground = root.Elements().First(element => (string?)element.Attribute("class") == "ground");
        var drawing = ground.ElementsAfterSelf().ToList();
        drawing.ForEach(element => element.Remove());
        var frame = new SvgWriter().Group("drawing", 0, 0, drawing);
        var bar = geometry.Box(drawn.Left, drawn.Bottom + Gap, drawn.Width, noteHeight);
        if (note.Count > 0) NoteBar(frame, note, bar);
        ground.AddAfterSelf(frame.Shapes);

        // The picture grows by the note, and its style learns the note's look.
        string outer = SvgWriter.N(shown.Grown(0, 0, 0, note.Count == 0 ? 0 : Gap + noteHeight).Height);
        root.SetAttributeValue("viewBox",
            $"{SvgWriter.N(shown.Left)} {SvgWriter.N(shown.Top)} {SvgWriter.N(shown.Width)} {outer}");
        root.SetAttributeValue("height", outer);
        ground.SetAttributeValue("height", outer);
        var style = root.Elements().First(element => element.Name.LocalName == "style");
        style.Value += SvgStyle.Frame;
        return SvgWriter.Serialize(root);
    }

    /// <summary>What a picture's file shows, as a box: its <c>viewBox</c>, which is a corner and a size.</summary>
    private static IBox Shown(XElement root, IGeometry geometry)
    {
        var view = root.Attribute("viewBox")!.Value.Split(' ')
            .Select(number => double.Parse(number, CultureInfo.InvariantCulture)).ToArray();
        return geometry.Box(view[0], view[1], view[2], view[3]);
    }

    /// <summary>The note bar under the drawing: an info icon by its first line, and the caption's lines.</summary>
    private static void NoteBar(SvgWriter writer, List<List<(string, string)>> lines, IBox bar)
    {
        writer.Box(bar, NoteCorner, "notebar")
            .Icon(bar.Left + IconLeft, bar.Top + NotePad, NoteIcon, DiagramIcon.Info, "note-icon");
        for (int at = 0; at < lines.Count; at++)
            writer.Spans(bar.Left + NoteLeft, bar.Top + NotePad + FirstLine + NoteLine * at, "notes", lines[at]);
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
        int most = Math.Max(FewestLetters, (int)NoteFace.Holds(width));
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
