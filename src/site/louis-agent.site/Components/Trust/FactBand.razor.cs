using LouisAgent.Site.Content.Trust;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Components.Trust;

/// <summary>
/// One part of the trust page (F165): its heading and a row of facts, each a tile that says whether it is in place.
/// The page sets five of them, so the band, the row and the tiles are written once.
/// </summary>
public partial class FactBand
{
    /// <summary>The part's address on the page, in words.</summary>
    [Parameter, EditorRequired]
    public string Id { get; set; } = "";

    /// <summary>The small word over the heading.</summary>
    [Parameter]
    public string? Topic { get; set; }

    /// <summary>The part's heading.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>A sentence or two under the heading.</summary>
    [Parameter]
    public string? Summary { get; set; }

    /// <summary>The facts, one tile each, in order.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<TrustFact> Facts { get; set; } = [];

    /// <summary>What the part shows under its tiles, such as a table.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
