using LouisAgent.Diagrams.Cards.Links;

namespace LouisAgent.Diagrams.Cards.Rails;

/// <summary>
/// Says where a gap's rails stand: each end under or over the middle of its card, two rails that are clear of each
/// other at one height, and one that would lie on another a step under it.
/// </summary>
/// <remarks>
/// The same idea a flow uses for the lines of a gap (<c>ApartArrows</c>), turned on its side: each rail is a span
/// from its leftmost end to its rightmost, spans that overlap may not share a height, and taking them from the
/// left, each at the first height clear of the ones before it, uses no more heights than that order needs.
/// </remarks>
public static class RailPlaces
{
    /// <summary>
    /// The line from the card above down to the first rail, the line from the last rail down to the cards below,
    /// with room for its arrowhead, and the step between two heights and between two ends on one card.
    /// </summary>
    public const double Stem = 20, Drop = 32, Step = 18;

    /// <summary>
    /// How far apart two rails must be to share a height, so that their glow doesn't run into one line.
    /// </summary>
    public const double Clear = 60;

    /// <summary>
    /// Each rail's ends across the picture and its height in its gap, given the middle of every card. The rails
    /// come as <see cref="CardRails"/> orders them, gap by gap and from the left.
    /// </summary>
    public static IReadOnlyList<RailSpan> Across(IReadOnlyList<Rail> rails,
        IReadOnlyDictionary<string, double> centres)
    {
        var spans = new List<RailSpan>();
        foreach (var gap in rails.GroupBy(rail => rail.Gap))
        {
            var inGap = gap.ToList();
            var stood = new List<RailSpan>();
            foreach (var rail in inGap)
            {
                List<RailSpot> spots =
                [
                    .. rail.Above.Select(card => new RailSpot(card, X(card, rail, inGap, true, centres), true)),
                    .. rail.Below.Select(card => new RailSpot(card, X(card, rail, inGap, false, centres), false))
                ];
                double left = spots.Min(spot => spot.X), right = spots.Max(spot => spot.X);

                // The first height where no rail already stands within reach of this one's span.
                int level = 0;
                while (stood.Any(other => other.Level == level && left < other.Right + Clear
                                                               && right > other.Left - Clear))
                    level++;
                stood.Add(new RailSpan(rail, level, spots));
            }

            spans.AddRange(stood);
        }

        return spans;
    }

    /// <summary>
    /// The room a gap needs for its rails: a stem, a drop, and a step for each height after the first. Nothing
    /// when no rail stands in it.
    /// </summary>
    public static double Room(IEnumerable<RailSpan> gap)
    {
        var levels = gap.Select(span => span.Level).ToList();
        return levels.Count == 0 ? 0 : Stem + Step * levels.Max() + Drop;
    }

    /// <summary>
    /// Whether a link of the rail points at a card: at its second card for a link down, at its first for one up,
    /// and at both for one both ways. An arrowhead belongs on that end.
    /// </summary>
    public static bool PointsAt(Rail rail, string card) => rail.Links.Any(link => link.Kind switch
    {
        CardLinkKind.Down => link.To == card,
        CardLinkKind.Up => link.From == card,
        _ => link.From == card || link.To == card
    });

    /// <summary>
    /// Where a rail's end stands on its card: at the card's middle, or a step from the next end when several rails
    /// of the gap end on that side of the card, in the rails' order.
    /// </summary>
    private static double X(string card, Rail rail, List<Rail> inGap, bool above,
        IReadOnlyDictionary<string, double> centres)
    {
        var sharing = inGap.Where(other => (above ? other.Above : other.Below).Contains(card)).ToList();
        return centres[card] + (sharing.IndexOf(rail) - (sharing.Count - 1) / 2.0) * Step;
    }
}
