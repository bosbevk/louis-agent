using LouisAgent.Site.Content.WorkWithUs;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Components.WorkWithUs;

/// <summary>
/// One slide's tiles on the Work with us page (F177): a row of them, each drawn from its words. The page sets four
/// such rows, so the row and its tiles are written once.
/// </summary>
public partial class WorkTiles
{
    /// <summary>The tiles, in order.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<WorkTile> Items { get; set; } = [];

    /// <summary>How many tiles stand in a row on a wide screen.</summary>
    [Parameter]
    public int Columns { get; set; } = 3;
}
