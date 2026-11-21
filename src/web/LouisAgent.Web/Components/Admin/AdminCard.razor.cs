using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Admin;

/// <summary>
/// The frame every area's card on the admin's home shares (F144): a title, the area's own body, and "Open →" to the
/// area's pages, so each area writes only what it has to say.
/// </summary>
/// <remarks>
/// Template Method in markup: the frame is fixed and the area fills its body, as <see cref="AdminShell"/> frames the
/// admin's pages; a card looks the same whichever area drew it.
/// </remarks>
public partial class AdminCard
{
    /// <summary>The area's name on its card.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = "";

    /// <summary>Where "Open →" goes: the area's own page.</summary>
    [Parameter, EditorRequired]
    public string Open { get; set; } = "";

    /// <summary>What the area has to say on the home.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
