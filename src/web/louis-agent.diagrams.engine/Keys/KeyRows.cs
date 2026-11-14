namespace LouisAgent.Diagrams.Engine.Keys;

/// <summary>
/// Places a key's items in rows (CL65.2): side by side, each after the one before, and on to a new row at the
/// left when the picture's width is full.
/// </summary>
/// <remarks>
/// Four kinds each had this loop with these five numbers, so a key's rows stood the same in every kind only
/// because four copies agreed. A kind's key layout now says what its items are and how wide each sample is, and
/// this places them. It takes the item's type and how to read and move one, so it knows no kind.
/// </remarks>
public static class KeyRows
{
    /// <summary>
    /// The room the key's rule and its heading take, how tall a row of items is, and how far after its sample an
    /// item's words start.
    /// </summary>
    public const double Heading = 34, Row = 28, Words = 10;

    /// <summary>
    /// How wide a character of an item's words is taken to be, generously, since no font is measured, and the room
    /// between two items.
    /// </summary>
    private const double Char = 6.6, Gap = 30;

    /// <summary>
    /// The items placed across a width, and how tall the key is. An item is read by how wide its sample is and by
    /// its words, and moved to where it stands by the caller's own function.
    /// </summary>
    public static (IReadOnlyList<TItem> Items, double Height) Place<TItem>(IEnumerable<TItem> items,
        Func<TItem, double> sample, Func<TItem, string> words, Func<TItem, double, double, TItem> at, double width)
    {
        var placed = new List<TItem>();
        double x = 0, y = Heading;
        foreach (var item in items)
        {
            double room = sample(item) + Words + words(item).Length * Char;
            if (x > 0 && x + room > width) (x, y) = (0, y + Row);
            placed.Add(at(item, x, y));
            x += room + Gap;
        }

        return (placed, y + Row);
    }
}
