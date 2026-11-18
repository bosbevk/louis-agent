using LouisAgent.Choices;
using LouisAgent.Diagrams.Cards;
using LouisAgent.Diagrams.Engine.Cards.Placed;
using LouisAgent.Diagrams.Engine.Cards.Sizes;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Words;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Overview;

/// <summary>
/// Draws a card of a cards picture by its look: a highlighted card as a headline with a badge, a heading as a
/// quieter headline, a plain card as glass, and a plain card no link touches as a quiet one.
/// </summary>
/// <remarks>
/// The look is a type code kept as an enum (Fowler's refactoring catalogue), switched on here and in
/// <see cref="CardSizes"/>, which says how tall each is: three drawings of a few lines each, too little to earn a
/// class each. Every height it draws at is one of <see cref="CardSizes"/>' numbers, so a word stands where the
/// layout made room for it. A badge and a pill are each a box of their own, asked of the picture's geometry.
/// </remarks>
internal static class OverviewCards
{
    /// <summary>A headline's badge and the icon in it, a heading's icon, and a headline's pill.</summary>
    private const double Badge = 48, BadgeIcon = 24, HeadingIcon = 22, PillHeight = 24, PillDrop = 16, PillPad = 12;

    /// <summary>A card's corner, a glass card's tinted badge, its corner and the icon in it, and its pill.</summary>
    private const double Corner = 14, Tint = 34, TintCorner = 10, TintIcon = 18, CodeHeight = 22, CodePad = 10;

    /// <summary>
    /// Where a glass card's title stands beside its badge, how far in its accent starts along its top, and where
    /// a quiet card's icon, title and pill stand.
    /// </summary>
    private const double TitleDrop = 41, EdgeIn = 28, QuietIcon = 20, QuietTitle = 34, QuietPill = 44;

    /// <summary>Where a code name's words stand in its pill, under a headline's title and in a plain card.</summary>
    private const double PillLine = 16, CodeLine = 15;

    /// <summary>A code name, in its 11px code font.</summary>
    private static readonly Face Code = new(6.8);

    /// <summary>One card, where the layout put it.</summary>
    public static void Draw(SvgWriter writer, PlacedCard card, IGeometry geometry)
    {
        switch (card)
        {
            case { Card.Look: CardLook.Highlight }:
                Headline(writer, card, geometry, CardSizes.HighlightTitle, CardSizes.HighlightText,
                    CardSizes.HeadlineLine);
                break;
            case { Card.Look: CardLook.Heading }:
                Headline(writer, card, geometry, CardSizes.HeadingTitle, CardSizes.HeadingText, CardSizes.HeadingLine);
                break;
            case { Quiet: true }:
                Quiet(writer, card, geometry);
                break;
            default:
                Glass(writer, card, geometry);
                break;
        }
    }

    /// <summary>
    /// A headline, centred and with no box: a highlighted card's icon in its badge or a heading's plain icon, the
    /// title, the code name in a pill under it, and the text.
    /// </summary>
    private static void Headline(SvgWriter writer, PlacedCard card, IGeometry geometry, double title, double text,
        double line)
    {
        bool bright = card.Card.Look == CardLook.Highlight;
        double centre = card.Box.Middle.X, top = card.Box.Top, titleY = top + title;
        if (bright)
            writer.Box(geometry.Box(centre - Badge / 2, top, Badge, Badge), Corner, Toned("crd-badge", card.Card)).Icon(
                centre - BadgeIcon / 2, top + (Badge - BadgeIcon) / 2, BadgeIcon, card.Card.Icon, "crd-badge-icon");
        else
            writer.Icon(centre - HeadingIcon / 2, top, HeadingIcon, card.Card.Icon, "crd-icon quiet");
        writer.Text(centre, titleY, card.Card.Title, bright ? "crd-headline" : "crd-heading");

        double under = titleY;
        if (card.Card.Code is { } code)
        {
            double width = Code.Width(code) + 2 * PillPad;
            var pill = geometry.Box(centre - width / 2, titleY + PillDrop, width, PillHeight);
            writer.Box(pill, pill.HalfHeight, "mrq-pill").Text(centre, pill.Top + PillLine, code, "mrq-code");
            under += CardSizes.PillRoom;
        }

        for (int at = 0; at < card.TextLines.Count; at++)
            writer.Text(centre, under + text + line * at, card.TextLines[at], "crd-summary");
    }

