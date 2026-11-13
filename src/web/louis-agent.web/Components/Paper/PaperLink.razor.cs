using LouisAgent.Web.Paper;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Paper;

/// <summary>
/// A link as it is printed (FX154, Louis: "in a pdf hyperlinks is qr codes always"): its QR code, its words and its
/// address written short. A link on paper can't be clicked, so this is drawn in its place when the page is printed,
/// and not on a screen. The code is the link as well, so a PDF read on a screen can still be clicked.
/// </summary>
/// <remarks>
/// Whether it shows is its style's doing (print only), so a page keeps its ordinary link beside it and hides that
/// one on paper. A page that is printed has no script running, which is why both are always in the page.
/// </remarks>
public partial class PaperLink
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    /// <summary>Where the link leads: a whole address, or a place on this site.</summary>
    [Parameter, EditorRequired]
    public string Href { get; set; } = "";

    /// <summary>The link's words.</summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = "";

    /// <summary>What is written beside the code. With none, the address, short.</summary>
    [Parameter]
    public string? Address { get; set; }

    /// <summary>Whether the code is drawn small, as on a card with little room.</summary>
    [Parameter]
    public bool Small { get; set; }

    /// <summary>
    /// The whole address the code holds. A phone that scans it knows nothing of which site the paper came from, so
    /// a place on this site is given the site's address in front.
    /// </summary>
    private string Target => Navigation.ToAbsoluteUri(Href).ToString();

    /// <summary>What is written beside the code.</summary>
    private string Written => Address ?? PaperAddress.Short(Target);
}
