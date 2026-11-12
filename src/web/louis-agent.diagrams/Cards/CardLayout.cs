using System.Globalization;
using LouisAgent.Diagrams.Cards.Placed;

namespace LouisAgent.Diagrams.Cards;

/// <summary>
/// Lays out an overview in Louis's card style: rows of cards top to bottom in one frame, a row's cards sharing its
/// width and its tallest card's height, each card's words wrapped to fit it, and a link from one card's edge to
/// the other's.
/// </summary>
/// <remarks>
/// Every word is in the code font, so a line's width is its length times one number and the layout can prove each
/// word fits its card (the tests do) without a browser to measure it, the same promise ADR 0103 makes for the flows.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0103-a-diagram-is-data-on-a-row-grid.md">ADR 0103: a diagram is data on a row grid</seealso>
public static class CardLayout
{
    /// <summary>The room inside the frame around the cards, and inside a card around its icon and words.</summary>
    public const double FramePad = 20, CardPad = 16;

    /// <summary>The room between two cards in a row, and between two rows, where the links run.</summary>
    public const double Gap = 16, RowGap = 40;

    /// <summary>The icon's size, and the room under it before the title.</summary>
    public const double IconSize = 20, IconGap = 12;

    /// <summary>How wide a character is in the title's 13px and the text's and code's 11px code font.</summary>
    public const double TitleCharWidth = 8.2, TextCharWidth = 6.8;

    /// <summary>A title's line, a line of text, and the room between the title and the text.</summary>
    public const double TitleLine = 18, TextLine = 16, TextGap = 8;

    /// <summary>
    /// A code chip's height, the room inside it either side of its code, and the room beside a title.
    /// </summary>
    public const double ChipHeight = 20, ChipPad = 7, ChipGap = 10;

    /// <summary>The overview placed across the given width.</summary>
    public static PlacedCards Place(CardsSpec spec, int width = 880)
    {
        double inner = width - 2 * FramePad;
        double y = FramePad;
        var cards = new List<PlacedCard>();
        foreach (var row in spec.Rows)
        {
            // A row's cards share its width; all stand as tall as its tallest.
            int count = row.Cards.Count;
            double cardWidth = (inner - Gap * (count - 1)) / count;
            var measured = row.Cards.Select(card => Measure(card, cardWidth)).ToList();
            double height = measured.Max(card => card.Height);
            for (int at = 0; at < count; at++)
                cards.Add(new PlacedCard(row.Cards[at], FramePad + at * (cardWidth + Gap), y, cardWidth, height,
                    measured[at].Lines, measured[at].ChipBelow));
            y += height + RowGap;
        }

        double bottom = y - RowGap + FramePad;
        var byId = cards.ToDictionary(card => card.Card.Id, StringComparer.Ordinal);
        var links = spec.Links.Select(link => new PlacedLink(link, PathOf(byId[link.From], byId[link.To]))).ToList();
        return new PlacedCards(width, bottom, cards, links, bottom + 18);
    }

    /// <summary>How wide a code chip is for its code.</summary>
    public static double ChipWidth(string code) => code.Length * TextCharWidth + 2 * ChipPad;

    /// <summary>A card's lines at this width, whether its chip must go under its title, and its height.</summary>
    private static (IReadOnlyList<string> Lines, bool ChipBelow, double Height) Measure(Card card, double width)
    {
        double room = width - 2 * CardPad;
        var lines = TextWrap.Lines(card.Text, (int)(room / TextCharWidth));
        bool chipBelow = card.Code is { } code && card.Title.Length * TitleCharWidth + ChipGap + ChipWidth(code) > room;
        double chip = chipBelow ? ChipHeight + 6 : 0;
        double height = CardPad + IconSize + IconGap + TitleLine + chip + TextGap + lines.Count * TextLine + CardPad;
        return (lines, chipBelow, height);
    }

    /// <summary>
    /// A link's path: from a card's bottom middle down to the next row's top middle (or up, from its top), turning
    /// halfway at right angles when the two middles differ; between two cards of one row, side to side.
    /// </summary>
    private static string PathOf(PlacedCard from, PlacedCard to)
    {
        if (from.Y == to.Y)
        {
            double middle = from.Y + from.H / 2;
            (double start, double end) = from.X < to.X ? (from.X + from.W, to.X) : (from.X, to.X + to.W);
            return $"M {N(start)} {N(middle)} L {N(end)} {N(middle)}";
        }

        bool down = from.Y < to.Y;
        double x1 = from.X + from.W / 2, y1 = down ? from.Y + from.H : from.Y;
        double x2 = to.X + to.W / 2, y2 = down ? to.Y : to.Y + to.H;
        double turn = (y1 + y2) / 2;
        return x1 == x2
            ? $"M {N(x1)} {N(y1)} L {N(x2)} {N(y2)}"
            : $"M {N(x1)} {N(y1)} L {N(x1)} {N(turn)} L {N(x2)} {N(turn)} L {N(x2)} {N(y2)}";
    }

    /// <summary>A number as the path writes it: invariant, at most one decimal.</summary>
    private static string N(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);
}
