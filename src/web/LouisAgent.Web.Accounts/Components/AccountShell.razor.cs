using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Accounts.Components;

/// <summary>The account pages' frame: a centred card, with a slot for the site's brand.</summary>
/// <remarks>
/// Template Method, as <c>AdminShell</c>: the frame is fixed, a site fills the slot, and a slot left empty leaves no
/// gap.
/// </remarks>
public partial class AccountShell
{
    /// <summary>The site's logo or name, drawn above the card's content; none leaves it out.</summary>
    [Parameter]
    public RenderFragment? Brand { get; set; }

    /// <summary>The title on the brand's link home.</summary>
    [Parameter]
    public string BrandTitle { get; set; } = "";

    /// <summary>The page.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
