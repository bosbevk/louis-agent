using LouisAgent.Web.Components.Site.Pieces;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Showcase;

/// <summary>
/// Fans three photographs out in a row: the middle one forward, the two beside it turned in, each a link with its
/// caption and a reflection; the one under the pointer comes forward.
/// </summary>
/// <remarks>
/// The design page's fan (FX162, Louis: "the design doc is the truth here"), its rules taken as they stand; a site
/// configures it with records (ADR 0125).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public partial class ShotFan
{
    /// <summary>The photographs, left to right; three is what the fan is drawn for.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<Shot> Items { get; set; } = [];

    /// <summary>Whether each photograph shows its reflection under it; off by default.</summary>
    [Parameter] public bool Reflect { get; set; }

    /// <summary>Whether a visitor or the guide can pick a photograph and ask about it; on by default.</summary>
    [Parameter] public bool Pick { get; set; } = true;

    /// <summary>A photograph's id from its title, for one that can be picked.</summary>
    private string? AddressOf(Shot shot) => Pick ? PieceId.Of(shot.Title) : null;

    /// <summary>
    /// Where a photograph stands: turned in on the left, forward in the middle, turned in on the right.
    /// </summary>
    private string PlaceOf(int index) => index switch
    {
        0 => "shot shot-left",
        _ when index == Items.Count - 1 => "shot shot-right",
        _ => "shot shot-middle"
    };
}
