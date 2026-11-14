using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Svg.Paths;
using LouisAgent.Diagrams.Engine.Svg.Renderers.Parts;
using LouisAgent.Diagrams.Engine.Svg.Theme;
using LouisAgent.Diagrams.Engine.Tickets;
using LouisAgent.Diagrams.Engine.Tickets.Placed;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers;

/// <summary>
/// Draws a ticket (F127.4.1): the card, its band with rounded top corners, the status pill, the torn edge with a
/// notch either side, an icon box per row, the note's strip at the foot, and every line of words where the layout
/// put it.
/// </summary>
/// <remarks>
/// One more Strategy beside the other kinds' renderers, picked by <see cref="DiagramRenderers"/>: it decides nothing
/// about where things go, only how they look, so <see cref="TicketLayout"/>'s numbers are the ones its tests check.
/// </remarks>
internal sealed class TicketRenderer : IDiagramRenderer
{
    /// <summary>
    /// The card's corner, the notch bitten out of each side where the band ends, and how far inside the notches
    /// the torn edge starts and ends.
    /// </summary>
    private const double Radius = 14, Notch = 8, TearIn = 4;

    /// <summary>An icon box's corner, the icon's size, and how far inside its box the icon stands.</summary>
    private const double IconCorner = 10, IconSize = 16, IconIn = 8;

    /// <summary>
    /// The pill: where its dot stands from its left, the dot's size, where its words start, and their line under
    /// its top.
    /// </summary>
    private const double DotIn = 12, Dot = 3, StatusIn = 20, StatusLine = 15;

    /// <inheritdoc />
    public DiagramKind Kind => DiagramKind.Ticket;

    /// <inheritdoc />
    public string Render(DiagramSource source)
    {
        var placed = TicketLayout.Place(source.Ticket!, geometry: Geometries.InUse);
        double w = placed.Width, h = placed.Height, band = placed.BandHeight;
        var writer = new SvgWriter().Path(Card(w, h, band), "ticket", false);

        // The band's top corners round with the card's; its foot is the torn edge, notched at both ends.
        var bandPath = Bite(TopEdge(w).Down(band - Notch), w - Notch, band).Across(Notch);
        writer.Path(Closed(Bite(bandPath, 0, band - Notch)), "ticket-band", false);
        if (placed.Pill is { } pill) DrawPill(writer, pill);
        writer.Path(PathWriter.Level(Notch + TearIn, w - Notch - TearIn, band), "ticket-tear", false);

        foreach (var row in placed.Rows)
            writer.Box(row.IconBox, IconCorner, "ticket-icon-box")
                .Icon(row.IconBox.Left + IconIn, row.IconBox.Top + IconIn, IconSize, row.Icon, "ticket-icon");

        // The note's strip runs from its line to the card's foot, its bottom corners round with the card's.
        if (placed.NoteY is { } top)
            writer.Path(Foot(new PathWriter().From(0, top).Across(w), w, h).Close().Text, "ticket-foot", false);

        foreach (var words in placed.Words)
            writer.Text(words.At.X, words.At.Y, words.Text, words.Kind.Cls, "start");
        return writer.Write(SvgTheme.Site, source.Title, w, h, TicketStyle.Of(SvgTheme.Site));
    }

    /// <summary>
    /// The card's outline: rounded corners, and a half-circle bitten out of each side where the band ends, so
    /// whatever is behind the card shows through the notches, on a dark page or a light one.
    /// </summary>
    private static string Card(double w, double h, double band)
    {
        var right = Bite(TopEdge(w).Down(band - Notch), w, band + Notch);
        var left = Foot(right, w, h).Down(band + Notch);
        return Closed(Bite(left, 0, band - Notch));
    }

    /// <summary>An outline's start: along the card's top from after its left corner, and round its right one.</summary>
    private static PathWriter TopEdge(double w) =>
        new PathWriter().From(Radius, 0).Across(w - Radius).Arc(Radius, Radius, false, true, w, Radius);

    /// <summary>An outline's end: up the card's left side and round its top left corner, to where it started.</summary>
    private static string Closed(PathWriter path) =>
        path.Down(Radius).Arc(Radius, Radius, false, true, Radius, 0).Close().Text;

    /// <summary>
    /// The card's foot on an outline: down its right side, round its two bottom corners, to its left side.
    /// </summary>
    private static PathWriter Foot(PathWriter path, double w, double h) => path.Down(h - Radius)
        .Arc(Radius, Radius, false, true, w - Radius, h).Across(Radius).Arc(Radius, Radius, false, true, 0, h - Radius);

    /// <summary>A notch on an outline: a part of a circle bitten into the card, to a point.</summary>
    private static PathWriter Bite(PathWriter path, double x, double y) => path.Arc(Notch, Notch, false, false, x, y);

    /// <summary>The pill: its box in its tone, a dot, and its words.</summary>
    private static void DrawPill(SvgWriter writer, PlacedPill pill)
    {
        var box = pill.Box;
        string words = TicketWords.Status.Cls;
        writer.Box(box, box.HalfHeight, $"ticket-pill{PictureParts.Tone(pill.Tone)}")
            .Circle(box.Left + DotIn, box.Middle.Y, Dot, words)
            .Text(box.Left + StatusIn, box.Top + StatusLine, pill.Label, words, "start");
    }
}
