using LouisAgent.Diagrams.Tickets.Placed;
using LouisAgent.Diagrams.Tickets.Spec;

namespace LouisAgent.Diagrams.Tickets;

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

    private const double IconBox = 32, IconGap = 12, RowGap = 12;
    private const double PillHeight = 22, PillPad = 22, PillGap = 10;
    private const double TitleLine = 28, SubtitleLine = 16, ValueLine = 18, NoteLine = 16;

    /// <summary>How wide one character of each kind of words runs, in its font.</summary>
    public static double CharWidth(string cls) => cls switch
    {
        "ticket-kicker" => 7.4,
        "ticket-title" => 12.2,
        "ticket-subtitle" or "ticket-note" => 6.6,
        "ticket-label" => 6.3,
        "ticket-pill-text" => 6.6,
        _ => 7.8
    };

    /// <summary>
    /// The ticket laid out, 320 wide unless told otherwise: a phone's chat gives it about 275, so it shrinks little.
    /// </summary>
    public static PlacedTicket Place(TicketSpec spec, double width = 320)
    {
        var words = new List<PlacedWords>();

        // The pill at the top right takes its room off the band's words, line by line.
        PlacedPill? pill = spec.Status is { } status
            ? new PlacedPill(width - Pad - PillWidth(status.Label), 14, PillWidth(status.Label), PillHeight,
                status.Label, status.Tone)
            : null;
        double besidePill = width - 2 * Pad - (pill is null ? 0 : pill.W + PillGap);

        // The kicker shares its line with the pill; the title stands under both, across the whole band.
        double y = 24;
        foreach (string line in Wrap(spec.Kicker, besidePill, "ticket-kicker"))
        {
            words.Add(Line(Pad, y, line, "ticket-kicker"));
            y += 14;
        }

        y += 16;
        foreach (string line in Wrap(spec.Title, width - 2 * Pad, "ticket-title"))
        {
            words.Add(Line(Pad, y, line, "ticket-title"));
            y += TitleLine;
        }

        y -= TitleLine - 20;
        foreach (string line in Wrap(spec.Subtitle ?? "", width - 2 * Pad, "ticket-subtitle"))
        {
            words.Add(Line(Pad, y, line, "ticket-subtitle"));
            y += SubtitleLine;
        }

        double band = y;
        var (rows, rowsEnd) = PlaceRows(spec, width, band + Pad, words);
        if (spec.Note is not { } note) return new PlacedTicket(width, rowsEnd + Pad, band, pill, rows, null, words);

        // The note's strip runs to the card's foot, its words wrapped inside it.
        double noteY = rowsEnd + Pad, baseline = noteY + 20;
        var noteLines = Wrap(note, width - 2 * Pad, "ticket-note");
        foreach (string line in noteLines)
        {
            words.Add(Line(Pad, baseline, line, "ticket-note"));
            baseline += NoteLine;
        }

        return new PlacedTicket(width, baseline - NoteLine + 14, band, pill, rows, noteY, words);
    }

    /// <summary>Each row's icon box and words, one under another; where the last one ends.</summary>
    private static (IReadOnlyList<PlacedRow> Rows, double End) PlaceRows(TicketSpec spec, double width, double top,
        List<PlacedWords> words)
    {
        var rows = new List<PlacedRow>();
        double textX = Pad + IconBox + IconGap, textWidth = width - textX - Pad;
        foreach (var row in spec.Rows)
        {
            words.Add(Line(textX, top + 13, row.Label, "ticket-label"));
            var values = Wrap(row.Value, textWidth, "ticket-value");
            for (int at = 0; at < values.Count; at++)
                words.Add(Line(textX, top + 31 + at * ValueLine, values[at], "ticket-value"));

            double height = Math.Max(IconBox, 36 + (values.Count - 1) * ValueLine);
            rows.Add(new PlacedRow(row.Icon, Pad, top, height));
            top += height + RowGap;
        }

        return (rows, top - RowGap);
    }

    /// <summary>The pill's width: its words and the dot before them, with room either side.</summary>
    private static double PillWidth(string label) => PillPad + label.Length * CharWidth("ticket-pill-text") + 10;

    /// <summary>Words wrapped to the room they have, in their own font.</summary>
    private static IReadOnlyList<string> Wrap(string text, double room, string cls) =>
        TextWrap.Lines(text, (int)Math.Floor(room / CharWidth(cls)));

    /// <summary>One line of words where it stands, with its width in its font.</summary>
    private static PlacedWords Line(double x, double y, string text, string cls) =>
        new(x, y, text.Length * CharWidth(cls), text, cls);
}
