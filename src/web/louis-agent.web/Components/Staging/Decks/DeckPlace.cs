namespace LouisAgent.Web.Components.Staging.Decks;

/// <summary>
/// Where each item of a deck stands (F134.1): how far from the one forward, counted the short way round, so the last
/// item stands just left of the first and not four places to its right.
/// </summary>
/// <remarks>
/// The deck's one rule, kept in a plain class so it is tested with no component and no browser. The component only
/// writes what this says into each item's style.
/// </remarks>
public static class DeckPlace
{
    /// <summary>
    /// How far an item stands from the one forward: 0 for it, 1 for the next, -1 for the one before. With an even
    /// count, the item opposite stands to the right.
    /// </summary>
    public static int Of(int index, int forward, int count)
    {
        if (count == 0) return 0;

        // Its distance going right, from 0 up to one short of the count.
        int right = ((index - forward) % count + count) % count;

        // Past the half-way mark it is nearer going left.
        return right > count / 2 ? right - count : right;
    }

    /// <summary>The item forward after a step right (1) or left (-1), coming round at either end.</summary>
    public static int Moved(int forward, int by, int count) =>
        count == 0 ? 0 : ((forward + by) % count + count) % count;
}
