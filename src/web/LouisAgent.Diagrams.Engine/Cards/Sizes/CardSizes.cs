using LouisAgent.Diagrams.Cards;
using LouisAgent.Diagrams.Engine.Words;

namespace LouisAgent.Diagrams.Engine.Cards.Sizes;

/// <summary>
/// Says how tall a card of an overview is in the new look, and where its words break (F134.9). A highlighted card
/// and a heading are headlines, a plain card is glass, and a plain card no link touches is quiet and lower.
/// </summary>
/// <remarks>
/// The words are set in the sans font, whose letters differ in width, so a line's width is taken as its length
/// times a number chosen wide. That is the measure a flow's boxes are sized by too: the layout can still say with
/// numbers that every line fits, and the pictures are looked at.
/// </remarks>
public static class CardSizes
{
    /// <summary>The room inside a glass card beside its words, and inside a quiet one.</summary>
    public const double Pad = 18, QuietPad = 20;

    /// <summary>
    /// The faces a card's words are measured in: a glass card's 15px title, a quiet card's 14px title, a card's
    /// 12px text and a headline's 13px text.
    /// </summary>
    public static readonly Face TitleFace = new(8), QuietTitleFace = new(7.6), TextFace = new(6.1),
        HeadlineFace = new(6.6);

    /// <summary>
    /// A headline, from its top: where a highlighted card's title stands under its badge and a heading's under its
    /// icon, what a code name's pill adds under the title, how far under them the text starts, a line of text for
    /// each, the room under the last line, and the room either side of the text.
    /// </summary>
    public const double HighlightTitle = 92, HeadingTitle = 58, PillRoom = 36, HighlightText = 32, HeadingText = 24,
        HeadlineLine = 19, HeadingLine = 18, HeadlineEnd = 10, HeadlineSide = 78;

    /// <summary>
    /// A glass card, from its top: its badge's row, what its title starts after beside the badge and the room it
    /// keeps at the card's right, a line of a title that stands under the badge, what a code name's pill adds, how
    /// far under them the text starts, a line of text, and the room under the last line.
    /// </summary>
    public const double Head = 58, TitleLeft = 64, TitleEnd = 14, TitleLine = 21, CodeRoom = 28, TextTop = 18,
        TextLine = 18, End = 20;

    /// <summary>
    /// A quiet card, from its top: where its text starts, what its title starts after beside its icon, a further
    /// line of its title, what a code name's pill adds, a line of text, and the room under the last line.
    /// </summary>
    public const double QuietText = 60, QuietTitleLeft = 50, QuietTitleLine = 19, QuietCodeRoom = 26, QuietLine = 17,
        QuietEnd = 14;

    /// <summary>A card's size at the width its row gives it. A headline is never quiet.</summary>
    public static CardSize Of(Card card, double width, bool quiet) => (card.Look, quiet) switch
    {
        (CardLook.Highlight, _) => Headline(card, width, HighlightTitle, HighlightText, HeadlineLine),
        (CardLook.Heading, _) => Headline(card, width, HeadingTitle, HeadingText, HeadingLine),
        (_, true) => Quiet(card, width),
        _ => Glass(card, width)
    };

    /// <summary>
    /// A headline: its title on one line, its code name's pill under it when it has one, and its text under that,
    /// kept well inside the picture's sides. With no text it ends under its pill, since the page around the picture
    /// says it (Louis: "leave these text out of these graphs, website will drive it").
    /// </summary>
    private static CardSize Headline(Card card, double width, double title, double text, double line)
    {
        var lines = TextWrap.Lines(card.Text, (int)HeadlineFace.Holds(width - 2 * HeadlineSide));
        double pill = card.Code is null ? 0 : PillRoom;
        double words = lines.Count == 0 ? 0 : text + line * (lines.Count - 1);
        return new CardSize(title + pill + words + HeadlineEnd, [card.Title], false, lines, false);
    }

    /// <summary>
    /// A glass card: its title beside its badge, or under it on lines of its own when it is too long for that
    /// room, then its code name's pill, then its text.
    /// </summary>
    private static CardSize Glass(Card card, double width)
    {
        bool below = TitleFace.Width(card.Title) > width - TitleLeft - TitleEnd;
        var titles = below ? TextWrap.Lines(card.Title, (int)TitleFace.Holds(width - 2 * Pad)) : [card.Title];
        var lines = TextWrap.Lines(card.Text, (int)TextFace.Holds(width - 2 * Pad));
        double head = Head + (below ? TitleLine * titles.Count : 0), code = card.Code is null ? 0 : CodeRoom;
        return new CardSize(head + code + TextTop + TextLine * (lines.Count - 1) + End, titles, below, lines, false);
    }

    /// <summary>
    /// A quiet card: no badge, so its title stands beside its plain icon, over more lines when it is long, and its
    /// text starts higher.
    /// </summary>
    private static CardSize Quiet(Card card, double width)
    {
        var titles = TextWrap.Lines(card.Title, (int)QuietTitleFace.Holds(width - QuietTitleLeft - QuietPad));
        var lines = TextWrap.Lines(card.Text, (int)TextFace.Holds(width - 2 * QuietPad));
        double code = card.Code is null ? 0 : QuietCodeRoom;
        return new CardSize(
            QuietText + QuietTitleLine * (titles.Count - 1) + code + QuietLine * (lines.Count - 1) + QuietEnd, titles,
            false, lines, true);
    }
}
