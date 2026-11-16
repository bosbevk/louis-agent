namespace LouisAgent.Web.Components.Site.Pieces;

/// <summary>
/// What a panel tells the theme's lens script about its switch: nothing while the lens is on, so a panel's markup
/// only says <c>data-lens="off"</c> when a site switched it off.
/// </summary>
public static class PaneSwitch
{
    /// <summary>The attribute's value for a lens that is on or off: none for on, <c>off</c> for off.</summary>
    public static string? Lens(bool on) => on ? null : "off";
}
