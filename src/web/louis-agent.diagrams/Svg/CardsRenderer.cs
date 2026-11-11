using LouisAgent.Diagrams.Cards;
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
    /// One card: its box, its icon at the top left, its title under the icon with its code chip beside it (or under
    /// it, when the two don't fit one line), and its words under them, as the layout measured.
    /// </summary>
    private static void Draw(SvgWriter writer, PlacedCard card)
    {
        double left = card.X + CardLayout.CardPad;
        double titleTop = card.Y + CardLayout.CardPad + CardLayout.IconSize + CardLayout.IconGap;
        writer.Box(card.X, card.Y, card.W, card.H, 10, "card")
            .Icon(left, card.Y + CardLayout.CardPad, CardLayout.IconSize, card.Card.Icon, "icon")
            .Text(left, titleTop + 13, card.Card.Title, "title", "start");

        double textTop = titleTop + CardLayout.TitleLine + CardLayout.TextGap;
        if (card.Card.Code is { } code)
        {
            double chipX = card.ChipBelow
                ? left
                : left + card.Card.Title.Length * CardLayout.TitleCharWidth + CardLayout.ChipGap;
            double chipY = card.ChipBelow ? titleTop + CardLayout.TitleLine + 2 : titleTop - 3;
            writer.Box(chipX, chipY, CardLayout.ChipWidth(code), CardLayout.ChipHeight, 5, "chip")
                .Text(chipX + CardLayout.ChipPad, chipY + 14, code, "code", "start");
            if (card.ChipBelow) textTop += CardLayout.ChipHeight + 6;
        }

        for (int at = 0; at < card.TextLines.Count; at++)
            writer.Text(left, textTop + 11 + at * CardLayout.TextLine, card.TextLines[at], "text", "start");
    }
}
