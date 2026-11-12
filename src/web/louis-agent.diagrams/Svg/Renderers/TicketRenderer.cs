using System.Globalization;
using LouisAgent.Choices;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Svg.Theme;
using LouisAgent.Diagrams.Tickets;
using LouisAgent.Diagrams.Tickets.Placed;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Svg.Renderers;

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
    private const double Radius = 14, Notch = 8;

    /// <inheritdoc />
    public DiagramKind Kind => DiagramKind.Ticket;

    /// <inheritdoc />
    public string Render(DiagramSource source)
    {
        var placed = TicketLayout.Place(source.Ticket!);
        double w = placed.Width, band = placed.BandHeight;
        var writer = new SvgWriter().Path(Card(w, placed.Height, band), "ticket", false);

        // The band's top corners round with the card's; its foot is the torn edge, notched at both ends.
        writer.Path($"M{N(Radius)} 0 H{N(w - Radius)} A{N(Radius)} {N(Radius)} 0 0 1 {N(w)} {N(Radius)}" +
                    $" V{N(band - Notch)} A{N(Notch)} {N(Notch)} 0 0 0 {N(w - Notch)} {N(band)} H{N(Notch)}" +
                    $" A{N(Notch)} {N(Notch)} 0 0 0 0 {N(band - Notch)} V{N(Radius)}" +
                    $" A{N(Radius)} {N(Radius)} 0 0 1 {N(Radius)} 0 Z", "ticket-band", false);
        if (placed.Pill is { } pill) DrawPill(writer, pill);
        writer.Path($"M{N(Notch + 4)} {N(band)} H{N(w - Notch - 4)}", "ticket-tear", false);

        foreach (var row in placed.Rows)
            writer.Box(row.X, row.Y, 32, 32, 10, "ticket-icon-box")
                .Icon(row.X + 8, row.Y + 8, 16, row.Icon, "ticket-icon");

        // The note's strip runs from its line to the card's foot, its bottom corners round with the card's.
        if (placed.NoteY is { } top)
            writer.Path($"M0 {N(top)} H{N(w)} V{N(placed.Height - Radius)} A{N(Radius)} {N(Radius)} 0 0 1" +
                        $" {N(w - Radius)} {N(placed.Height)} H{N(Radius)} A{N(Radius)} {N(Radius)} 0 0 1 0" +
                        $" {N(placed.Height - Radius)} Z", "ticket-foot", false);

        foreach (var words in placed.Words) writer.Text(words.X, words.Y, words.Text, words.Cls, "start");
        return writer.Write(SvgTheme.Site, source.Title, w, placed.Height, TicketStyle.Of(SvgTheme.Site));
    }

    /// <summary>
    /// The card's outline: rounded corners, and a half-circle bitten out of each side where the band ends, so
    /// whatever is behind the card shows through the notches, on a dark page or a light one.
    /// </summary>
    private static string Card(double w, double h, double band) =>
        $"M{N(Radius)} 0 H{N(w - Radius)} A{N(Radius)} {N(Radius)} 0 0 1 {N(w)} {N(Radius)} V{N(band - Notch)}" +
        $" A{N(Notch)} {N(Notch)} 0 0 0 {N(w)} {N(band + Notch)} V{N(h - Radius)}" +
        $" A{N(Radius)} {N(Radius)} 0 0 1 {N(w - Radius)} {N(h)} H{N(Radius)}" +
        $" A{N(Radius)} {N(Radius)} 0 0 1 0 {N(h - Radius)} V{N(band + Notch)}" +
        $" A{N(Notch)} {N(Notch)} 0 0 0 0 {N(band - Notch)} V{N(Radius)}" +
        $" A{N(Radius)} {N(Radius)} 0 0 1 {N(Radius)} 0 Z";

    /// <summary>The pill: its box in its tone, a dot, and its words.</summary>
    private static void DrawPill(SvgWriter writer, PlacedPill pill)
    {
        string tone = pill.Tone == DiagramTone.Plain ? "" : $" tone-{pill.Tone.Wire()}";
        writer.Box(pill.X, pill.Y, pill.W, pill.H, pill.H / 2, $"ticket-pill{tone}")
            .Circle(pill.X + 12, pill.Y + pill.H / 2, 3, "ticket-pill-text")
            .Text(pill.X + 20, pill.Y + 15, pill.Label, "ticket-pill-text", "start");
    }

    /// <summary>A number as the file writes it.</summary>
    private static string N(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
