namespace LouisAgent.Composition.Packs.Screens.Reading;

/// <summary>One page of a screen: its rows in order, and how many rows there are in all.</summary>
public sealed record ScreenPage(IReadOnlyList<ScreenRow> Rows, long Total)
{
    /// <summary>A page with nothing on it, for a store that's down or a search that found nothing.</summary>
    public static readonly ScreenPage Empty = new([], 0);
}
