using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Tiles;

/// <summary>
/// A row of tiles on a site's page (F118): one, two or three across, two on a tablet and one on a phone. A site used
/// to write the grid by hand on each page; the row is the same on every site, so it is the framework's.
/// </summary>
/// <remarks>
/// Composite, in Razor: the row only lays out what it holds, and each <see cref="Tile"/> draws itself, so a page
/// says what the tiles say and never how the grid works.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0106-the-landing-is-a-business-website-from-the-framework.md">ADR 0106: the landing is a business website from the framework</seealso>
public partial class TileRow
{
    /// <summary>How many tiles stand side by side on a wide screen: one, two or three.</summary>
    [Parameter] public int Columns { get; set; } = 3;

    /// <summary>The tiles.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>The row's classes: one per width, so the styles can narrow each on a smaller screen.</summary>
    private string Classes => Columns switch
    {
        1 => "tiles tiles-1",
        2 => "tiles tiles-2",
        _ => "tiles tiles-3"
    };
}
