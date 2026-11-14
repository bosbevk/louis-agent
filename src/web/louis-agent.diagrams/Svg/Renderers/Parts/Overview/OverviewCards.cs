using LouisAgent.Diagrams.Cards;
using LouisAgent.Diagrams.Cards.Placed;
using LouisAgent.Diagrams.Cards.Sizes;

namespace LouisAgent.Diagrams.Svg.Renderers.Parts.Overview;

/// <summary>
/// Draws a card of a cards picture by its look: a highlighted card as a headline with a badge, a heading as a
/// quieter headline, a plain card as glass, and a plain card no link touches as a quiet one.
/// </summary>
/// <remarks>
/// The look is a type code kept as an enum (Fowler's refactoring catalogue), switched on here and in
/// <see cref="CardSizes"/>, which says how tall each is: three drawings of a few lines each, too little to earn a
/// class each. Every height it draws at is one of <see cref="CardSizes"/>' numbers, so a word stands where the
/// layout made room for it.
/// </remarks>
internal static class OverviewCards
{
    /// <summary>A headline's badge and the icon in it, a heading's icon, and a headline's pill.</summary>
    private const double Badge = 48, BadgeIcon = 24, HeadingIcon = 22, PillHeight = 24, PillDrop = 16, PillPad = 12;

    /// <summary>A card's corner, a glass card's tinted badge and the icon in it, and its pill.</summary>
    private const double Corner = 14, Tint = 34, TintIcon = 18, CodeHeight = 22, CodePad = 10;

    /// <summary>
    /// Where a glass card's title stands beside its badge, how far in its accent starts along its top, and where
    /// a quiet card's icon, title and pill stand.
    /// </summary>
    private const double TitleDrop = 41, EdgeIn = 28, QuietIcon = 20, QuietTitle = 34, QuietPill = 44;

    /// <summary>How wide a character of a code name is in its 11px code font.</summary>
    private const double CodeChar = 6.8;

    /// <summary>One card, where the layout put it.</summary>
    public static void Draw(SvgWriter writer, PlacedCard card)
    {
        switch (card)
        {
            case { Card.Look: CardLook.Highlight }:
                Headline(writer, card, CardSizes.HighlightTitle, CardSizes.HighlightText, CardSizes.HeadlineLine);
                break;
            case { Card.Look: CardLook.Heading }:
                Headline(writer, card, CardSizes.HeadingTitle, CardSizes.HeadingText, CardSizes.HeadingLine);
                break;
            case { Quiet: true }:
                Quiet(writer, card);
                break;
            default:
                Glass(writer, card);
                break;
        }
    }

    /// <summary>
    /// A headline, centred and with no box: a highlighted card's icon in its badge or a heading's plain icon, the
    /// title, the code name in a pill under it, and the text.
    /// </summary>
    private static void Headline(SvgWriter writer, PlacedCard card, double title, double text, double line)
    {
        bool bright = card.Card.Look == CardLook.Highlight;
        double centre = card.X + card.W / 2, titleY = card.Y + title;
        if (bright)
            writer.Box(centre - Badge / 2, card.Y, Badge, Badge, Corner, "crd-badge").Icon(centre - BadgeIcon / 2,
                card.Y + (Badge - BadgeIcon) / 2, BadgeIcon, card.Card.Icon, "crd-badge-icon");
        else
            writer.Icon(centre - HeadingIcon / 2, card.Y, HeadingIcon, card.Card.Icon, "crd-icon quiet");
        writer.Text(centre, titleY, card.Card.Title, bright ? "crd-headline" : "crd-heading");

        double under = titleY;
        if (card.Card.Code is { } code)
        {
            double width = code.Length * CodeChar + 2 * PillPad;
            writer.Box(centre - width / 2, titleY + PillDrop, width, PillHeight, PillHeight / 2, "mrq-pill")
                .Text(centre, titleY + PillDrop + 16, code, "mrq-code");
            under += CardSizes.PillRoom;
        }

        for (int at = 0; at < card.TextLines.Count; at++)
            writer.Text(centre, under + text + line * at, card.TextLines[at], "crd-lede");
    }

    /// <summary>
    /// A glass card: its panel with the accent along its top, its icon in a tinted badge, its title beside the
    /// badge or under it, its code name's pill, and its text.
    /// </summary>
    private static void Glass(SvgWriter writer, PlacedCard card)
    {
        double left = card.X + CardSizes.Pad;
        int titles = card.TitleLines.Count;
        writer.Box(card.X, card.Y, card.W, card.H, Corner, "mrq-glass")
            .Box(card.X + EdgeIn, card.Y - 0.5, card.W - 2 * EdgeIn, 1.6, 0, "mrq-edge")
            .Box(left, card.Y + CardSizes.Pad, Tint, Tint, 10, "crd-tint")
            .Icon(left + (Tint - TintIcon) / 2, card.Y + CardSizes.Pad + (Tint - TintIcon) / 2, TintIcon,
                card.Card.Icon, "crd-icon");

        for (int at = 0; at < titles; at++)
        {
            (double x, double y) = card.TitleBelow
                ? (left, card.Y + CardSizes.Head + CardSizes.TextTop + CardSizes.TitleLine * at)
                : (card.X + CardSizes.TitleLeft, card.Y + TitleDrop);
            writer.Text(x, y, card.TitleLines[at], "crd-name", "start");
        }

        double top = card.Y + CardSizes.Head + (card.TitleBelow ? CardSizes.TitleLine * titles : 0);
        double code = Pill(writer, card, left, top, CardSizes.CodeRoom);
        for (int at = 0; at < card.TextLines.Count; at++)
            writer.Text(left, top + code + CardSizes.TextTop + CardSizes.TextLine * at, card.TextLines[at], "crd-text",
                "start");
    }

    /// <summary>
    /// A quiet card: a faint panel, a plain icon, its title beside the icon over as many lines as it needs, its
    /// code name's pill, and its text.
    /// </summary>
    private static void Quiet(SvgWriter writer, PlacedCard card)
    {
        double left = card.X + CardSizes.QuietPad;
        int titles = card.TitleLines.Count;
        writer.Box(card.X, card.Y, card.W, card.H, Corner, "crd-quiet")
            .Icon(left, card.Y + QuietIcon, TintIcon, card.Card.Icon, "crd-icon quiet");
        for (int at = 0; at < titles; at++)
            writer.Text(card.X + CardSizes.QuietTitleLeft, card.Y + QuietTitle + CardSizes.QuietTitleLine * at,
                card.TitleLines[at], "crd-name quiet", "start");

        double more = CardSizes.QuietTitleLine * Math.Max(0, titles - 1);
        double code = Pill(writer, card, left, card.Y + QuietPill + more, CardSizes.QuietCodeRoom);
        for (int at = 0; at < card.TextLines.Count; at++)
            writer.Text(left, card.Y + CardSizes.QuietText + more + code + CardSizes.QuietLine * at,
                card.TextLines[at], "crd-text", "start");
    }

    /// <summary>
    /// A plain card's code name in a pill, from the card's left, and the room it took. Nothing for a card with no
    /// code name.
    /// </summary>
    private static double Pill(SvgWriter writer, PlacedCard card, double left, double top, double room)
    {
        if (card.Card.Code is not { } code) return 0;

        writer.Box(left, top, code.Length * CodeChar + 2 * CodePad, CodeHeight, CodeHeight / 2, "mrq-pill")
            .Text(left + CodePad, top + 15, code, "mrq-code", "start");
        return room;
    }
}
