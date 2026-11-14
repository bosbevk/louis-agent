using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Svg.Paths;
using LouisAgent.Diagrams.Engine.Svg.Renderers.Parts;
using LouisAgent.Diagrams.Engine.Svg.Theme;
using LouisAgent.Diagrams.Engine.Svg.Theme.Looks;
using LouisAgent.Diagrams.Engine.Tickets;
using LouisAgent.Diagrams.Engine.Tickets.Placed;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers;


/// <summary>
/// Draws a ticket (F127.4.1, in the pictures' look since FX156): a glass card with the accent along its top and a
/// soft light behind its head, the status pill, a hairline under the head, an icon chip per row, the note in a well
/// at the foot, and every line of words where the layout put it.
/// </summary>
/// <remarks>
/// One more Strategy beside the other kinds' renderers, picked by <see cref="DiagramRenderers"/>: it decides nothing
/// about where things go, only how they look, so <see cref="TicketLayout"/>'s numbers are the ones its tests check.
/// It writes its own picture, with no title and no ground, since it is a card in a chat. Its glass, its accent and
/// its pill are the shared look's (<see cref="MarqueeLook"/>, <see cref="PictureLook"/>).
/// </remarks>
internal sealed class TicketRenderer : IDiagramRenderer
{
    /// <summary>The card's corner, which is a panel's, and how thick the hairline under its head is.</summary>
    private const double Radius = PictureParts.Radius, Rule = 1;

    /// <summary>
    /// Where along their run the fades change: the light behind the head from its first colour to its second,
    /// and the accent to its full strength, in its middle.
    /// </summary>
    private const double LightTurns = 0.55, AccentFull = 0.5;

    /// <summary>
    /// An icon chip's corner, the icon's size, how far inside its chip the icon stands, and the corner of the
    /// note's well.
    /// </summary>
    private const double IconCorner = 10, IconSize = 16, IconIn = 8, WellCorner = 10;

    /// <summary>
    /// The pill: where its dot stands from its left, the dot's size, where its words start, and their line under
    /// its top.
    /// </summary>
    private const double DotIn = 12, Dot = 3, StatusIn = 20, StatusLine = 15;

    /// <inheritdoc />
    public DiagramKind Kind => DiagramKind.Ticket;

    /// <inheritdoc />
    public string Render(DiagramSource source, IGeometry geometry)
    {
        var placed = TicketLayout.Place(source.Ticket!, geometry: geometry);
        var card = placed.Card;

        // The card, back to front: its paper, the light behind its head, the glass over both, the accent along
        // its top, and a hairline under its head.
        var writer = new SvgWriter()
            .Paint(TicketStyle.LightPaint, PaintRun.Down, 0, LightTurns, 1)
            .Paint(MarqueeLook.GlassPaint, PaintRun.Down, 0, 1)
            .Paint(MarqueeLook.EdgePaint, PaintRun.Across, 0, AccentFull, 1)
            .Box(card, Radius, "ticket-paper")
            .Path(HeadLight(placed.Head), "ticket-light", false)
            .Box(card, Radius, "mrq-glass ticket");
        PictureParts.Accent(writer, geometry, card, Radius)
            .Box(geometry.Box(card.Left, placed.Head.Bottom, card.Width, Rule), 0, "ticket-rule");

        if (placed.Pill is { } pill) DrawPill(writer, pill);
        foreach (var row in placed.Rows)
            writer.Box(row.IconBox, IconCorner, "ticket-icon-box")
                .Icon(row.IconBox.Left + IconIn, row.IconBox.Top + IconIn, IconSize, row.Icon, "ticket-icon");
        if (placed.Note is { } well) writer.Box(well, WellCorner, "ticket-well");

        foreach (var words in placed.Words)
            writer.Text(words.At.X, words.At.Y, words.Text, words.Kind.Cls, "start");
        string style = PictureLook.Of(SvgTheme.Site) + MarqueeLook.Rules + TicketStyle.Rules;
        return writer.Write(SvgTheme.Site, source.Title, card.Width, card.Height, style);
    }

    /// <summary>
    /// The shape the light behind the head fills: the head, its top corners round with the card's and its foot
    /// straight along the hairline.
    /// </summary>
    private static string HeadLight(IBox head) => new PathWriter()
        .From(head.Left + Radius, head.Top).Across(head.Right - Radius)
        .Arc(Radius, Radius, false, true, head.Right, head.Top + Radius)
        .Down(head.Bottom).Across(head.Left).Down(head.Top + Radius)
        .Arc(Radius, Radius, false, true, head.Left + Radius, head.Top).Close().Text;

    /// <summary>The pill: its box tinted and edged in its tone, a dot, and its words in the tone's ink.</summary>
    private static void DrawPill(SvgWriter writer, PlacedPill pill)
    {
        var box = pill.Box;
        string tone = PictureParts.Tone(pill.Tone);
        writer.Box(box, box.HalfHeight, $"ticket-pill pic-tint pic-edge{tone}")
            .Circle(box.Left + DotIn, box.Middle.Y, Dot, $"pic-fill{tone}")
            .Text(box.Left + StatusIn, box.Top + StatusLine, pill.Label, $"{TicketWords.Status.Cls} pic-ink{tone}",
                "start");
    }
}
