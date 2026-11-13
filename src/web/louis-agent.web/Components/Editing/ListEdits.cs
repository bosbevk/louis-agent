namespace LouisAgent.Web.Components.Editing;

/// <summary>
/// What a person does to a list they edit (F142), as plain operations: add to its end, take one out, change one in
/// its place, move one a step. Each hands back a new list. The one it was given belongs to whoever drew it, so it
/// is never changed.
/// </summary>
public static class ListEdits
{
    /// <summary>The list with this item at its end.</summary>
    public static IReadOnlyList<T> Added<T>(this IReadOnlyList<T> items, T item) => [.. items, item];

    /// <summary>The list without the item at this place.</summary>
    public static IReadOnlyList<T> Without<T>(this IReadOnlyList<T> items, int at) =>
        [.. items.Where((_, index) => index != at)];

    /// <summary>The list with this item in that place.</summary>
    public static IReadOnlyList<T> Replaced<T>(this IReadOnlyList<T> items, int at, T item) =>
        [.. items.Select((old, index) => index == at ? item : old)];

    /// <summary>
    /// The list with the item at this place moved a step, changing places with its neighbour. Past either end
    /// there is nowhere to go, and the list comes back as it was.
    /// </summary>
    public static IReadOnlyList<T> Moved<T>(this IReadOnlyList<T> items, int at, int step)
    {
        int to = at + step;
        if (to < 0 || to >= items.Count) return items;

        var moved = items.ToArray();
        (moved[at], moved[to]) = (moved[to], moved[at]);
        return moved;
    }
}
