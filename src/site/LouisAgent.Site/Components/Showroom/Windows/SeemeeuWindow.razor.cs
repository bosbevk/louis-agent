using LouisAgent.Web.Components.Staging.Windows;

namespace LouisAgent.Site.Components.Showroom.Windows;

/// <summary>
/// The guesthouse's window on the start (F134.1): Seemeeu's website as a still picture, with one moment of its
/// recorded talk beside it.
/// </summary>
/// <remarks>
/// It fills the slot the site's list names for the demo, so the framework's start draws it without knowing what it
/// is. The window says nothing of its own: the website's words are the demo page's and the talk is the recording's.
/// </remarks>
public partial class SeemeeuWindow
{
    /// <summary>The website's menu, as the demo's page has it.</summary>
    private static readonly string[] Menu = ["Home", "Rooms", "Stay"];

    /// <summary>The moment of its talk the window shows.</summary>
    private static WindowTalk Talk => SiteWindowTalks.Seemeeu;

    /// <summary>The room the agent showed, as the demo's page names and prices it.</summary>
    private static readonly ShownCard Room =
        new("The garden room", "R1,250 a night", "images/showroom/seemeeu-garden.webp");
}
