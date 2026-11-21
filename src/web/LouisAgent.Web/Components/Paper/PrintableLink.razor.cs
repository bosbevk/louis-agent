using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Paper;

/// <summary>
/// A link that prints: on a screen the link with its words, on paper its QR code in the link's place, since a link
/// on paper can't be clicked.
/// </summary>
/// <remarks>
/// Both are always in the page, as <see cref="PaperLink"/> says, because a printed page runs no script; which one
/// shows is the styles' doing.
/// </remarks>
public partial class PrintableLink
{
    /// <summary>Where the link leads: a whole address, or a place on this site.</summary>
    [Parameter, EditorRequired] public string Href { get; set; } = "";

    /// <summary>The words beside the code on paper.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = "";

    /// <summary>Whether the code is drawn small, as on a panel with little room.</summary>
    [Parameter] public bool Small { get; set; }

    /// <summary>The link's words on a screen.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Whether it opens in a new tab, as a link that leaves the site does.</summary>
    [Parameter] public bool NewTab { get; set; }

    /// <summary>A new tab, when asked.</summary>
    private string? Target => NewTab ? "_blank" : null;

    /// <summary>And then it tells the other site nothing of where it came from.</summary>
    private string? Rel => NewTab ? "noopener" : null;
}
