using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Tickets.Placed;
using LouisAgent.Diagrams.Tickets.Spec;

namespace LouisAgent.Diagrams.Engine.Tickets;

/// <summary>
/// Where each part of a ticket stands, at the width of a chat's card: the band with its kicker, title and subtitle
/// and the status pill at its top right, the rows under the torn edge, each an icon box beside its label and value,
/// and the note's strip at the foot. Long words wrap, and the card grows to hold them.
/// </summary>
/// <remarks>
/// Every line of words is placed here, with how wide it runs in its font, so the renderer only draws what the layout
/// decided and a test measures exactly what will be drawn (the same split as the flows' and the cards' layouts).
/// </remarks>
public static class TicketLayout
{
    /// <summary>The space kept inside the card's edges.</summary>
    public const double Pad = 16;

    /// <summary>A row's icon box, the room between it and the row's words, and the room between two rows.</summary>
    private const double IconBox = 32, IconGap = 12, RowGap = 12;

    /// <summary>
    /// The pill: its top, its height, the room before its words, where its dot stands, and after them, and the
    /// room between it and the band's words.
    /// </summary>
    private const double PillTop = 14, PillHeight = 22, PillPad = 22, PillEnd = 10, PillGap = 10;

    /// <summary>
    /// The band: the kicker's first line and a line of it, the room between it and the title's first line, a line
    /// of the title, how far under the title's last line the subtitle starts, and a line of the subtitle.
    /// </summary>
    private const double KickerTop = 24, KickerLine = 14, TitleGap = 16, TitleLine = 28, SubtitleDrop = 20,
        SubtitleLine = 16;

    /// <summary>
    /// A row: its label's line under its top, its value's first line, a line of its value, and how far under
    /// its value's last line its words end.
    /// </summary>
    private const double LabelLine = 13, ValueTop = 31, ValueLine = 18, ValueFoot = 5;

    /// <summary>The note: its first line under its strip's top, a line of it, and the room under its last.</summary>
    private const double NoteTop = 20, NoteLine = 16, NoteFoot = 14;

    /// <summary>
    /// The ticket laid out, 320 wide unless told otherwise: a phone's chat gives it about 275, so it shrinks little.
    /// Its boxes and points are made by a geometry: the one in use when none is told.
    /// </summary>
    public static PlacedTicket Place(TicketSpec spec, double width = 320, IGeometry? geometry = null)
    {
        geometry ??= Geometries.InUse;
        var words = new List<PlacedWords>();
        double inside = width - 2 * Pad;

        // The pill at the top right takes its room off the band's words, line by line.
        var pill = spec.Status is { } status ? Pill(status, width, geometry) : null;
        double besidePill = inside - (pill is null ? 0 : pill.Box.Width + PillGap);

        // The kicker shares its line with the pill; the title stands under both, across the whole band, and the
        // subtitle under the title's last line.
        var kicker = geometry.Point(Pad, KickerTop);
        var title = geometry.Point(Pad, Stack(words, spec.Kicker, TicketWords.Kicker, kicker, besidePill, KickerLine)
                                        + TitleGap);
        double afterTitle = Stack(words, spec.Title, TicketWords.Title, title, inside, TitleLine);
        var subtitle = geometry.Point(Pad, afterTitle - TitleLine + SubtitleDrop);
        double band = Stack(words, spec.Subtitle ?? "", TicketWords.Subtitle, subtitle, inside, SubtitleLine);

        var (rows, rowsEnd) = PlaceRows(spec, width, band + Pad, words, geometry);
        if (spec.Note is not { } note) return new PlacedTicket(width, rowsEnd + Pad, band, pill, rows, null, words);

        // The note's strip runs to the card's foot, its words wrapped inside it.
        double noteY = rowsEnd + Pad;
        double afterNote = Stack(words, note, TicketWords.Note, geometry.Point(Pad, noteY + NoteTop), inside, NoteLine);
        return new PlacedTicket(width, afterNote - NoteLine + NoteFoot, band, pill, rows, noteY, words);
    }

    /// <summary>Each row's icon box and words, one under another; where the last one ends.</summary>
    private static (IReadOnlyList<PlacedRow> Rows, double End) PlaceRows(TicketSpec spec, double width, double top,
        List<PlacedWords> words, IGeometry geometry)
    {
        var rows = new List<PlacedRow>();
        double textX = Pad + IconBox + IconGap, textWidth = width - textX - Pad;
        foreach (var row in spec.Rows)
        {
            var value = geometry.Point(textX, top + ValueTop);
            words.Add(new PlacedWords(geometry.Point(textX, top + LabelLine), row.Label, TicketWords.Label));
            double lastLine = Stack(words, row.Value, TicketWords.Value, value, textWidth, ValueLine) - ValueLine;

            // A row is as tall as its icon's box, or as its words when its value runs over a line.
            var box = geometry.Box(Pad, top, width - 2 * Pad, Math.Max(IconBox, lastLine + ValueFoot - top));
            rows.Add(new PlacedRow(row.Icon, box, geometry.Box(Pad, top, IconBox, IconBox)));
            top = box.Bottom + RowGap;
        }

        return (rows, top - RowGap);
    }

    /// <summary>The pill at the band's top right, as wide as its words and the dot before them need.</summary>
    private static PlacedPill Pill(TicketStatus status, double width, IGeometry geometry)
    {
        double wide = PillPad + TicketWords.Status.Face.Width(status.Label) + PillEnd;
        return new PlacedPill(geometry.Box(width - Pad - wide, PillTop, wide, PillHeight), status.Label, status.Tone);
    }

    /// <summary>
    /// Words of one kind wrapped to the room they have, a line under a line from where the first stands. Says
    /// where the line after their last would stand.
    /// </summary>
    private static double Stack(List<PlacedWords> words, string text, TicketWords kind, IPoint first, double room,
        double step)
    {
        var lines = TextWrap.Lines(text, (int)Math.Floor(kind.Face.Holds(room)));
        words.AddRange(lines.Select((line, at) => new PlacedWords(first.Moved(0, at * step), line, kind)));
        return first.Y + lines.Count * step;
    }
}
