namespace LouisAgent.Composition.Packs.Screens.Reading;

/// <summary>Which rows the page wants: what was typed in the search box, and which page, counting from 1.</summary>
public sealed record ScreenQuery(string? Search, int Page)
{
    /// <summary>How many rows one page holds.</summary>
    public const int PageSize = 50;
}
