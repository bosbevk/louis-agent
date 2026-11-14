using LouisAgent.Diagrams.Cards.Links;

namespace LouisAgent.Diagrams.Cards.Rails;

/// <summary>
/// Turns an overview's links into its rails. In the gap between two rows, the cards above that are linked to the
/// same cards below share one rail, and every other card above has a rail of its own, so you can still see which
/// card feeds which.
/// </summary>
/// <remarks>
/// The org chart's connector, which graph tools call bus-style routing: a parent's line drops to a bar its children
/// hang from. Louis's pick of three, "a rail for each sender": one rail a gap would have been simpler, but it stops
/// saying which card feeds which as soon as two cards feed different cards. It is a pure function of the data, with
/// no places in it, so it is tested with ids alone.
/// </remarks>
public static class CardRails
{
    /// <summary>What stands between two ids in a rail's key. No id holds it.</summary>
    private const char Between = '\n';

    /// <summary>The overview's rails, gap by gap from the top, and in a gap from the left.</summary>
    public static IReadOnlyList<Rail> Of(CardsSpec spec)
    {
        // Where each card stands: its row from the top, and its place in the row from the left.
        var placeOf = new Dictionary<string, (int Row, int At)>(StringComparer.Ordinal);
        for (int row = 0; row < spec.Rows.Count; row++)
            for (int at = 0; at < spec.Rows[row].Cards.Count; at++)
                placeOf[spec.Rows[row].Cards[at].Id] = (row, at);

        // A link's upper card and its lower one, whichever way the link was written.
        string Upper(CardLink link) => placeOf[link.From].Row < placeOf[link.To].Row ? link.From : link.To;
        string Lower(CardLink link) => Upper(link) == link.From ? link.To : link.From;

        // Each card above with the cards it is linked to below, in their row's order.
        var feeders = spec.Links.GroupBy(Upper, StringComparer.Ordinal).Select(links =>
        (
            Above: links.Key,
            Below: links.Select(Lower).Distinct(StringComparer.Ordinal).OrderBy(id => placeOf[id].At).ToList(),
            Links: links.ToList()
        ));

        // The ones with the same cards below share a rail.
        return
        [
            .. feeders.GroupBy(feeder => (placeOf[feeder.Above].Row, Below: string.Join(Between, feeder.Below)))
                .Select(sharing => new Rail(sharing.Key.Row,
                    [.. sharing.Select(feeder => feeder.Above).OrderBy(id => placeOf[id].At)],
                    sharing.First().Below,
                    [.. sharing.SelectMany(feeder => feeder.Links)]))
                .OrderBy(rail => rail.Gap).ThenBy(rail => placeOf[rail.Above[0]].At)
        ];
    }
}
