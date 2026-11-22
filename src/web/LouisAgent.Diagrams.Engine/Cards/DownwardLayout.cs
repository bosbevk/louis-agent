using LouisAgent.Diagrams.Cards;
using LouisAgent.Diagrams.Engine.Cards.Placed;
using LouisAgent.Diagrams.Engine.Cards.Rails;
using LouisAgent.Diagrams.Engine.Cards.Sizes;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Messages;

namespace LouisAgent.Diagrams.Engine.Cards;

/// <summary>
/// Lays out an overview that runs down, for a narrow screen (F169.5): each card under the one before, row after row,
/// the card a rail leaves as wide as the picture, and the rest just in from a trunk down the picture's left, with a
/// branch to each card the rail fed.
/// </summary>
/// <remarks>
/// The same picture's data laid out a second way, so nobody redraws a picture for a phone (ADR 0118). It is the
/// sketch Louis picked, in code: a trunk can say who feeds whom only while one card feeds them all, so a picture
/// whose links leave more than one card has no downward layout and is refused by name. A pure function of the data,
/// as <see cref="OverviewLayout"/> is, so it is tested with numbers.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0118-a-diagram-is-data-our-renderer-draws.md">ADR 0118: a diagram is data our renderer draws</seealso>
internal static class DownwardLayout
{
    /// <summary>The room between two cards of one kind, the linked or the quiet, running down.</summary>
    private const double Gap = 12;

    /// <summary>Where the trunk stands, in the room left of the cards it feeds.</summary>
    private const double TrunkX = 8;

    /// <summary>Whether one trunk can carry the picture's links: at most one rail, leaving one card.</summary>
    public static bool Allows(CardsSpec spec)
    {
        var rails = CardRails.Of(spec);
        return rails.Count == 0 || (rails.Count == 1 && rails[0].Above.Count == 1);
    }

    /// <summary>The overview placed down the given width, from the origin.</summary>
    public static PlacedOverview Place(CardsSpec spec, int width, IGeometry geometry)
    {
        if (!Allows(spec)) throw new NotSupportedException(DiagramText.Format(DrawingMessage.CardsCantRunDown));

        var rail = CardRails.Of(spec).SingleOrDefault();
        var linked = spec.Links.SelectMany(link => new[] { link.From, link.To }).ToHashSet(StringComparer.Ordinal);
        bool Quiet(Card card) => linked.Count > 0 && !linked.Contains(card.Id);
        bool Fed(Card card) => rail is not null && rail.Below.Contains(card.Id);
        bool Whole(Card card) => rail is null || rail.Above.Contains(card.Id);

        // Each card in its rows' order, under the one before: the plain gap within a kind, the rows' gap between two.
        var cards = new List<PlacedCard>();
        double y = 0;
        foreach (var card in spec.Rows.SelectMany(row => row.Cards))
        {
            if (cards.Count > 0) y += Kind(cards[^1].Card) == Kind(card) ? Gap : OverviewLayout.RowGap;
            double left = Whole(card) ? 0 : OverviewLayout.TrunkRoom;
            var size = CardSizes.Of(card, width - left, Quiet(card));
            cards.Add(new PlacedCard(card, geometry.Box(left, y, width - left, size.Height), size.TitleLines,
                size.TitleBelow, size.TextLines, size.Quiet));
            y = cards[^1].Box.Bottom;
        }

        IReadOnlyList<PlacedTrunk> trunks = rail is null ? [] : [Trunk(rail, cards, geometry)];
        return new PlacedOverview(width, y, cards, [], trunks);

        // A card's kind, for the gap before it: whether a rail leaves it, and whether one feeds it.
        (bool Leaves, bool Fed) Kind(Card card) => (rail is not null && Whole(card), Fed(card));
    }

    /// <summary>
    /// The trunk: from the foot of the card the rail leaves down to the middle of the last card it feeds, and a
    /// branch to the middle of each card's left edge, with an arrowhead where the link points at that card.
    /// </summary>
    private static PlacedTrunk Trunk(Rail rail, List<PlacedCard> cards, IGeometry geometry)
    {
        var above = cards.Single(card => card.Card.Id == rail.Above[0]);
        var fed = cards.Where(card => rail.Below.Contains(card.Card.Id)).ToList();
        var branches = fed.Select(card => new RailEnd(card.Card.Id,
            geometry.Point(card.Box.Left, card.Box.Middle.Y), false, RailPlaces.PointsAt(rail, card.Card.Id)));

        return new PlacedTrunk(rail, geometry.Point(TrunkX, OverviewLayout.Edge(above, true)),
            geometry.Point(TrunkX, fed[^1].Box.Middle.Y), [.. branches]);
    }
}
