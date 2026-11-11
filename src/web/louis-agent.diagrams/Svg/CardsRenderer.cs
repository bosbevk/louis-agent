using LouisAgent.Diagrams.Cards;
using LouisAgent.Diagrams.Cards.Links;
using LouisAgent.Diagrams.Cards.Placed;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Svg.Theme;

namespace LouisAgent.Diagrams.Svg;

/// <summary>
/// Draws an overview in Louis's card style: one rounded frame, the links between the rows, and each card with its
/// icon, its title and code chip, and its words, the caption under the cards inside the frame.
/// </summary>
internal sealed class CardsRenderer : IDiagramRenderer
{
    /// <summary>The room under the cards a caption takes, inside the frame.</summary>
    private const double CaptionRoom = 30;

    public DiagramKind Kind => DiagramKind.Cards;

    public string Render(DiagramSource source)
    {
        var placed = CardLayout.Place(source.Cards!);
        double height = source.Caption is null ? placed.Height : placed.Height + CaptionRoom;
        var writer = new SvgWriter().Box(0, 0, placed.Width, height, 14, "frame");

        // The links first, so the cards stand over their ends; the arrowheads point the link's way.
        foreach (var link in placed.Links)
            writer.Path(link.Path, "edge", link.Link.Kind != CardLinkKind.Up,
                link.Link.Kind != CardLinkKind.Down);

        foreach (var card in placed.Cards) Draw(writer, card);

        if (source.Caption is { } caption)
            writer.Text(CardLayout.FramePad, placed.CaptionY, caption, "caption", "start");
        return writer.Write(SvgTheme.Site, source.Title, placed.Width, height);
    }

    /// <summary>
    /// One card: its box, its icon on top, its title under the icon with its code chip beside it (or under it, when
    /// the two don't fit one line), and its words under them, as the layout measured. A plain card starts them at
    /// its left; a highlighted card and a heading centre them, the heading with no box.
    /// </summary>
    /// <remarks>
    /// The look is a type code kept as an enum (Fowler's refactoring catalogue), switched on here and nowhere else:
    /// the looks differ only in where the words start and how the box is filled, too little to earn a class each.
    /// </remarks>
    private static void Draw(SvgWriter writer, PlacedCard card)
    {
        var look = card.Card.Look;
        bool centred = look != CardLook.Plain;
        double left = card.X + CardLayout.CardPad, centre = card.X + card.W / 2;
        if (look != CardLook.Heading)
            writer.Box(card.X, card.Y, card.W, card.H, 10, look == CardLook.Highlight ? "card highlight" : "card");

        double iconX = centred ? centre - CardLayout.IconSize / 2 : left;
        writer.Icon(iconX, card.Y + CardLayout.CardPad, CardLayout.IconSize, card.Card.Icon, "icon");
        double textTop = DrawTitle(writer, card, centred);

        (double x, string anchor) = centred ? (centre, "middle") : (left, "start");
        for (int at = 0; at < card.TextLines.Count; at++)
            writer.Text(x, textTop + 11 + at * CardLayout.TextLine, card.TextLines[at], "text", anchor);
    }

    /// <summary>
    /// A card's title and its code chip, from its left or centred on it, and where its words start under them.
    /// </summary>
    private static double DrawTitle(SvgWriter writer, PlacedCard card, bool centred)
    {
        double left = card.X + CardLayout.CardPad, centre = card.X + card.W / 2;
        double titleTop = card.Y + CardLayout.CardPad + CardLayout.IconSize + CardLayout.IconGap;
        double textTop = titleTop + CardLayout.TitleLine + CardLayout.TextGap;
        double titleWidth = card.Card.Title.Length * CardLayout.TitleCharWidth;
        if (card.Card.Code is not { } code)
        {
            // Centred on the card by the reader's own font, so a title whose font runs wide stays in the middle.
            (double x, string anchor) = centred ? (centre, "middle") : (left, "start");
            writer.Text(x, titleTop + 13, card.Card.Title, "title", anchor);
            return textTop;
        }

        // A title and its chip beside it are one line, centred together; a chip under the title is centred alone.
        double chipWidth = CardLayout.ChipWidth(code);
        double line = card.ChipBelow ? titleWidth : titleWidth + CardLayout.ChipGap + chipWidth;
        double start = centred ? centre - line / 2 : left;
        writer.Text(start, titleTop + 13, card.Card.Title, "title", "start");

        double chipX = (card.ChipBelow, centred) switch
        {
            (false, _) => start + titleWidth + CardLayout.ChipGap,
            (true, true) => centre - chipWidth / 2,
            (true, false) => left
        };
        double chipY = card.ChipBelow ? titleTop + CardLayout.TitleLine + 2 : titleTop - 3;
        writer.Box(chipX, chipY, chipWidth, CardLayout.ChipHeight, 5, "chip")
            .Text(chipX + CardLayout.ChipPad, chipY + 14, code, "code", "start");
        return card.ChipBelow ? textTop + CardLayout.ChipHeight + 6 : textTop;
    }
}