    /// <summary>
    /// A glass card: its panel with the accent along its top, its icon in a tinted badge, its title beside the
    /// badge or under it, its code name's pill, and its text.
    /// </summary>
    private static void Glass(SvgWriter writer, PlacedCard card, IGeometry geometry)
    {
        var box = card.Box;
        double left = box.Left + CardSizes.Pad;
        int titles = card.TitleLines.Count;
        var tint = geometry.Box(left, box.Top + CardSizes.Pad, Tint, Tint);
        writer.Box(box, Corner, "mrq-glass");
        PictureParts.Accent(writer, geometry, box, EdgeIn).Box(tint, TintCorner, Toned("crd-tint", card.Card))
            .Icon(tint.Left + (Tint - TintIcon) / 2, tint.Top + (Tint - TintIcon) / 2, TintIcon, card.Card.Icon,
                Toned("crd-icon", card.Card));

        for (int at = 0; at < titles; at++)
        {
            (double x, double y) = card.TitleBelow
                ? (left, box.Top + CardSizes.Head + CardSizes.TextTop + CardSizes.TitleLine * at)
                : (box.Left + CardSizes.TitleLeft, box.Top + TitleDrop);
            writer.Text(x, y, card.TitleLines[at], "crd-name", "start");
        }

        double top = box.Top + CardSizes.Head + (card.TitleBelow ? CardSizes.TitleLine * titles : 0);
        double code = Pill(writer, card, geometry, left, top, CardSizes.CodeRoom);
        for (int at = 0; at < card.TextLines.Count; at++)
            writer.Text(left, top + code + CardSizes.TextTop + CardSizes.TextLine * at, card.TextLines[at], "pic-text",
                "start");
    }

    /// <summary>
    /// A quiet card: a faint panel, a plain icon, its title beside the icon over as many lines as it needs, its
    /// code name's pill, and its text.
    /// </summary>
    private static void Quiet(SvgWriter writer, PlacedCard card, IGeometry geometry)
    {
        var box = card.Box;
        double left = box.Left + CardSizes.QuietPad;
        int titles = card.TitleLines.Count;
        writer.Box(box, Corner, "crd-quiet")
            .Icon(left, box.Top + QuietIcon, TintIcon, card.Card.Icon, Toned("crd-icon quiet", card.Card));
        for (int at = 0; at < titles; at++)
            writer.Text(box.Left + CardSizes.QuietTitleLeft, box.Top + QuietTitle + CardSizes.QuietTitleLine * at,
                card.TitleLines[at], "crd-name quiet", "start");

        double more = CardSizes.QuietTitleLine * Math.Max(0, titles - 1);
        double code = Pill(writer, card, geometry, left, box.Top + QuietPill + more, CardSizes.QuietCodeRoom);
        for (int at = 0; at < card.TextLines.Count; at++)
            writer.Text(left, box.Top + CardSizes.QuietText + more + code + CardSizes.QuietLine * at,
                card.TextLines[at], "pic-text", "start");
    }

    /// <summary>
    /// A plain card's code name in a pill, from the card's left, and the room it took. Nothing for a card with no
    /// code name.
    /// </summary>
    private static double Pill(SvgWriter writer, PlacedCard card, IGeometry geometry, double left, double top,
        double room)
    {
        if (card.Card.Code is not { } code) return 0;

        var pill = geometry.Box(left, top, Code.Width(code) + 2 * CodePad, CodeHeight);
        writer.Box(pill, pill.HalfHeight, "mrq-pill").Text(left + CodePad, top + CodeLine, code, "mrq-code", "start");
        return room;
    }

    /// <summary>A part's class, with its card's tone after it when the card names one (F161.1).</summary>
    private static string Toned(string cls, Card card) => card.Tone is { } tone ? $"{cls} tone-{tone.Wire()}" : cls;
}
