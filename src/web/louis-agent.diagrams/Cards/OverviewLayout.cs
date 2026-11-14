using LouisAgent.Diagrams.Cards.Placed;
using LouisAgent.Diagrams.Cards.Rails;
using LouisAgent.Diagrams.Cards.Sizes;

namespace LouisAgent.Diagrams.Cards;

/// <summary>
/// Lays out an overview in the new look (F134.9): rows of cards under one another, a row's cards sharing its width
/// and its tallest card's height, and between two rows the rails their links make, in a gap as tall as the rails
/// need.
/// </summary>
/// <remarks>
/// A pure function of the data, as every layout here is, so it is tested with numbers and no browser. It decides
/// every place, and the drawing none. It does three small jobs in order and hands each to the part that knows it:
/// <see cref="CardSizes"/> says how tall a card is, <see cref="CardRails"/> which links share a rail, and
/// <see cref="RailPlaces"/> where a gap's rails stand.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0118-a-diagram-is-data-our-renderer-draws.md">ADR 0118: a diagram is data our renderer draws</seealso>
public static class OverviewLayout
{
    /// <summary>The room between two cards of a row, and between two rows no link crosses.</summary>
    public const double Gap = 22, RowGap = 26;

    /// <summary>How far above a headline a drop into it ends, so its arrowhead stands clear of the badge.</summary>
    public const double HeadlineClear = 4;

    /// <summary>The overview placed across the given width, from the origin.</summary>
    public static PlacedOverview Place(CardsSpec spec, int width = 896)
    {
        // A card no link touches is quiet, in a picture that has links.
        var linked = spec.Links.SelectMany(link => new[] { link.From, link.To }).ToHashSet(StringComparer.Ordinal);
        bool Quiet(Card card) => linked.Count > 0 && !linked.Contains(card.Id);

        // Each row across the width first, since where a rail stands depends only on where its cards' middles are.
        var rows = spec.Rows.Select(row => Across(row, width, Quiet)).ToList();
        var centres = rows.SelectMany(row => row)
            .ToDictionary(card => card.Card.Id, card => card.X + card.W / 2, StringComparer.Ordinal);
        var spans = RailPlaces.Across(CardRails.Of(spec), centres);

        // Then down the picture: a row, and under it a gap as tall as its rails need, or the plain gap.
        var cards = new List<PlacedCard>();
        var heights = new Dictionary<RailSpan, double>();
        double y = 0, bottom = 0;
        for (int at = 0; at < rows.Count; at++)
        {
            double top = y;
            cards.AddRange(rows[at].Select(card => card with { Y = top }));
            bottom = top + rows[at].Select(card => card.H).DefaultIfEmpty(0).Max();

            var inGap = spans.Where(span => span.Rail.Gap == at).ToList();
            foreach (var span in inGap) heights[span] = bottom + RailPlaces.Stem + RailPlaces.Step * span.Level;
            y = bottom + (inGap.Count == 0 ? RowGap : RailPlaces.Room(inGap));
        }

        // A rail's ends last, since they stand on the edges of cards in the row under it.
        var byId = cards.ToDictionary(card => card.Card.Id, StringComparer.Ordinal);
        return new PlacedOverview(width, bottom, cards,
        [
            .. spans.Select(span =>
                new PlacedRail(span.Rail, heights[span], span.Left, span.Right, Ends(span, byId)))
        ]);
    }

    /// <summary>A row's cards across the width: a gap between them, each as tall as the row's tallest.</summary>
    private static List<PlacedCard> Across(CardRow row, double width, Func<Card, bool> quiet)
    {
        int count = row.Cards.Count;
        double cardWidth = (width - Gap * (count - 1)) / count;
        var sizes = row.Cards.Select(card => CardSizes.Of(card, cardWidth, quiet(card))).ToList();
        double height = sizes.Select(size => size.Height).DefaultIfEmpty(0).Max();
        return
        [
            .. row.Cards.Select((card, at) => new PlacedCard(card, at * (cardWidth + Gap), 0, cardWidth, height,
                sizes[at].TextLines, card.Code is not null, sizes[at].TitleLines, sizes[at].TitleBelow,
                sizes[at].Quiet))
        ];
    }

    /// <summary>
    /// A rail's ends on its cards' edges: the bottom of a card above it and the top of a card below it, a little
    /// above a headline, each with an arrowhead where a link points at its card.
    /// </summary>
    private static List<RailEnd> Ends(RailSpan span, Dictionary<string, PlacedCard> cards) =>
    [
        .. span.Spots.Select(spot => new RailEnd(spot.Card, spot.X, Edge(cards[spot.Card], spot.Above), spot.Above,
            RailPlaces.PointsAt(span.Rail, spot.Card)))
    ];

    /// <summary>The edge of a card a rail's line runs to.</summary>
    private static double Edge(PlacedCard card, bool above) => (above, card.Card.Look) switch
    {
        (true, _) => card.Y + card.H,
        (false, CardLook.Plain) => card.Y,
        _ => card.Y - HeadlineClear
    };
}
